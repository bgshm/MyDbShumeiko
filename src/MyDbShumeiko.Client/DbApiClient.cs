using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using MyDbShumeiko.Contracts;

namespace MyDbShumeiko.Client;

/// <summary>Сервер відхилив запит; <see cref="Errors"/> — його повідомлення.</summary>
public sealed class ApiException(IReadOnlyList<string> errors) : Exception(string.Join(" ", errors))
{
    public IReadOnlyList<string> Errors { get; } = errors;
}

/// <summary>Типізований клієнт REST API, спільний для десктопної та вебверсії.</summary>
public sealed class DbApiClient(HttpClient http, string serverUrl)
{
    private static ContractsJsonContext Json => ContractsJsonContext.Default;

    public string ServerUrl { get; set; } = serverUrl;

    public Task<List<string>> GetTypesAsync() => GetAsync("api/types", Json.ListString);

    public Task<List<DatabaseSummary>> GetDatabasesAsync() => GetAsync("api/databases", Json.ListDatabaseSummary);

    public Task CreateDatabaseAsync(string name) =>
        PostAsync("api/databases", new CreateDatabaseRequest(name), Json.CreateDatabaseRequest);

    public Task<DatabaseInfo> GetDatabaseAsync(string db) => GetAsync(Db(db), Json.DatabaseInfo);

    public Task SaveDatabaseAsync(string db) => SendAsync(HttpMethod.Post, $"{Db(db)}/save");

    public Task LoadDatabaseAsync(string db) => SendAsync(HttpMethod.Post, $"{Db(db)}/load");

    public Task CreateTableAsync(string db, string name, IReadOnlyList<ColumnDto> columns) =>
        PostAsync($"{Db(db)}/tables", new CreateTableRequest(name, columns), Json.CreateTableRequest);

    public Task<TableDto> GetTableAsync(string db, string table) => GetAsync(Table(db, table), Json.TableDto);

    public Task DropTableAsync(string db, string table) => SendAsync(HttpMethod.Delete, Table(db, table));

    public Task AddRowAsync(string db, string table, IReadOnlyList<string?> values) =>
        PostAsync($"{Table(db, table)}/rows", new RowRequest(values), Json.RowRequest);

    public Task UpdateRowAsync(string db, string table, int index, IReadOnlyList<string?> values) =>
        SendAsync(HttpMethod.Put, $"{Table(db, table)}/rows/{index}", JsonContent.Create(new RowRequest(values), Json.RowRequest));

    public Task DeleteRowAsync(string db, string table, int index) =>
        SendAsync(HttpMethod.Delete, $"{Table(db, table)}/rows/{index}");

    public Task CartesianProductAsync(string db, string first, string second, string resultName) =>
        PostAsync($"{Db(db)}/product", new ProductRequest(first, second, resultName), Json.ProductRequest);

    private static string Db(string db) => $"api/databases/{Uri.EscapeDataString(db)}";

    private static string Table(string db, string table) => $"{Db(db)}/tables/{Uri.EscapeDataString(table)}";

    private async Task<T> GetAsync<T>(string path, JsonTypeInfo<T> type)
    {
        using var response = await SendCoreAsync(HttpMethod.Get, path, null);
        return await response.Content.ReadFromJsonAsync(type)
            ?? throw new ApiException(["Сервер повернув порожню відповідь."]);
    }

    private Task PostAsync<T>(string path, T body, JsonTypeInfo<T> type) =>
        SendAsync(HttpMethod.Post, path, JsonContent.Create(body, type));

    private async Task SendAsync(HttpMethod method, string path, HttpContent? content = null)
    {
        using var response = await SendCoreAsync(method, path, content);
    }

    private async Task<HttpResponseMessage> SendCoreAsync(HttpMethod method, string path, HttpContent? content)
    {
        if (!Uri.TryCreate(ServerUrl.Trim().TrimEnd('/') + "/", UriKind.Absolute, out var root)
            || (root.Scheme != Uri.UriSchemeHttp && root.Scheme != Uri.UriSchemeHttps))
            throw new ApiException([$"Некоректна адреса сервера «{ServerUrl}»."]);

        using var request = new HttpRequestMessage(method, new Uri(root, path)) { Content = content };
        var response = await http.SendAsync(request);
        if (response.IsSuccessStatusCode)
            return response;

        using (response)
        {
            ErrorResponse? error = null;
            try
            {
                error = await response.Content.ReadFromJsonAsync(Json.ErrorResponse);
            }
            catch (JsonException)
            {
                // Тіло відповіді не у форматі API — покажемо лише код.
            }

            throw new ApiException(error?.Errors is { Count: > 0 } errors
                ? errors
                : [$"Сервер відповів кодом {(int)response.StatusCode}."]);
        }
    }
}
