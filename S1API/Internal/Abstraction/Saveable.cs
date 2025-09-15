#if (MONOMELON || MONOBEPINEX)
using System.Collections.Generic;
#elif (IL2CPPMELON || IL2CPPBEPINEX)
using Il2CppSystem.Collections.Generic;
#endif

using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using S1API.Internal.Utils;
using S1API.Saveables;
#if (IL2CPPMELON)
using S1Datas = Il2CppScheduleOne.Persistence.Datas;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Datas = ScheduleOne.Persistence.Datas;
#endif

#if (IL2CPPMELON)
using S1Persistence = Il2CppScheduleOne.Persistence;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Persistence = ScheduleOne.Persistence;
#endif

namespace S1API.Internal.Abstraction
{
    /// <summary>
    /// Generic wrapper for saveable classes.
    /// </summary>
    public abstract class Saveable : Registerable, ISaveable
    {
        /// <summary>
        /// Requests the game to perform a save operation. If a game is not currently loaded,
        /// the request is ignored and the method returns false.
        /// </summary>
        /// <param name="immediate">When true, saves immediately; otherwise schedules a short delayed save.</param>
        /// <returns>True if a save was requested; false if the game is not in a savable state.</returns>
        public static bool RequestGameSave(bool immediate = false)
        {
            try
            {
                var loadManager = S1Persistence.LoadManager.Instance;
                if (loadManager == null || !loadManager.IsGameLoaded)
                    return false;

                var saveManager = S1Persistence.SaveManager.Instance;
                if (saveManager == null)
                    return false;

                if (immediate)
                {
                    saveManager.Save();
                }
                else
                {
                    saveManager.DelayedSave();
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// INTERNAL: Explicit interface implementation that delegates to the internal LoadInternal method.
        /// Loads all fields marked with the <see cref="SaveableField"/> attribute from JSON files in the specified folder.
        /// </summary>
        /// <param name="folderPath">The folder path containing the save files to load.</param>
        void ISaveable.LoadInternal(string folderPath) =>
            LoadInternal(folderPath);

        /// <summary>
        /// INTERNAL: Loads all fields marked with the <see cref="SaveableField"/> attribute from JSON files in the specified folder.
        /// This method uses reflection to find fields with the SaveableField attribute and deserializes their values from JSON files.
        /// After loading all fields, it calls the <see cref="OnLoaded"/> method to allow derived classes to perform additional initialization.
        /// </summary>
        /// <param name="folderPath">The folder path containing the save files to load from.</param>
        internal virtual void LoadInternal(string folderPath)
        {
            FieldInfo[] saveableFields = GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (FieldInfo saveableField in saveableFields)
            {
                SaveableField? saveableFieldAttribute = saveableField.GetCustomAttribute<SaveableField>();
                if (saveableFieldAttribute == null)
                    continue;

                string filename = saveableFieldAttribute.SaveName.EndsWith(".json")
                    ? saveableFieldAttribute.SaveName
                    : $"{saveableFieldAttribute.SaveName}.json";

                string saveDataPath = Path.Combine(folderPath, filename);
                if (!File.Exists(saveDataPath))
                    continue;

                string json = File.ReadAllText(saveDataPath);
                Type type = saveableField.FieldType;
                object? value = JsonConvert.DeserializeObject(json, type, ISaveable.SerializerSettings);
                saveableField.SetValue(this, value);
            }

            OnLoaded();
        }

        /// <summary>
        /// INTERNAL: Explicit interface implementation that delegates to the internal SaveInternal method.
        /// Saves all fields marked with the <see cref="SaveableField"/> attribute to JSON files in the specified folder.
        /// </summary>
        /// <param name="folderPath">The folder path where save files should be written.</param>
        /// <param name="extraSaveables">Reference to a list of extra saveable files that should not be deleted during cleanup.</param>
        void ISaveable.SaveInternal(string folderPath, ref List<string> extraSaveables) =>
            SaveInternal(folderPath, ref extraSaveables);

        /// <summary>
        /// INTERNAL: Saves all fields marked with the <see cref="SaveableField"/> attribute to JSON files in the specified folder.
        /// This method uses reflection to find fields with the SaveableField attribute and serializes their values to JSON files.
        /// Null fields result in their corresponding save files being deleted. Non-null fields are added to the extraSaveables list
        /// to prevent the base game from deleting them during cleanup. After saving all fields, it calls the <see cref="OnSaved"/> method
        /// to allow derived classes to perform additional finalization.
        /// </summary>
        /// <param name="folderPath">The folder path where save files should be written.</param>
        /// <param name="extraSaveables">Reference to a list of extra saveable files that should not be deleted during cleanup.</param>
        internal virtual void SaveInternal(string folderPath, ref List<string> extraSaveables)
        {
            FieldInfo[] saveableFields = ReflectionUtils.GetAllFields(GetType(), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (FieldInfo saveableField in saveableFields)
            {
                SaveableField? saveableFieldAttribute = saveableField.GetCustomAttribute<SaveableField>();
                if (saveableFieldAttribute == null)
                    continue;

                string saveFileName = saveableFieldAttribute.SaveName.EndsWith(".json")
                    ? saveableFieldAttribute.SaveName
                    : $"{saveableFieldAttribute.SaveName}.json";

                string saveDataPath = Path.Combine(folderPath, saveFileName);

                object? value = saveableField.GetValue(this);
                if (value == null)
                    // Remove the save if the field is null
                    File.Delete(saveDataPath);
                else
                {
                    // We add this to the extra saveables to prevent the game from deleting it
                    // Otherwise, it'll delete it after it finishes saving and does clean up
                    extraSaveables.Add(saveFileName);

                    // Write our data
                    string data = JsonConvert.SerializeObject(value, Formatting.Indented, ISaveable.SerializerSettings);
                    File.WriteAllText(saveDataPath, data);
                }
            }

            OnSaved();
        }

        /// <summary>
        /// INTERNAL: Writes fields marked with <see cref="SaveableField"/> into a DynamicSaveData blob
        /// to support the base game's consolidated JSON save format.
        /// </summary>
        /// <param name="dynamicSaveData">The dynamic save data record to write into.</param>
        internal void SaveToDynamic(S1Datas.DynamicSaveData dynamicSaveData)
        {
            if (dynamicSaveData == null)
                return;

            FieldInfo[] saveableFields = ReflectionUtils.GetAllFields(GetType(), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (FieldInfo saveableField in saveableFields)
            {
                SaveableField? saveableFieldAttribute = saveableField.GetCustomAttribute<SaveableField>();
                if (saveableFieldAttribute == null)
                    continue;

                object? value = saveableField.GetValue(this);
                if (value == null)
                    continue; // Do not write nulls

                string data = JsonConvert.SerializeObject(value, Formatting.None, ISaveable.SerializerSettings);
                // Use the declared save name as the dynamic key
                dynamicSaveData.AddData(saveableFieldAttribute.SaveName, data);
            }

            OnSaved();
        }

        /// <summary>
        /// INTERNAL: Reads fields marked with <see cref="SaveableField"/> from a DynamicSaveData blob
        /// to support the base game's consolidated JSON save format.
        /// </summary>
        /// <param name="dynamicSaveData">The dynamic save data record to read from.</param>
        internal void LoadFromDynamic(S1Datas.DynamicSaveData dynamicSaveData)
        {
            if (dynamicSaveData == null)
                return;

            FieldInfo[] saveableFields = ReflectionUtils.GetAllFields(GetType(), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (FieldInfo saveableField in saveableFields)
            {
                SaveableField? saveableFieldAttribute = saveableField.GetCustomAttribute<SaveableField>();
                if (saveableFieldAttribute == null)
                    continue;

                // Read the raw json for this save name and deserialize to the field type
                if (!dynamicSaveData.TryGetData(saveableFieldAttribute.SaveName, out string json) || string.IsNullOrEmpty(json))
                    continue;

                Type type = saveableField.FieldType;
                object? value = JsonConvert.DeserializeObject(json, type, ISaveable.SerializerSettings);
                saveableField.SetValue(this, value);
            }

            OnLoaded();
        }

        /// <summary>
        /// INTERNAL: Explicit interface implementation that delegates to the virtual OnLoaded method.
        /// Called after all saveable fields have been loaded from their respective JSON files.
        /// </summary>
        void ISaveable.OnLoaded() => OnLoaded();

        /// <summary>
        /// Called after all saveable fields have been loaded from their respective JSON files.
        /// This method can be overridden in derived classes to perform additional initialization
        /// or processing after the save data has been restored.
        /// </summary>
        protected virtual void OnLoaded() { }

        /// <summary>
        /// INTERNAL: Explicit interface implementation that delegates to the virtual OnSaved method.
        /// Called after all saveable fields have been saved to their respective JSON files.
        /// </summary>
        void ISaveable.OnSaved() => OnSaved();

        /// <summary>
        /// Called after all saveable fields have been saved to their respective JSON files.
        /// This method can be overridden in derived classes to perform additional finalization
        /// or processing after the save data has been written to disk.
        /// </summary>
        protected virtual void OnSaved() { }
    }
}
