using MyDbShumeiko.Core;
using MyDbShumeiko.Core.Types;

namespace MyDbShumeiko.Tests;

public class ColumnTypeTests
{
    [Theory]
    [InlineData("integer", " 42 ", "42")]
    [InlineData("integer", "-7", "-7")]
    [InlineData("real", "3,50", "3.5")]
    [InlineData("real", "1e3", "1000")]
    [InlineData("char", "я", "я")]
    [InlineData("string", "", "")]
    [InlineData("color", "ff8000", "#FF8000")]
    [InlineData("color", "#0a0B0c", "#0A0B0C")]
    [InlineData("colorInvl", "#000000 - #ff00ff", "#000000-#FF00FF")]
    [InlineData("colorInvl", "#102030-#102030", "#102030-#102030")]
    public void ValidValueIsNormalized(string type, string input, string expected)
    {
        Assert.True(ColumnType.FromName(type).TryNormalize(input, out var normalized, out _));
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData("integer", "1.5")]
    [InlineData("integer", "99999999999")]
    [InlineData("integer", "")]
    [InlineData("real", "abc")]
    [InlineData("real", "NaN")]
    [InlineData("char", "ab")]
    [InlineData("char", "")]
    [InlineData("color", "#12345")]
    [InlineData("color", "#GG0000")]
    [InlineData("color", "red")]
    [InlineData("colorInvl", "#000000")]
    [InlineData("colorInvl", "#000000-#FFFFFF-#000000")]
    [InlineData("colorInvl", "#FF0000-#00FFFF")] // канал R початку більший за R кінця
    public void InvalidValueIsRejected(string type, string input)
    {
        Assert.False(ColumnType.FromName(type).TryNormalize(input, out _, out var error));
        Assert.NotEmpty(error);
    }

    [Fact]
    public void UnknownTypeNameIsRejected()
    {
        Assert.Throws<DbValidationException>(() => ColumnType.FromName("date"));
    }

    [Fact]
    public void ColorIntervalContainsOnlyColorsInsideEveryChannel()
    {
        Assert.True(ColorInterval.TryParse("#101010-#202020", out var interval, out _));

        Assert.True(interval.Contains(new RgbColor(0x10, 0x18, 0x20)));
        Assert.False(interval.Contains(new RgbColor(0x10, 0x18, 0x21)));
    }
}
