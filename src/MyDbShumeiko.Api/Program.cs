using Microsoft.AspNetCore.Diagnostics;
using MyDbShumeiko.Api;
using MyDbShumeiko.Contracts;
using MyDbShumeiko.Core;
using MyDbShumeiko.Core.Types;

var builder = WebApplication.CreateBuilder(args);

var dataDirectory = Path.GetFullPath(
    builder.Configuration["DataDirectory"] ?? "data",
    builder.Environment.ContentRootPath);
builder.Services.AddSingleton(new DatabaseStore(dataDirectory));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Вебклієнт під час розробки працює з іншого порту, тому дозволяємо запити з будь-якого джерела.
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Помилки домену перетворюємо на 400/404 зі списком повідомлень.
app.UseExceptionHandler(handler => handler.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var (status, errors) = exception switch
    {
        DbValidationException e => (StatusCodes.Status400BadRequest, e.Errors),
        DbNotFoundException e => (StatusCodes.Status404NotFound, [e.Message]),
        BadHttpRequestException e => (e.StatusCode, [e.Message]),
        _ => (StatusCodes.Status500InternalServerError, (IReadOnlyList<string>)["Внутрішня помилка сервера."]),
    };
    context.Response.StatusCode = status;
    await context.Response.WriteAsJsonAsync(new ErrorResponse(errors));
}));

app.UseCors();
// Якщо поруч із сервером лежить опублікований вебклієнт (wwwroot), роздаємо його з тієї ж адреси.
app.UseDefaultFiles();
app.UsePrecompressedStaticFiles();
app.UseSwagger();
app.UseSwaggerUI();

var api = app.MapGroup("/api");

api.MapGet("/types", () => ColumnType.All.Select(t => t.Name));

api.MapGet("/databases", (DatabaseStore store) => store.List());

api.MapPost("/databases", (CreateDatabaseRequest request, DatabaseStore store) =>
{
    store.Create(request.Name);
    return Results.Created($"/api/databases/{Uri.EscapeDataString(request.Name.Trim())}", null);
});

api.MapGet("/databases/{db}", (string db, DatabaseStore store) =>
    store.Read(db, database => new DatabaseInfo(
        database.Name,
        database.Tables.Select(t => new TableSummary(t.Name, ColumnDtos(t.Columns), t.Rows.Count)).ToList())));

api.MapPost("/databases/{db}/save", (string db, DatabaseStore store) =>
{
    store.Save(db);
    return Results.NoContent();
});

api.MapPost("/databases/{db}/load", (string db, DatabaseStore store) =>
{
    store.Load(db);
    return Results.NoContent();
});

api.MapPost("/databases/{db}/tables", (string db, CreateTableRequest request, DatabaseStore store) =>
{
    var columns = (request.Columns ?? []).Select(c => new Column(c.Name, ColumnType.FromName(c.Type))).ToList();
    store.Write(db, database => database.CreateTable(request.Name, columns));
    return Results.Created(TableUrl(db, request.Name), null);
});

api.MapGet("/databases/{db}/tables/{table}", (string db, string table, DatabaseStore store) =>
    store.Read(db, database => ToDto(database.GetTable(table))));

api.MapDelete("/databases/{db}/tables/{table}", (string db, string table, DatabaseStore store) =>
{
    store.Write(db, database => database.DropTable(table));
    return Results.NoContent();
});

api.MapPost("/databases/{db}/tables/{table}/rows", (string db, string table, RowRequest request, DatabaseStore store) =>
{
    store.Write(db, database => database.GetTable(table).AddRow(request.Values ?? []));
    return Results.NoContent();
});

api.MapPut("/databases/{db}/tables/{table}/rows/{index:int}",
    (string db, string table, int index, RowRequest request, DatabaseStore store) =>
    {
        store.Write(db, database => database.GetTable(table).UpdateRow(index, request.Values ?? []));
        return Results.NoContent();
    });

api.MapDelete("/databases/{db}/tables/{table}/rows/{index:int}",
    (string db, string table, int index, DatabaseStore store) =>
    {
        store.Write(db, database => database.GetTable(table).DeleteRow(index));
        return Results.NoContent();
    });

// Прямий добуток двох таблиць; результат зберігається в базі як нова таблиця.
api.MapPost("/databases/{db}/product", (string db, ProductRequest request, DatabaseStore store) =>
{
    store.Write(db, database => database.CartesianProduct(request.First, request.Second, request.ResultName));
    return Results.Created(TableUrl(db, request.ResultName), null);
});

app.Run();

static string TableUrl(string db, string? table) =>
    $"/api/databases/{Uri.EscapeDataString(db.Trim())}/tables/{Uri.EscapeDataString(table?.Trim() ?? "")}";

static List<ColumnDto> ColumnDtos(IReadOnlyList<Column> columns) =>
    columns.Select(c => new ColumnDto(c.Name, c.Type.Name)).ToList();

static TableDto ToDto(Table table) =>
    new(table.Name, ColumnDtos(table.Columns), table.Rows.Select(r => r.Values).ToList());

public partial class Program;
