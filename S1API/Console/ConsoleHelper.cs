#if (IL2CPPMELON)
using Il2CppSystem.Collections.Generic;
using static Il2CppScheduleOne.Console;
#else
using static ScheduleOne.Console;
using System.Collections.Generic;
#endif
using System.Linq;
using S1API.Entities;
using S1API.Products;
using S1API.Quests.Constants;

namespace S1API.Console
{
    /// <summary>
    /// Provides a stable, modder-friendly abstraction over the in-game console system.
    /// Use these helpers from mods instead of referencing the game's console types directly.
    /// </summary>
    public static class ConsoleHelper
    {
        /// <summary>
        /// Submits a raw console command string (e.g. "settime 1530").
        /// Works across both IL2CPP and Mono builds.
        /// </summary>
        /// <param name="command">The full command line to execute.</param>
        public static void Submit(string command)
        {
#if (IL2CPPMELON || IL2CPPBEPINEX)
            Il2CppScheduleOne.Console.SubmitCommand(command);
#else
            ScheduleOne.Console.SubmitCommand(command);
#endif
        }

        /// <summary>
        /// Submits a console command and arguments (e.g. Submit(["settime","1530"]).
        /// Works across both IL2CPP and Mono builds.
        /// </summary>
        /// <param name="arguments">Command word followed by its arguments.</param>
        public static void Submit(IEnumerable<string> arguments)
        {
#if (IL2CPPMELON || IL2CPPBEPINEX)
            var args = new Il2CppSystem.Collections.Generic.List<string>();
            var enumerable = arguments as System.Collections.IEnumerable;
            if (enumerable != null)
            {
                var enumerator = enumerable.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var current = enumerator.Current as string;
                    if (current != null)
                    {
                        args.Add(current);
                    }
                }
            }
            Il2CppScheduleOne.Console.SubmitCommand(args);
#else
            var args = new List<string>(arguments);
            ScheduleOne.Console.SubmitCommand(args);
#endif
        }

        /// <summary>
        /// Executes the ChangeCash command with the given amount.
        /// Positive values add cash; negative values remove cash.
        /// </summary>
        public static void RunCashCommand(int amount)
        {
#if (IL2CPPMELON || IL2CPPBEPINEX)
            var command = new ChangeCashCommand();
            var args = new Il2CppSystem.Collections.Generic.List<string>();
#else
            var command = new ChangeCashCommand();
            var args = new List<string>();
#endif
            args.Add(amount.ToString());
            command.Execute(args);
        }

        /// <summary>
        /// Changes the player's online bank balance by the specified amount.
        /// </summary>
        public static void RunOnlineBalanceCommand(int amount)
        {
#if (IL2CPPMELON || IL2CPPBEPINEX)
            var command = new ChangeOnlineBalanceCommand();
            var args = new Il2CppSystem.Collections.Generic.List<string>();
#else
            var command = new ChangeOnlineBalanceCommand();
            var args = new List<string>();
#endif
            args.Add(amount.ToString());
            command.Execute(args);
        }

        /// <summary>
        /// Gives the player an item by code. Optionally specify a quantity.
        /// </summary>
        public static void AddItemToInventory(string itemCode, int? quantity = null)
        {
#if (IL2CPPMELON || IL2CPPBEPINEX)
            var command = new AddItemToInventoryCommand();
            var args = new Il2CppSystem.Collections.Generic.List<string>();
#else
            var command = new AddItemToInventoryCommand();
            var args = new List<string>();
#endif
            args.Add(itemCode);
            if (quantity.HasValue)
            {
                args.Add(quantity.Value.ToString());
            }
            command.Execute(args);
        }

        /// <summary>
        /// Clears the player's inventory.
        /// </summary>
        public static void ClearInventory()
        {
#if (IL2CPPMELON || IL2CPPBEPINEX)
            var command = new ClearInventoryCommand();
            var args = new Il2CppSystem.Collections.Generic.List<string>();
#else
            var command = new ClearInventoryCommand();
            var args = new List<string>();
#endif
            command.Execute(args);
        }

        /// <summary>
        /// Instantly removes all trash from the world.
        /// </summary>
        public static void ClearTrash()
        {
#if (IL2CPPMELON || IL2CPPBEPINEX)
            var command = new ClearTrash();
            var args = new Il2CppSystem.Collections.Generic.List<string>();
#else
            var command = new ClearTrash();
            var args = new List<string>();
#endif
            command.Execute(args);
        }

