using System.Reflection;
using S1API.Internal.Properties;
using S1API.Property;
using UnityEngine;

#if IL2CPPMELON
using S1Employees = Il2CppScheduleOne.Employees;
#elif MONOMELON
using S1Employees = ScheduleOne.Employees;
#endif

namespace S1API.Tests.Property;

public sealed class EmployeeIdlePointTests
{
    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(3, 2, 0)]
    [InlineData(3, 3, 0)]
    [InlineData(3, 5, 2)]
    [InlineData(0, 4, 4)]
    [InlineData(2, -1, 0)]
    public void MissingCountsOnlyThePointsBelowTheWantedCount(int have, int wanted, int missing)
    {
        Assert.Equal(missing, EmployeeIdlePointsRuntime.Missing(have, wanted));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(4, 5)]
    [InlineData(-2, 1)]
    public void AssignNeedsOnePointMoreThanTheEmployeesBeforeTheCall(int employees, int needed)
    {
        Assert.Equal(needed, EmployeeIdlePointsRuntime.NeededForAssign(employees));
    }

    [Fact]
    public void PatchTargetExistsInTargetRuntime()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        Assert.NotNull(typeof(S1Employees.Employee).GetMethod("AssignProperty", flags));
    }

    [Fact]
    public void WrapperExposesTheIdlePointMethods()
    {
        MethodInfo? add = typeof(PropertyWrapper).GetMethod(nameof(PropertyWrapper.AddEmployeeIdlePoint));
        MethodInfo? ensure = typeof(PropertyWrapper).GetMethod(nameof(PropertyWrapper.EnsureEmployeeIdlePoints));

        Assert.NotNull(add);
        Assert.Equal(typeof(Transform), add!.ReturnType);
        Assert.Equal(new[] { typeof(Vector3), typeof(Quaternion) }, Array.ConvertAll(add.GetParameters(), p => p.ParameterType));
        Assert.NotNull(ensure);
        Assert.Equal(new[] { typeof(int) }, Array.ConvertAll(ensure!.GetParameters(), p => p.ParameterType));
    }
}
