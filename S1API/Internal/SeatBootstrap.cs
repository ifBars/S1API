using System.Collections;
using MelonLoader;
#if (IL2CPPMELON)
using S1AvatarAnimation = Il2CppScheduleOne.AvatarFramework.Animation;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1AvatarAnimation = ScheduleOne.AvatarFramework.Animation;
#endif
using S1API.Avatar;
using UnityEngine;

namespace S1API.Internal
{
    /// <summary>
    /// INTERNAL: Performs delayed scanning/registration of AvatarSeat components after the Main scene initializes.
    /// </summary>
    internal static class SeatBootstrap
    {
        public static void OnMainSceneInitialized()
        {
            // Run a short delayed scan to allow scene objects to finish awakening/initializing
            MelonCoroutines.Start(ScanAndRegisterSeats());
        }

        private static IEnumerator ScanAndRegisterSeats()
        {
            yield return new WaitForSeconds(0.25f);

            RegisterAllCurrentSeats();

            // Run a follow-up scan shortly after to catch any late-instantiated seats
            yield return new WaitForSeconds(1.0f);

            RegisterAllCurrentSeats();
        }

        private static void RegisterAllCurrentSeats()
        {
            var seats = Object.FindObjectsOfType<S1AvatarAnimation.AvatarSeat>(includeInactive: true);
            if (seats == null)
                return;

            for (int i = 0; i < seats.Length; i++)
            {
                var s = seats[i];
                if (s != null)
                    Seat.Register(s);
            }
        }
    }
}


