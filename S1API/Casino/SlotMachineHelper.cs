#if (IL2CPPMELON)
using S1Casino = Il2CppScheduleOne.Casino;
using S1Money = Il2CppScheduleOne.Money;
using S1DevUtilities = Il2CppScheduleOne.DevUtilities;
using S1Items = Il2CppScheduleOne.ItemFramework;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Casino = ScheduleOne.Casino;
using S1Money = ScheduleOne.Money;
using S1DevUtilities = ScheduleOne.DevUtilities;
using S1Items = ScheduleOne.ItemFramework;
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using S1API.Entities;
using S1API.Lifecycle;
using S1API.Logging;
using S1API.Utils;
using UnityEngine;
#if (IL2CPPMELON)
using MelonLoader;
#endif

namespace S1API.Casino
{
    /// <summary>
    /// Provides a modder-facing API for interacting with slot machines without exposing game types.
    /// Handles finding slot machines, managing NPC cash, and triggering spins.
    /// </summary>
    public static class SlotMachineHelper
    {
        private static readonly Log Logger = new Log("SlotMachineHelper");
        private static bool _lifecycleHooksRegistered;
        private static bool _isSceneChangeInProgress;

        private sealed class ActiveSpin
        {
            public S1Casino.SlotMachine Machine { get; set; }
#if (IL2CPPMELON)
            public object CoroutineHandle { get; set; }
#else
            public Coroutine CoroutineHandle { get; set; }
#endif
        }

        private static readonly List<ActiveSpin> ActiveSpins = new();

        /// <summary>
        /// Outcome categories for slot machine spins.
        /// </summary>
        public enum Outcome
        {
            /// <summary>Jackpot - three sevens.</summary>
            Jackpot = 0,
            /// <summary>Big win - three bells.</summary>
            BigWin = 1,
            /// <summary>Small win - three matching fruits.</summary>
            SmallWin = 2,
            /// <summary>Mini win - any three fruits.</summary>
            MiniWin = 3,
            /// <summary>No win.</summary>
            NoWin = 4
        }

