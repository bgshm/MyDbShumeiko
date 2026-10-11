using MyDbShumeiko.Core;
using MyDbShumeiko.Core.Types;

namespace MyDbShumeiko.Tests;

public class TableTests
{
    private static Table Palette() => new("palette",
    [
        new Column("id", ColumnType.Integer),
        new Column("name", ColumnType.String),
        new Column("tone", ColumnType.Color),
    ]);

    [Fact]
    public void TableWithoutColumnsIsRejected()
    {
        Assert.Throws<DbValidationException>(() => new Table("empty", []));
    }

    [Fact]
    public void DuplicateColumnNamesAreRejected()
    {
        Assert.Throws<DbValidationException>(() => new Table("t",
        [
            new Column("id", ColumnType.Integer),
            new Column("ID", ColumnType.String),
        ]));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("../secret")]
    [InlineData("a/b")]
    public void InvalidTableNameIsRejected(string name)
    {
        Assert.Throws<DbValidationException>(() => new Table(name, [new Column("id", ColumnType.Integer)]));
    }

    [Fact]
    public void AddRowStoresNormalizedValues()
    {
        var table = Palette();

        table.AddRow(["1", "помаранчевий", "ff8000"]);

        Assert.Equal(["1", "помаранчевий", "#FF8000"], Assert.Single(table.Rows).Values);
    }

    [Fact]
    public void AddRowReportsEveryInvalidField()
    {
        var table = Palette();

        var error = Assert.Throws<DbValidationException>(() => table.AddRow(["one", "x", "red"]));

        Assert.Equal(2, error.Errors.Count);
        Assert.Empty(table.Rows);
    }

    [Fact]
    public void AddRowWithWrongValueCountIsRejected()
    {
        Assert.Throws<DbValidationException>(() => Palette().AddRow(["1", "x"]));
    }

    [Fact]
    public void InvalidEditLeavesRowUnchanged()
    {
        var table = Palette();
        table.AddRow(["1", "чорний", "#000000"]);

        Assert.Throws<DbValidationException>(() => table.UpdateRow(0, ["1", "чорний", "#00000"]));

        Assert.Equal(["1", "чорний", "#000000"], table.Rows[0].Values);
    }

    [Fact]
    public void UpdateAndDeleteRow()
    {
        var table = Palette();
        table.AddRow(["1", "чорний", "#000000"]);
        table.AddRow(["2", "білий", "#FFFFFF"]);

        table.UpdateRow(1, ["3", "сірий", "#808080"]);
        table.DeleteRow(0);

        Assert.Equal(["3", "сірий", "#808080"], Assert.Single(table.Rows).Values);
        Assert.Throws<DbNotFoundException>(() => table.DeleteRow(5));
    }
}
