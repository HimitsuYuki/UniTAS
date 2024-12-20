using System.IO;
using BepInEx;

namespace UniTAS.Patcher.Utils;

public static class UniTASPaths
{
    static UniTASPaths()
    {
        Directory.CreateDirectory(AssemblyCache);
        Directory.CreateDirectory(ConfigCache);
    }

    private static string UniTASBase { get; } = Path.Combine(Paths.GameRootPath, "UniTAS");
    public static string Benchmarks { get; } = Path.Combine(UniTASBase, "benchmarks");
    public static string Resources { get; } = Utility.CombinePaths(Paths.PatcherPluginPath, "UniTAS", "Resources");
    public static string Cache { get; } = Path.Combine(UniTASBase, "cache");
    public static string AssemblyCache { get; } = Path.Combine(Cache, "assemblies");
    public static string ConfigCache { get; } = Path.Combine(Cache, "config");
    public static string ConfigBepInEx { get; } = Path.Combine(Paths.ConfigPath, BEPINEX_CONFIG_FILE_NAME);
    public const string BEPINEX_CONFIG_FILE_NAME = "UniTAS.cfg";
    public static string ConfigBackend { get; } = Path.Combine(UniTASBase, BACKEND_CONFIG_FILE_NAME);
    private const string BACKEND_CONFIG_FILE_NAME = "save.dat";
}