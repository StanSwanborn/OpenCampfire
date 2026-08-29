from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
HEADER = ROOT / "src/Discord.Social.CSharp/native/include/cdiscord.h"
NATIVE = ROOT / "src/Discord.Social.CSharp/Discord/Native"


def name(c_name):
    special = {
        "Discord_Activity": "DiscordActivityNative",
        "Discord_Client": "DiscordClientNative",
        "Discord_ClientResult": "DiscordClientResultNative",
        "Discord_ActivityTypes": "DiscordActivityType",
    }
    return special.get(c_name, c_name.replace("Discord_", "Discord").replace("_", ""))


def managed_type(c_type):
    c_type = re.sub(r"\s+", " ", c_type.replace("const", "")).strip()
    primitives = {"void": "void", "void*": "nint", "bool": "bool", "size_t": "nuint",
                  "uint8_t": "byte", "int16_t": "short", "int32_t": "int",
                  "uint32_t": "uint", "uint64_t": "ulong", "int64_t": "long",
                  "float": "float", "double": "double"}
    if c_type in primitives:
        return primitives[c_type]
    if c_type.endswith("*"):
        return "ref " + managed_type(c_type[:-1].strip())
    return name(c_type)


def args(text):
    text = re.sub(r"\s+", " ", text.strip())
    if not text or text == "void":
        return []
    result = []
    for item in text.split(","):
        item = item.strip()
        match = re.search(r"(\w+)$", item)
        parameter_name = match.group(1)
        result.append((item[:-len(parameter_name)].strip(), parameter_name))
    return result


def parameter(c_type, parameter_name):
    c_type = re.sub(r"\s+", " ", c_type.replace("const", "")).strip()
    attribute = "[MarshalAs(UnmanagedType.I1)] " if c_type == "bool" else ""
    return attribute + managed_type(c_type) + " " + parameter_name


def category(owner):
    groups = {
        "Activity": {"Activity", "ActivityInvite", "ActivityAssets", "ActivityTimestamps", "ActivityParty", "ActivitySecrets", "ActivityButton"},
        "Authorization": {"AuthorizationCodeChallenge", "AuthorizationCodeVerifier", "AuthorizationArgs", "DeviceAuthorizationArgs"},
        "Call": {"Call", "CallInfoHandle", "VoiceStateHandle", "VADThresholdSettings"},
        "Channel": {"ChannelHandle"}, "Guild": {"GuildMinimal", "GuildChannel"},
        "Lobby": {"LinkedLobby", "LinkedChannel", "LobbyHandle", "LobbyMemberHandle"},
        "Relationship": {"RelationshipHandle"}, "User": {"UserHandle", "UserApplicationProfileHandle"},
        "Messaging": {"MessageHandle", "AdditionalContent", "UserMessageSummary"},
        "Audio": {"AudioDevice"}, "Result": {"ClientResult"}, "ClientOptions": {"ClientCreateOptions"},
    }
    for group, owners in groups.items():
        if owner in owners:
            return group
    return "Runtime"


def client_category(member):
    if any(word in member for word in ("Audio", "Device", "Volume", "Mute", "Deaf", "Noise", "Echo", "Opus", "Speaker", "VAD", "PTT", "Voice")):
        return "ClientAudio"
    if any(word in member for word in ("Authorize", "Token", "Provisional", "Authenticated")):
        return "ClientAuthorization"
    if any(word in member for word in ("Message", "Chat")):
        return "ClientMessaging"
    if any(word in member for word in ("Lobby", "GuildChannel", "LinkedChannel", "ChannelToLobby")):
        return "ClientLobbies"
    if any(word in member for word in ("Activity", "RichPresence", "OnlineStatus", "Launch", "Invite")):
        return "ClientPresence"
    if any(word in member for word in ("Friend", "Relationship", "Block", "Unblock", "UserUpdated", "SearchFriends")):
        return "ClientRelationships"
    return "ClientLifecycle"


def method_group(function_name):
    owner, _, member = function_name.removeprefix("Discord_").partition("_")
    if owner == "Client":
        return client_category(member)
    return category(owner)


