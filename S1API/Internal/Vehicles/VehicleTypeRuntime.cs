#if (IL2CPPMELON)
using FishNetInstanceFinder = Il2CppFishNet.InstanceFinder;
using FishNetNetworkObject = Il2CppFishNet.Object.NetworkObject;
using S1DevUtilities = Il2CppScheduleOne.DevUtilities;
using S1Vehicles = Il2CppScheduleOne.Vehicles;
#elif MONOMELON
using FishNetInstanceFinder = FishNet.InstanceFinder;
using FishNetNetworkObject = FishNet.Object.NetworkObject;
using S1DevUtilities = ScheduleOne.DevUtilities;
using S1Vehicles = ScheduleOne.Vehicles;
#endif
using System;
using System.Collections.Generic;
using System.Reflection;
using S1API.Internal.Utils;
using S1API.Logging;
using S1API.Vehicles;
using UnityEngine;
using Object = UnityEngine.Object;

namespace S1API.Internal.Vehicles
{
    /// <summary>
    /// INTERNAL: Builds the vehicle types mods register and hands them to the game's vehicle manager.
    /// </summary>
    /// <remarks>
    /// Each type is cloned once per game process from its base vehicle, while the clone is inactive, so no Awake runs
    /// on the template. On every <c>VehicleManager.Awake</c> the template is added to FishNet's spawnable prefabs and
    /// to <c>VehicleManager.VehiclePrefabs</c>, in registration order. The save stores only the vehicle code and
    /// <c>GetVehiclePrefab</c> looks codes up in that list, so spawning and loading by code need nothing else.
    /// </remarks>
    internal static class VehicleTypeRuntime
    {
        private sealed class Entry
        {
            internal string Code = string.Empty;
            internal string BaseCode = string.Empty;
            internal string Name = string.Empty;
            internal float Price;
            internal float TopSpeed;
            internal Action<GameObject>? OnBuild;
            internal GameObject? Template;
            internal bool Failed;
        }

        private static readonly Log Logger = new Log("VehicleTypes");
        private static readonly List<Entry> Entries = new List<Entry>();
        private static readonly object Gate = new object();

        internal static bool Register(VehicleTypeDefinition? definition)
        {
            if (definition == null)
            {
                Fail("RegisterType got no definition.");
                return false;
            }

            string code = definition.Code?.Trim() ?? string.Empty;
            string baseCode = definition.BaseCode?.Trim() ?? string.Empty;
            if (code.Length == 0)
            {
                Fail($"RegisterType from base '{baseCode}': the code is empty; the type is not added.");
                return false;
            }
            if (baseCode.Length == 0)
            {
                Fail($"Vehicle type '{code}': the base code is empty; the type is not added.");
                return false;
            }
            if (string.Equals(code, baseCode, StringComparison.OrdinalIgnoreCase))
            {
                Fail($"Vehicle type '{code}': the code equals its base code; the type is not added.");
                return false;
            }

            var entry = new Entry
            {
                Code = code,
                BaseCode = baseCode,
                Name = definition.Name ?? string.Empty,
                Price = definition.Price,
                TopSpeed = definition.TopSpeed,
                OnBuild = definition.OnBuild,
            };

            lock (Gate)
            {
                if (Find(code) != null)
                {
                    Fail($"Vehicle type '{code}' is already registered; the second registration is ignored.");
                    return false;
                }
                Entries.Add(entry);
            }

            Info($"Vehicle type '{code}' registered (base '{baseCode}').");

            if (ApplyNowForTesting != null)
            {
                ApplyNowForTesting(code);
                return true;
            }

            // A mod that registers after the world loaded still gets its type, without waiting for the next load.
            S1Vehicles.VehicleManager? manager = CurrentManager();
            if (manager != null)
                Apply(manager);
            return true;
        }

        internal static bool IsRegistered(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;
            lock (Gate)
                return Find(code!.Trim()) != null;
        }

