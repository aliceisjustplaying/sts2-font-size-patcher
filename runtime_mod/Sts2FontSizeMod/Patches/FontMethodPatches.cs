using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.addons.mega_text;

namespace ZSts2FontSizeMod.Patches;

[HarmonyPatch(typeof(MegaLabel), "SetFontSize")]
public static class MegaLabelSetFontSizePatch
{
    [HarmonyPrefix]
    private static void Prefix(ref int size) => size = FontScaleState.Scale(size);
}

[HarmonyPatch(typeof(MegaRichTextLabel), "SetFontSize")]
public static class MegaRichTextLabelSetFontSizePatch
{
    [HarmonyPrefix]
    private static void Prefix(ref int size) => size = FontScaleState.Scale(size);
}

[HarmonyPatch(typeof(MegaLabel), nameof(MegaLabel._Ready))]
public static class MegaLabelReadyPatch
{
    [HarmonyPostfix]
    private static void Postfix(MegaLabel __instance)
    {
        FontScaleState.ApplyMegaLabelBase(__instance);
        __instance.SetTextAutoSize(__instance.Text);
    }
}

[HarmonyPatch(typeof(MegaRichTextLabel), nameof(MegaRichTextLabel._Ready))]
public static class MegaRichTextLabelReadyPatch
{
    [HarmonyPostfix]
    private static void Postfix(MegaRichTextLabel __instance)
    {
        FontScaleState.ApplyMegaRichTextBase(__instance);
        __instance.SetTextAutoSize(__instance.Text);
    }
}

[HarmonyPatch(typeof(NGame), nameof(NGame._Ready))]
public static class NGameReadyPatch
{
    [HarmonyPostfix]
    private static void Postfix(NGame __instance) => FontScaleState.HookGame(__instance);
}

[HarmonyPatch]
public static class RichTextSetTextPatch
{
    private static MethodBase TargetMethod() =>
        AccessTools.Method(typeof(RichTextLabel), "SetText", [typeof(string)])!;

    [HarmonyPrefix]
    private static void Prefix(ref string text) => text = FontScaleState.ScaleBbcode(text);
}

[HarmonyPatch]
public static class RichTextParseBbcodePatch
{
    private static MethodBase TargetMethod() =>
        AccessTools.Method(typeof(RichTextLabel), "ParseBbcode", [typeof(string)])!;

    [HarmonyPrefix]
    private static void Prefix(ref string bbcode) => bbcode = FontScaleState.ScaleBbcode(bbcode);
}

[HarmonyPatch]
public static class RichTextAppendTextPatch
{
    private static MethodBase TargetMethod() =>
        AccessTools.Method(typeof(RichTextLabel), "AppendText", [typeof(string)])!;

    [HarmonyPrefix]
    private static void Prefix(ref string bbcode) => bbcode = FontScaleState.ScaleBbcode(bbcode);
}

[HarmonyPatch]
public static class RichTextPushFontPatch
{
    private static MethodBase TargetMethod() =>
        AccessTools.Method(typeof(RichTextLabel), "PushFont", [typeof(Font), typeof(int)])!;

    [HarmonyPrefix]
    private static void Prefix(ref int fontSize) => fontSize = FontScaleState.Scale(fontSize);
}

[HarmonyPatch]
public static class RichTextPushFontSizePatch
{
    private static MethodBase TargetMethod() =>
        AccessTools.Method(typeof(RichTextLabel), "PushFontSize", [typeof(int)])!;

    [HarmonyPrefix]
    private static void Prefix(ref int fontSize) => fontSize = FontScaleState.Scale(fontSize);
}

[HarmonyPatch]
public static class RichTextPushOutlineSizePatch
{
    private static MethodBase TargetMethod() =>
        AccessTools.Method(typeof(RichTextLabel), "PushOutlineSize", [typeof(int)])!;

    [HarmonyPrefix]
    private static void Prefix(ref int outlineSize) => outlineSize = FontScaleState.Scale(outlineSize);
}

[HarmonyPatch]
public static class RichTextPushDropcapPatch
{
    private static MethodBase TargetMethod() =>
        AccessTools.Method(
            typeof(RichTextLabel),
            "PushDropcap",
            [
                typeof(string),
                typeof(Font),
                typeof(int),
                typeof(Rect2?),
                typeof(Color?),
                typeof(int),
                typeof(Color?)
            ])!;

    [HarmonyPrefix]
    private static void Prefix(ref int size, ref int outlineSize)
    {
        size = FontScaleState.Scale(size);
        outlineSize = FontScaleState.Scale(outlineSize);
    }
}