        /// <summary>
        /// Gets the total cash value from an NPC's inventory (cash items).
        /// </summary>
        /// <param name="npc">The NPC whose cash to count.</param>
        /// <returns>Total cash amount in dollars.</returns>
        public static float GetNPCCash(NPC npc)
        {
            if (npc?.S1NPC?.Inventory == null)
                return 0;

            try
            {
                float totalCash = 0;
                int cashItemsFound = 0;
                var inventory = npc.S1NPC.Inventory;
                
                totalCash = inventory.GetCashInInventory();
                return totalCash;
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to get cash for NPC {npc?.ID}: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Removes a specified amount of cash from an NPC's inventory.
        /// </summary>
        /// <param name="npc">The NPC to remove cash from.</param>
        /// <param name="amount">The amount of cash to remove in dollars.</param>
        /// <returns>True if the full amount was successfully removed; false otherwise.</returns>
        public static bool RemoveNPCCash(NPC npc, int amount)
        {
            if (npc?.S1NPC?.Inventory == null || amount <= 0)
                return false;

            try
            {
                var inventory = npc.S1NPC.Inventory;
                
                // Get cash before removal to verify we have enough
                float cashBefore = GetNPCCash(npc);
                if (cashBefore < amount)
                    return false;

                // Use the inventory's RemoveCash method which properly handles CashInstance objects
                // This method uses ChangeBalance on CashInstance objects, which is the correct approach
                var removeCashMethod = inventory.GetType().GetMethod("RemoveCash", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (removeCashMethod != null)
                {
                    removeCashMethod.Invoke(inventory, new object[] { (float)amount });
                    
                    // Verify removal was successful
                    float cashAfter = GetNPCCash(npc);
                    float actualRemoved = cashBefore - cashAfter;
                    
                    if (actualRemoved >= amount - 0.01f) // Allow small floating point tolerance
                    {
                        return true;
                    }
                    else
                    {
                        Logger.Warning($"[{npc.ID}] RemoveNPCCash: Partial removal (Requested: ${amount}, Actual: ${actualRemoved})");
                        return false;
                    }
                }
                else
                {
                    Logger.Warning($"[{npc.ID}] RemoveNPCCash: RemoveCash method not found on inventory type");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to remove cash for NPC {npc?.ID}: {ex.Message}");
                Logger.Warning($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Adds cash to an NPC's inventory. Tries to reuse existing cash stacks, otherwise ensures
        /// there's a spare slot (expanding the slot count if needed) and inserts a single cash item.
        /// </summary>
        /// <param name="npc">The NPC to add cash to.</param>
        /// <param name="amount">The amount of cash to add in dollars.</param>
        public static void AddNPCCash(NPC npc, int amount)
        {
            if (npc?.S1NPC?.Inventory == null || amount <= 0)
            {
                Logger.Warning($"AddNPCCash: Invalid parameters - NPC={npc?.ID}, Amount={amount}, Inventory={(npc?.S1NPC?.Inventory != null ? "Valid" : "Null")}");
                return;
            }

            try
            {
                var inventory = npc.S1NPC.Inventory;
                
                // CRITICAL: Ensure inventory slots are initialized before adding cash
                // AddCash/InsertItem internally checks CanItemFit, which requires slots to exist
                var inventoryWrapper = npc.Inventory;
                if (inventoryWrapper != null)
                    inventoryWrapper.EnsureInitialized();
                
                // Verify slots exist before proceeding
                if (inventory.ItemSlots == null || inventory.ItemSlots.Count == 0)
                {
                    Logger.Warning($"[{npc.ID}] AddNPCCash: Inventory slots not initialized (ItemSlots is null or empty). Cannot add cash.");
                    return;
                }
                
                // Diagnostic: Check slot lock states
                int unlockedSlots = 0;
                int freeSlots = 0;
                for (int i = 0; i < inventory.ItemSlots.Count; i++)
                {
                    var slot = inventory.ItemSlots[i];
                    if (slot != null)
                    {
                        try
                        {
                            var isLocked = ReflectionUtils.TryGetFieldOrProperty(slot, "IsLocked") as bool? ?? false;
                            var isAddLocked = ReflectionUtils.TryGetFieldOrProperty(slot, "IsAddLocked") as bool? ?? false;
                            if (!isLocked && !isAddLocked)
                            {
                                unlockedSlots++;
                                if (slot.ItemInstance == null)
                                {
                                    freeSlots++;
                                }
                            }
                        }
                        catch { }
                    }
                }
                
                // Use the proper AddCash method on NPCInventory, which handles chunking and network sync correctly
                // This is the same method used by the game's internal systems (splits amounts > 1000 into chunks)
                // Now that EnsureInitialized() unlocks all slots, this should work correctly
                try
                {
                    var addCashMethod = inventory.GetType().GetMethod("AddCash", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (addCashMethod != null)
                    {
                        addCashMethod.Invoke(inventory, new object[] { (float)amount });
                    }
                    else
                    {
                        throw new System.MissingMethodException("AddCash method not found! Did Schedule 1 update recently?");
                    }
                }
                catch (Exception methodEx)
                {
                    // Fallback to manual insertion if AddCash method not accessible
                    Logger.Warning($"[{npc.ID}] AddNPCCash: Could not use AddCash method ({methodEx.Message}), falling back to manual insertion");
                    var moneyManager = S1DevUtilities.NetworkSingleton<S1Money.MoneyManager>.Instance;
                    if (moneyManager == null)
                    {
                        Logger.Warning($"[{npc.ID}] AddNPCCash: MoneyManager not available to create cash item");
                        return;
                    }

                    // Split large amounts into chunks of 1000 (same as AddCash does internally)
                    float remaining = amount;
                    while (remaining > 0.1f)
                    {
                        float chunk = Mathf.Min(remaining, 1000f);
                        remaining -= chunk;
                        
                        var cashItem = moneyManager.GetCashInstance((int)chunk);
                        if (cashItem == null)
                        {
                            Logger.Warning($"[{npc.ID}] AddNPCCash: GetCashInstance returned null for chunk ${chunk}");
                            break;
                        }
                        
                        if (!inventory.CanItemFit(cashItem))
                        {
                            Logger.Warning($"[{npc.ID}] AddNPCCash: Cash item (${chunk}) cannot fit. UnlockedSlots={unlockedSlots}, FreeSlots={freeSlots}");
                            break;
                        }
                        
                        inventory.InsertItem(cashItem, network: true);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"[{npc.ID}] AddNPCCash: Failed with exception: {ex.Message}");
                Logger.Warning($"[{npc.ID}] AddNPCCash: Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Makes an NPC use a slot machine with the specified bet amount.
        /// This handles all cash transactions, animations, and outcome determination automatically.
        /// </summary>
        /// <param name="npc">The NPC that will use the slot machine.</param>
        /// <param name="machinePosition">The world position of the slot machine.</param>
        /// <param name="betAmount">The amount to bet in dollars.</param>
        /// <param name="maxSearchDistance">Maximum distance to search for a slot machine from the specified position.</param>
        /// <returns>True if the NPC successfully started using a slot machine; false otherwise.</returns>
        public static bool UseSlotMachine(NPC npc, Vector3 machinePosition, int betAmount, float maxSearchDistance = 5f)
        {
            if (npc == null || betAmount <= 0)
                return false;

            try
            {
                EnsureLifecycleHooks();

                if (_isSceneChangeInProgress)
                    return false;

                var machine = FindNearestSlotMachine(machinePosition, maxSearchDistance);
                if (machine == null)
                {
                    Logger.Warning($"No slot machine found near position {machinePosition}");
                    return false;
                }

                if (machine.IsSpinning)
                    return false;

                float npcCash = GetNPCCash(npc);
                if (npcCash < betAmount)
                    return false;

                if (!RemoveNPCCash(npc, betAmount))
                {
                    Logger.Warning($"Failed to remove cash from NPC {npc.ID}");
                    return false;
                }

                var symbols = new S1Casino.SlotMachine.ESymbol[machine.Reels.Length];
                for (int i = 0; i < symbols.Length; i++)
                {
                    symbols[i] = S1Casino.SlotMachine.GetRandomSymbol();
                }

                var activeSpin = new ActiveSpin
                {
                    Machine = machine
                };

#if (IL2CPPMELON)
                activeSpin.CoroutineHandle = MelonCoroutines.Start(SpinSlotMachineForNPC(npc, machine, symbols, betAmount, activeSpin));
#else
                var coroutineService = S1DevUtilities.Singleton<S1DevUtilities.CoroutineService>.Instance;
                if (coroutineService == null)
                {
                    Logger.Warning("CoroutineService is not available to start slot machine spin");
                    AddNPCCash(npc, betAmount);
                    return false;
                }

                activeSpin.CoroutineHandle = coroutineService.StartCoroutine(
                    SpinSlotMachineForNPC(npc, machine, symbols, betAmount, activeSpin));
#endif

                RegisterActiveSpin(activeSpin);

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to use slot machine for NPC {npc.ID}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Finds the nearest slot machine to a given position.
        /// </summary>
        /// <param name="position">The position to search from.</param>
        /// <param name="maxDistance">Maximum distance to search.</param>
        /// <returns>The nearest slot machine, or null if none found.</returns>
        public static S1Casino.SlotMachine FindNearestSlotMachine(Vector3 position, float maxDistance)
        {
            try
            {
                var machines = UnityEngine.Object.FindObjectsOfType<S1Casino.SlotMachine>();
                if (machines == null || machines.Length == 0)
                    return null;

                S1Casino.SlotMachine nearest = null;
                float nearestDistance = float.MaxValue;

                foreach (var machine in machines)
                {
                    if (machine == null || machine.gameObject == null)
                        continue;

                    float distance = Vector3.Distance(position, machine.transform.position);
                    if (distance < nearestDistance && distance <= maxDistance)
                    {
                        nearestDistance = distance;
                        nearest = machine;
                    }
                }

                return nearest;
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to find slot machine: {ex.Message}");
                return null;
            }
        }

        internal static bool IsSceneTransitionInProgress => _isSceneChangeInProgress;

        private static IEnumerator SpinSlotMachineForNPC(
            NPC npc,
            S1Casino.SlotMachine machine,
            S1Casino.SlotMachine.ESymbol[] symbols,
            int betAmount,
            ActiveSpin activeSpin)
        {
            try
            {
                SetMachineSpinning(machine, true);

                for (int i = 0; i < machine.Reels.Length; i++)
                {
                    yield return new WaitForSeconds(0.2f);
                    if (ShouldAbortSpin(npc, machine))
                        yield break;

                    if (!TryInvokeMachineStep(machine, () => machine.Reels[i].Spin(), $"spin reel {i}"))
                        yield break;

                    if (i == 0 && machine.SpinLoop != null &&
                        !TryInvokeMachineStep(machine, () => machine.SpinLoop.Play(), "start spin loop"))
                    {
                        yield break;
                    }
                }

                yield return new WaitForSeconds(0.5f);
                if (ShouldAbortSpin(npc, machine))
                    yield break;

                var outcome = EvaluateOutcome(machine, symbols);

                for (int i = 0; i < machine.Reels.Length; i++)
                {
                    if (i == machine.Reels.Length - 1 && outcome != S1Casino.SlotMachine.EOutcome.Jackpot &&
                        symbols.Length >= 3 && symbols[i - 1] == symbols[i - 2])
                    {
                        yield return new WaitForSeconds(0.3f);
                        if (ShouldAbortSpin(npc, machine))
                            yield break;
                    }

                    yield return new WaitForSeconds(0.6f);
                    if (ShouldAbortSpin(npc, machine))
                        yield break;

                    if (outcome == S1Casino.SlotMachine.EOutcome.Jackpot)
                    {
                        if (i == 0)
                        {
                            if (machine.JackpotSound != null &&
                                !TryInvokeMachineStep(machine, () => machine.JackpotSound.Play(), "play jackpot sound"))
                            {
                                yield break;
                            }
                        }
                        else
                        {
                            yield return new WaitForSeconds(0.35f);
                            if (ShouldAbortSpin(npc, machine))
                                yield break;
                        }
                    }

                    if (!TryInvokeMachineStep(machine, () => machine.Reels[i].Stop(symbols[i]), $"stop reel {i}"))
                        yield break;
                }

                if (machine.SpinLoop != null &&
                    !TryInvokeMachineStep(machine, () => machine.SpinLoop.Stop(), "stop spin loop"))
                {
                    yield break;
                }

                int winAmount = GetWinAmount(outcome, betAmount);
                if (winAmount > 0 && npc != null && npc.gameObject != null)
                    AddNPCCash(npc, winAmount);

                try
                {
                    var displayMethod = typeof(S1Casino.SlotMachine).GetMethod("DisplayOutcome",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (displayMethod != null && !ShouldAbortSpin(npc, machine))
                        displayMethod.Invoke(machine, new object[] { outcome, winAmount });
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Failed to display outcome: {ex.Message}");
                }
            }
            finally
            {
                if (machine != null && machine.SpinLoop != null)
                {
                    TryInvokeMachineStep(machine, () => machine.SpinLoop.Stop(), "final spin loop stop");
                }

                SetMachineSpinning(machine, false);
                UnregisterActiveSpin(activeSpin);
            }
        }

        private static void EnsureLifecycleHooks()
        {
            if (_lifecycleHooksRegistered)
                return;

            GameLifecycle.OnPreSceneChange += HandlePreSceneChange;
            GameLifecycle.OnLoadComplete += HandleLoadComplete;
            _lifecycleHooksRegistered = true;
        }

        private static void HandlePreSceneChange()
        {
            _isSceneChangeInProgress = true;
            CancelAllActiveSpins();
        }

        private static void HandleLoadComplete()
        {
            _isSceneChangeInProgress = false;
        }

        private static void RegisterActiveSpin(ActiveSpin activeSpin)
        {
            ActiveSpins.Add(activeSpin);
        }

        private static void UnregisterActiveSpin(ActiveSpin activeSpin)
        {
            ActiveSpins.Remove(activeSpin);
        }

        private static void CancelAllActiveSpins()
        {
            if (ActiveSpins.Count == 0)
                return;

            var activeSpins = ActiveSpins.ToArray();
            ActiveSpins.Clear();

            foreach (var activeSpin in activeSpins)
            {
                try
                {
#if (IL2CPPMELON)
                    if (activeSpin.CoroutineHandle != null)
                        MelonCoroutines.Stop(activeSpin.CoroutineHandle);
#else
                    var coroutineService = S1DevUtilities.Singleton<S1DevUtilities.CoroutineService>.Instance;
                    if (coroutineService != null && activeSpin.CoroutineHandle != null)
                        coroutineService.StopCoroutine(activeSpin.CoroutineHandle);
#endif
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Failed to stop active slot machine coroutine during scene change: {ex.Message}");
                }

                SetMachineSpinning(activeSpin.Machine, false);
            }
        }

        private static bool ShouldAbortSpin(NPC npc, S1Casino.SlotMachine machine)
        {
            if (_isSceneChangeInProgress)
                return true;

            if (npc == null || npc.gameObject == null)
                return true;

            if (machine == null || machine.gameObject == null || machine.Reels == null)
                return true;

            return false;
        }

        private static bool TryInvokeMachineStep(S1Casino.SlotMachine machine, Action action, string stepName)
        {
            if (_isSceneChangeInProgress || machine == null || machine.gameObject == null)
                return false;

            try
            {
                action();
                return true;
            }
            catch (Exception ex)
            {
                if (!_isSceneChangeInProgress)
                {
                    Logger.Warning($"Aborting slot machine spin during {stepName}: {ex.Message}");
                }

                return false;
            }
        }

        private static void SetMachineSpinning(S1Casino.SlotMachine machine, bool isSpinning)
        {
            if (machine == null)
                return;

            try
            {
                var isSpinningProp = typeof(S1Casino.SlotMachine).GetProperty("IsSpinning");
                if (isSpinningProp != null && isSpinningProp.CanWrite)
                    isSpinningProp.SetValue(machine, isSpinning);
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to set slot machine spinning state: {ex.Message}");
            }
        }

        private static S1Casino.SlotMachine.EOutcome EvaluateOutcome(S1Casino.SlotMachine machine, S1Casino.SlotMachine.ESymbol[] outcome)
        {
            try
            {
                var method = typeof(S1Casino.SlotMachine).GetMethod("EvaluateOutcome", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (method != null)
                {
                    return (S1Casino.SlotMachine.EOutcome)method.Invoke(machine, new object[] { outcome });
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to call EvaluateOutcome via reflection: {ex.Message}");
            }

            if (IsUniform(outcome))
            {
                if (outcome[0] == S1Casino.SlotMachine.ESymbol.Seven)
                    return S1Casino.SlotMachine.EOutcome.Jackpot;
                if (outcome[0] == S1Casino.SlotMachine.ESymbol.Bell)
                    return S1Casino.SlotMachine.EOutcome.BigWin;
                if (IsFruit(outcome[0]))
                    return S1Casino.SlotMachine.EOutcome.SmallWin;
            }
            if (IsAllFruit(outcome))
                return S1Casino.SlotMachine.EOutcome.MiniWin;
            
            return S1Casino.SlotMachine.EOutcome.NoWin;
        }

        private static int GetWinAmount(S1Casino.SlotMachine.EOutcome outcome, int betAmount)
        {
            return outcome switch
            {
                S1Casino.SlotMachine.EOutcome.Jackpot => betAmount * 100,
                S1Casino.SlotMachine.EOutcome.BigWin => betAmount * 25,
                S1Casino.SlotMachine.EOutcome.SmallWin => betAmount * 10,
                S1Casino.SlotMachine.EOutcome.MiniWin => betAmount * 2,
                _ => 0,
            };
        }

        private static bool IsUniform(S1Casino.SlotMachine.ESymbol[] symbols)
        {
            for (int i = 1; i < symbols.Length; i++)
            {
                if (symbols[i] != symbols[i - 1])
                    return false;
            }
            return true;
        }

        private static bool IsFruit(S1Casino.SlotMachine.ESymbol symbol)
        {
            return symbol == S1Casino.SlotMachine.ESymbol.Cherry ||
                   symbol == S1Casino.SlotMachine.ESymbol.Lemon ||
                   symbol == S1Casino.SlotMachine.ESymbol.Grape ||
                   symbol == S1Casino.SlotMachine.ESymbol.Watermelon;
        }

        private static bool IsAllFruit(S1Casino.SlotMachine.ESymbol[] symbols)
        {
            for (int i = 0; i < symbols.Length; i++)
            {
                if (!IsFruit(symbols[i]))
                    return false;
            }
            return true;
        }
    }
}

