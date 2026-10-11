namespace MyDbShumeiko.Core;

/// <summary>Дані не пройшли валідацію (назва, схема таблиці або значення рядка).</summary>
public sealed class DbValidationException : Exception
{
    public DbValidationException(params string[] errors)
        : this((IReadOnlyList<string>)errors) { }

    public DbValidationException(IReadOnlyList<string> errors)
        : base(string.Join(" ", errors))
    {
        Errors = errors;
    }

    public IReadOnlyList<string> Errors { get; }
}

/// <summary>Запитаної бази, таблиці або рядка не існує.</summary>
public sealed class DbNotFoundException(string message) : Exception(message);
