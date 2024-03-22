using MelonLoader;

namespace QAPI;

internal static class Config
{
    public static string FilePath { get; private set; } = "";

    public static bool QListPresent() =>
        MelonTypeBase<MelonMod>.RegisteredMelons.Any(m => m.Info.Name == "QList");

    public static void SetFilePath(MelonMod mod)
    {
        FilePath = $"{MelonLoader.Utils.MelonEnvironment.UserDataDirectory}/{mod.Info.Name}.cfg";
    }
}
