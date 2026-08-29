namespace OpenCampfire.Helpers;

public static class AssemblyVersionHelper
{
    public static Version Current => typeof(Program).Assembly.GetName().Version!;

    public static int Major => Current.Major;
}