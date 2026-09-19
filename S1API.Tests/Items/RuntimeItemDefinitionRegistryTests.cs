#if IL2CPPMELON
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
#elif MONOMELON
using S1ItemFramework = ScheduleOne.ItemFramework;
#endif

using S1API.Internal.Items;

namespace S1API.Tests.Items;

public sealed class RuntimeItemDefinitionRegistryTests : IDisposable
{
    private readonly RecordingAdapter _adapter = new();

    public RuntimeItemDefinitionRegistryTests()
    {
        RuntimeItemDefinitionRegistry.ResetForTesting(_adapter);
    }

    public void Dispose()
    {
        RuntimeItemDefinitionRegistry.RestoreRuntimeAdapterForTesting();
    }

    [Fact]
    public void PreLoadReappliesRetainedDefinitionWhenNativeRegistryRemovedIt()
    {
        S1ItemFramework.ItemDefinition definition = CreateDefinition();
        RuntimeItemDefinitionRegistry.Retain("example.mod:crystals", definition);

        RuntimeItemDefinitionRegistry.InvokePreLoadForTesting();

        Assert.Same(definition, Assert.Single(_adapter.Registered));
    }

    [Fact]
    public void PreLoadDoesNotDuplicateDefinitionStillPresentInNativeRegistry()
    {
        _adapter.RegisteredIds.Add("example.mod:press");
        RuntimeItemDefinitionRegistry.Retain(
            "example.mod:press",
            CreateDefinition());

        RuntimeItemDefinitionRegistry.InvokePreLoadForTesting();

        Assert.Empty(_adapter.Registered);
    }

    [Fact]
    public void ForgottenDefinitionIsNotReapplied()
    {
        RuntimeItemDefinitionRegistry.Retain(
            "example.mod:temporary",
            CreateDefinition());
        RuntimeItemDefinitionRegistry.Forget("example.mod:temporary");

        RuntimeItemDefinitionRegistry.InvokePreLoadForTesting();

        Assert.Empty(_adapter.Registered);
    }

    private static S1ItemFramework.ItemDefinition CreateDefinition()
    {
        return (S1ItemFramework.ItemDefinition)
            TestObjectFactory.CreateUninitialized(
                typeof(S1ItemFramework.StorableItemDefinition));
    }

    private sealed class RecordingAdapter : IRuntimeItemDefinitionRegistryAdapter
    {
        internal HashSet<string> RegisteredIds { get; } =
            new(StringComparer.OrdinalIgnoreCase);

        internal List<S1ItemFramework.ItemDefinition> Registered { get; } = new();

        public bool IsRegistered(string itemId)
        {
            return RegisteredIds.Contains(itemId);
        }

        public void Register(S1ItemFramework.ItemDefinition definition)
        {
            Registered.Add(definition);
        }
    }
}
