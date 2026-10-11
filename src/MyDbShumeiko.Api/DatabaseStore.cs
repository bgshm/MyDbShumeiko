using MyDbShumeiko.Contracts;
using MyDbShumeiko.Core;
using MyDbShumeiko.Core.Storage;

namespace MyDbShumeiko.Api;

/// <summary>
/// Відкриті бази в пам'яті сервера та їхні файли на диску.
/// Усі звернення до баз ідуть через цей клас під одним замком.
/// </summary>
public sealed class DatabaseStore(string directory)
{
    private const string Extension = ".json";

    private readonly Lock _lock = new();
    private readonly Dictionary<string, Database> _open = new(Names.Comparer);

    public IReadOnlyList<DatabaseSummary> List()
    {
        lock (_lock)
        {
            var saved = Directory.Exists(directory)
                ? Directory.EnumerateFiles(directory, "*" + Extension)
                    .Select(Path.GetFileNameWithoutExtension)
                    .OfType<string>()
                    .ToHashSet(Names.Comparer)
                : [];

            return _open.Values.Select(d => d.Name)
                .Concat(saved.Where(name => !_open.ContainsKey(name)))
                .Order(Names.Comparer)
                .Select(name => new DatabaseSummary(name, _open.ContainsKey(name), saved.Contains(name)))
                .ToList();
        }
    }

    public void Create(string name)
    {
        lock (_lock)
        {
            var database = new Database(name);
            if (_open.ContainsKey(database.Name) || File.Exists(PathOf(database.Name)))
                throw new DbValidationException($"База «{database.Name}» вже існує.");
            _open.Add(database.Name, database);
        }
    }

    public void Save(string name)
    {
        lock (_lock)
        {
            var database = Get(name);
            DatabaseStorage.Save(database, PathOf(database.Name));
        }
    }

    /// <summary>Зчитує базу з диска; незбережені зміни відкритої бази втрачаються.</summary>
    public void Load(string name)
    {
        lock (_lock)
        {
            var path = PathOf(Names.Validate(name, "бази"));
            if (!File.Exists(path))
                throw new DbNotFoundException($"Базу «{name}» не збережено на диску.");
            var database = DatabaseStorage.Load(path);
            _open[database.Name] = database;
        }
    }

    public T Read<T>(string name, Func<Database, T> read)
    {
        lock (_lock)
            return read(Get(name));
    }

    public void Write(string name, Action<Database> write)
    {
        lock (_lock)
            write(Get(name));
    }

    private Database Get(string name) =>
        _open.TryGetValue(name.Trim(), out var database)
            ? database
            : throw new DbNotFoundException($"Базу «{name}» не відкрито. Створіть її або зчитайте з диска.");

    private string PathOf(string validName) => Path.Combine(directory, validName + Extension);
}