        /// <summary>
        /// Adds every registered type to this vehicle manager, then raises <see cref="VehicleRegistry.OnTypesRegistered"/>.
        /// </summary>
        internal static void Apply(S1Vehicles.VehicleManager manager)
        {
            if (manager == null)
            {
                Warn("Vehicle types not applied: there is no VehicleManager.");
                return;
            }

            Entry[] entries;
            lock (Gate)
                entries = Entries.ToArray();
            if (entries.Length == 0)
                return;

            foreach (Entry entry in entries)
            {
                try
                {
                    ApplyOne(manager, entry);
                }
                catch (Exception ex)
                {
                    Fail($"Vehicle type '{entry.Code}' could not be added. {ex}");
                }
            }

            VehicleRegistry.RaiseTypesRegistered();
        }

        private static void ApplyOne(S1Vehicles.VehicleManager manager, Entry entry)
        {
            if (entry.Failed)
                return;

            if (entry.Template == null && !Build(manager, entry))
                return;

            S1Vehicles.LandVehicle vehicle = entry.Template!.GetComponent<S1Vehicles.LandVehicle>();
            FishNetNetworkObject networkObject = entry.Template.GetComponent<FishNetNetworkObject>();

            var spawnables = FishNetInstanceFinder.NetworkManager?.SpawnablePrefabs;
            if (spawnables == null)
            {
                Warn($"Vehicle type '{entry.Code}': FishNet's spawnable prefabs are not available yet; it is added at the next VehicleManager.Awake.");
                return;
            }
            if (!IsSpawnable(networkObject))
                global::S1API.Entities.NPC.RegisterSpawnablePrefabInOrder(spawnables, networkObject);

            var prefabs = manager.VehiclePrefabs;
            for (int i = 0; i < prefabs.Count; i++)
            {
                if (prefabs[i] == vehicle)
                {
                    Debug($"Vehicle type '{entry.Code}' is already in this VehicleManager.");
                    return;
                }
            }

            S1Vehicles.LandVehicle? taken = manager.GetVehiclePrefab(entry.Code);
            if (taken != null)
            {
                Fail($"Vehicle type '{entry.Code}': the code is already used by '{taken.VehicleName}' ({taken.gameObject.name}); the type is not added.");
                return;
            }

            prefabs.Add(vehicle);
            Info($"Vehicle type '{entry.Code}' added as vehicle prefab #{prefabs.Count - 1}, FishNet prefab id {networkObject.PrefabId}.");
        }

        private static bool Build(S1Vehicles.VehicleManager manager, Entry entry)
        {
            S1Vehicles.LandVehicle? taken = manager.GetVehiclePrefab(entry.Code);
            if (taken != null)
            {
                entry.Failed = true;
                Fail($"Vehicle type '{entry.Code}': the code is already used by '{taken.VehicleName}' ({taken.gameObject.name}); the type is not added.");
                return false;
            }

            S1Vehicles.LandVehicle? donor = manager.GetVehiclePrefab(entry.BaseCode);
            if (donor == null)
            {
                entry.Failed = true;
                Fail($"Vehicle type '{entry.Code}': base vehicle '{entry.BaseCode}' does not exist; the type is not added.");
                return false;
            }

            GameObject clone = InactiveObjectCloner.CloneGameObject(donor.gameObject);
            try
            {
                clone.name = "S1API_Vehicle_" + entry.Code;
                clone.transform.position = new Vector3(0f, -100f, 0f);
                clone.transform.rotation = Quaternion.identity;
                Object.DontDestroyOnLoad(clone);

                S1Vehicles.LandVehicle vehicle = clone.GetComponent<S1Vehicles.LandVehicle>();
                if (clone.GetComponent<FishNetNetworkObject>() == null)
                    throw new InvalidOperationException($"base vehicle '{entry.BaseCode}' has no FishNet NetworkObject");

                SetMember(vehicle, "vehicleCode", entry.Code, entry);
                if (entry.Name.Length > 0)
                    SetMember(vehicle, "vehicleName", entry.Name, entry);
                if (entry.Price >= 0f)
                    SetMember(vehicle, "vehiclePrice", entry.Price, entry);
                if (entry.TopSpeed >= 0f)
                    vehicle.TopSpeed = entry.TopSpeed;
                if (!string.Equals(vehicle.VehicleCode, entry.Code, StringComparison.Ordinal))
                    throw new InvalidOperationException($"the clone kept the code '{vehicle.VehicleCode}'");
            }
            catch (Exception ex)
            {
                entry.Failed = true;
                Object.DestroyImmediate(clone);
                Fail($"Vehicle type '{entry.Code}' could not be built from '{entry.BaseCode}'; the type is not added. {ex.Message}");
                return false;
            }

            if (entry.OnBuild != null)
            {
                try
                {
                    entry.OnBuild(clone);
                }
                catch (Exception ex)
                {
                    Fail($"Vehicle type '{entry.Code}': OnBuild of {OwnerOf(entry.OnBuild)} threw; the type keeps the look of '{entry.BaseCode}'. {ex}");
                }
            }

            entry.Template = clone;
            Info($"Vehicle type '{entry.Code}' built from '{entry.BaseCode}'.");
            return true;
        }

