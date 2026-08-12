using System.Reflection;
using S1API.Entities;
using S1API.Entities.Dealer;

namespace S1API.Tests.Entities;

public sealed class NPCDiagnosticCompatibilityTests
{
    private const string RegistrationObsoleteMessage =
        "S1API automatically pre-registers NPC prefabs. Remove this call.";

    [Theory]
    [InlineData("PreRegisterAllNpcPrefabs")]
    [InlineData("PreRegisterPrefabForType")]
    public void ManualPrefabRegistrationApisRetainTheirPublicShape(string methodName)
    {
        MethodInfo? method = typeof(NPC).GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull(method);
        Assert.True(method.IsStatic);
        Assert.Equal(typeof(void), method.ReturnType);
        ObsoleteAttribute obsolete = Assert.Single(method.GetCustomAttributes<ObsoleteAttribute>());
        Assert.Equal(RegistrationObsoleteMessage, obsolete.Message);
        Assert.False(obsolete.IsError);

        ParameterInfo[] parameters = method.GetParameters();
        if (methodName == "PreRegisterAllNpcPrefabs")
        {
            Assert.Empty(parameters);
        }
        else
        {
            ParameterInfo parameter = Assert.Single(parameters);
            Assert.Equal("npcType", parameter.Name);
            Assert.Equal(typeof(System.Type), parameter.ParameterType);
        }
    }

    [Fact]
    public void DealerDefaultsDoNotReportUnsupportedOptionsWhenTheyWereOmitted()
    {
        var data = new DealerDataBuilder().BuildInternal();

        Assert.False(data.InsufficientQualityConfigured);
        Assert.False(data.ExcessQualityConfigured);
        Assert.False(data.CompletedDealsVariableConfigured);
    }

    [Fact]
    public void ReviveRetainsItsPublicShape()
    {
        MethodInfo? method = typeof(NPC).GetMethod(
            nameof(NPC.Revive),
            BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(method);
        Assert.False(method!.IsStatic);
        Assert.Equal(typeof(void), method.ReturnType);
        Assert.Empty(method.GetParameters());
    }

    [Fact]
    public void DealerDefaultsRememberEveryExplicitUnsupportedOption()
    {
        var data = new DealerDataBuilder()
            .AllowInsufficientQuality(false)
            .AllowExcessQuality(true)
            .WithCompletedDealsVariable(null!)
            .BuildInternal();

        Assert.True(data.InsufficientQualityConfigured);
        Assert.True(data.ExcessQualityConfigured);
        Assert.True(data.CompletedDealsVariableConfigured);
        Assert.Equal(string.Empty, data.CompletedDealsVariable);
    }
}
