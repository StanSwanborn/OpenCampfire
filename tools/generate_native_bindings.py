from pathlib import Path
import re


ROOT = Path(__file__).resolve().parents[1]
HEADER = ROOT / "src/Discord.Social.CSharp/native/include/cdiscord.h"
NATIVE = ROOT / "src/Discord.Social.CSharp/Discord/Native"


def convert_cname_to_csharpname(c_name):
    if c_name == "Discord_ActivityTypes":
        return "DiscordActivityType"
    
    if c_name == "Discord_Activity":
        return "DiscordActivityNative"
    
    if c_name == "Discord_ClientResult":
        return "DiscordClientResultNative"
    
    if c_name == "Discord_Client":
        return "DiscordClientNative"
    
    return c_name.replace("Discord_", "Discord").replace("_", "")


def type_name(c_type):
    c_type = re.sub(r"\bconst\b", "", c_type).strip()

    if c_type == "void":
        return "void"
    
    if c_type == "void*":
        return "nint"
    
    primitive = {
        "bool": "bool", "size_t": "nuint", "uint8_t": "byte", "int16_t": "short",
        "int32_t": "int", "uint32_t": "uint", "uint64_t": "ulong", "int64_t": "long",
        "float": "float", "double": "double",
    }

    if c_type.endswith("*"):
        return "ref " + type_name(c_type[:-1].strip())
    
    return primitive.get(c_type, convert_cname_to_csharpname(c_type))


def split_args(args):
    args = re.sub(r"\s+", " ", args.strip())
    if not args or args == "void":
        return []
    result = []
    for arg in args.split(","):
        arg = arg.strip()
        name = re.search(r"(\w+)\s*$", arg).group(1)
        result.append((arg[: -len(name)].strip(), name))
    return result


def parameter(c_type, name):
    c_type = re.sub(r"\s+", " ", c_type).strip()
    managed = type_name(c_type)
    bool_attribute = "[MarshalAs(UnmanagedType.I1)] " if c_type == "bool" else ""
    return f"{bool_attribute}{managed} {name}"


def parse_enums(text):
    enums = []
    for match in re.finditer(r"typedef enum (\w+) \{(.*?)\} \w+;", text, re.S):
        values = []
        for item in match.group(2).split(","):
            item = item.strip()
            if not item or "forceint" in item:
                continue
            name, value = [part.strip() for part in item.split("=", 1)]
            member = name.removeprefix(match.group(1) + "_").replace("_", "")
            values.append((member, value))
        enums.append((convert_cname_to_csharpname(match.group(1)), values))
    return enums


def parse_structs(text):
    structs = [(convert_cname_to_csharpname(name), 3 if name == "Discord_Call" else 1)
               for name in re.findall(r"typedef struct (Discord_\w+) Discord_\w+;", text)]
    for name, body in re.findall(r"typedef struct (Discord_\w+) \{(.*?)\} \w+;", text, re.S):
        if name == "Discord_String":
            continue
        structs.append((convert_cname_to_csharpname(name), 1 if "opaque[3]" not in body else 3))
    return list(dict.fromkeys(structs))


def parse_callbacks(text):
    return {convert_cname_to_csharpname(name): split_args(args)
            for name, args in re.findall(r"typedef void \(\*(Discord_\w+)\)\((.*?)\);", text, re.S)}


def parse_functions(text):
    text = re.sub(r"\s+", " ", text)
    pattern = r"((?:void|bool|uint\d+_t|int\d+_t|float|Discord_\w+)\s+)DISCORD_API\s+(Discord_\w+)\s*\((.*?)\);"
    return [(return_type.strip(), name, split_args(args))
            for return_type, name, args in re.findall(pattern, text)]


def emit_types(text):
    lines = ["using System.Runtime.InteropServices;", "", "namespace Discord.Social.CSharp.Native.Sdk;", "", "[StructLayout(LayoutKind.Sequential)]", "internal struct DiscordString", "{", "    internal nint Ptr;", "    internal nuint Size;", "}", ""]
    for name, opaque_count in parse_structs(text):
        if name.endswith("Span"):
            fields = ["    internal nint Ptr;", "    internal nuint Size;"]
        elif name == "DiscordProperties":
            fields = ["    internal nuint Size;", "    internal nint Keys;", "    internal nint Values;"]
        elif name == "DiscordAllocator":
            fields = ["    internal nint FnMalloc;", "    internal nint FnFree;"]
        else:
            fields = [f"    internal nint Opaque{index};" for index in range(opaque_count)]
        lines += ["[StructLayout(LayoutKind.Sequential)]", f"internal struct {name}", "{"] + fields + ["}", ""]
    for enum, values in parse_enums(text):
        visibility = "public" if enum == "DiscordActivityTypes" else "internal"
        lines += [f"{visibility} enum {enum}", "{"]
        lines += [f"    {member} = {value}," for member, value in values]
        lines += ["}", ""]
    for name, args in parse_callbacks(text).items():
        parameters = ", ".join(parameter(c_type, arg_name) for c_type, arg_name in args)
        lines += ["[UnmanagedFunctionPointer(CallingConvention.Cdecl)]", f"internal delegate void {name}({parameters});", ""]
    lines += ["[UnmanagedFunctionPointer(CallingConvention.Cdecl)]", "internal delegate nint DiscordMallocFn(nuint size);", ""]
    return "\n".join(lines)


def emit_methods(text):
    lines = ["using System.Runtime.InteropServices;", "", "namespace Discord.Social.CSharp.Native.Sdk;", "", "internal static class NativeMethods", "{", '    private const string LibraryName = "discord_partner_sdk";', ""]
    for return_type, name, args in parse_functions(text):
        parameters = ",\n        ".join(parameter(c_type, arg_name) for c_type, arg_name in args)
        lines += ["    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]"]
        if return_type == "bool":
            lines.append("    [return: MarshalAs(UnmanagedType.I1)]")
        lines += [f"    internal static extern {type_name(return_type)} {name}(", f"        {parameters});" if parameters else "        );", ""]
    lines.append("}")
    return "\n".join(lines)


header = HEADER.read_text()
NATIVE.joinpath("NativeTypes.cs").write_text(emit_types(header) + "\n")
NATIVE.joinpath("NativeMethods.cs").write_text(emit_methods(header) + "\n")
print(f"Generated {len(parse_structs(header))} structs, {len(parse_enums(header))} enums, {len(parse_callbacks(header))} callbacks, and {len(parse_functions(header))} methods.")