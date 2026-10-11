using MyDbShumeiko.Core.Types;

namespace MyDbShumeiko.Core;

public sealed record Column(string Name, ColumnType Type);

/// <summary>Рядок таблиці: значення в канонічному записі, по одному на кожне поле.</summary>
public sealed record Row(IReadOnlyList<string> Values);

public sealed class Table
{
    private readonly List<Column> _columns;
    private readonly List<Row> _rows = [];

    public Table(string name, IEnumerable<Column> columns)
    {
        Name = Names.Validate(name, "таблиці");
        _columns = columns.Select(c => new Column(Names.Validate(c.Name, "поля"), c.Type)).ToList();

        if (_columns.Count == 0)
            throw new DbValidationException("Таблиця повинна мати хоча б одне поле.");

        var duplicates = _columns
            .GroupBy(c => c.Name, Names.Comparer)
            .Where(g => g.Count() > 1)
            .Select(g => $"Назва поля «{g.Key}» повторюється.")
            .ToList();
        if (duplicates.Count > 0)
            throw new DbValidationException(duplicates);
    }

    public string Name { get; }
    public IReadOnlyList<Column> Columns => _columns;
    public IReadOnlyList<Row> Rows => _rows;

    public Row AddRow(IReadOnlyList<string?> values)
    {
        var row = new Row(Normalize(values));
        _rows.Add(row);
        return row;
    }

    public Row UpdateRow(int index, IReadOnlyList<string?> values)
    {
        CheckIndex(index);
        var row = new Row(Normalize(values));
        _rows[index] = row;
        return row;
    }

    public void DeleteRow(int index)
    {
        CheckIndex(index);
        _rows.RemoveAt(index);
    }

    /// <summary>Перевіряє значення рядка за типами полів і повертає їх у канонічному записі.</summary>
    public string[] Normalize(IReadOnlyList<string?> values)
    {
        if (values.Count != _columns.Count)
            throw new DbValidationException(
                $"Рядок має містити {_columns.Count} значень, а отримано {values.Count}.");

        var normalized = new string[values.Count];
        var errors = new List<string>();
        for (var i = 0; i < values.Count; i++)
        {
            var column = _columns[i];
            if (!column.Type.TryNormalize(values[i], out normalized[i], out var error))
                errors.Add($"Поле «{column.Name}» ({column.Type.Name}): {error}.");
        }

        if (errors.Count > 0)
            throw new DbValidationException(errors);
        return normalized;
    }

    /// <summary>
    /// Прямий (декартів) добуток: поля першої таблиці, за ними поля другої;
    /// рядки — усі пари «рядок першої, рядок другої».
    /// </summary>
    public static Table CartesianProduct(Table first, Table second, string resultName)
    {
        var result = new Table(resultName, ProductColumns(first, second));
        foreach (var left in first._rows)
            foreach (var right in second._rows)
                result._rows.Add(new Row([.. left.Values, .. right.Values]));
        return result;
    }

    private static List<Column> ProductColumns(Table first, Table second)
    {
        var sameName = Names.Comparer.Equals(first.Name, second.Name);
        var firstPrefix = sameName ? first.Name + "1" : first.Name;
        var secondPrefix = sameName ? second.Name + "2" : second.Name;

        var clashing = first._columns.Select(c => c.Name)
            .Intersect(second._columns.Select(c => c.Name), Names.Comparer)
            .ToHashSet(Names.Comparer);

        // Поля з однаковими назвами отримують префікс своєї таблиці.
        var names = first._columns
            .Select(c => clashing.Contains(c.Name) ? $"{firstPrefix}.{c.Name}" : c.Name)
            .Concat(second._columns
                .Select(c => clashing.Contains(c.Name) ? $"{secondPrefix}.{c.Name}" : c.Name))
            .ToList();

        var used = new HashSet<string>(Names.Comparer);
        var types = first._columns.Concat(second._columns).Select(c => c.Type).ToList();
        var columns = new List<Column>(names.Count);
        for (var i = 0; i < names.Count; i++)
        {
            var name = names[i];
            while (!used.Add(name))
                name += "_";
            columns.Add(new Column(name, types[i]));
        }

        return columns;
    }

    private void CheckIndex(int index)
    {
        if (index < 0 || index >= _rows.Count)
            throw new DbNotFoundException($"У таблиці «{Name}» немає рядка з номером {index}.");
    }
}