def emit_types(text):
    groups = {}
    struct_names = re.findall(r"typedef struct (Discord_\w+) Discord_\w+;", text)
    for c_name in struct_names:
        groups.setdefault(category(c_name.removeprefix("Discord_")), []).append(("struct", c_name, None))
    for c_name, body in re.findall(r"typedef struct (Discord_\w+) \{(.*?)\} \w+;", text, re.S):
        if c_name == "Discord_String":
            groups.setdefault("Shared", []).append(("string", c_name, None))
        elif c_name in {"Discord_Properties", "Discord_Allocator"} or c_name.endswith("Span"):
            groups.setdefault("Shared", []).append(("special", c_name, body))
    for c_name, body in re.findall(r"typedef enum (\w+) \{(.*?)\} \w+;", text, re.S):
        groups.setdefault(category(c_name.removeprefix("Discord_")), []).append(("enum", c_name, body))
    callbacks = re.findall(r"typedef (?:void|nint) \(\*(Discord_\w+)\)\((.*?)\);", re.sub(r"\s+", " ", text), re.S)
    groups.setdefault("Shared", []).append(("malloc", "Discord_MallocFn", None))
    for c_name, body in callbacks:
        groups.setdefault("ClientCallbacks", []).append(("callback", c_name, body))
    for group, declarations in groups.items():
        lines = ["using System.Runtime.InteropServices;", "", "namespace Discord.Social.CSharp.Native.Sdk;", ""]
        for kind, c_name, body in declarations:
            cs_name = name(c_name)
            if kind == "struct":
                count = 3 if c_name == "Discord_Call" else 1
                lines += ["[StructLayout(LayoutKind.Sequential)]", f"internal struct {cs_name}", "{"]
                lines += [f"    internal nint Opaque{i};" for i in range(count)] + ["}", ""]
            elif kind == "string":
                lines += ["[StructLayout(LayoutKind.Sequential)]", "internal struct DiscordString", "{", "    internal nint Ptr;", "    internal nuint Size;", "}", ""]
            elif kind == "special":
                lines += ["[StructLayout(LayoutKind.Sequential)]", f"internal struct {cs_name}", "{"]
                if c_name == "Discord_Properties": fields = ["internal nuint Size;", "internal nint Keys;", "internal nint Values;"]
                elif c_name == "Discord_Allocator": fields = ["internal nint FnMalloc;", "internal nint FnFree;"]
                else: fields = ["internal nint Ptr;", "internal nuint Size;"]
                lines += ["    " + field for field in fields] + ["}", ""]
            elif kind == "enum":
                visibility = "public" if cs_name == "DiscordActivityType" else "internal"
                lines += [f"{visibility} enum {cs_name}", "{"]
                for item in body.split(","):
                    if "forceint" in item or "=" not in item: continue
                    enum_name, value = [part.strip() for part in item.split("=", 1)]
                    lines.append(f"    {enum_name.removeprefix(c_name + '_')} = {value},")
                lines += ["}", ""]
            elif kind in {"free", "malloc"}:
                return_type = "void" if kind == "free" else "nint"
                parameter_type = "nint ptr" if kind == "free" else "nuint size"
                lines += ["[UnmanagedFunctionPointer(CallingConvention.Cdecl)]", f"internal delegate {return_type} {cs_name}({parameter_type});", ""]
            else:
                callback_args = ", ".join(parameter(t, n) for t, n in args(body))
                lines += ["[UnmanagedFunctionPointer(CallingConvention.Cdecl)]", f"internal delegate void {cs_name}({callback_args});", ""]
        (NATIVE / f"NativeDiscord{group}Types.cs").write_text("\n".join(lines))
    return sum(len(items) for items in groups.values()), len(groups)


def emit_methods(text):
    normalized = re.sub(r"\s+", " ", text)
    pattern = r"((?:void\s*\*|void|bool|uint\d+_t|int\d+_t|float|double|Discord_\w+)) DISCORD_API (Discord_\w+)\((.*?)\);"
    groups = {}
    methods = re.findall(pattern, normalized)
    methods += [(return_type, function_name, body)
                for return_type, function_name, body in re.findall(
                    r"DISCORD_API ((?:void\s*\*|void)) (Discord_\w+)\((.*?)\);",
                    normalized)]
    for return_type, function_name, body in methods:
        groups.setdefault(method_group(function_name), []).append((return_type, function_name, args(body)))
    for group, methods in groups.items():
        lines = ["using System.Runtime.InteropServices;", "", "namespace Discord.Social.CSharp.Native.Sdk;", "", f"internal static class NativeDiscord{group}Methods", "{", '    private const string LibraryName = "discord_partner_sdk";', ""]
        for return_type, function_name, parameters in methods:
            lines += ["    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]"]
            if return_type == "bool": lines += ["    [return: MarshalAs(UnmanagedType.I1)]"]
            lines += [f"    internal static extern {managed_type(return_type)} {function_name}("]
            if parameters: lines += ["        " + ",\n        ".join(parameter(t, n) for t, n in parameters) + ");"]
            else: lines += ["        );"]
            lines.append("")
        lines.append("}")
        (NATIVE / f"NativeDiscord{group}Methods.cs").write_text("\n".join(lines) + "\n")
    return sum(len(items) for items in groups.values()), len(groups)


header = HEADER.read_text()
types_count, type_groups = emit_types(header)
methods_count, method_groups = emit_methods(header)
print(f"Generated {methods_count} methods in {method_groups} files and {types_count} types in {type_groups} files.")
