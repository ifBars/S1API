#if (IL2CPPMELON)
using S1Economy = Il2CppScheduleOne.Economy;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Economy = ScheduleOne.Economy;
#endif

using System;
using UnityEngine;

namespace S1API.Map
{
    /// <summary>
    /// Wrapper for a base-game delivery location.
    /// </summary>
    public sealed class DeliveryLocation
    {
        internal readonly S1Economy.DeliveryLocation S1Location;

        internal DeliveryLocation(S1Economy.DeliveryLocation s1)
        {
            S1Location = s1;
        }

        /// <summary>
        /// Location display name.
        /// </summary>
        public string Name =>
            S1Location != null ? S1Location.LocationName : string.Empty;

        /// <summary>
        /// Human-readable description for UI.
        /// </summary>
        public string Description =>
            S1Location != null ? S1Location.LocationDescription : string.Empty;

        /// <summary>
        /// GUID string identifier used by the base game.
        /// </summary>
        public string GUID
        {
            get
            {
                try
                {
                    return S1Location != null ? S1Location.GUID.ToString() : string.Empty;
                }
                catch
                {
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// Customer standing position.
        /// </summary>
        public Transform CustomerStandPoint =>
            S1Location != null ? S1Location.CustomerStandPoint : null;

        /// <summary>
        /// Teleport target point near the location.
        /// </summary>
        public Transform TeleportPoint =>
            S1Location != null ? S1Location.TeleportPoint : null;
    }
}


