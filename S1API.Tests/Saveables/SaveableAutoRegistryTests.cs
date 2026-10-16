using S1API.Internal.Abstraction;
using S1API.Saveables;

namespace S1API.Tests.Saveables;

/// <summary>
/// Freezes which types <see cref="SaveableAutoRegistry"/> discovers and how it caches their instances.
/// </summary>
public sealed class SaveableAutoRegistryTests
{
    /// <summary>
    /// A direct, concrete <see cref="Saveable"/> subclass is registered; indirect and abstract subclasses and
    /// S1API's own types are not.
    /// </summary>
    [Fact]
    public void DiscoveryRegistersOnlyDirectConcreteSaveablesOutsideS1Api()
    {
        SaveableAutoRegistry.ClearCache();
        try
        {
            Type[] registered = SaveableAutoRegistry.GetRegisteredSaveables()
                .Select(saveable => saveable.GetType())
                .ToArray();

            Assert.Contains(typeof(DirectSaveableShape), registered);
            Assert.DoesNotContain(typeof(IndirectSaveableShape), registered);
            Assert.DoesNotContain(typeof(AbstractSaveableShape), registered);
            Assert.DoesNotContain(registered, type => type.Assembly == typeof(Saveable).Assembly);
        }
        finally
        {
            SaveableAutoRegistry.ClearCache();
        }
    }

    /// <summary>
    /// Discovery creates each saveable once; later calls return the cached instance.
    /// </summary>
    [Fact]
    public void DiscoveryReturnsTheSameInstanceOnRepeatedCalls()
    {
        SaveableAutoRegistry.ClearCache();
        try
        {
            Saveable first = SaveableAutoRegistry.GetRegisteredSaveables().OfType<DirectSaveableShape>().Single();
            Saveable second = SaveableAutoRegistry.GetRegisteredSaveables().OfType<DirectSaveableShape>().Single();

            Assert.Same(first, second);
        }
        finally
        {
            SaveableAutoRegistry.ClearCache();
        }
    }

    private sealed class DirectSaveableShape : Saveable
    {
    }

    private abstract class AbstractSaveableShape : Saveable
    {
    }

    private sealed class IndirectSaveableShape : AbstractSaveableShape
    {
    }
}
