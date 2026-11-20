#if (IL2CPPMELON)
using S1Property = Il2CppScheduleOne.Property;
#elif MONOMELON
using S1Property = ScheduleOne.Property;
#endif
using System;
using S1API.Logging;
using UnityEngine;

namespace S1API.Internal.Properties
{
    /// <summary>
    /// INTERNAL: Keeps <c>Property.EmployeeIdlePoints</c> at least as long as the employees a property can hold.
    /// </summary>
    /// <remarks>
    /// <c>Employee.AssignProperty</c> reads <c>EmployeeIdlePoints[EmployeeIndex]</c> without a bounds check. A property
    /// whose capacity was raised above its point count throws on the next hire, and the employee keeps no idle point.
    /// Points S1API adds live under one child of the property, <see cref="HolderName"/>, so a later call extends the
    /// same set instead of starting a second one.
    /// </remarks>
    internal static class EmployeeIdlePointsRuntime
    {
        internal const string HolderName = "S1API_EmployeeIdlePoints";

        private static readonly Log Logger = new Log("EmployeeIdlePoints");

        /// <summary>
        /// Adds idle points until the property has <paramref name="count"/>. Each new point copies the position and
        /// rotation of the last existing one; with no point at all it starts at the NPC spawn point.
        /// </summary>
        /// <returns>The number of points added.</returns>
        internal static int Ensure(S1Property.Property? property, int count, string reason)
        {
            if (property == null)
            {
                Logger.Warning($"Idle points not checked ({reason}): there is no property.");
                return 0;
            }

            int have = Count(property);
            int missing = Missing(have, count);
            if (missing == 0)
                return 0;

            Transform? last = LastPoint(property);
            Vector3 position;
            Quaternion rotation;
            if (last != null)
            {
                position = last.position;
                rotation = last.rotation;
            }
            else if (property.NPCSpawnPoint != null)
            {
                position = property.NPCSpawnPoint.position;
                rotation = property.NPCSpawnPoint.rotation;
                Logger.Warning($"{Describe(property)} has no idle point to copy; the new ones start at its NPC spawn point.");
            }
            else
            {
                position = property.transform.position;
                rotation = property.transform.rotation;
                Logger.Warning($"{Describe(property)} has neither an idle point nor an NPC spawn point; the new idle points start at the property's origin.");
            }

            var points = new Transform[have + missing];
            CopyExisting(property, points, have);
            Transform holder = Holder(property);
            for (int i = have; i < points.Length; i++)
                points[i] = NewPoint(holder, i, position, rotation);
            property.EmployeeIdlePoints = points;

            Logger.Msg($"{Describe(property)}: added {missing} employee idle point(s) for {reason}, {points.Length} in total.");
            return missing;
        }

        /// <summary>
        /// Appends one idle point at a position and returns its transform.
        /// </summary>
        internal static Transform? Add(S1Property.Property? property, Vector3 position, Quaternion rotation)
        {
            if (property == null)
            {
                Logger.Error("AddEmployeeIdlePoint: there is no property; no point added.");
                return null;
            }

            int have = Count(property);
            var points = new Transform[have + 1];
            CopyExisting(property, points, have);
            Transform point = NewPoint(Holder(property), have, position, rotation);
            points[have] = point;
            property.EmployeeIdlePoints = points;

            Logger.Msg($"{Describe(property)}: idle point #{have} added at {position}, {points.Length} in total.");
            return point;
        }

        /// <summary>
        /// Runs before <c>Employee.AssignProperty</c>: <c>RegisterEmployee</c> appends the employee, so the call reads
        /// the point at <c>Employees.Count</c>.
        /// </summary>
        internal static void EnsureForAssign(S1Property.Property? property)
        {
            if (property == null)
            {
                Logger.Warning("Employee.AssignProperty ran without a property; idle points not checked.");
                return;
            }
            if (property.Employees == null)
            {
                Logger.Warning($"{Describe(property)} has no employee list; idle points not checked.");
                return;
            }

            Ensure(property, NeededForAssign(property.Employees.Count), "a new employee");
        }

        /// <summary>
        /// How many points are missing to reach <paramref name="wanted"/>. Never negative.
        /// </summary>
        internal static int Missing(int have, int wanted) => wanted > have ? wanted - have : 0;

        /// <summary>
        /// The point count <c>AssignProperty</c> needs when the property has <paramref name="employees"/> employees before
        /// the call.
        /// </summary>
        internal static int NeededForAssign(int employees) => Math.Max(0, employees) + 1;

        private static int Count(S1Property.Property property) => property.EmployeeIdlePoints?.Length ?? 0;

        private static void CopyExisting(S1Property.Property property, Transform[] target, int have)
        {
            var known = property.EmployeeIdlePoints;
            for (int i = 0; i < have; i++)
                target[i] = known![i];
        }

        private static Transform? LastPoint(S1Property.Property property)
        {
            var known = property.EmployeeIdlePoints;
            if (known == null)
                return null;
            for (int i = known.Length - 1; i >= 0; i--)
            {
                if (known[i] != null)
                    return known[i];
            }
            return null;
        }

        private static Transform Holder(S1Property.Property property)
        {
            Transform? holder = property.transform.Find(HolderName);
            if (holder != null)
                return holder;
            holder = new GameObject(HolderName).transform;
            holder.SetParent(property.transform, false);
            return holder;
        }

        private static Transform NewPoint(Transform holder, int index, Vector3 position, Quaternion rotation)
        {
            Transform point = new GameObject("IdlePoint " + index).transform;
            point.SetParent(holder, false);
            point.SetPositionAndRotation(position, rotation);
            return point;
        }

        private static string Describe(S1Property.Property property)
        {
            string code = property.PropertyCode;
            return string.IsNullOrEmpty(code) ? property.name : $"Property '{code}'";
        }
    }
}
