#if IL2CPPMELON
using S1VoiceOver = Il2CppScheduleOne.VoiceOver;
#elif MONOMELON
using S1VoiceOver = ScheduleOne.VoiceOver;
#endif

using System;
using S1API.Entities.Voices;
using S1API.Internal.Utils;
using UnityEngine;

namespace S1API.Internal.Entities
{
    internal static class NPCVoiceResolver
    {
        private static readonly SceneObjectIndex<S1VoiceOver.VODatabase> Databases =
            new SceneObjectIndex<S1VoiceOver.VODatabase>(
                () => Resources.FindObjectsOfTypeAll<S1VoiceOver.VODatabase>(),
                database => database.name,
                database => database != null,
                StringComparer.OrdinalIgnoreCase);

        internal static S1VoiceOver.VODatabase Resolve(NPCVoiceDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            S1VoiceOver.VODatabase? match = null;
            foreach (S1VoiceOver.VODatabase database in Databases.Get(definition.DatabaseName))
            {
                if (match != null && match.GetInstanceID() != database.GetInstanceID())
                {
                    throw new InvalidOperationException(
                        $"Multiple loaded voice databases match S1API voice '{definition.Id}' ({definition.DatabaseName}).");
                }

                match = database;
            }

            return match ?? throw new InvalidOperationException(
                $"The voice database for S1API voice '{definition.Id}' ({definition.DatabaseName}) is not loaded.");
        }
    }
}
