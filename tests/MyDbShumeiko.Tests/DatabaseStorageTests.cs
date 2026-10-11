using MyDbShumeiko.Core;
using MyDbShumeiko.Core.Storage;
using MyDbShumeiko.Core.Types;

namespace MyDbShumeiko.Tests;

public class DatabaseStorageTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "MyDbShumeiko.Tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public void SavedDatabaseIsLoadedBackUnchanged()
    {
        var database = new Database("демо");
        var table = database.CreateTable("усі типи", ColumnType.All.Select(t => new Column(t.Name, t)));
        table.AddRow(["-5", "2.25", "ї", "рядок із \"лапками\"", "#00FF7F", "#000000-#FFFFFF"]);
        database.CreateTable("порожня", [new Column("id", ColumnType.Integer)]);
        var path = Path.Combine(_directory, "demo.json");

        DatabaseStorage.Save(database, path);
        var loaded = DatabaseStorage.Load(path);

        Assert.Equal(database.Name, loaded.Name);
        Assert.Equal(["усі типи", "порожня"], loaded.Tables.Select(t => t.Name));
        var loadedTable = loaded.GetTable("усі типи");
        Assert.Equal(table.Columns, loadedTable.Columns);
        Assert.Equal(table.Rows[0].Values, Assert.Single(loadedTable.Rows).Values);
    }

    [Fact]
    public void LoadingMissingFileFails()
    {
        Assert.Throws<DbNotFoundException>(() => DatabaseStorage.Load(Path.Combine(_directory, "none.json")));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("""{"name":"d","tables":[{"name":"t","columns":[{"name":"c","type":"date"}],"rows":[]}]}""")]
    [InlineData("""{"name":"d","tables":[{"name":"t","columns":[{"name":"c","type":"color"}],"rows":[["red"]]}]}""")]
    public void CorruptedFileIsRejected(string json)
    {
        Assert.Throws<DbValidationException>(() => DatabaseStorage.Deserialize(json));
    }
}
