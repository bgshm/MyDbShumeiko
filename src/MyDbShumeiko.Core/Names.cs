using System.Text.RegularExpressions;

namespace MyDbShumeiko.Core;

/// <summary>Правила іменування баз, таблиць і полів.</summary>
public static partial class Names
{
    public const int MaxLength = 64;

    public static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    // Лише літери, цифри, "_", "-", "." та пробіл усередині: назва бази стає іменем файлу.
    [GeneratedRegex(@"^[\p{L}\p{N}_]([\p{L}\p{N}_\-. ]*[\p{L}\p{N}_])?$")]
    private static partial Regex Pattern();

    public static string Validate(string? name, string what)
    {
        var trimmed = name?.Trim() ?? "";
        if (trimmed.Length == 0)
            throw new DbValidationException($"Назва {what} не може бути порожньою.");
        if (trimmed.Length > MaxLength)
            throw new DbValidationException($"Назва {what} задовга (максимум {MaxLength} символів).");
        if (!Pattern().IsMatch(trimmed))
            throw new DbValidationException(
                $"Назва {what} «{trimmed}» містить недопустимі символи (дозволено літери, цифри, «_», «-», «.» та пробіл).");
        return trimmed;
    }
}
