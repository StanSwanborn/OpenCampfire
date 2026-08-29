namespace OpenCampfire.Helpers;

public static class AssemblyVersionHelper
{
    private static Version Current => typeof(AssemblyVersionHelper).Assembly.GetName().Version ?? new Version(0, 0);

    public static int Major => Current.Major;
}