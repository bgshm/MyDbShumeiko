using System.Globalization;

namespace MyDbShumeiko.Core.Types;

/// <summary>Колір у форматі RGB, текстовий запис — <c>#RRGGBB</c>.</summary>
public readonly record struct RgbColor(byte R, byte G, byte B)
{
    public static bool TryParse(string? text, out RgbColor color)
    {
        color = default;
        var s = text?.Trim() ?? "";
        if (s.StartsWith('#'))
            s = s[1..];
        if (s.Length != 6 || !s.All(Uri.IsHexDigit))
            return false;

        color = new RgbColor(
            byte.Parse(s.AsSpan(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            byte.Parse(s.AsSpan(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            byte.Parse(s.AsSpan(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
        return true;
    }

    public override string ToString() => $"#{R:X2}{G:X2}{B:X2}";
}
