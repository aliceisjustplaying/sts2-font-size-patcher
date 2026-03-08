using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Debug;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace ZSts2FontSizeMod.Patches;

[HarmonyPatch(typeof(NDebugInfoLabelManager), nameof(NDebugInfoLabelManager._Ready))]
public static class DebugInfoReadyPatch
{
    [HarmonyPostfix]
    private static void Postfix(NDebugInfoLabelManager __instance) => FontScaleState.ApplyFooterExtras(__instance);
}

[HarmonyPatch(typeof(NDebugInfoLabelManager), "UpdateText")]
public static class DebugInfoUpdateTextPatch
{
    [HarmonyPostfix]
    private static void Postfix(NDebugInfoLabelManager __instance) => FontScaleState.RewriteFooterText(__instance);
}

[HarmonyPatch(typeof(NPatchNotesScreen), "CreateNewPatchEntry")]
public static class PatchNotesScreenPatch
{
    [HarmonyPostfix]
    private static void Postfix(NPatchNotesScreen __instance) => FontScaleState.ApplyPatchNotesExtras(__instance);
}

[HarmonyPatch(typeof(NCharacterSelectScreen), "SelectCharacter")]
public static class CharacterSelectScreenPatch
{
    [HarmonyPostfix]
    private static void Postfix(NCharacterSelectScreen __instance) =>
        FontScaleState.ApplyCharacterSelectRelicDescriptionFix(__instance);
}
