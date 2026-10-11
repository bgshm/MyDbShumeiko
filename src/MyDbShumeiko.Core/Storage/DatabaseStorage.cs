using System.Text.Encodings.Web;
using System.Text.Json;
using MyDbShumeiko.Core.Types;

namespace MyDbShumeiko.Core.Storage;

/// <summary>Збереження бази у JSON-файл і зчитування з нього.</summary>
public static class DatabaseStorage
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static void Save(Database database, string path)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        // Пишемо в тимчасовий файл, щоб збій під час запису не зіпсував попередню версію бази.
        var temp = path + ".tmp";
        File.WriteAllText(temp, Serialize(database));
        File.Move(temp, path, overwrite: true);
    }

    public static Database Load(string path)
    {
        if (!File.Exists(path))
            throw new DbNotFoundException($"Файл бази «{path}» не знайдено.");
        return Deserialize(File.ReadAllText(path));
    }

    public static string Serialize(Database database)
    {
        var file = new DatabaseFile(
            database.Name,
            database.Tables.Select(t => new TableFile(
                t.Name,
                t.Columns.Select(c => new ColumnFile(c.Name, c.Type.Name)).ToList(),
                t.Rows.Select(r => r.Values).ToList())).ToList());
        return JsonSerializer.Serialize(file, Options);
    }

    /// <summary>Відновлює базу, заново перевіряючи схему та кожен рядок.</summary>
    public static Database Deserialize(string json)
    {
        DatabaseFile? file;
        try
        {
            file = JsonSerializer.Deserialize<DatabaseFile>(json, Options);
        }
        catch (JsonException e)
        {
            throw new DbValidationException($"Файл бази пошкоджено: {e.Message}");
        }

        if (file is null)
            throw new DbValidationException("Файл бази порожній.");

        var database = new Database(file.Name);
        foreach (var tableFile in file.Tables ?? [])
        {
            var table = database.CreateTable(
                tableFile.Name,
                (tableFile.Columns ?? []).Select(c => new Column(c.Name, ColumnType.FromName(c.Type))));
            foreach (var row in tableFile.Rows ?? [])
                table.AddRow(row);
        }

        return database;
    }

    private sealed record DatabaseFile(string Name, List<TableFile>? Tables);

    private sealed record TableFile(string Name, List<ColumnFile>? Columns, List<IReadOnlyList<string>>? Rows);

    private sealed record ColumnFile(string Name, string Type);
}
