#if IL2CPPMELON
using S1Jukebox = Il2CppScheduleOne.ObjectScripts.Jukebox;
#elif MONOMELON
using S1Jukebox = ScheduleOne.ObjectScripts.Jukebox;
#endif

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using S1API.Lifecycle;
using UnityEngine;

namespace S1API.Audio
{
    /// <summary>
    /// Discovers placed native jukeboxes in the current scene.
    /// </summary>
    public static class JukeboxManager
    {
        private static readonly Dictionary<int, Jukebox> Jukeboxes = new Dictionary<int, Jukebox>();
        private static bool _lifecycleHooked;

        /// <summary>
        /// Gets an immutable snapshot of all active jukeboxes in the current scene.
        /// </summary>
        public static IReadOnlyList<Jukebox> GetAll()
        {
            EnsureLifecycleHook();
            var nativeJukeboxes = UnityEngine.Object.FindObjectsOfType<S1Jukebox>();
            var jukeboxes = new List<Jukebox>(nativeJukeboxes.Length);
            for (int index = 0; index < nativeJukeboxes.Length; index++)
            {
                S1Jukebox? nativeJukebox = nativeJukeboxes[index];
                if (nativeJukebox != null)
                    jukeboxes.Add(Wrap(nativeJukebox));
            }

            return new ReadOnlyCollection<Jukebox>(jukeboxes);
        }

        /// <summary>
        /// Gets an active jukebox by its placed-object identifier.
        /// </summary>
        /// <param name="guid">The placed-object identifier assigned by the game.</param>
        /// <returns>The matching jukebox, or <see langword="null"/> when it is not active in the current scene.</returns>
        public static Jukebox? GetByGUID(string guid)
        {
            if (string.IsNullOrEmpty(guid))
                return null;

            foreach (Jukebox jukebox in GetAll())
            {
                if (string.Equals(jukebox.GUID, guid, StringComparison.OrdinalIgnoreCase))
                    return jukebox;
            }

            return null;
        }

        internal static Jukebox Wrap(S1Jukebox nativeJukebox)
        {
            EnsureLifecycleHook();
            int instanceId = nativeJukebox.GetInstanceID();
            if (Jukeboxes.TryGetValue(instanceId, out Jukebox? jukebox))
            {
                if (jukebox.S1Jukebox != null)
                    return jukebox;

                jukebox = new Jukebox(nativeJukebox);
                Jukeboxes[instanceId] = jukebox;
                return jukebox;
            }

            jukebox = new Jukebox(nativeJukebox);
            Jukeboxes.Add(instanceId, jukebox);
            return jukebox;
        }

        private static void EnsureLifecycleHook()
        {
            if (_lifecycleHooked)
                return;

            GameLifecycle.OnPreSceneChange += ClearSceneState;
            _lifecycleHooked = true;
        }

        private static void ClearSceneState()
        {
            foreach (Jukebox jukebox in Jukeboxes.Values)
                jukebox.Cleanup();

            Jukeboxes.Clear();
        }
    }
}
