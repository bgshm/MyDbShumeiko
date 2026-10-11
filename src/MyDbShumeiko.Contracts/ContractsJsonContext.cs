using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyDbShumeiko.Contracts;

/// <summary>
/// Згенерована серіалізація контрактів: у WebAssembly-клієнті код обрізається,
/// і серіалізація через рефлексію там недоступна.
/// </summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(List<DatabaseSummary>))]
[JsonSerializable(typeof(DatabaseInfo))]
[JsonSerializable(typeof(TableDto))]
[JsonSerializable(typeof(CreateDatabaseRequest))]
[JsonSerializable(typeof(CreateTableRequest))]
[JsonSerializable(typeof(RowRequest))]
[JsonSerializable(typeof(ProductRequest))]
[JsonSerializable(typeof(ErrorResponse))]
public sealed partial class ContractsJsonContext : JsonSerializerContext;
