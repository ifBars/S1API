using S1API.Internal.Items;

namespace S1API.Tests.Items;

public sealed class StoredItemPlaceholderTests
{
    [Theory]
    [InlineData(1f, 1f, 0.2f, 0.1f, 0.1f)]
    [InlineData(1f, 2f, 0.4f, 0.4f, 0.2f)]
    [InlineData(2f, 1f, 0.1f, 0.3f, 0.05f)]
    public void FitScaleKeepsTheSpriteInsideTheSpot(
        float spriteWidth,
        float spriteHeight,
        float spotWidth,
        float spotHeight,
        float expected)
    {
        float scale = StoredItemPlaceholder.FitScale(
            spriteWidth,
            spriteHeight,
            spotWidth,
            spotHeight);

        Assert.Equal(expected, scale, 4);
        Assert.True(spriteWidth * scale <= spotWidth + 1e-5f);
        Assert.True(spriteHeight * scale <= spotHeight + 1e-5f);
    }

    [Theory]
    [InlineData(0f, 1f, 0.1f, 0.1f)]
    [InlineData(1f, 0f, 0.1f, 0.1f)]
    [InlineData(1f, 1f, 0f, 0.1f)]
    [InlineData(1f, 1f, 0.1f, 0f)]
    public void FitScaleIsZeroForAnEmptySide(
        float spriteWidth,
        float spriteHeight,
        float spotWidth,
        float spotHeight)
    {
        Assert.Equal(
            0f,
            StoredItemPlaceholder.FitScale(
                spriteWidth,
                spriteHeight,
                spotWidth,
                spotHeight));
    }

    [Fact]
    public void TemplateIdsAreDistinctVanillaIds()
    {
        Assert.NotEmpty(StoredItemPlaceholder.TemplateIds);
        Assert.Equal(
            StoredItemPlaceholder.TemplateIds.Length,
            StoredItemPlaceholder.TemplateIds.Distinct().Count());
        Assert.All(StoredItemPlaceholder.TemplateIds, id => Assert.False(string.IsNullOrWhiteSpace(id)));
    }
}