        /// <summary>
        /// Clears the player's wanted level and crimes.
        /// </summary>
        public static void ClearWanted()
        {
#if (IL2CPPMELON || IL2CPPBEPINEX)
            var command = new ClearWanted();
            var args = new Il2CppSystem.Collections.Generic.List<string>();
#else
            var command = new ClearWanted();
            var args = new List<string>();
#endif
            command.Execute(args);
        }

        /// <summary>
        /// Gives the player experience points.
        /// </summary>
        public static void GiveXp(int amount)
        {
#if (IL2CPPMELON || IL2CPPBEPINEX)
            var command = new GiveXP();
            var args = new Il2CppSystem.Collections.Generic.List<string>();
#else
            var command = new GiveXP();
            var args = new List<string>();
#endif
            args.Add(amount.ToString());
            command.Execute(args);
        }

        /// <summary>
        /// Instantly sets all plants in the world to fully grown.
        /// </summary>
        public static void GrowPlants()
        {
#if (IL2CPPMELON || IL2CPPBEPINEX)
            var command = new GrowPlants();
            var args = new Il2CppSystem.Collections.Generic.List<string>();
#else
            var command = new GrowPlants();
            var args = new List<string>();
#endif
            command.Execute(args);
        }

        /// <summary>
        /// Lowers the player's wanted level.
        /// </summary>
        public static void LowerWanted()
        {
#if (IL2CPPMELON || IL2CPPBEPINEX)
            var command = new LowerWanted();
            var args = new Il2CppSystem.Collections.Generic.List<string>();
#else
            var command = new LowerWanted();
            var args = new List<string>();
#endif
            command.Execute(args);
        }

        /// <summary>
        /// Raises the player's wanted level.
        /// </summary>
        public static void RaiseWanted()
        {
#if (IL2CPPMELON || IL2CPPBEPINEX)
            var command = new RaisedWanted();
            var args = new Il2CppSystem.Collections.Generic.List<string>();
#else
            var command = new RaisedWanted();
            var args = new List<string>();
#endif
            command.Execute(args);
        }

        /// <summary>
        /// Forces a save of the current game state.
        /// </summary>
        public static void SaveGame()
        {
#if (IL2CPPMELON || IL2CPPBEPINEX)
            var command = new Save();
            var args = new Il2CppSystem.Collections.Generic.List<string>();
#else
            var command = new Save();
            var args = new List<string>();
#endif
            command.Execute(args);
        }

        /// <summary>
        /// Marks a product as discovered by its item code.
        /// </summary>
        public static void DiscoverProduct(string productCode)
        {
#if (IL2CPPMELON || IL2CPPBEPINEX)
            var command = new SetDiscovered();
            var args = new Il2CppSystem.Collections.Generic.List<string>();
#else
            var command = new SetDiscovered();
            var args = new List<string>();
#endif
            args.Add(productCode);
            command.Execute(args);
        }

        /// <summary>
        /// Sets the player's energy to a value between 0 and 100.
        /// </summary>
        public static void SetPlayerEnergyLevel(float amount)
        {
#if (IL2CPPMELON || IL2CPPBEPINEX)
            var command = new SetEnergy();
            var args = new Il2CppSystem.Collections.Generic.List<string>();
#else
            var command = new SetEnergy();
            var args = new List<string>();
#endif
            args.Add(amount.ToString(System.Globalization.CultureInfo.InvariantCulture));
            command.Execute(args);
        }

        /// <summary>
        /// Sets the player's health to the specified value.
        /// </summary>
        public static void SetPlayerHealth(float amount)
        {
#if (IL2CPPMELON || IL2CPPBEPINEX)
            var command = new SetHealth();
            var args = new Il2CppSystem.Collections.Generic.List<string>();
#else
            var command = new SetHealth();
            var args = new List<string>();
#endif
            args.Add(amount.ToString(System.Globalization.CultureInfo.InvariantCulture));
            command.Execute(args);
        }

        /// <summary>
        /// Sets the player's jump force multiplier. Must be non-negative.
        /// </summary>
        public static void SetPlayerJumpMultiplier(float multiplier)
        {
#if (IL2CPPMELON || IL2CPPBEPINEX)
            var command = new SetJumpMultiplier();
            var args = new Il2CppSystem.Collections.Generic.List<string>();
#else
            var command = new SetJumpMultiplier();
            var args = new List<string>();
#endif
            args.Add(multiplier.ToString(System.Globalization.CultureInfo.InvariantCulture));
            command.Execute(args);
        }

