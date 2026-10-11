using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using MyDbShumeiko.Client;
using MyDbShumeiko.Ui.ViewModels;

namespace MyDbShumeiko.Tests;

/// <summary>Сценарії користувача: ViewModel клієнта працює зі справжнім сервером у пам'яті.</summary>
public class MainViewModelTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "MyDbShumeiko.Tests", Guid.NewGuid().ToString("N"));
    private readonly WebApplicationFactory<Program> _factory;
    private readonly MainViewModel _vm;

    public MainViewModelTests()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting("DataDirectory", _directory));
        _vm = new MainViewModel(new DbApiClient(_factory.CreateClient(), "http://localhost"));
    }

    public void Dispose()
    {
        _factory.Dispose();
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    private async Task CreateTableAsync(string name, params (string Name, string Type)[] columns)
    {
        _vm.NewTableName = name;
        _vm.NewColumns.Clear();
        foreach (var (columnName, type) in columns)
        {
            _vm.AddColumnCommand.Execute(null);
            _vm.NewColumns[^1].Name = columnName;
            _vm.NewColumns[^1].Type = type;
        }

        await _vm.CreateTableCommand.ExecuteAsync(null);
        Assert.False(_vm.IsError, _vm.StatusMessage);
    }

    private async Task AddRowAsync(params string[] values)
    {
        for (var i = 0; i < values.Length; i++)
            _vm.Editors[i].Value = values[i];
        await _vm.AddRowCommand.ExecuteAsync(null);
        Assert.False(_vm.IsError, _vm.StatusMessage);
    }

    [Fact]
    public async Task UserCreatesTablesEditsRowsAndBuildsProduct()
    {
        await _vm.ConnectCommand.ExecuteAsync(null);
        Assert.Equal(["integer", "real", "char", "string", "color", "colorInvl"], _vm.TypeNames);

        _vm.NewDatabaseName = "shop";
        await _vm.CreateDatabaseCommand.ExecuteAsync(null);
        Assert.Equal("shop", _vm.SelectedDatabase?.Name);

        await CreateTableAsync("items", ("id", "integer"), ("title", "string"));
        await AddRowAsync("1", "чашка");
        await AddRowAsync("2", "глечик");

        await CreateTableAsync("paints", ("tone", "color"), ("range", "colorInvl"));
        Assert.Equal("paints", _vm.SelectedTable?.Name);
        await AddRowAsync("ff0000", "#800000-#ff0000");

        // Невалідне редагування: сервер відмовляє, рядок лишається без змін.
        _vm.SelectedRow = _vm.Rows[0];
        _vm.Editors[0].Value = "червоний";
        await _vm.UpdateRowCommand.ExecuteAsync(null);
        Assert.True(_vm.IsError);
        Assert.Equal("#FF0000", _vm.Rows[0].Values[0]);

        _vm.Editors[0].Value = "#00ff00";
        await _vm.UpdateRowCommand.ExecuteAsync(null);
        Assert.False(_vm.IsError, _vm.StatusMessage);
        Assert.Equal("#00FF00", _vm.Rows[0].Values[0]);

        _vm.ProductFirst = _vm.Tables.Single(t => t.Name == "items");
        _vm.ProductSecond = _vm.Tables.Single(t => t.Name == "paints");
        _vm.ProductResultName = "mix";
        await _vm.ProductCommand.ExecuteAsync(null);

        Assert.False(_vm.IsError, _vm.StatusMessage);
        Assert.Equal("mix", _vm.SelectedTable?.Name);
        Assert.Equal(["id", "title", "tone", "range"], _vm.Columns.Select(c => c.Name));
        Assert.Equal(
            [
                ["1", "чашка", "#00FF00", "#800000-#FF0000"],
                ["2", "глечик", "#00FF00", "#800000-#FF0000"],
            ],
            _vm.Rows.Select(r => r.Values.ToArray()).ToArray());

        await _vm.DropTableCommand.ExecuteAsync(null);
        Assert.Equal(["items", "paints"], _vm.Tables.Select(t => t.Name));
    }

    [Fact]
    public async Task ReloadDiscardsUnsavedChanges()
    {
        await _vm.ConnectCommand.ExecuteAsync(null);
        _vm.NewDatabaseName = "demo";
        await _vm.CreateDatabaseCommand.ExecuteAsync(null);
        await CreateTableAsync("t", ("c", "char"));
        await AddRowAsync("a");
        await _vm.SaveDatabaseCommand.ExecuteAsync(null);
        Assert.True(_vm.SelectedDatabase!.IsSaved);

        await AddRowAsync("b");
        Assert.Equal(2, _vm.Rows.Count);
        await _vm.ReloadDatabaseCommand.ExecuteAsync(null);

        Assert.False(_vm.IsError, _vm.StatusMessage);
        Assert.Equal("t", _vm.SelectedTable?.Name);
        Assert.Equal(["a"], Assert.Single(_vm.Rows).Values);
    }
}
