namespace MyDbShumeiko.Core;

public sealed class Database
{
    private readonly List<Table> _tables = [];

    public Database(string name)
    {
        Name = Names.Validate(name, "бази");
    }

    public string Name { get; }
    public IReadOnlyList<Table> Tables => _tables;

    public Table? FindTable(string name) =>
        _tables.FirstOrDefault(t => Names.Comparer.Equals(t.Name, name?.Trim()));

    public Table GetTable(string name) =>
        FindTable(name) ?? throw new DbNotFoundException($"У базі «{Name}» немає таблиці «{name}».");

    public Table CreateTable(string name, IEnumerable<Column> columns) => AddTable(new Table(name, columns));

    public Table AddTable(Table table)
    {
        if (FindTable(table.Name) is not null)
            throw new DbValidationException($"Таблиця «{table.Name}» вже існує в базі «{Name}».");
        _tables.Add(table);
        return table;
    }

    public void DropTable(string name) => _tables.Remove(GetTable(name));

    /// <summary>Будує прямий добуток двох таблиць бази та додає його до бази як нову таблицю.</summary>
    public Table CartesianProduct(string firstName, string secondName, string resultName) =>
        AddTable(Table.CartesianProduct(GetTable(firstName), GetTable(secondName), resultName));
}
