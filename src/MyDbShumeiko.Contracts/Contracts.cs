namespace MyDbShumeiko.Contracts;

// Об'єкти, якими обмінюються REST API та клієнти.

/// <param name="IsOpen">База завантажена в пам'ять сервера.</param>
/// <param name="IsSaved">На диску сервера є файл цієї бази.</param>
public sealed record DatabaseSummary(string Name, bool IsOpen, bool IsSaved);

public sealed record DatabaseInfo(string Name, IReadOnlyList<TableSummary> Tables);

public sealed record TableSummary(string Name, IReadOnlyList<ColumnDto> Columns, int RowCount);

public sealed record TableDto(string Name, IReadOnlyList<ColumnDto> Columns, IReadOnlyList<IReadOnlyList<string>> Rows);

public sealed record ColumnDto(string Name, string Type);

public sealed record CreateDatabaseRequest(string Name);

public sealed record CreateTableRequest(string Name, IReadOnlyList<ColumnDto> Columns);

public sealed record RowRequest(IReadOnlyList<string?> Values);

public sealed record ProductRequest(string First, string Second, string ResultName);

/// <summary>Тіло відповіді з кодом 400 або 404.</summary>
public sealed record ErrorResponse(IReadOnlyList<string> Errors);
