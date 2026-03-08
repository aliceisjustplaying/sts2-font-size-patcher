using System.Reflection;
using System.Text.RegularExpressions;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Debug;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.addons.mega_text;

namespace ZSts2FontSizeMod;

public static class FontScaleState
{
    private const string ScaledMetaKey = "zsts2_font_scaled";
    private const string FooterScaledMetaKey = "zsts2_font_footer_scaled";
    private const string PatchNotesScaledMetaKey = "zsts2_font_patch_notes_scaled";
    private const string PreviewCardDescriptionScaledMetaKey = "zsts2_font_preview_card_description_scaled";
    private const string CharacterSelectRelicDescriptionScaledMetaKey = "zsts2_font_character_select_relic_description_scaled";

    private static readonly Regex FontSizeRegex = new(@"(?<=\bfont_size=)\d+", RegexOptions.Compiled);
    private static readonly Regex OutlineSizeRegex = new(@"(?<=\boutline_size=)\d+", RegexOptions.Compiled);
    private static readonly Regex FooterRegex = new(@"^\[(?<version>[^\]]+)\] \[(?<date>[^\]]+)\]$", RegexOptions.Compiled);

    private static readonly HashSet<ulong> HookedTrees = [];

    private static readonly FieldInfo? ReleaseInfoField =
        AccessTools.Field(typeof(NDebugInfoLabelManager), "_releaseInfo");
    private static readonly FieldInfo? SeedField =
        AccessTools.Field(typeof(NDebugInfoLabelManager), "_seed");
    private static readonly FieldInfo? PatchTextField =
        AccessTools.Field(typeof(NPatchNotesScreen), "_patchText");
    private static readonly FieldInfo? CardDescriptionField =
        AccessTools.Field(typeof(NCard), "_descriptionLabel");
    private static readonly FieldInfo? CharacterSelectRelicDescriptionField =
        AccessTools.Field(typeof(NCharacterSelectScreen), "_relicDescription");
    private static readonly FieldInfo? MegaRichTextMinField =
        AccessTools.Field(typeof(MegaRichTextLabel), "_minFontSize");
    private static readonly FieldInfo? MegaRichTextMaxField =
        AccessTools.Field(typeof(MegaRichTextLabel), "_maxFontSize");

    private static MegaCrit.Sts2.Core.Logging.Logger? _logger;

    public static FontSizeConfig Config { get; private set; } = new();

    public static void Initialize(MegaCrit.Sts2.Core.Logging.Logger logger)
    {
        _logger = logger;
        var assemblyPath = Assembly.GetExecutingAssembly().Location;
        var configPath = Path.Combine(Path.GetDirectoryName(assemblyPath) ?? ".", "font_size_config.json");
        Config = FontSizeConfig.Load(configPath, msg => logger.Info(msg));
    }

    public static int Scale(int value) => Scale(value, Config.BaseScale);

    public static int Scale(int value, double factor)
    {
        return value <= 0 ? value : (int)Math.Round(value * factor, MidpointRounding.AwayFromZero);
    }

    public static string ScaleBbcode(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        text = FontSizeRegex.Replace(text, match => ScaleMatch(match, Config.BaseScale));
        return OutlineSizeRegex.Replace(text, match => ScaleMatch(match, Config.BaseScale));
    }

    public static void ApplyMegaLabelBase(MegaLabel label)
    {
        if (HasMeta(label, ScaledMetaKey))
        {
            return;
        }

        ScaleMegaAutoSizeBounds(label);
        ApplyThemeOverride(label, "font_size", "Label", Config.BaseScale);
        label.SetMeta(ScaledMetaKey, true);
    }

    public static void ApplyMegaRichTextBase(MegaRichTextLabel label)
    {
        if (HasMeta(label, ScaledMetaKey))
        {
            return;
        }

        ScaleMegaAutoSizeBounds(label);
        ApplyRichTextOverrides(label, Config.BaseScale);
        label.SetMeta(ScaledMetaKey, true);
    }

    public static void ApplyFooterExtras(NDebugInfoLabelManager manager)
    {
        ApplyFooterLabel(GetFieldValue<MegaLabel>(ReleaseInfoField, manager));
        ApplyFooterLabel(GetFieldValue<MegaLabel>(SeedField, manager));
    }

    public static void ApplyPatchNotesExtras(NPatchNotesScreen screen)
    {
        var label = GetFieldValue<MegaRichTextLabel>(PatchTextField, screen);
        if (label == null || HasMeta(label, PatchNotesScaledMetaKey))
        {
            return;
        }

        ApplyRichTextOverrides(label, Config.PatchNotesScale);
        label.SetMeta(PatchNotesScaledMetaKey, true);
    }

    public static void ApplyPreviewCardDescriptionExtras(NCard card)
    {
        var label = GetFieldValue<MegaRichTextLabel>(CardDescriptionField, card);
        if (label == null || HasMeta(label, PreviewCardDescriptionScaledMetaKey))
        {
            return;
        }

        ApplyMegaRichTextBase(label);

        var ratio = GetAdditionalRatio(Config.PreviewCardDescriptionScale);
        if (ratio > 1.0)
        {
            ApplyRichTextOverrides(label, ratio);
            ScaleMegaRichTextBoundsDirect(label, ratio);
        }

        label.SetMeta(PreviewCardDescriptionScaledMetaKey, true);
    }

    public static void ApplyCharacterSelectRelicDescriptionFix(NCharacterSelectScreen screen)
    {
        var label = GetFieldValue<MegaRichTextLabel>(CharacterSelectRelicDescriptionField, screen);
        if (label == null || HasMeta(label, CharacterSelectRelicDescriptionScaledMetaKey))
        {
            return;
        }

        ApplyMegaRichTextBase(label);
        label.AutoSizeEnabled = false;
        label.Text = label.Text;
        label.SetMeta(CharacterSelectRelicDescriptionScaledMetaKey, true);
    }

