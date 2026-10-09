using S1API.Rendering;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace S1API.Tests.Rendering;

[Collection(RuntimeResourceRegistryCollection.Name)]
public sealed class RuntimeResourceRegistryTests
{
    private const string Path = "Example/Accessories/Cap";

#if MONOMELON
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void RegisteredLookupHonorsTheRequestedType(bool typed, bool compatible)
    {
        string path = Path + "/" + Guid.NewGuid().ToString("N");
        Type requestedType = compatible ? typeof(Texture2D) : typeof(GameObject);
        Object asset = TestObjectFactory.CreateUninitialized<Texture2D>();
        string key = typed ? path + "|" + requestedType.FullName : path;
        string fieldName = typed ? "_typedAssets" : "_registeredAssets";
        var registry = (Dictionary<string, Object>)typeof(RuntimeResourceRegistry)
            .GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;

        try
        {
            registry.Add(key, asset);
            Object? result = RuntimeResourceRegistry.GetRegisteredAssetForType(path, requestedType);
            if (compatible)
                Assert.Same(asset, result);
            else
                Assert.Null(result);
        }
        finally
        {
            registry.Remove(key);
        }
    }

#endif

    [Fact]
    public void FindCompatibleTypedAssetSkipsEntriesTheRequestedTypeRejects()
    {
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
