using System.Reflection;
using S1API.Internal.Vehicles;
using S1API.Vehicles;

#if IL2CPPMELON
using S1Vehicles = Il2CppScheduleOne.Vehicles;
#elif MONOMELON
using S1Vehicles = ScheduleOne.Vehicles;
#endif

namespace S1API.Tests.Vehicles;

public sealed class VehicleTypeTests : IDisposable
{
    private readonly List<string> _log = new List<string>();
    private readonly List<string> _applied = new List<string>();

    public VehicleTypeTests()
    {
        VehicleTypeRuntime.ResetForTesting();
        VehicleTypeRuntime.LogSinkForTesting = _log.Add;
        VehicleTypeRuntime.ApplyNowForTesting = _applied.Add;
    }

    public void Dispose()
    {
        VehicleTypeRuntime.ResetForTesting();
        VehicleTypeRuntime.LogSinkForTesting = null;
        VehicleTypeRuntime.ApplyNowForTesting = null;
        VehicleTypeRuntime.CoopAfterLoadForTesting = null;
    }

    [Fact]
    public void ValidTypeIsRegistered()
    {
        bool added = VehicleRegistry.RegisterType(new VehicleTypeDefinition
        {
            Code = "test_car",
            BaseCode = "hounddog",
            Name = "Test Car",
        });

        Assert.True(added);
        Assert.True(VehicleRegistry.IsTypeRegistered("test_car"));
        Assert.Equal(new[] { "test_car" }, _applied);
        Assert.Contains("info: Vehicle type 'test_car' registered (base 'hounddog').", _log);
    }

    [Fact]
    public void CodeAndBaseCodeAreTrimmed()
    {
        Assert.True(VehicleRegistry.RegisterType(new VehicleTypeDefinition { Code = "  test_car ", BaseCode = " hounddog " }));

        Assert.True(VehicleRegistry.IsTypeRegistered("test_car"));
        Assert.Contains("info: Vehicle type 'test_car' registered (base 'hounddog').", _log);
    }

    [Fact]
    public void NullDefinitionIsRefusedWithError()
    {
        Assert.False(VehicleRegistry.RegisterType(null!));

        Assert.Contains("error: RegisterType got no definition.", _log);
        Assert.Empty(_applied);
    }

    [Fact]
    public void EmptyCodeIsRefusedWithError()
    {
        Assert.False(VehicleRegistry.RegisterType(new VehicleTypeDefinition { Code = " ", BaseCode = "hounddog" }));

        Assert.Single(_log, line => line.StartsWith("error: ") && line.Contains("the code is empty"));
        Assert.Empty(_applied);
    }

    [Fact]
    public void EmptyBaseCodeIsRefusedWithError()
    {
        Assert.False(VehicleRegistry.RegisterType(new VehicleTypeDefinition { Code = "test_car" }));

        Assert.Single(_log, line => line.StartsWith("error: ") && line.Contains("the base code is empty"));
        Assert.False(VehicleRegistry.IsTypeRegistered("test_car"));
    }

    [Fact]
    public void CodeEqualToBaseCodeIsRefusedWithError()
    {
        Assert.False(VehicleRegistry.RegisterType(new VehicleTypeDefinition { Code = "Hounddog", BaseCode = "hounddog" }));

        Assert.Single(_log, line => line.StartsWith("error: ") && line.Contains("equals its base code"));
    }

    [Fact]
    public void DuplicateCodeIsRefusedWithoutCase()
    {
        Assert.True(VehicleRegistry.RegisterType(new VehicleTypeDefinition { Code = "test_car", BaseCode = "hounddog" }));
        Assert.False(VehicleRegistry.RegisterType(new VehicleTypeDefinition { Code = "TEST_CAR", BaseCode = "shitbox" }));

        Assert.Single(_log, line => line.StartsWith("error: ") && line.Contains("already registered"));
        Assert.Equal(new[] { "test_car" }, _applied);
    }

    [Fact]
    public void IsTypeRegisteredIgnoresCaseAndBlank()
    {
        VehicleRegistry.RegisterType(new VehicleTypeDefinition { Code = "test_car", BaseCode = "hounddog" });

        Assert.True(VehicleRegistry.IsTypeRegistered("Test_Car"));
        Assert.False(VehicleRegistry.IsTypeRegistered(""));
        Assert.False(VehicleRegistry.IsTypeRegistered("other"));
    }

    [Fact]
    public void ThrowingTypesRegisteredHandlerIsLoggedAndNextHandlerRuns()
    {
        bool secondRan = false;
        Action first = () => throw new InvalidOperationException("boom");
        Action second = () => secondRan = true;
        VehicleRegistry.OnTypesRegistered += first;
        VehicleRegistry.OnTypesRegistered += second;
        try
        {
            VehicleRegistry.RaiseTypesRegistered();
        }
        finally
        {
            VehicleRegistry.OnTypesRegistered -= first;
            VehicleRegistry.OnTypesRegistered -= second;
        }

        Assert.True(secondRan);
        Assert.Single(_log, line => line.StartsWith("error: OnTypesRegistered handler of S1API.Tests threw."));
    }

    [Fact]
    public void LateRegistrationInCoopWarnsAboutPrefabIds()
    {
        VehicleTypeRuntime.CoopAfterLoadForTesting = () => true;

        Assert.True(VehicleRegistry.RegisterType(new VehicleTypeDefinition { Code = "test_car", BaseCode = "hounddog" }));

        Assert.Single(_log, line => line.StartsWith("warn: Vehicle type 'test_car' was registered after the world loaded in a co-op session")
            && line.Contains("Register it in OnInitializeMelon."));
        Assert.Equal(new[] { "test_car" }, _applied);
    }

    [Fact]
    public void RegistrationOutsideCoopDoesNotWarn()
    {
        VehicleTypeRuntime.CoopAfterLoadForTesting = () => false;

        Assert.True(VehicleRegistry.RegisterType(new VehicleTypeDefinition { Code = "test_car", BaseCode = "hounddog" }));

        Assert.DoesNotContain(_log, line => line.StartsWith("warn: "));
    }

    [Fact]
    public void DefinitionDefaultsKeepTheBaseValues()
    {
        var definition = new VehicleTypeDefinition();

        Assert.Equal(string.Empty, definition.Name);
        Assert.True(definition.Price < 0f);
        Assert.True(definition.TopSpeed < 0f);
        Assert.Null(definition.OnBuild);
    }

    [Fact]
    public void PatchTargetExistsInTargetRuntime()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        Assert.NotNull(typeof(S1Vehicles.VehicleManager).GetMethod("Awake", flags, null, Type.EmptyTypes, null));
        Assert.NotNull(typeof(S1Vehicles.VehicleManager).GetMethod("GetVehiclePrefab", flags));
    }
}
