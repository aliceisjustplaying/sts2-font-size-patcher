using System;
using System.Text;

namespace StsFontRuntimeHelper;

public static class RichTextScaler
{
    public static int ScaleInt(int value, double factor)
    {
        if (value <= 0)
        {
            return value;
        }

        return (int)Math.Round(value * factor, MidpointRounding.AwayFromZero);
    }

    public static string? ScaleBbcode(string? text, double factor)
    {
        if (text == null)
        {
            return null;
        }

        if (text.IndexOf("font_size=", StringComparison.Ordinal) < 0 &&
            text.IndexOf("outline_size=", StringComparison.Ordinal) < 0)
        {
            return text;
        }

        text = ScaleAttribute(text, "font_size=", factor);
        text = ScaleAttribute(text, "outline_size=", factor);
        return text;
    }

    private static string ScaleAttribute(string text, string marker, double factor)
    {
        StringBuilder? sb = null;
        var start = 0;

        while (true)
        {
            var markerIndex = text.IndexOf(marker, start, StringComparison.Ordinal);
            if (markerIndex < 0)
            {
                break;
            }

            var valueStart = markerIndex + marker.Length;
            var valueEnd = valueStart;

            if (valueEnd < text.Length && text[valueEnd] == '-')
            {
                valueEnd++;
            }

            while (valueEnd < text.Length && char.IsDigit(text[valueEnd]))
            {
                valueEnd++;
            }

            if (valueEnd == valueStart || (text[valueStart] == '-' && valueEnd == valueStart + 1))
            {
                start = valueStart;
                continue;
            }

            sb ??= new StringBuilder(text.Length + 16);
            sb.Append(text, start, valueStart - start);

            var value = int.Parse(text.Substring(valueStart, valueEnd - valueStart));
            sb.Append(ScaleInt(value, factor));
            start = valueEnd;
        }

        if (sb == null)
        {
            return text;
        }

        sb.Append(text, start, text.Length - start);
        return sb.ToString();
    }
}
