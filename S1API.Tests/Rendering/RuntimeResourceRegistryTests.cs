using S1API.Rendering;
using UnityEngine;
using Object = UnityEngine.Object;

namespace S1API.Tests.Rendering;

public sealed class RuntimeResourceRegistryTests
{
    private const string Path = "Example/Accessories/Cap";

    [Fact]
    public void FindCompatibleTypedAssetSkipsEntriesTheRequestedTypeRejects()
    {
        // The GameObject is registered first, as RegisterAsset does, ahead of the typed component entry.
        Object gameObject = TestObjectFactory.CreateUninitialized<GameObject>();
        Object texture = TestObjectFactory.CreateUninitialized<Texture2D>();
        var typedAssets = new Dictionary<string, Object>
        {
            [Path + "|UnityEngine.GameObject"] = gameObject,
            [Path + "|UnityEngine.Texture2D"] = texture,
        };

        Object? found = RuntimeResourceRegistry.FindCompatibleTypedAsset(
            typedAssets,
            Path,
            candidate => candidate is Texture2D);

        Assert.Same(texture, found);
    }

    [Fact]
    public void FindCompatibleTypedAssetReturnsNullWhenNothingIsCompatible()
    {
        // A Load<T> for a type nothing registered here satisfies must not be answered with the wrong object,
        // so the caller can fall through to its component lookup instead of failing a cast.
        var typedAssets = new Dictionary<string, Object>
        {
            [Path + "|UnityEngine.GameObject"] = TestObjectFactory.CreateUninitialized<GameObject>(),
        };

        Assert.Null(
            RuntimeResourceRegistry.FindCompatibleTypedAsset(
                typedAssets,
                Path,
                candidate => candidate is Texture2D));
    }

    [Fact]
    public void FindCompatibleTypedAssetKeepsReturningTheFirstEntryWhenTheTypeAcceptsAll()
    {
        Object first = TestObjectFactory.CreateUninitialized<GameObject>();
        var typedAssets = new Dictionary<string, Object>
        {
            [Path + "|UnityEngine.GameObject"] = first,
            [Path + "|UnityEngine.Texture2D"] = TestObjectFactory.CreateUninitialized<Texture2D>(),
        };

        Assert.Same(
            first,
            RuntimeResourceRegistry.FindCompatibleTypedAsset(typedAssets, Path, _ => true));
    }

    [Fact]
    public void FindCompatibleTypedAssetOnlyLooksAtEntriesForTheRequestedPath()
    {
        var typedAssets = new Dictionary<string, Object>
        {
            [Path + "Other|UnityEngine.GameObject"] = TestObjectFactory.CreateUninitialized<GameObject>(),
            ["Elsewhere|UnityEngine.GameObject"] = TestObjectFactory.CreateUninitialized<GameObject>(),
        };

        Assert.Null(
            RuntimeResourceRegistry.FindCompatibleTypedAsset(typedAssets, Path, _ => true));
    }
}
