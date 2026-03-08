using System.Text.Json;

namespace ZSts2FontSizeMod;

public sealed class FontSizeConfig
{
    public double BaseScale { get; set; } = 1.20;
    public double DebugFooterExtraScale { get; set; } = 0.50;
    public double PatchNotesExtraScale { get; set; } = 0.25;

    public double DebugFooterScale => BaseScale + DebugFooterExtraScale;
    public double PatchNotesScale => BaseScale + PatchNotesExtraScale;

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
