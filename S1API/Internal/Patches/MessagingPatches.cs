#if (IL2CPPMELON)
using S1Messaging = Il2CppScheduleOne.Messaging;
using S1NPCs = Il2CppScheduleOne.NPCs;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Messaging = ScheduleOne.Messaging;
using S1NPCs = ScheduleOne.NPCs;
#endif

using System;
using HarmonyLib;

namespace S1API.Internal.Patches
{
    /// <summary>
    /// INTERNAL: All patches related to messaging and conversations.
    /// </summary>
    [HarmonyPatch]
    internal class MessagingPatches
    {
        private static readonly Logging.Log Logger = new Logging.Log("MessagingPatches");

        /// <summary>
        /// Before MSGConversation UI is created, sync the Categories from the NPC's current ConversationCategories.
        /// This ensures dealers show the Dealer category instead of Customer, even if categories were updated after conversation creation.
        /// </summary>
        [HarmonyPatch(typeof(S1Messaging.MSGConversation), "CreateUI")]
        [HarmonyPrefix]
        private static void MSGConversation_CreateUI_Prefix(S1Messaging.MSGConversation __instance)
        {
            try
            {
                // Sync categories from sender NPC before UI creation
                if (__instance.sender != null)
                {
                    var npcCategories = __instance.sender.ConversationCategories;
                    if (npcCategories != null && npcCategories.Count > 0)
                    {
                        // Check if categories differ
                        bool needsUpdate = false;
                        
                        if (__instance.Categories == null || __instance.Categories.Count != npcCategories.Count)
                        {
                            needsUpdate = true;
                        }
                        else
                        {
                            // Check if any category differs
                            for (int i = 0; i < npcCategories.Count; i++)
                            {
                                if (i >= __instance.Categories.Count || __instance.Categories[i] != npcCategories[i])
                                {
                                    needsUpdate = true;
                                    break;
                                }
                            }
                        }
                        
                        if (needsUpdate)
                            __instance.SetCategories(npcCategories);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Exception in MSGConversation_CreateUI_Prefix: {ex.Message}");
            }
        }
    }
}

