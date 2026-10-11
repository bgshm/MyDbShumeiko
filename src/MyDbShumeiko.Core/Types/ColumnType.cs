using System.Globalization;

namespace MyDbShumeiko.Core.Types;

/// <summary>
/// Тип поля таблиці. Значення зберігаються як рядки в канонічному записі свого типу,
/// тому тип відповідає за перевірку введеного тексту та зведення його до цього запису.
/// </summary>
public abstract class ColumnType
{
    public static readonly ColumnType Integer = new IntegerType();
    public static readonly ColumnType Real = new RealType();
    public static readonly ColumnType Char = new CharType();
    public static readonly ColumnType String = new StringType();
    public static readonly ColumnType Color = new ColorType();
    public static readonly ColumnType ColorInvl = new ColorInvlType();

    public static IReadOnlyList<ColumnType> All { get; } = [Integer, Real, Char, String, Color, ColorInvl];

    public abstract string Name { get; }

    /// <summary>Перевіряє значення та повертає його канонічний запис.</summary>
    public abstract bool TryNormalize(string? input, out string normalized, out string error);

    public static ColumnType FromName(string? name) =>
        All.FirstOrDefault(t => string.Equals(t.Name, name?.Trim(), StringComparison.OrdinalIgnoreCase))
        ?? throw new DbValidationException(
            $"Невідомий тип «{name}». Доступні типи: {string.Join(", ", All.Select(t => t.Name))}.");

    public override string ToString() => Name;

    private sealed class IntegerType : ColumnType
    {
        public override string Name => "integer";

        public override bool TryNormalize(string? input, out string normalized, out string error)
        {
            if (int.TryParse(input?.Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value))
            {
                normalized = value.ToString(CultureInfo.InvariantCulture);
                error = "";
                return true;
            }

            normalized = "";
            error = $"очікується ціле число від {int.MinValue} до {int.MaxValue}";
            return false;
        }
    }

    private sealed class RealType : ColumnType
    {
        public override string Name => "real";

        public override bool TryNormalize(string? input, out string normalized, out string error)
        {
            // Приймаємо і крапку, і кому як десятковий роздільник.
            var text = input?.Trim().Replace(',', '.');
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                && double.IsFinite(value))
            {
                normalized = value.ToString("R", CultureInfo.InvariantCulture);
                error = "";
                return true;
            }

            normalized = "";
            error = "очікується дійсне число";
            return false;
        }
    }

    private sealed class CharType : ColumnType
    {
        public override string Name => "char";

        public override bool TryNormalize(string? input, out string normalized, out string error)
        {
            if (input is { Length: 1 })
            {
                normalized = input;
                error = "";
                return true;
            }

            normalized = "";
            error = "очікується рівно один символ";
            return false;
        }
    }

    private sealed class StringType : ColumnType
    {
        public override string Name => "string";

        public override bool TryNormalize(string? input, out string normalized, out string error)
        {
            normalized = input ?? "";
            error = "";
            return true;
        }
    }

    private sealed class ColorType : ColumnType
    {
        public override string Name => "color";

        public override bool TryNormalize(string? input, out string normalized, out string error)
        {
            if (RgbColor.TryParse(input, out var color))
            {
                normalized = color.ToString();
                error = "";
                return true;
            }

            normalized = "";
            error = "очікується RGB-код кольору у форматі #RRGGBB";
            return false;
        }
    }

    private sealed class ColorInvlType : ColumnType
    {
        public override string Name => "colorInvl";

        public override bool TryNormalize(string? input, out string normalized, out string error)
        {
            if (ColorInterval.TryParse(input, out var interval, out error))
            {
                normalized = interval.ToString();
                return true;
            }

            normalized = "";
            return false;
        }
    }
}
