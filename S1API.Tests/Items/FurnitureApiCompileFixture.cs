using S1API.Items.Buildable;
using UnityEngine;

namespace S1API.Tests.Items;

internal static class FurnitureApiCompileFixture
{
    internal static FurnitureDefinitionBuilder Configure(GameObject model, Sprite icon)
    {
        return FurnitureCreator.CreateBuilder()
            .WithBasicInfo("example.mod:sofa-chair", "Sofa Chair", "A compact chair.")
            .WithModel(model)
            .WithPlacement(FurniturePlacementMode.Grid)
            .WithFootprint(2, 2)
            .WithBuildSound(BuildSoundType.Wood)
            .WithPricing(175f)
            .WithStackLimit(4)
            .WithIcon(icon);
    }

    internal static FurnitureDefinitionBuilder ConfigureNativeVariant(
        string donorId,
        Action<GameObject> configure)
    {
        return FurnitureCreator.CloneFrom(donorId)
            .WithBasicInfo("example.mod:blue-chair", "Blue Chair", "A recolored chair.")
            .ConfigureModel(configure)
            .WithGeneratedIcon();
    }

    internal static FurnitureDefinitionBuilder ConfigureNativeVariant(
        BuildableItemDefinition donor,
        Action<GameObject> configure)
    {
        return FurnitureCreator.CloneFrom(donor)
            .WithBasicInfo("example.mod:green-chair", "Green Chair", "Another recolored chair.")
            .ConfigureModel(configure);
    }
}
