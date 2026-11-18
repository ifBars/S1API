#if (IL2CPPMELON)
using S1Employees = Il2CppScheduleOne.Employees;
using S1Property = Il2CppScheduleOne.Property;
#elif MONOMELON
using S1Employees = ScheduleOne.Employees;
using S1Property = ScheduleOne.Property;
#endif
using System;
using HarmonyLib;
using S1API.Internal.Properties;
using S1API.Logging;

namespace S1API.Internal.Patches
{
    /// <summary>
    /// INTERNAL: Gives every new employee an idle point. <c>Employee.AssignProperty</c> reads
    /// <c>EmployeeIdlePoints[EmployeeIndex]</c> without a bounds check; when a mod raised the capacity above the point
    /// count, the call throws and the employee keeps no idle point. Runs on the host and on every client, because
    /// <c>Employee.Initialize</c> assigns the property on each peer. The subclass overrides call this base method.
    /// </summary>
    [HarmonyPatch(typeof(S1Employees.Employee), "AssignProperty")]
    internal static class EmployeeIdlePointPatches
    {
        private static readonly Log Logger = new Log("EmployeeIdlePoints");

        [HarmonyPrefix]
        private static void Prefix(S1Property.Property prop)
        {
            try
            {
                EmployeeIdlePointsRuntime.EnsureForAssign(prop);
            }
            catch (Exception ex)
            {
                Logger.Error($"Idle points for a new employee could not be added; the game's assignment runs unchanged. {ex}");
            }
        }
    }
}
