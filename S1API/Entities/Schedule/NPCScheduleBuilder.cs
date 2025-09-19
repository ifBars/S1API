#if (IL2CPPMELON)
using S1NPCsSchedules = Il2CppScheduleOne.NPCs.Schedules;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1NPCsSchedules = ScheduleOne.NPCs.Schedules;
#endif

using System;
using UnityEngine;

namespace S1API.Entities.Schedule
{
    /// <summary>
    /// Fluent builder for composing an NPC's schedule programmatically.
    /// </summary>
    public sealed class NPCScheduleBuilder
    {
        private readonly NPCSchedule _schedule;

        internal NPCScheduleBuilder(NPCSchedule schedule)
        {
            _schedule = schedule;
        }

        /// <summary>
        /// Adds a walk-to action at the given start time.
        /// </summary>
        public NPCScheduleBuilder WalkTo(Vector3 destination, int startTime, bool faceDestinationDir = true, float within = 1f, bool warpIfSkipped = false, string name = null)
        {
            _schedule.AddWalkTo(destination, startTime, faceDestinationDir, within, warpIfSkipped, name);
            return this;
        }

        /// <summary>
        /// Ensures the customer deal signal exists under this schedule.
        /// </summary>
        public NPCScheduleBuilder EnsureDealSignal()
        {
            _schedule.EnsureDealSignal();
            return this;
        }

        /// <summary>
        /// Adds a custom schedule action using an S1API spec, without exposing game types.
        /// </summary>
        public NPCScheduleBuilder Add(IScheduleActionSpec spec)
        {
            if (spec == null)
                return this;
            spec.ApplyTo(_schedule);
            return this;
        }

        /// <summary>
        /// INTERNAL: Adds a custom action type with an optional configuration callback.
        /// </summary>
        internal NPCScheduleBuilder Add<T>(int startTime, Action<T> configure = null, string name = null) where T : S1NPCsSchedules.NPCAction
        {
            var action = _schedule.AddActionInternal<T>(startTime, name);
            configure?.Invoke(action);
            return this;
        }

        /// <summary>
        /// Clears all configured actions. Use carefully on NPCs with authored schedules.
        /// </summary>
        public NPCScheduleBuilder ClearAll()
        {
            _schedule.ClearActions();
            return this;
        }
    }
}