        private static void SetMember(S1Vehicles.LandVehicle vehicle, string member, object value, Entry entry)
        {
            if (!ReflectionUtils.TrySetFieldOrProperty(vehicle, member, value))
                throw new InvalidOperationException($"LandVehicle.{member} could not be set for '{entry.Code}'");
        }

        private static bool IsSpawnable(FishNetNetworkObject networkObject)
        {
            var spawnables = FishNetInstanceFinder.NetworkManager?.SpawnablePrefabs;
            if (spawnables == null)
                return false;

            int count = spawnables.GetObjectCount();
            for (int i = 0; i < count; i++)
            {
                if (spawnables.GetObject(true, i) == networkObject)
                    return true;
            }
            return false;
        }

        private static S1Vehicles.VehicleManager? CurrentManager()
        {
            try
            {
                return S1DevUtilities.NetworkSingleton<S1Vehicles.VehicleManager>.InstanceExists
                    ? S1DevUtilities.NetworkSingleton<S1Vehicles.VehicleManager>.Instance
                    : null;
            }
            catch (Exception ex)
            {
                Warn($"VehicleManager could not be read; types are added at the next VehicleManager.Awake. {ex.Message}");
                return null;
            }
        }

        private static Entry? Find(string code)
        {
            foreach (Entry entry in Entries)
            {
                if (string.Equals(entry.Code, code, StringComparison.OrdinalIgnoreCase))
                    return entry;
            }
            return null;
        }

        internal static string OwnerOf(Delegate handler)
        {
            Assembly? assembly = handler.Method?.DeclaringType?.Assembly;
            return assembly?.GetName().Name ?? "unknown mod";
        }

        /// <summary>
        /// INTERNAL: Removes every registered type. Only for tests.
        /// </summary>
        internal static void ResetForTesting()
        {
            lock (Gate)
                Entries.Clear();
        }

        /// <summary>
        /// INTERNAL: Called with the code instead of touching the game after a registration. Only for tests.
        /// </summary>
        internal static Action<string>? ApplyNowForTesting { get; set; }

        /// <summary>
        /// INTERNAL: Receives log lines instead of MelonLoader while set. Only for tests, which run without MelonLoader.
        /// </summary>
        internal static Action<string>? LogSinkForTesting { get; set; }

        internal static void Debug(string message)
        {
            if (LogSinkForTesting != null)
                LogSinkForTesting("debug: " + message);
            else
                Logger.Debug(message);
        }

        internal static void Info(string message)
        {
            if (LogSinkForTesting != null)
                LogSinkForTesting("info: " + message);
            else
                Logger.Msg(message);
        }

        internal static void Warn(string message)
        {
            if (LogSinkForTesting != null)
                LogSinkForTesting("warn: " + message);
            else
                Logger.Warning(message);
        }

        internal static void Fail(string message)
        {
            if (LogSinkForTesting != null)
                LogSinkForTesting("error: " + message);
            else
                Logger.Error(message);
        }
    }
}
