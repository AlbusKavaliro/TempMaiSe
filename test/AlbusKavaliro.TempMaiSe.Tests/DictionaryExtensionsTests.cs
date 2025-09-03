using AlbusKavaliro.TempMaiSe.Mailer;

namespace AlbusKavaliro.TempMaiSe.Tests;

[Trait("Category", "Unit")]
public class DictionaryExtensionsTests
{
    [Fact]
    public void TryGetValue_Throws_ArgumentNullException_When_Given_Null_Dictionary()
    {
        // Arrange
        Dictionary<string, object>? dictionary = null;

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>("dictionary", () => DictionaryExtensions.TryGetValue<string>(dictionary!, "key", out _));
    }

    [Theory]
    [InlineData("foo", "bar")]
    [InlineData("42", "")]
    public void TryGetValue_Returns_Correct_Value(string key, string value)
    {
        // Arrange
        Dictionary<string, object>? dictionary = new() { [key] = value, ["other"] = Guid.NewGuid() };

        // Act
        bool result = dictionary.TryGetValue(key, out object? actualValue);

        // Assert
        Assert.True(result);
        Assert.Equal(value, actualValue);
    }

    [Theory]
    [InlineData("foo", "bar")]
    [InlineData("42", "")]
    public void TryGetValue_Returns_False_If_Value_Has_Different_Type(string key, string value)
    {
        // Arrange
        Dictionary<string, object>? dictionary = new() { [key] = value, ["other"] = Guid.NewGuid() };

        // Act
        bool result = dictionary.TryGetValue(key, out Guid actualValue);

        // Assert
        Assert.False(result);
        Assert.Equal(default, actualValue);
    }

    [Theory]
    [InlineData("foo")]
    [InlineData("42")]
    public void TryGetValue_Returns_False_If_Value_Is_Null(string key)
    {
        // Arrange
        Dictionary<string, object>? dictionary = new() { [key] = null, ["other"] = Guid.NewGuid() };

        // Act
        bool result = dictionary.TryGetValue(key, out Guid actualValue);

        // Assert
        Assert.False(result);
        Assert.Equal(default, actualValue);
    }
}
