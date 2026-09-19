using System.Reflection;
using S1API.Internal.Temperature;
using S1API.Temperature;
using UnityEngine;

namespace S1API.Tests.Temperature;

public sealed class TemperatureContractTests
{
#if MONOMELON
    [Fact]
    public void SnapshotUsesLinearRangeAndClampsToNativeEmitterBounds()
    {
        var minimum = new TemperatureEmitterInfo(-10f, -5f, Vector3.zero);
        var maximum = new TemperatureEmitterInfo(50f, 200f, Vector3.one);

        Assert.Equal(TemperatureEmitter.MinTemperature, minimum.Temperature);
        Assert.Equal(TemperatureEmitter.MinRange, minimum.Range);
        Assert.Equal(TemperatureEmitter.MaxTemperature, maximum.Temperature);
        Assert.Equal(TemperatureEmitter.MaxRange, maximum.Range);
        Assert.Null(typeof(TemperatureEmitterInfo).GetProperty("SqrRange", BindingFlags.Instance | BindingFlags.Public));
    }

    [Fact]
    public void SnapshotRejectsNonFiniteValues()
    {
        foreach (float value in new[] { float.NaN, float.NegativeInfinity, float.PositiveInfinity })
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new TemperatureEmitterInfo(value, TemperatureEmitter.DefaultRange, Vector3.zero));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new TemperatureEmitterInfo(TemperatureEmitter.DefaultAmbientTemperature, value, Vector3.zero));
        }

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TemperatureEmitterInfo(
                TemperatureEmitter.DefaultAmbientTemperature,
                TemperatureEmitter.DefaultRange,
                new Vector3(float.NaN, 0f, 0f)));
    }

    [Fact]
    public void QueryRejectsNullSnapshotsBeforeCallingTheNativeRuntime()
    {
        Assert.Throws<ArgumentNullException>(() => TemperatureAlgorithm.GetTemperatureAtPoint(
            TemperatureEmitter.DefaultAmbientTemperature,
            Vector3.zero,
            Vector3.zero,
            null!));
    }
#endif

    [Fact]
    public void ManagedScalarValidationMatchesNativeEmitterBounds()
    {
        Assert.Equal(
            TemperatureValidation.MinTemperature,
            TemperatureValidation.ClampTemperature(-10f, "temperature"));
        Assert.Equal(
            TemperatureValidation.MaxTemperature,
            TemperatureValidation.ClampTemperature(50f, "temperature"));
        Assert.Equal(
            TemperatureValidation.MinRange,
            TemperatureValidation.ClampRange(-5f, "range"));
        Assert.Equal(
            TemperatureValidation.MaxRange,
            TemperatureValidation.ClampRange(200f, "range"));

        foreach (float value in new[] { float.NaN, float.NegativeInfinity, float.PositiveInfinity })
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                TemperatureValidation.ClampTemperature(value, "temperature"));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                TemperatureValidation.ClampRange(value, "range"));
        }
    }

    [Fact]
    public void PublicSurfaceExposesManagedTemperatureContractsOnly()
    {
        Assert.NotNull(typeof(TemperatureUtility).GetProperty(
            nameof(TemperatureUtility.TemperatureSystemEnabled),
            BindingFlags.Public | BindingFlags.Static));
        Assert.NotNull(typeof(TemperatureEmitter).GetMethod(
            nameof(TemperatureEmitter.ToInfo),
            BindingFlags.Public | BindingFlags.Instance));

        Type[] publicTypes =
        {
            typeof(TemperatureEmitter),
            typeof(TemperatureEmitterInfo),
            typeof(TemperatureAlgorithm),
            typeof(TemperatureUtility)
        };
        foreach (Type type in publicTypes)
        {
            foreach (MemberInfo member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                foreach (Type exposedType in GetExposedTypes(member))
                {
                    Assert.DoesNotContain("ScheduleOne", exposedType.Namespace ?? string.Empty, StringComparison.Ordinal);
                    Assert.DoesNotContain("Il2Cpp", exposedType.FullName ?? string.Empty, StringComparison.Ordinal);
                }
            }
        }
    }

    private static IEnumerable<Type> GetExposedTypes(MemberInfo member)
    {
        switch (member)
        {
            case MethodInfo method:
                yield return method.ReturnType;
                foreach (ParameterInfo parameter in method.GetParameters())
                    yield return parameter.ParameterType;
                break;
            case PropertyInfo property:
                yield return property.PropertyType;
                break;
            case FieldInfo field:
                yield return field.FieldType;
                break;
            case EventInfo eventInfo:
                yield return eventInfo.EventHandlerType!;
                break;
        }
    }
}
