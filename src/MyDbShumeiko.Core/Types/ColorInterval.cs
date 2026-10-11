namespace MyDbShumeiko.Core.Types;

/// <summary>
/// Інтервал кольорів <c>#RRGGBB-#RRGGBB</c>: кожен канал початку не більший за відповідний канал кінця.
/// </summary>
public readonly record struct ColorInterval
{
    public const char Separator = '-';

    private ColorInterval(RgbColor from, RgbColor to)
    {
        From = from;
        To = to;
    }

    public RgbColor From { get; }
    public RgbColor To { get; }

    public static bool TryCreate(RgbColor from, RgbColor to, out ColorInterval interval)
    {
        interval = new ColorInterval(from, to);
        return from.R <= to.R && from.G <= to.G && from.B <= to.B;
    }

    public static bool TryParse(string? text, out ColorInterval interval, out string error)
    {
        interval = default;
        var parts = (text ?? "").Split(Separator);
        if (parts.Length != 2
            || !RgbColor.TryParse(parts[0], out var from)
            || !RgbColor.TryParse(parts[1], out var to))
        {
            error = "очікується інтервал у форматі #RRGGBB-#RRGGBB";
            return false;
        }

        if (!TryCreate(from, to, out interval))
        {
            error = $"початок інтервалу {from} має бути не більшим за кінець {to} у кожному з каналів R, G, B";
            return false;
        }

        error = "";
        return true;
    }

    public bool Contains(RgbColor color) =>
        From.R <= color.R && color.R <= To.R &&
        From.G <= color.G && color.G <= To.G &&
        From.B <= color.B && color.B <= To.B;

    public override string ToString() => $"{From}{Separator}{To}";
}
