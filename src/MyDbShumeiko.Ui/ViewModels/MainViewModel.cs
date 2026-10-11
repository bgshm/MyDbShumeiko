using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyDbShumeiko.Client;
using MyDbShumeiko.Contracts;

namespace MyDbShumeiko.Ui.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly DbApiClient _api;

    // Поки колекції перезаповнюються, елементи керування скидають вибір у null —
    // на такі зміни реагувати не треба.
    private bool _updating;

    public MainViewModel(DbApiClient api)
    {
        _api = api;
        _serverUrl = api.ServerUrl;
        AddColumn();
    }

    [ObservableProperty] private string _serverUrl;
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private bool _isError;

    public ObservableCollection<DatabaseSummary> Databases { get; } = [];
    [ObservableProperty] private DatabaseSummary? _selectedDatabase;
    [ObservableProperty] private string _newDatabaseName = "";

    public ObservableCollection<TableSummary> Tables { get; } = [];
    [ObservableProperty] private TableSummary? _selectedTable;

    /// <summary>Поля відкритої таблиці; за ними подання будує стовпці сітки.</summary>
    [ObservableProperty] private IReadOnlyList<ColumnDto> _columns = [];
    public ObservableCollection<RowViewModel> Rows { get; } = [];
    [ObservableProperty] private RowViewModel? _selectedRow;
    public ObservableCollection<CellEditorViewModel> Editors { get; } = [];

    public ObservableCollection<string> TypeNames { get; } = [];
    [ObservableProperty] private string _newTableName = "";
    public ObservableCollection<NewColumnViewModel> NewColumns { get; } = [];

    [ObservableProperty] private TableSummary? _productFirst;
    [ObservableProperty] private TableSummary? _productSecond;
    [ObservableProperty] private string _productResultName = "";

    private string Db => SelectedDatabase?.Name ?? throw new ApiException(["Спершу виберіть або створіть базу."]);
    private string Tbl => SelectedTable?.Name ?? throw new ApiException(["Спершу виберіть таблицю."]);

    partial void OnSelectedDatabaseChanged(DatabaseSummary? value)
    {
        if (_updating)
            return;

        // Вибір таблиці стосувався попередньої бази.
        _updating = true;
        SelectedTable = null;
        _updating = false;
        _ = Run(() => OpenDatabaseAsync(value));
    }

    partial void OnSelectedTableChanged(TableSummary? value)
    {
        if (!_updating)
            _ = Run(LoadTableAsync);
    }

    partial void OnSelectedRowChanged(RowViewModel? value)
    {
        if (value is null)
            return;
        for (var i = 0; i < Editors.Count && i < value.Values.Count; i++)
            Editors[i].Value = value.Values[i];
    }

    [RelayCommand]
    private Task Connect() => Run(async () =>
    {
        _api.ServerUrl = ServerUrl;
        var types = await _api.GetTypesAsync();
        TypeNames.Clear();
        foreach (var type in types)
            TypeNames.Add(type);
        foreach (var column in NewColumns)
            column.Type ??= TypeNames.FirstOrDefault();

        await RefreshDatabasesAsync(SelectedDatabase?.Name);
        await OpenDatabaseAsync(SelectedDatabase);
    }, "Підключено до сервера.");

    [RelayCommand]
    private Task CreateDatabase() => Run(async () =>
    {
        var name = NewDatabaseName.Trim();
        await _api.CreateDatabaseAsync(name);
        NewDatabaseName = "";
        await RefreshDatabasesAsync(name);
        await LoadTablesAsync();
    }, "Базу створено.");

    [RelayCommand]
    private Task SaveDatabase() => Run(async () =>
    {
        await _api.SaveDatabaseAsync(Db);
        await RefreshDatabasesAsync(Db);
    }, "Базу збережено на диску сервера.");

    [RelayCommand]
    private Task ReloadDatabase() => Run(async () =>
    {
        await _api.LoadDatabaseAsync(Db);
        await RefreshDatabasesAsync(Db);
        await LoadTablesAsync();
    }, "Базу зчитано з диска сервера.");

    [RelayCommand]
    private void AddColumn() => NewColumns.Add(new NewColumnViewModel(TypeNames, c => NewColumns.Remove(c)));

    [RelayCommand]
    private Task CreateTable() => Run(async () =>
    {
        var name = NewTableName.Trim();
        var columns = NewColumns.Select(c => new ColumnDto(c.Name, c.Type ?? "")).ToList();
        await _api.CreateTableAsync(Db, name, columns);

        NewTableName = "";
        NewColumns.Clear();
        AddColumn();
        await LoadTablesAsync(name);
    }, "Таблицю створено.");

    [RelayCommand]
    private Task DropTable() => Run(async () =>
    {
        await _api.DropTableAsync(Db, Tbl);
        await LoadTablesAsync();
    }, "Таблицю знищено.");

    [RelayCommand]
    private Task AddRow() => Run(async () =>
    {
        await _api.AddRowAsync(Db, Tbl, EditorValues());
        await LoadTablesAsync(Tbl);
    }, "Рядок додано.");

    [RelayCommand]
    private Task UpdateRow() => Run(async () =>
    {
        var row = SelectedRow ?? throw new ApiException(["Спершу виберіть рядок у таблиці."]);
        await _api.UpdateRowAsync(Db, Tbl, row.Index, EditorValues());
        await LoadTableAsync();
        SelectedRow = Rows.ElementAtOrDefault(row.Index);
    }, "Рядок змінено.");

    [RelayCommand]
    private Task DeleteRow() => Run(async () =>
    {
        var row = SelectedRow ?? throw new ApiException(["Спершу виберіть рядок у таблиці."]);
        await _api.DeleteRowAsync(Db, Tbl, row.Index);
        await LoadTablesAsync(Tbl);
    }, "Рядок знищено.");

    [RelayCommand]
    private Task Product() => Run(async () =>
    {
        if (ProductFirst is null || ProductSecond is null)
            throw new ApiException(["Виберіть обидві таблиці для прямого добутку."]);
        var name = ProductResultName.Trim();
        await _api.CartesianProductAsync(Db, ProductFirst.Name, ProductSecond.Name, name);
        ProductResultName = "";
        await LoadTablesAsync(name);
    }, "Прямий добуток збережено як нову таблицю.");

    private List<string?> EditorValues() => Editors.Select(e => (string?)e.Value).ToList();

    private async Task RefreshDatabasesAsync(string? select)
    {
        var databases = await _api.GetDatabasesAsync();
        _updating = true;
        try
        {
            Databases.Clear();
            foreach (var database in databases)
                Databases.Add(database);
            SelectedDatabase = Find(Databases, d => d.Name, select);
        }
        finally
        {
            _updating = false;
        }
    }

    private async Task OpenDatabaseAsync(DatabaseSummary? database)
    {
        // Базу, яка є лише на диску, спершу зчитуємо в пам'ять сервера.
        if (database is { IsOpen: false })
        {
            await _api.LoadDatabaseAsync(database.Name);
            await RefreshDatabasesAsync(database.Name);
        }

        await LoadTablesAsync();
    }

    private async Task LoadTablesAsync(string? select = null)
    {
        // Без явної вказівки лишаємо вибраною ту саму таблицю.
        select ??= SelectedTable?.Name;
        var tables = SelectedDatabase is null ? [] : (await _api.GetDatabaseAsync(Db)).Tables;
        var first = ProductFirst?.Name;
        var second = ProductSecond?.Name;

        _updating = true;
        try
        {
            Tables.Clear();
            foreach (var table in tables)
                Tables.Add(table);
            SelectedTable = Find(Tables, t => t.Name, select);
            ProductFirst = Find(Tables, t => t.Name, first);
            ProductSecond = Find(Tables, t => t.Name, second);
        }
        finally
        {
            _updating = false;
        }

        await LoadTableAsync();
    }

    private async Task LoadTableAsync()
    {
        var table = SelectedTable is null ? null : await _api.GetTableAsync(Db, Tbl);
        var columns = table?.Columns ?? [];

        // Старі рядки прибираємо до зміни схеми: вони не відповідають новим стовпцям сітки.
        Rows.Clear();

        // Форму рядка перебудовуємо лише при зміні схеми, щоб не губити введене.
        if (!Columns.SequenceEqual(columns))
        {
            Columns = columns;
            Editors.Clear();
            foreach (var column in columns)
                Editors.Add(new CellEditorViewModel(column.Name, column.Type));
        }

        for (var i = 0; i < (table?.Rows.Count ?? 0); i++)
            Rows.Add(new RowViewModel(i, table!.Rows[i]));
    }

    private static T? Find<T>(IEnumerable<T> items, Func<T, string> name, string? wanted) where T : class =>
        wanted is null ? null : items.FirstOrDefault(i => string.Equals(name(i), wanted, StringComparison.OrdinalIgnoreCase));

    private async Task Run(Func<Task> action, string? success = null)
    {
        try
        {
            await action();
            if (success is not null)
                SetStatus(success, isError: false);
        }
        catch (ApiException e)
        {
            SetStatus(string.Join(" ", e.Errors), isError: true);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            SetStatus($"Немає зв'язку із сервером {ServerUrl}: {e.Message}", isError: true);
        }
        catch (Exception e)
        {
            // Непередбачена помилка не повинна закривати застосунок.
            SetStatus($"Помилка: {e.Message}", isError: true);
        }
    }

    private void SetStatus(string message, bool isError)
    {
        StatusMessage = message;
        IsError = isError;
    }
}
