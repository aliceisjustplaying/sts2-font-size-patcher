using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZSts2FontSizeMod;

public sealed class FontSizeConfig
{
    [JsonPropertyName("base_scale")]
    public double BaseScale { get; set; } = 1.20;

    [JsonPropertyName("debug_footer_extra_scale")]
    public double DebugFooterExtraScale { get; set; } = 0.50;

    [JsonPropertyName("patch_notes_extra_scale")]
    public double PatchNotesExtraScale { get; set; } = 0.25;

    [JsonPropertyName("preview_card_description_extra_scale")]
    public double PreviewCardDescriptionExtraScale { get; set; } = 0.20;

    public double DebugFooterScale => BaseScale + DebugFooterExtraScale;
    public double PatchNotesScale => BaseScale + PatchNotesExtraScale;
    public double PreviewCardDescriptionScale => BaseScale + PreviewCardDescriptionExtraScale;

    public static FontSizeConfig Load(string path, Action<string> log)
    {
        try
        {
            if (!File.Exists(path))
            {
                log($"Config not found at {path}, using defaults.");
                return new FontSizeConfig();
            }

            var config = JsonSerializer.Deserialize<FontSizeConfig>(File.ReadAllText(path));
            if (config == null)
            {
                log($"Config at {path} was empty, using defaults.");
                return new FontSizeConfig();
            }

            return config;
        }
        catch (Exception ex)
        {
            log($"Failed to load config at {path}: {ex}");
            return new FontSizeConfig();
        }
    }
}