        /// <summary>
        /// Sets the intensity of law enforcement activity (0-10).
        /// </summary>
        public static void SetLawIntensity(float intensity)
        {
#if (IL2CPPMELON || IL2CPPBEPINEX)
            var command = new SetLawIntensity();
            var args = new Il2CppSystem.Collections.Generic.List<string>();
#else
            var command = new SetLawIntensity();
            var args = new List<string>();
#endif
            args.Add(intensity.ToString(System.Globalization.CultureInfo.InvariantCulture));
            command.Execute(args);
        }

        /// <summary>
        /// Sets the player's movement speed multiplier. Must be non-negative.
        /// </summary>
        public static void SetPlayerMoveSpeedMultiplier(float multiplier)
        {
#if (IL2CPPMELON || IL2CPPBEPINEX)
            var command = new SetMoveSpeedCommand();
            var args = new Il2CppSystem.Collections.Generic.List<string>();
#else
            var command = new SetMoveSpeedCommand();
            var args = new List<string>();
#endif
            args.Add(multiplier.ToString(System.Globalization.CultureInfo.InvariantCulture));
            command.Execute(args);
        }

        /// <summary>
        /// Sets the equipped item's quality.
        /// </summary>
        /// <param name="quality">API quality value to set.</param>
        public static void SetQuality(Quality quality)
        {
#if (IL2CPPMELON || IL2CPPBEPINEX)
            var command = new SetQuality();
            var args = new Il2CppSystem.Collections.Generic.List<string>();
#else
            var command = new SetQuality();
            var args = new List<string>();
#endif
            args.Add(quality.ToString());
            command.Execute(args);
        }

        /// <summary>
        /// Sets the state of a quest by name.
        /// </summary>
        public static void SetQuestState(string questName, QuestState state)
        {
#if (IL2CPPMELON || IL2CPPBEPINEX)
            var command = new SetQuestState();
            var args = new Il2CppSystem.Collections.Generic.List<string>();
#else
            var command = new SetQuestState();
            var args = new List<string>();
#endif
            args.Add(questName);
            args.Add(state.ToString());
            command.Execute(args);
        }

        /// <summary>
        /// Sets the relationship scalar for an NPC by id (0-5).
        /// </summary>
        public static void SetNpcRelationship(string npcId, float level)
        {
#if (IL2CPPMELON || IL2CPPBEPINEX)
            var command = new SetRelationship();
            var args = new Il2CppSystem.Collections.Generic.List<string>();
#else
            var command = new SetRelationship();
            var args = new List<string>();
#endif
            args.Add(npcId);
            args.Add(level.ToString(System.Globalization.CultureInfo.InvariantCulture));
            command.Execute(args);
        }

        /// <summary>
        /// Sets the relationship scalar for an NPC (0-5).
        /// </summary>
        public static void SetNpcRelationship(NPC npc, float level)
        {
            SetNpcRelationship(npc.ID, level);
        }

        /// <summary>
        /// Unlocks the given NPC's connection.
        /// </summary>
        public static void UnlockNpc(NPC npc)
        {
#if (IL2CPPMELON || IL2CPPBEPINEX)
            var command = new SetUnlocked();
            var args = new Il2CppSystem.Collections.Generic.List<string>();
#else
            var command = new SetUnlocked();
            var args = new List<string>();
#endif
            args.Add(npc.ID);
            command.Execute(args);
        }

        /// <summary>
        /// Sets the time of day using a 24h HHmm string (e.g., "1530").
        /// </summary>
        public static void SetTime(string hhmm)
        {
#if (IL2CPPMELON || IL2CPPBEPINEX)
            var command = new SetTimeCommand();
            var args = new Il2CppSystem.Collections.Generic.List<string>();
#else
            var command = new SetTimeCommand();
            var args = new List<string>();
#endif
            args.Add(hhmm);
            command.Execute(args);
        }

        /// <summary>
        /// Spawns a vehicle by code at the player's location.
        /// </summary>
        public static void SpawnVehicle(string vehicleCode)
        {
#if (IL2CPPMELON || IL2CPPBEPINEX)
            var command = new SpawnVehicleCommand();
            var args = new Il2CppSystem.Collections.Generic.List<string>();
#else
            var command = new SpawnVehicleCommand();
            var args = new List<string>();
#endif
            args.Add(vehicleCode);
            command.Execute(args);
        }
    }
}
