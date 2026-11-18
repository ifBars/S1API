using System.Reflection;
using S1API.Shops;

#if IL2CPPMELON
using S1UICustomization = Il2CppScheduleOne.UI.CharacterCustomization;
#elif MONOMELON
using S1UICustomization = ScheduleOne.UI.CharacterCustomization;
#endif

namespace S1API.Tests.Shops;

[Collection("CustomizationShops")]
public sealed class CustomizationShopsTests : IDisposable
{
    private readonly List<(string Level, string Message)> _log = new();

    public CustomizationShopsTests()
    {
        CustomizationShops.ResetForTesting();
        CustomizationShops.SkipLiveShopsForTesting = true;
        CustomizationShops.LogSinkForTesting = (level, message) => _log.Add((level, message));
    }

    public void Dispose()
    {
        CustomizationShops.LogSinkForTesting = null;
        CustomizationShops.SkipLiveShopsForTesting = false;
        CustomizationShops.ResetForTesting();
    }

    private static CustomizationOptionSpec Spec(string category = "Chest", string id = "my_tattoo") => new()
    {
        Category = category,
        Label = "My Tattoo",
        Price = 250f,
        AvatarObjectId = id
    };

    [Fact]
    public void PatchTargetsExistInTargetRuntime()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        var category = typeof(S1UICustomization.CharacterCustomizationCategory);
        Assert.NotNull(category.GetMethod("Awake", flags, null, Type.EmptyTypes, null));
        foreach (var name in new[] { "OptionSelected", "OptionDeselected", "OptionPurchased" })
            Assert.NotNull(category.GetMethod(name, flags, null, new[] { typeof(S1UICustomization.CharacterCustomizationOption) }, null));
    }

    [Fact]
    public void AddOptionRegistersOneEntry()
    {
        using var handle = CustomizationShops.AddOption(CustomizationShop.Tattoo, Spec());

        var entries = CustomizationShops.EntriesFor(CustomizationShop.Tattoo);
        var entry = Assert.Single(entries);
        Assert.Equal("Chest", entry.Category);
        Assert.Equal("My Tattoo", entry.Label);
        Assert.Equal(250f, entry.Price);
        Assert.Equal("my_tattoo", entry.AvatarObjectId);
        Assert.Empty(CustomizationShops.EntriesFor(CustomizationShop.Barber));
    }

    [Fact]
    public void DisposeRemovesTheEntryAndIsIdempotent()
    {
        var handle = CustomizationShops.AddOption(CustomizationShop.Tattoo, Spec());
        Assert.Equal(1, CustomizationShops.ActiveCountForTesting);

        handle.Dispose();
        handle.Dispose();

        Assert.Equal(0, CustomizationShops.ActiveCountForTesting);
        Assert.Single(_log, l => l.Message.StartsWith("Removed option"));
    }

    [Fact]
    public void SameAvatarObjectInSameCategoryIsRegisteredOnce()
    {
        using var first = CustomizationShops.AddOption(CustomizationShop.Tattoo, Spec("Chest", "my_tattoo"));
        var second = CustomizationShops.AddOption(CustomizationShop.Tattoo, Spec("chest", "MY_TATTOO"));

        Assert.Equal(1, CustomizationShops.ActiveCountForTesting);
        Assert.Contains(_log, l => l.Level == "warn" && l.Message.Contains("already registered"));

        // Disposing the ignored second handle must not remove the first registration.
        second.Dispose();
        Assert.Equal(1, CustomizationShops.ActiveCountForTesting);
    }

    [Fact]
    public void SameAvatarObjectInAnotherCategoryOrShopIsAllowed()
    {
        using var a = CustomizationShops.AddOption(CustomizationShop.Tattoo, Spec("Chest"));
        using var b = CustomizationShops.AddOption(CustomizationShop.Tattoo, Spec("Face"));
        using var c = CustomizationShops.AddOption(CustomizationShop.Barber, Spec("Chest"));

        Assert.Equal(3, CustomizationShops.ActiveCountForTesting);
    }

    [Fact]
    public void UnsupportedShopWarnsAndListsNothing()
    {
        var handle = CustomizationShops.AddOption(CustomizationShop.Clothing, Spec());

        Assert.NotNull(handle);
        Assert.Equal(0, CustomizationShops.ActiveCountForTesting);
        Assert.Contains(_log, l => l.Level == "warn" && l.Message.Contains("Clothing"));
        handle.Dispose();
    }

    [Fact]
    public void LaterChangesToTheSpecDoNotAffectTheEntry()
    {
        var spec = Spec();
        using var handle = CustomizationShops.AddOption(CustomizationShop.Tattoo, spec);
        spec.Label = "Changed";
        spec.Price = 1f;

        var entry = Assert.Single(CustomizationShops.EntriesFor(CustomizationShop.Tattoo));
        Assert.Equal("My Tattoo", entry.Label);
        Assert.Equal(250f, entry.Price);
    }

    [Fact]
    public void NullSpecThrows()
    {
        Assert.Throws<ArgumentNullException>(() => CustomizationShops.AddOption(CustomizationShop.Tattoo, null!));
    }

    [Theory]
    [InlineData("", "L", "id", 0f)]
    [InlineData("Chest", " ", "id", 0f)]
    [InlineData("Chest", "L", "", 0f)]
    [InlineData("Chest", "L", "id", -1f)]
    public void InvalidSpecThrowsAndRegistersNothing(string category, string label, string id, float price)
    {
        var spec = new CustomizationOptionSpec { Category = category, Label = label, AvatarObjectId = id, Price = price };

        Assert.Throws<ArgumentException>(() => CustomizationShops.AddOption(CustomizationShop.Tattoo, spec));
        Assert.Equal(0, CustomizationShops.ActiveCountForTesting);
    }
}
