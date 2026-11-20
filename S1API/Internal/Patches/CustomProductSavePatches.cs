#if IL2CPPMELON
using S1Persistence = Il2CppScheduleOne.Persistence;
#elif MONOMELON
using S1Persistence = ScheduleOne.Persistence;
#endif

using System;
using HarmonyLib;
using S1API.Internal.Products;

namespace S1API.Internal.Patches
{
    /// <summary>INTERNAL: Persists custom-product descriptors outside vanilla product data.</summary>
    [HarmonyPatch]
    internal static class CustomProductSavePatches
    {
        [HarmonyPatch(typeof(S1Persistence.SaveManager), "Save", new Type[] { typeof(string) })]
        [HarmonyPostfix]
        private static void SavePostfix(string saveFolderPath, bool __runOriginal)
        {
            if (!__runOriginal) { MelonLoader.MelonLogger.Msg("[CustomProductSave] Save was blocked; custom products are not written."); return; }
            try { CustomProductSavePersistence.Save(saveFolderPath); }
            catch (Exception exception) { MelonLoader.MelonLogger.Warning("[CustomProductSave] save failed: " + exception.Message); }
        }

        [HarmonyPatch(typeof(S1Persistence.LoadManager), nameof(S1Persistence.LoadManager.QueueLoadRequest))]
        [HarmonyPrefix]
        private static void RestorePrefix()
        {
            CustomProductSavePersistence.RestoreBeforeBaseLoaders(S1Persistence.LoadManager.Instance);
            CustomProductManifestRuntime.FinalizeHostManifestAfterDescriptorRestore();
        }
    }
}
