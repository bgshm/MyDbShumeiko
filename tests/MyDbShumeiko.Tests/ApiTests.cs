using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using MyDbShumeiko.Contracts;

namespace MyDbShumeiko.Tests;

public class ApiTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "MyDbShumeiko.Tests", Guid.NewGuid().ToString("N"));
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ApiTests()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting("DataDirectory", _directory));
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    private async Task CreateSampleAsync()
    {
        await Ok(_client.PostAsJsonAsync("/api/databases", new CreateDatabaseRequest("shop")));
        await Ok(_client.PostAsJsonAsync("/api/databases/shop/tables",
            new CreateTableRequest("items", [new("id", "integer"), new("title", "string")])));
        await Ok(_client.PostAsJsonAsync("/api/databases/shop/tables",
            new CreateTableRequest("paints", [new("tone", "color"), new("range", "colorInvl")])));
        await Ok(_client.PostAsJsonAsync("/api/databases/shop/tables/items/rows", new RowRequest(["1", "чашка"])));
        await Ok(_client.PostAsJsonAsync("/api/databases/shop/tables/items/rows", new RowRequest(["2", "глечик"])));
        await Ok(_client.PostAsJsonAsync("/api/databases/shop/tables/paints/rows",
            new RowRequest(["ff0000", "#800000-#ff0000"])));
    }

    private static async Task Ok(Task<HttpResponseMessage> request)
    {
        var response = await request;
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ProductEndpointCreatesTableWithAllPairs()
    {
        await CreateSampleAsync();

        await Ok(_client.PostAsJsonAsync("/api/databases/shop/product", new ProductRequest("items", "paints", "mix")));

        var mix = await _client.GetFromJsonAsync<TableDto>("/api/databases/shop/tables/mix");
        Assert.Equal(["id", "title", "tone", "range"], mix!.Columns.Select(c => c.Name));
        Assert.Equal(
            [
                ["1", "чашка", "#FF0000", "#800000-#FF0000"],
                ["2", "глечик", "#FF0000", "#800000-#FF0000"],
            ],
            mix.Rows.Select(r => r.ToArray()).ToArray());
    }

    [Fact]
    public async Task InvalidRowIsRejectedWithErrorList()
    {
        await CreateSampleAsync();

        var response = await _client.PutAsJsonAsync("/api/databases/shop/tables/paints/rows/0",
            new RowRequest(["red", "#FF0000-#000000"]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal(2, error!.Errors.Count);
    }

    [Fact]
    public async Task MissingThingsReturnNotFound()
    {
        await CreateSampleAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/databases/nope")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/databases/shop/tables/nope")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await _client.DeleteAsync("/api/databases/shop/tables/items/rows/9")).StatusCode);
    }

    [Fact]
    public async Task StaticFileIsServedCompressedOnlyWhenClientAcceptsIt()
    {
        var webRoot = Path.Combine(_directory, "wwwroot");
        Directory.CreateDirectory(webRoot);
        File.WriteAllText(Path.Combine(webRoot, "app.js"), "plain");
        File.WriteAllText(Path.Combine(webRoot, "app.js.gz"), "packed");
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseWebRoot(webRoot));
        using var client = factory.CreateClient();

        var plain = await client.GetAsync("/app.js");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/app.js");
        request.Headers.AcceptEncoding.ParseAdd("gzip, deflate");
        var packed = await client.SendAsync(request);

        Assert.Equal("plain", await plain.Content.ReadAsStringAsync());
        Assert.Empty(plain.Content.Headers.ContentEncoding);
        Assert.Equal("packed", await packed.Content.ReadAsStringAsync());
        Assert.Equal(["gzip"], packed.Content.Headers.ContentEncoding);
        Assert.Equal("text/javascript", packed.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task SavedDatabaseSurvivesServerRestart()
    {
        await CreateSampleAsync();
        await Ok(_client.DeleteAsync("/api/databases/shop/tables/paints"));
        await Ok(_client.PostAsync("/api/databases/shop/save", null));

        // Новий екземпляр сервера з тією ж текою даних.
        using var restarted = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting("DataDirectory", _directory));
        using var client = restarted.CreateClient();

        var list = await client.GetFromJsonAsync<List<DatabaseSummary>>("/api/databases");
        Assert.Equal([new DatabaseSummary("shop", IsOpen: false, IsSaved: true)], list);

        await Ok(client.PostAsync("/api/databases/shop/load", null));
        var info = await client.GetFromJsonAsync<DatabaseInfo>("/api/databases/shop");
        var items = Assert.Single(info!.Tables);
        Assert.Equal(("items", 2), (items.Name, items.RowCount));
    }
}
