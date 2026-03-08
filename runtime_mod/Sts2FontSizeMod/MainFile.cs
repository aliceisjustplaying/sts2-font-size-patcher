using System.Runtime.InteropServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace ZSts2FontSizeMod;

[ModInitializer(nameof(Initialize))]
public static class MainFile
{
    public const string ModId = "ZSts2FontSizeMod";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    private static IntPtr _linuxLibgccHandle;

    public static void Initialize()
    {
        EnsureLinuxHarmonySupport();

        FontScaleState.Initialize(Logger);

        var harmony = new Harmony(ModId);
        harmony.PatchAll();

        Logger.Info($"Initialized with base scale {FontScaleState.Config.BaseScale:F2}x");
    }

    [DllImport("libdl.so.2")]
    private static extern IntPtr dlopen(string filename, int flags);

    [DllImport("libdl.so.2")]
    private static extern IntPtr dlerror();

    private static void EnsureLinuxHarmonySupport()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return;
        }

        _linuxLibgccHandle = dlopen("libgcc_s.so.1", 2 | 256);
        if (_linuxLibgccHandle == IntPtr.Zero)
        {
            Logger.Info($"Failed to preload libgcc_s.so.1: {Marshal.PtrToStringAnsi(dlerror())}");
        }
    }
}