    public static void RewriteFooterText(NDebugInfoLabelManager manager)
    {
        var label = GetFieldValue<MegaLabel>(ReleaseInfoField, manager);
        if (label == null)
        {
            return;
        }

        var currentText = label.Text;
        if (string.IsNullOrEmpty(currentText) || currentText.Contains("Font Patch", StringComparison.Ordinal))
        {
            return;
        }

        var match = FooterRegex.Match(currentText);
        if (!match.Success)
        {
            return;
        }

        var version = match.Groups["version"].Value;
        var date = match.Groups["date"].Value;
        label.SetTextAutoSize($"[{version} + Font Patch {Config.BaseScale:F2}x] [{date}]");
    }

    public static void HookGame(NGame game)
    {
        var tree = game.GetTree();
        var treeId = tree.GetInstanceId();
        if (HookedTrees.Add(treeId))
        {
            tree.NodeAdded += OnNodeAdded;
        }

        ScaleSubtree(game);
    }

    public static void OnNodeAdded(Node node)
    {
        ScaleNode(node);
    }

    public static void ScaleSubtree(Node node)
    {
        ScaleNode(node);
        for (var i = 0; i < node.GetChildCount(); i++)
        {
            ScaleSubtree(node.GetChild(i));
        }
    }

    private static void ScaleNode(Node node)
    {
        switch (node)
        {
            case MegaLabel:
            case MegaRichTextLabel:
                return;
            case RichTextLabel richTextLabel:
                ApplyPlainRichTextOverrides(richTextLabel);
                return;
            case Label label:
                ApplyPlainLabelOverrides(label);
                return;
        }
    }

    private static void ApplyPlainRichTextOverrides(RichTextLabel label)
    {
        if (HasMeta(label, ScaledMetaKey))
        {
            return;
        }

        ApplyRichTextOverrides(label, Config.BaseScale);
        label.SetMeta(ScaledMetaKey, true);
    }

    private static void ApplyPlainLabelOverrides(Label label)
    {
        if (HasMeta(label, ScaledMetaKey))
        {
            return;
        }

        var settings = label.LabelSettings;
        if (settings != null)
        {
            var clone = (LabelSettings)settings.Duplicate(false);
            clone.FontSize = Scale(clone.FontSize);
            clone.OutlineSize = Scale(clone.OutlineSize);
            clone.ShadowSize = Scale(clone.ShadowSize);
            label.LabelSettings = clone;
        }

        ApplyThemeOverride(label, "font_size", "Label", Config.BaseScale);
        label.SetMeta(ScaledMetaKey, true);
    }

    private static void ApplyFooterLabel(MegaLabel? label)
    {
        if (label == null || HasMeta(label, FooterScaledMetaKey))
        {
            return;
        }

        ApplyThemeOverride(label, "font_size", "Label", Config.DebugFooterScale);
        label.SetMeta(FooterScaledMetaKey, true);
    }

    private static void ApplyRichTextOverrides(Control control, double factor)
    {
        ApplyThemeOverride(control, "normal_font_size", "RichTextLabel", factor);
        ApplyThemeOverride(control, "bold_font_size", "RichTextLabel", factor);
        ApplyThemeOverride(control, "italics_font_size", "RichTextLabel", factor);
        ApplyThemeOverride(control, "bold_italics_font_size", "RichTextLabel", factor);
        ApplyThemeOverride(control, "mono_font_size", "RichTextLabel", factor);
    }

    private static void ScaleMegaAutoSizeBounds(MegaLabel label)
    {
        if (label.MinFontSize > 0)
        {
            label.MinFontSize = label.MinFontSize;
        }

        if (label.MaxFontSize > 0)
        {
            label.MaxFontSize = label.MaxFontSize;
        }
    }

    private static void ScaleMegaRichTextBoundsDirect(MegaRichTextLabel label, double factor)
    {
        ScaleFieldDirect(MegaRichTextMinField, label, factor);
        ScaleFieldDirect(MegaRichTextMaxField, label, factor);
        label.SetTextAutoSize(label.Text);
    }

    private static void ScaleFieldDirect(FieldInfo? field, object instance, double factor)
    {
        if (field?.GetValue(instance) is not int value || value <= 0)
        {
            return;
        }

        field.SetValue(instance, Scale(value, factor));
    }

    private static double GetAdditionalRatio(double totalFactor)
    {
        if (Config.BaseScale <= 0)
        {
            return totalFactor;
        }

        return totalFactor / Config.BaseScale;
    }

    private static void ScaleMegaAutoSizeBounds(MegaRichTextLabel label)
    {
        if (label.MinFontSize > 0)
        {
            label.MinFontSize = label.MinFontSize;
        }

        if (label.MaxFontSize > 0)
        {
            label.MaxFontSize = label.MaxFontSize;
        }
    }

    private static void ApplyThemeOverride(Control control, string propertyName, string themeType, double factor)
    {
        var size = control.GetThemeFontSize(propertyName, themeType);
        control.AddThemeFontSizeOverride(propertyName, Scale(size, factor));
    }

    private static bool HasMeta(GodotObject obj, string key)
    {
        return obj.HasMeta(key) && obj.GetMeta(key).AsBool();
    }

    private static T? GetFieldValue<T>(FieldInfo? field, object instance)
        where T : class
    {
        return field?.GetValue(instance) as T;
    }

    private static string ScaleMatch(Match match, double factor)
    {
        return int.TryParse(match.Value, out var value)
            ? Scale(value, factor).ToString()
            : match.Value;
    }
}
