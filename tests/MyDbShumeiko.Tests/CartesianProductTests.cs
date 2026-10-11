using MyDbShumeiko.Core;
using MyDbShumeiko.Core.Types;

namespace MyDbShumeiko.Tests;

/// <summary>Тести індивідуальної операції варіанту — прямого добутку двох таблиць.</summary>
public class CartesianProductTests
{
    private static Database Sample()
    {
        var database = new Database("shop");

        var items = database.CreateTable("items",
        [
            new Column("id", ColumnType.Integer),
            new Column("title", ColumnType.String),
        ]);
        items.AddRow(["1", "чашка"]);
        items.AddRow(["2", "тарілка"]);
        items.AddRow(["3", "глечик"]);

        var paints = database.CreateTable("paints",
        [
            new Column("id", ColumnType.Integer),
            new Column("tone", ColumnType.Color),
            new Column("range", ColumnType.ColorInvl),
        ]);
        paints.AddRow(["10", "#FF0000", "#800000-#FF0000"]);
        paints.AddRow(["20", "#0000FF", "#000080-#0000FF"]);

        return database;
    }

    [Fact]
    public void ProductContainsEveryPairOfRowsInOrder()
    {
        var database = Sample();

        var product = database.CartesianProduct("items", "paints", "items_x_paints");

        Assert.Equal(3 * 2, product.Rows.Count);
        Assert.Equal(
            [
                ["1", "чашка", "10", "#FF0000", "#800000-#FF0000"],
                ["1", "чашка", "20", "#0000FF", "#000080-#0000FF"],
                ["2", "тарілка", "10", "#FF0000", "#800000-#FF0000"],
                ["2", "тарілка", "20", "#0000FF", "#000080-#0000FF"],
                ["3", "глечик", "10", "#FF0000", "#800000-#FF0000"],
                ["3", "глечик", "20", "#0000FF", "#000080-#0000FF"],
            ],
            product.Rows.Select(r => r.Values.ToArray()).ToArray());
    }

    [Fact]
    public void ProductSchemaJoinsColumnsAndPrefixesClashingNames()
    {
        var product = Sample().CartesianProduct("items", "paints", "p");

        Assert.Equal(
            ["items.id", "title", "paints.id", "tone", "range"],
            product.Columns.Select(c => c.Name));
        Assert.Equal(
            [ColumnType.Integer, ColumnType.String, ColumnType.Integer, ColumnType.Color, ColumnType.ColorInvl],
            product.Columns.Select(c => c.Type));
    }

    [Fact]
    public void ProductIsStoredInDatabaseAndSourcesStayUntouched()
    {
        var database = Sample();

        var product = database.CartesianProduct("items", "paints", "p");

        Assert.Same(product, database.GetTable("p"));
        Assert.Equal(3, database.GetTable("items").Rows.Count);
        Assert.Equal(2, database.GetTable("paints").Rows.Count);
    }

    [Fact]
    public void ProductWithEmptyTableHasSchemaButNoRows()
    {
        var database = Sample();
        database.CreateTable("empty", [new Column("flag", ColumnType.Char)]);

        var product = database.CartesianProduct("items", "empty", "p");

        Assert.Equal(["id", "title", "flag"], product.Columns.Select(c => c.Name));
        Assert.Empty(product.Rows);
    }

    [Fact]
    public void ProductOfTableWithItselfGetsDistinctColumnNames()
    {
        var product = Sample().CartesianProduct("items", "items", "squared");

        Assert.Equal(
            ["items1.id", "items1.title", "items2.id", "items2.title"],
            product.Columns.Select(c => c.Name));
        Assert.Equal(9, product.Rows.Count);
    }

    [Fact]
    public void ProductFailsForMissingTableOrTakenResultName()
    {
        var database = Sample();

        Assert.Throws<DbNotFoundException>(() => database.CartesianProduct("items", "nope", "p"));
        Assert.Throws<DbValidationException>(() => database.CartesianProduct("items", "paints", "items"));
        Assert.Equal(2, database.Tables.Count);
    }
}
