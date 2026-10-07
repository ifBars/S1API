using S1API.Internal.Utils;

namespace S1API.Tests.Internal.Utils;

public sealed class SceneObjectIndexTests
{
    private sealed class Item
    {
        public string? Key;
        public bool Alive = true;
    }

    private static (SceneObjectIndex<Item> index, List<Item> scene) Make()
    {
        var scene = new List<Item>();
        var index = new SceneObjectIndex<Item>(() => scene.ToArray(), i => i.Key, i => i != null && i.Alive);
        return (index, scene);
    }

    [Fact]
    public void ScansOnceForRepeatedLookups()
    {
        var (index, scene) = Make();
        scene.Add(new Item { Key = "a" });
        Assert.Single(index.Get("a"));
        Assert.Single(index.Get("a"));
        Assert.Equal(1, index.Scans);
    }

    [Fact]
    public void FindsAnObjectCreatedAfterALookupFoundNothing()
    {
        var (index, scene) = Make();
        Assert.Empty(index.Get("b"));
        scene.Add(new Item { Key = "b" });
        Assert.Single(index.Get("b"));
    }

    [Fact]
    public void FindsAnObjectCreatedLaterUnderANewKeyWithoutLosingIndexedOnes()
    {
        var (index, scene) = Make();
        var first = new Item { Key = "a" };
        scene.Add(first);
        Assert.Same(first, Assert.Single(index.Get("a")));
        var late = new Item { Key = "late" };
        scene.Add(late);
        Assert.Same(late, Assert.Single(index.Get("late")));
        Assert.Same(first, Assert.Single(index.Get("a")));
    }

    [Fact]
    public void RebuildsWhenAnIndexedObjectsKeyChanged()
    {
        var (index, scene) = Make();
        var item = new Item { Key = "old" };
        scene.Add(item);
        index.Get("old");
        item.Key = "new";
        Assert.Empty(index.Get("old"));
        Assert.Same(item, Assert.Single(index.Get("new")));
    }

    [Fact]
    public void AllScansAgainWhileItFindsNothing()
    {
        var (index, scene) = Make();
        Assert.Empty(index.All());
        var late = new Item();
        scene.Add(late);
        Assert.Same(late, Assert.Single(index.All()));
        Assert.Same(late, Assert.Single(index.All()));
        Assert.Equal(2, index.Scans);
    }

    [Fact]
    public void RebuildsWhenAnIndexedObjectWasDestroyed()
    {
        var (index, scene) = Make();
        var old = new Item { Key = "c" };
        scene.Add(old);
        index.Get("c");
        old.Alive = false;
        scene.Remove(old);
        var replacement = new Item { Key = "c" };
        scene.Add(replacement);
        Assert.Same(replacement, Assert.Single(index.Get("c")));
        Assert.Equal(2, index.Scans);
    }

    [Fact]
    public void AllKeepsScanOrderIncludingUnkeyedObjects()
    {
        var (index, scene) = Make();
        var first = new Item();
        var second = new Item { Key = "x" };
        var third = new Item();
        scene.AddRange(new[] { first, second, third });
        Assert.Equal(new[] { first, second, third }, index.All());
    }

    [Fact]
    public void ResetAllClearsEveryIndex()
    {
        var (index, scene) = Make();
        index.Get("d");
        scene.Add(new Item { Key = "d" });
        SceneObjectIndexes.ResetAll();
        Assert.Single(index.Get("d"));
    }
}
