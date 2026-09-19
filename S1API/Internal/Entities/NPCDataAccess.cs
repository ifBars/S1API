#if IL2CPPMELON
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using S1AvatarFramework = Il2CppScheduleOne.AvatarFramework;
using S1DevUtilities = Il2CppScheduleOne.DevUtilities;
using S1Dialogue = Il2CppScheduleOne.Dialogue;
using S1Economy = Il2CppScheduleOne.Economy;
using S1Employees = Il2CppScheduleOne.Employees;
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
using S1Messaging = Il2CppScheduleOne.Messaging;
using S1NPCFramework = Il2CppScheduleOne.NPCs.Framework;
using S1NPCs = Il2CppScheduleOne.NPCs;
using S1VoiceOver = Il2CppScheduleOne.VoiceOver;
#elif MONOMELON
using S1AvatarFramework = ScheduleOne.AvatarFramework;
using S1DevUtilities = ScheduleOne.DevUtilities;
using S1Dialogue = ScheduleOne.Dialogue;
using S1Economy = ScheduleOne.Economy;
using S1Employees = ScheduleOne.Employees;
using S1ItemFramework = ScheduleOne.ItemFramework;
using S1Messaging = ScheduleOne.Messaging;
using S1NPCFramework = ScheduleOne.NPCs.Framework;
using S1NPCs = ScheduleOne.NPCs;
using S1VoiceOver = ScheduleOne.VoiceOver;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using S1API.Entities.Dealer;
using S1API.Entities.Supplier;
using S1API.Internal.Utils;
using UnityEngine;

namespace S1API.Internal.Entities
{
    internal static class NPCDataAccess
    {
        private static readonly Logging.Log Logger = new Logging.Log("NPCDataAccess");
#if !IL2CPPMELON
        private static readonly FieldInfo NpcDataObjectField =
            typeof(S1NPCs.NPC).GetField("_npcData", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(typeof(S1NPCs.NPC).FullName, "_npcData");
        private static readonly FieldInfo CurrentNpcDataField =
            typeof(S1NPCs.NPC).GetField("<NPCData>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(typeof(S1NPCs.NPC).FullName, "<NPCData>k__BackingField");
#endif

        internal static void AssignNewData(
            S1NPCs.NPC npc,
            NpcRootRole rootRole,
            S1NPCs.NPC? sourceNpc = null)
        {
            if (npc == null)
                throw new ArgumentNullException(nameof(npc));

            S1NPCFramework.BaseNPCDataObject dataObject = rootRole switch
            {
                NpcRootRole.Dealer => ScriptableObject.CreateInstance<S1NPCFramework.DealerNPCDataObject>(),
                NpcRootRole.Supplier => ScriptableObject.CreateInstance<S1NPCFramework.SupplierNPCDataObject>(),
                _ => ScriptableObject.CreateInstance<S1NPCFramework.NPCDataObject>()
            };

            if (dataObject == null)
                throw new InvalidOperationException("Failed to create the beta NPC data object.");

            dataObject.hideFlags = HideFlags.DontUnloadUnusedAsset;
            dataObject.Initialize();
            S1NPCFramework.NPCData data = dataObject.GetOriginalData();
            PrepareData(data);
            if (rootRole == NpcRootRole.Supplier)
            {
                // Native supplier messaging presets do not allow their persistent
                // order conversations to be hidden from the Messages app.
                data.Messaging.ConversationCanBeHidden = false;
            }
            EnsureDialogueDatabase(data, sourceNpc);
            if (rootRole == NpcRootRole.Supplier)
                EnsureSupplierDialogueDatabase(data, required: false);
            SetDataObject(npc, dataObject);
            SetCurrentData(npc, data);
        }

        internal static void InitializeCurrentDataForConstruction(S1NPCs.NPC npc)
        {
            if (npc == null)
                throw new ArgumentNullException(nameof(npc));

            S1NPCFramework.BaseNPCDataObject dataObject = GetDataObject(npc)
                ?? throw new InvalidOperationException("The custom NPC prefab has no framework data object.");
            S1NPCFramework.NPCData data = dataObject.GetRuntimeData()
                ?? throw new InvalidOperationException("The custom NPC data object returned no runtime data.");
            PrepareData(data);
            SetCurrentData(npc, data);
        }

        internal static bool ApplyIdentity(
            S1NPCs.NPC npc,
            string? id,
            string? firstName,
            string? lastName)
        {
            return ApplyToData(npc, data =>
            {
                S1NPCFramework.BasicInfo basicInfo = data.BasicInfo;
                if (!string.IsNullOrWhiteSpace(id))
                    basicInfo.ID = id;
                if (!string.IsNullOrWhiteSpace(firstName))
                    basicInfo.FirstName = firstName;

                basicInfo.HasLastName = !string.IsNullOrWhiteSpace(lastName);
                basicInfo.LastName = basicInfo.HasLastName ? lastName : string.Empty;
            });
        }

        internal static string GetId(S1NPCs.NPC npc) =>
            GetCurrentData(npc)?.BasicInfo?.ID ?? string.Empty;

        internal static string GetFirstName(S1NPCs.NPC npc) =>
            GetCurrentData(npc)?.BasicInfo?.FirstName ?? string.Empty;

        internal static string GetLastName(S1NPCs.NPC npc) =>
            GetCurrentData(npc)?.BasicInfo?.LastName ?? string.Empty;

        internal static bool ApplyFirstName(S1NPCs.NPC npc, string firstName) =>
            ApplyToData(npc, data => data.BasicInfo.FirstName = firstName ?? string.Empty);

        internal static bool ApplyLastName(S1NPCs.NPC npc, string lastName) =>
            ApplyToData(npc, data =>
            {
                data.BasicInfo.HasLastName = !string.IsNullOrWhiteSpace(lastName);
                data.BasicInfo.LastName = data.BasicInfo.HasLastName ? lastName : string.Empty;
            });

        internal static bool ApplyId(S1NPCs.NPC npc, string id) =>
            ApplyToData(npc, data => data.BasicInfo.ID = id ?? string.Empty);

        internal static bool ApplyIcon(S1NPCs.NPC npc, Sprite? icon)
        {
            return ApplyToData(npc, data => data.Appearance.Mugshot = icon);
        }

        internal static Sprite? GetIcon(S1NPCs.NPC npc) =>
            GetCurrentData(npc)?.Appearance?.Mugshot;

        internal static bool ApplyVoice(
            S1NPCs.NPC npc,
            S1VoiceOver.VODatabase database,
            float? pitch)
        {
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            return ApplyToData(npc, data =>
            {
                if (data.Voice == null)
                    throw new InvalidOperationException("The custom NPC data has no voice settings.");

                data.Voice.VoiceDatabase = database;
                if (pitch.HasValue)
                    data.Voice.VoicePitch = pitch.Value;
            });
        }

        internal static bool GetConversationCanBeHidden(S1NPCs.NPC npc) =>
            GetCurrentData(npc)?.Messaging?.ConversationCanBeHidden ?? false;

        internal static bool ApplyConversationCanBeHidden(S1NPCs.NPC npc, bool value) =>
            ApplyToData(npc, data => data.Messaging.ConversationCanBeHidden = value);

        internal static IReadOnlyList<S1Messaging.EConversationCategory> GetConversationCategories(
            S1NPCs.NPC npc)
        {
            var categories = GetCurrentData(npc)?.Messaging?.ConversationCategories;
            if (categories == null)
                return Array.Empty<S1Messaging.EConversationCategory>();

            var result = new List<S1Messaging.EConversationCategory>(categories.Length);
            foreach (S1Messaging.EConversationCategory category in categories)
                result.Add(category);
            return result;
        }

        internal static bool ApplyConversationCategories(
            S1NPCs.NPC npc,
            IEnumerable<S1Messaging.EConversationCategory> categories)
        {
            var values = categories?.ToArray() ?? Array.Empty<S1Messaging.EConversationCategory>();
            return ApplyToData(npc, data =>
            {
#if IL2CPPMELON
                var array = new Il2CppStructArray<S1Messaging.EConversationCategory>(values.Length);
                for (int i = 0; i < values.Length; i++)
                    array[i] = values[i];
                data.Messaging.ConversationCategories = array;
#else
                data.Messaging.ConversationCategories = values;
#endif
            });
        }

        internal static bool ApplyAppearance(S1NPCs.NPC npc, S1AvatarFramework.AvatarSettings? settings)
        {
            if (settings == null)
                return false;

            S1NPCFramework.Appearance? appearance = GetOriginalData(npc)?.Appearance;
            if (appearance == null)
                return false;

            appearance.DefaultAppearance = settings.EquivalentNakedAppearance;
            appearance.DefaultOutfit = settings.EquivalentOutfit;
            return true;
        }

        internal static bool ApplyDealerDefaults(
            S1Economy.Dealer dealer,
            DealerDataBuilder.DealerConfigData data)
        {
            if (dealer == null || data == null)
                return false;

            S1NPCFramework.NPCData? originalData = GetOriginalData(dealer);
            if (originalData == null
                || !CrossType.Is(originalData, out S1NPCFramework.DealerNPCData dealerData))
                return false;

            ApplyDealerDefaults(dealerData, data);

            S1NPCFramework.NPCData? currentData = GetCurrentData(dealer);
            if (currentData != null
                && CrossType.Is(currentData, out S1NPCFramework.DealerNPCData currentDealerData)
                && !ReferenceEquals(currentDealerData, dealerData))
            {
                ApplyDealerDefaults(currentDealerData, data);
            }

            return true;
        }

        internal static bool ApplySupplierDefaults(
            S1Economy.Supplier supplier,
            SupplierDataBuilder.SupplierConfigData data)
        {
            if (supplier == null || data == null)
                return false;

            S1NPCFramework.NPCData? originalData = GetOriginalData(supplier);
            if (originalData == null
                || !CrossType.Is(originalData, out S1NPCFramework.SupplierNPCData supplierData))
                return false;

            ApplySupplierDefaults(supplierData, data);

            S1NPCFramework.NPCData? currentData = GetCurrentData(supplier);
            if (currentData != null
                && CrossType.Is(currentData, out S1NPCFramework.SupplierNPCData currentSupplierData)
                && !ReferenceEquals(currentSupplierData, supplierData))
            {
                ApplySupplierDefaults(currentSupplierData, data);
            }

            return true;
        }

        internal static void PrepareForRuntime(S1NPCs.NPC npc)
        {
            S1NPCFramework.NPCData data = GetOriginalData(npc)
                ?? throw new InvalidOperationException("The beta NPC has no framework data before network spawn.");

            PrepareData(data);
            EnsureDialogueDatabase(data);

            if (CrossType.Is(data, out S1NPCFramework.SupplierNPCData _))
                EnsureSupplierDialogueDatabase(data, required: true);

            if (CrossType.Is(data, out S1NPCFramework.DealerNPCData _))
                EnsureDealerDialogueDefaults(npc, logFailure: false);
        }

        private static void ApplyDealerDefaults(
            S1NPCFramework.DealerNPCData dealerData,
            DealerDataBuilder.DealerConfigData data)
        {
            dealerData.SigningFee = data.SigningFee;
            dealerData.SalesCutPercentage = data.Cut;
            dealerData.DealerType = (S1Economy.EDealerType)(int)data.DealerType;
            if (!string.IsNullOrWhiteSpace(data.HomeName))
                dealerData.HomeName = data.HomeName;
        }

        private static void ApplySupplierDefaults(
            S1NPCFramework.SupplierNPCData supplierData,
            SupplierDataBuilder.SupplierConfigData data)
        {
            supplierData.MinimumDeaddropOrderLimit = data.MinimumDeaddropOrderLimit;
            supplierData.MaximumDeaddropOrderLimit = data.MaximumDeaddropOrderLimit;
            supplierData.SupplierRecommendMessage = data.SupplierRecommendMessage;
            supplierData.SupplierUnlockHint = data.SupplierUnlockHint;

#if IL2CPPMELON
            IReadOnlyList<S1ItemFramework.StorableItemDefinition> deliveryItems =
                data.ResolveDeliveryItems();
            var listings = new Il2CppReferenceArray<Il2CppScheduleOne.UI.Phone.PhoneShopInterface.Listing>(deliveryItems.Count);
            for (int i = 0; i < deliveryItems.Count; i++)
                listings[i] = new Il2CppScheduleOne.UI.Phone.PhoneShopInterface.Listing(deliveryItems[i]);
#else
            IReadOnlyList<S1ItemFramework.StorableItemDefinition> deliveryItems =
                data.ResolveDeliveryItems();
            var listings = new ScheduleOne.UI.Phone.PhoneShopInterface.Listing[deliveryItems.Count];
            for (int i = 0; i < deliveryItems.Count; i++)
                listings[i] = new ScheduleOne.UI.Phone.PhoneShopInterface.Listing(deliveryItems[i]);
#endif
            supplierData.DeliveryShopListings = listings;
        }

        private static S1NPCFramework.NPCData? GetOriginalData(S1NPCs.NPC? npc)
        {
            return npc == null ? null : GetDataObject(npc)?.GetOriginalData();
        }

        private static S1NPCFramework.NPCData? GetCurrentData(S1NPCs.NPC? npc)
        {
            if (npc == null)
                return null;

            return npc.NPCData ?? GetOriginalData(npc);
        }

        private static bool ApplyToData(S1NPCs.NPC? npc, Action<S1NPCFramework.NPCData> apply)
        {
            if (npc == null)
                return false;

            S1NPCFramework.NPCData? originalData = GetOriginalData(npc);
            S1NPCFramework.NPCData? currentData = npc.NPCData;
            if (originalData == null && currentData == null)
                return false;

            if (originalData != null)
                apply(originalData);
            if (currentData != null && !ReferenceEquals(currentData, originalData))
                apply(currentData);
            return true;
        }

        internal static S1NPCFramework.BaseNPCDataObject? GetDataObject(S1NPCs.NPC npc)
        {
            return ReflectionUtils.TryGetFieldOrProperty(
                npc,
                "_defaultNPCData") as S1NPCFramework.BaseNPCDataObject;
        }

        private static void SetDataObject(
            S1NPCs.NPC npc,
            S1NPCFramework.BaseNPCDataObject dataObject)
        {
            ReflectionUtils.TrySetFieldOrProperty(npc, "_defaultNPCData", dataObject);
        }

        private static void SetCurrentData(S1NPCs.NPC npc, S1NPCFramework.NPCData data)
        {
#if IL2CPPMELON
            npc.NPCData = data;
#else
            CurrentNpcDataField.SetValue(npc, data);
#endif
        }

        private static void PrepareData(S1NPCFramework.NPCData data)
        {
            if (data == null)
                throw new InvalidOperationException("The beta NPC data object returned no data.");

#if IL2CPPMELON
            data.Inventory.RandomInventoryItems ??=
                new Il2CppReferenceArray<S1NPCFramework.Inventory.WeightedItem>(0);
            data.Inventory.StartingInventoryItems ??=
                new Il2CppReferenceArray<S1ItemFramework.ItemDefinition>(0);
            data.Messaging.ConversationCategories ??=
                new Il2CppStructArray<S1Messaging.EConversationCategory>(0);

            if (CrossType.Is(data, out S1NPCFramework.SupplierNPCData supplierData))
                supplierData.DeliveryShopListings ??=
                    new Il2CppReferenceArray<Il2CppScheduleOne.UI.Phone.PhoneShopInterface.Listing>(0);
#else
            data.Inventory.RandomInventoryItems ??= Array.Empty<S1NPCFramework.Inventory.WeightedItem>();
            data.Inventory.StartingInventoryItems ??= Array.Empty<ScheduleOne.ItemFramework.ItemDefinition>();
            data.Messaging.ConversationCategories ??= Array.Empty<ScheduleOne.Messaging.EConversationCategory>();

            if (CrossType.Is(data, out S1NPCFramework.SupplierNPCData supplierData))
                supplierData.DeliveryShopListings ??= Array.Empty<ScheduleOne.UI.Phone.PhoneShopInterface.Listing>();
#endif

            EnsureMovementDefaults(data);
        }

        /// <summary>
        /// A freshly created NPCDataObject has no MovementPreset assigned, so
        /// NPCData.Movement falls back to the hardcoded class default (WalkSpeed 1.8),
        /// which is noticeably faster than every vanilla NPC archetype - they're all tuned
        /// to a slower shared preset (observed: WalkSpeed 1.2). Source the real value from
        /// any loaded preset so custom NPCs match vanilla walking pace.
        /// </summary>
        private static void EnsureMovementDefaults(S1NPCFramework.NPCData data)
        {
            S1NPCFramework.Movement movement = data.Movement;
            if (movement == null)
                return;

            S1NPCFramework.Movement? donor = Resources
                .FindObjectsOfTypeAll<S1NPCFramework.MovementPreset>()
                .Select(preset => preset?.GetValue())
                .FirstOrDefault(value => value != null);

            movement.WalkSpeed = donor?.WalkSpeed ?? 1.2f;
            movement.MaxSpeed = donor?.MaxSpeed ?? movement.MaxSpeed;
        }

        private static void EnsureDialogueDatabase(
            S1NPCFramework.NPCData data,
            S1NPCs.NPC? sourceNpc = null)
        {
            if (data.Dialogue == null)
                throw new InvalidOperationException("The beta NPC data has no dialogue settings.");

            if (data.Dialogue.DialogueDatabase != null)
                return;

            S1NPCFramework.NPCData? sourceData = GetOriginalData(sourceNpc) ?? GetCurrentData(sourceNpc);
            bool sourceIsEmployee = sourceNpc is S1Employees.Employee;
            if (ShouldReuseSourceDialogueDatabase(sourceIsEmployee)
                && sourceData?.Dialogue?.DialogueDatabase != null)
            {
                data.Dialogue.DialogueDatabase = sourceData.Dialogue.DialogueDatabase;
            }

            if (data.Dialogue.DialogueDatabase == null)
            {
                S1Dialogue.DialogueManager manager =
                    S1DevUtilities.Singleton<S1Dialogue.DialogueManager>.Instance;
                if (manager != null)
                    data.Dialogue.DialogueDatabase = manager.DefaultDialogueDatabase;
            }

            if (data.Dialogue.DialogueDatabase == null)
            {
                S1Dialogue.DialogueDatabase[] databases =
                    Resources.FindObjectsOfTypeAll<S1Dialogue.DialogueDatabase>();
                if (databases.Length > 0)
                    data.Dialogue.DialogueDatabase = databases[0];
            }

            if (data.Dialogue.DialogueDatabase == null)
                throw new InvalidOperationException("No dialogue database is loaded for the custom NPC.");

            if (sourceIsEmployee)
            {
                Logger.Debug(
                    $"[S1API][BaseEmployeeFallback][Dialogue] Rebased employee source dialogue " +
                    $"'{sourceData?.Dialogue?.DialogueDatabase?.name ?? "<null>"}' to " +
                    $"'{data.Dialogue.DialogueDatabase.name}' for the replacement NPC data.");
            }
        }

        /// <summary>
        /// Determines whether a source NPC's dialogue database may be inherited by a rebuilt custom NPC.
        /// Employee databases contain employee-only greeting and transfer content, so the BaseEmployee
        /// fallback must instead resolve the native default database for the replacement NPC role.
        /// </summary>
        internal static bool ShouldReuseSourceDialogueDatabase(bool sourceIsEmployee) => !sourceIsEmployee;

        private static void EnsureSupplierDialogueDatabase(
            S1NPCFramework.NPCData data,
            bool required)
        {
            var dialogue = data.Dialogue;
            if (dialogue == null)
            {
                if (required)
                    throw new InvalidOperationException("The beta supplier NPC data has no dialogue settings.");

                return;
            }

            S1Dialogue.DialogueDatabase? current = dialogue.DialogueDatabase;
            if (current != null
                && current.name.StartsWith("S1API_SupplierDialogue_", StringComparison.Ordinal)
                && HasRequiredSupplierDialogue(current))
            {
                return;
            }

            S1Dialogue.DialogueDatabase? donor = Resources
                .FindObjectsOfTypeAll<S1Dialogue.DialogueDatabase>()
                .Where(HasRequiredSupplierDialogue)
                .OrderBy(database => database.name, StringComparer.Ordinal)
                .FirstOrDefault();

            if (donor == null)
            {
                if (required)
                {
                    throw new InvalidOperationException(
                        "No loaded supplier dialogue database contains the native supplier meeting entries.");
                }

                return;
            }

            S1Dialogue.DialogueDatabase clone = UnityEngine.Object.Instantiate(donor);
            clone.name = "S1API_SupplierDialogue_" + donor.name;
            clone.hideFlags = HideFlags.DontUnloadUnusedAsset;
            ApplyGenericSupplierDialogue(clone);
            dialogue.DialogueDatabase = clone;
        }

        private static bool HasRequiredSupplierDialogue(S1Dialogue.DialogueDatabase database)
        {
            if (database?.GenericEntries == null)
                return false;

            var requiredKeys = new HashSet<string>(StringComparer.Ordinal)
            {
                "supplier_unlocked",
                "supplier_meet_confirm",
                "supplier_meeting_greeting",
                "meeting_order_complete",
                "supplier_meetings_unlocked",
                "supplier_deliveries_unlocked"
            };

            foreach (S1Dialogue.Entry entry in database.GenericEntries)
            {
                if (!string.IsNullOrEmpty(entry.Key))
                    requiredKeys.Remove(entry.Key);
            }

            return requiredKeys.Count == 0;
        }

        private static void ApplyGenericSupplierDialogue(S1Dialogue.DialogueDatabase database)
        {
            SetSupplierDialogueLines(
                database,
                "supplier_unlocked",
                "I've heard you're looking for supplies.",
                "Send me a message when you'd like to place an order. You can pay off the balance later.");
            SetSupplierDialogueLines(
                database,
                "supplier_meet_confirm",
                "Agreed. I'll be <LOCATION> for the next 6 hours.");
            SetSupplierDialogueLines(
                database,
                "supplier_meeting_greeting",
                "Ready to look over the supplies?");
            SetSupplierDialogueLines(
                database,
                "meeting_order_complete",
                "Good doing business with you.");
            SetSupplierDialogueLines(
                database,
                "supplier_meetings_unlocked",
                "You've proven reliable, so we can now arrange in-person meetings for larger orders.",
                "Send me a message when you'd like to meet.");
            SetSupplierDialogueLines(
                database,
                "supplier_deliveries_unlocked",
                "I can now deliver supplies directly to your properties. Use the deliveries app to place an order.");
        }

        private static void SetSupplierDialogueLines(
            S1Dialogue.DialogueDatabase database,
            string key,
            params string[] values)
        {
            if (database?.GenericEntries == null)
                return;

            foreach (S1Dialogue.Entry entry in database.GenericEntries)
            {
                if (!string.Equals(entry.Key, key, StringComparison.Ordinal) || entry.Chains == null)
                    continue;

                for (int i = 0; i < entry.Chains.Length; i++)
                {
                    S1Dialogue.DialogueChain? chain = entry.Chains[i];
                    if (chain == null)
                        continue;

#if IL2CPPMELON
                    var lines = new Il2CppStringArray(values.Length);
                    for (int lineIndex = 0; lineIndex < values.Length; lineIndex++)
                        lines[lineIndex] = values[lineIndex];
                    chain.Lines = lines;
#else
                    chain.Lines = values.ToArray();
#endif
                }

                return;
            }
        }

        internal static bool EnsureDealerDialogueDefaults(S1NPCs.NPC npc, bool logFailure = true)
        {
            if (npc == null)
                return false;

            var targets = new List<S1NPCFramework.DealerNPCData>();
            AddDealerDataTarget(targets, GetOriginalData(npc));
            AddDealerDataTarget(targets, GetCurrentData(npc));
            if (CrossType.Is(npc, out S1Economy.Dealer dealer)
                && dealer.DealerData != null)
                targets.Add(dealer.DealerData);

            bool ready = targets.Count > 0;
            foreach (S1NPCFramework.DealerNPCData target in targets)
                ready &= PopulateDealerDialogueDefaults(target);

            if (!ready && logFailure)
            {
                Logger.Error(
                    $"Dealer '{GetId(npc)}' has no complete native dealer dialogue template. " +
                    "Recruitment, cash collection, or customer assignment dialogue may be unavailable.");
            }

            return ready;
        }

        private static void AddDealerDataTarget(
            ICollection<S1NPCFramework.DealerNPCData> targets,
            S1NPCFramework.NPCData? data)
        {
            if (data != null
                && CrossType.Is(data, out S1NPCFramework.DealerNPCData dealerData))
            {
                targets.Add(dealerData);
            }
        }

        internal static bool HasCompleteDealerDialogueSet(
            bool hasRecruitDialogue,
            bool hasCollectCashDialogue,
            bool hasAssignCustomersDialogue) =>
            hasRecruitDialogue && hasCollectCashDialogue && hasAssignCustomersDialogue;

        // Despite the dealer context, this is the stable name of the vanilla recruitment asset.
        internal const string DealerRecruitDialogueName = "Supplier_Recruitment";
        internal const string DealerCollectCashDialogueName = "Dealer_CollectCash";
        internal const string DealerAssignCustomersDialogueName = "Dealer_AssignCustomers";

        private static S1Dialogue.Conversation? _fallbackRecruitDialogue;
        private static S1Dialogue.Conversation? _fallbackCollectCashDialogue;
        private static S1Dialogue.Conversation? _fallbackAssignCustomersDialogue;
        private static S1Dialogue.Conversation? _cachedRecruitDialogue;
        private static S1Dialogue.Conversation? _cachedCollectCashDialogue;
        private static S1Dialogue.Conversation? _cachedAssignCustomersDialogue;

        private static bool PopulateDealerDialogueDefaults(S1NPCFramework.DealerNPCData dealerData)
        {
            if (HasCompleteDealerDialogueSet(
                    dealerData.RecruitDialogue != null,
                    dealerData.CollectCashDialogue != null,
                    dealerData.AssignCustomersDialogue != null))
            {
                CacheDealerDialogueDefaults(dealerData);
                return true;
            }

            if (TryCopyCachedDealerDialogueDefaults(dealerData))
                return true;

            // DealerNPCDataObject assets are no longer reliably enumerated in 0.4.6, but the
            // loaded native Dealer components still expose their complete current/original data.
            foreach (S1Economy.Dealer donorDealer in
                     Resources.FindObjectsOfTypeAll<S1Economy.Dealer>())
            {
                if (donorDealer == null)
                    continue;

                S1NPCFramework.DealerNPCData? source = donorDealer.DealerData;
                if (source == null)
                    TryGetDealerData(GetOriginalData(donorDealer), out source);
                if (!TryCopyDealerDialogueDefaults(dealerData, source))
                    continue;

                CacheDealerDialogueDefaults(source!);
                return true;
            }

            S1NPCFramework.DealerNPCDataObject[] donors =
                Resources.FindObjectsOfTypeAll<S1NPCFramework.DealerNPCDataObject>();
            foreach (S1NPCFramework.DealerNPCDataObject donor in donors)
            {
                if (donor == null)
                    continue;

                TryGetDealerData(donor.GetOriginalData(), out S1NPCFramework.DealerNPCData? source);
                if (source == null)
                    TryGetDealerData(donor.GetRuntimeData(), out source);
                if (!TryCopyDealerDialogueDefaults(dealerData, source))
                    continue;

                CacheDealerDialogueDefaults(source!);
                return true;
            }

            // 0.4.6 no longer guarantees that native DealerNPCDataObject assets are surfaced by
            // FindObjectsOfTypeAll. Their referenced DialogueContainer assets are still loaded for
            // base-game dealers, so resolve the same three vanilla assets by stable asset name.
            S1Dialogue.Conversation[] dialogues =
                Resources.FindObjectsOfTypeAll<S1Dialogue.Conversation>();
            if (dealerData.RecruitDialogue == null)
                dealerData.RecruitDialogue =
                    FindDialogueContainer(dialogues, DealerRecruitDialogueName);
            if (dealerData.CollectCashDialogue == null)
                dealerData.CollectCashDialogue =
                    FindDialogueContainer(dialogues, DealerCollectCashDialogueName);
            if (dealerData.AssignCustomersDialogue == null)
                dealerData.AssignCustomersDialogue =
                    FindDialogueContainer(dialogues, DealerAssignCustomersDialogueName);

            // On 0.4.6 the three containers can be absent from Unity's loaded-object set until a
            // native dealer has already initialized. Custom NPCs spawn earlier than that in some
            // saves, so preserve the native dialogue graph as an always-available final fallback.
            if (dealerData.RecruitDialogue == null)
                dealerData.RecruitDialogue = GetFallbackRecruitDialogue();
            if (dealerData.CollectCashDialogue == null)
                dealerData.CollectCashDialogue = GetFallbackCollectCashDialogue();
            if (dealerData.AssignCustomersDialogue == null)
                dealerData.AssignCustomersDialogue = GetFallbackAssignCustomersDialogue();

            bool ready = HasCompleteDealerDialogueSet(
                dealerData.RecruitDialogue != null,
                dealerData.CollectCashDialogue != null,
                dealerData.AssignCustomersDialogue != null);
            if (!ready)
            {
                Logger.Error(
                    "[S1API][DealerDialogue] Failed to persist fallback containers on DealerNPCData. " +
                    $"RecruitManagedNull={ReferenceEquals(dealerData.RecruitDialogue, null)}, " +
                    $"CollectManagedNull={ReferenceEquals(dealerData.CollectCashDialogue, null)}, " +
                    $"AssignManagedNull={ReferenceEquals(dealerData.AssignCustomersDialogue, null)}, " +
                    $"FallbackRecruitManagedNull={ReferenceEquals(_fallbackRecruitDialogue, null)}, " +
                    $"FallbackCollectManagedNull={ReferenceEquals(_fallbackCollectCashDialogue, null)}, " +
                    $"FallbackAssignManagedNull={ReferenceEquals(_fallbackAssignCustomersDialogue, null)}.");
            }
            else
            {
                CacheDealerDialogueDefaults(dealerData);
            }

            return ready;
        }

        private static bool TryCopyCachedDealerDialogueDefaults(
            S1NPCFramework.DealerNPCData target)
        {
            if (!HasCompleteDealerDialogueSet(
                    _cachedRecruitDialogue != null,
                    _cachedCollectCashDialogue != null,
                    _cachedAssignCustomersDialogue != null))
            {
                return false;
            }

            if (target.RecruitDialogue == null)
                target.RecruitDialogue = _cachedRecruitDialogue;
            if (target.CollectCashDialogue == null)
                target.CollectCashDialogue = _cachedCollectCashDialogue;
            if (target.AssignCustomersDialogue == null)
                target.AssignCustomersDialogue = _cachedAssignCustomersDialogue;

            return HasCompleteDealerDialogueSet(
                target.RecruitDialogue != null,
                target.CollectCashDialogue != null,
                target.AssignCustomersDialogue != null);
        }

        private static void CacheDealerDialogueDefaults(
            S1NPCFramework.DealerNPCData source)
        {
            if (!HasCompleteDealerDialogueSet(
                    source.RecruitDialogue != null,
                    source.CollectCashDialogue != null,
                    source.AssignCustomersDialogue != null))
            {
                return;
            }

            _cachedRecruitDialogue = source.RecruitDialogue;
            _cachedCollectCashDialogue = source.CollectCashDialogue;
            _cachedAssignCustomersDialogue = source.AssignCustomersDialogue;
        }

        private static bool TryGetDealerData(
            S1NPCFramework.NPCData? data,
            out S1NPCFramework.DealerNPCData? dealerData)
        {
            if (data != null
                && CrossType.Is(data, out S1NPCFramework.DealerNPCData typedData))
            {
                dealerData = typedData;
                return true;
            }

            dealerData = null;
            return false;
        }

        private static bool TryCopyDealerDialogueDefaults(
            S1NPCFramework.DealerNPCData target,
            S1NPCFramework.DealerNPCData? source)
        {
            if (source == null
                || !HasCompleteDealerDialogueSet(
                    source.RecruitDialogue != null,
                    source.CollectCashDialogue != null,
                    source.AssignCustomersDialogue != null))
            {
                return false;
            }

            if (target.RecruitDialogue == null)
                target.RecruitDialogue = source.RecruitDialogue;
            if (target.CollectCashDialogue == null)
                target.CollectCashDialogue = source.CollectCashDialogue;
            if (target.AssignCustomersDialogue == null)
                target.AssignCustomersDialogue = source.AssignCustomersDialogue;
            return HasCompleteDealerDialogueSet(
                target.RecruitDialogue != null,
                target.CollectCashDialogue != null,
                target.AssignCustomersDialogue != null);
        }

        private static S1Dialogue.Conversation? FindDialogueContainer(
            IEnumerable<S1Dialogue.Conversation> dialogues,
            string name) =>
            dialogues.FirstOrDefault(dialogue =>
                dialogue != null
                && string.Equals(dialogue.name, name, StringComparison.Ordinal));

        private static S1Dialogue.Conversation GetFallbackRecruitDialogue()
        {
            if (_fallbackRecruitDialogue != null)
                return _fallbackRecruitDialogue;

            const string entryGuid = "e53bd440-29c4-4dce-a5cd-7415dcda83f8";
            const string confirmGuid = "4854545e-b2f9-4cba-9703-94e68c058352";
            const string exitGuid = "1575094b-13a3-4d54-9c06-136b571f8c51";
            const string acceptedGuid = "dbffb23a-034f-4d0f-9815-36cc247335b6";

            S1Dialogue.Conversation dialogue = CreateDialogueContainer(
                DealerRecruitDialogueName);
            AddDialogueNode(
                dialogue,
                CreateDialogueNode(
                    entryGuid,
                    "Alright, I'll be a dealer for you but there is a <SIGNING_FEE> signing fee, " +
                    "and I'll take a <CUT> cut of each sale. We got a deal?",
                    "ENTRY",
                    new Vector2(416f, 458f),
                    CreateDialogueChoices(
                        CreateDialogueChoice(confirmGuid, "Deal (<SIGNING_FEE>)", "CONFIRM"),
                        CreateDialogueChoice(exitGuid, "Nevermind", string.Empty))));
            AddDialogueNode(
                dialogue,
                CreateDialogueNode(
                    acceptedGuid,
                    "Thank you. Bring me some product and assign some customers to me, and I'll get to work.",
                    string.Empty,
                    new Vector2(1313f, 566f),
                    CreateDialogueChoices()));
            AddNodeLink(dialogue, entryGuid, confirmGuid, acceptedGuid);

            _fallbackRecruitDialogue = dialogue;
            return dialogue;
        }

        private static S1Dialogue.Conversation GetFallbackCollectCashDialogue()
        {
            if (_fallbackCollectCashDialogue != null)
                return _fallbackCollectCashDialogue;

            S1Dialogue.Conversation dialogue = CreateDialogueContainer(
                DealerCollectCashDialogueName);
            AddDialogueNode(
                dialogue,
                CreateDialogueNode(
                    "4057413b-2c8a-411d-a9a0-d8e606558846",
                    "No worries, here you are.",
                    "ENTRY",
                    new Vector2(765f, 412f),
                    CreateDialogueChoices()));

            _fallbackCollectCashDialogue = dialogue;
            return dialogue;
        }

        private static S1Dialogue.Conversation GetFallbackAssignCustomersDialogue()
        {
            if (_fallbackAssignCustomersDialogue != null)
                return _fallbackAssignCustomersDialogue;

            S1Dialogue.Conversation dialogue = CreateDialogueContainer(
                DealerAssignCustomersDialogueName);
            AddDialogueNode(
                dialogue,
                CreateDialogueNode(
                    "56a2eae5-0e64-4ca1-b296-148df22793f9",
                    "You can assign customers to me with the dealer management app on your phone.",
                    "ENTRY",
                    new Vector2(622f, 287f),
                    CreateDialogueChoices()));

            _fallbackAssignCustomersDialogue = dialogue;
            return dialogue;
        }

        private static S1Dialogue.Conversation CreateDialogueContainer(string name)
        {
            S1Dialogue.Conversation dialogue =
                ScriptableObject.CreateInstance<S1Dialogue.Conversation>();
            if (dialogue == null)
                throw new InvalidOperationException($"Failed to create dealer dialogue '{name}'.");

            dialogue.name = name;
            dialogue.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return dialogue;
        }

        private static S1Dialogue.DialogueNodeData CreateDialogueNode(
            string guid,
            string text,
            string label,
            Vector2 position,
#if IL2CPPMELON
            Il2CppReferenceArray<S1Dialogue.DialogueChoiceData> choices)
#else
            S1Dialogue.DialogueChoiceData[] choices)
#endif
        {
            return new S1Dialogue.DialogueNodeData
            {
                Guid = guid,
                DialogueText = text,
                DialogueNodeLabel = label,
                Position = position,
                choices = choices,
                VoiceLine = S1VoiceOver.EVOLineType.None
            };
        }

        private static S1Dialogue.DialogueChoiceData CreateDialogueChoice(
            string guid,
            string text,
            string label) =>
            new S1Dialogue.DialogueChoiceData
            {
                Guid = guid,
                ChoiceText = text,
                ChoiceLabel = label,
                ShowWorldspaceDialogue = true
            };

#if IL2CPPMELON
        private static Il2CppReferenceArray<S1Dialogue.DialogueChoiceData> CreateDialogueChoices(
            params S1Dialogue.DialogueChoiceData[] choices)
        {
            var result = new Il2CppReferenceArray<S1Dialogue.DialogueChoiceData>(choices.Length);
            for (int i = 0; i < choices.Length; i++)
                result[i] = choices[i];
            return result;
        }
#else
        private static S1Dialogue.DialogueChoiceData[] CreateDialogueChoices(
            params S1Dialogue.DialogueChoiceData[] choices) => choices;
#endif

        private static void AddDialogueNode(
            S1Dialogue.Conversation dialogue,
            S1Dialogue.DialogueNodeData node)
        {
            if (dialogue.DialogueNodeData == null)
            {
#if IL2CPPMELON
                dialogue.DialogueNodeData =
                    new Il2CppSystem.Collections.Generic.List<S1Dialogue.DialogueNodeData>();
#else
                dialogue.DialogueNodeData = new List<S1Dialogue.DialogueNodeData>();
#endif
            }

            dialogue.DialogueNodeData.Add(node);
        }

        private static void AddNodeLink(
            S1Dialogue.Conversation dialogue,
            string baseNodeGuid,
            string baseChoiceGuid,
            string targetNodeGuid)
        {
            if (dialogue.NodeLinks == null)
            {
#if IL2CPPMELON
                dialogue.NodeLinks =
                    new Il2CppSystem.Collections.Generic.List<S1Dialogue.NodeLinkData>();
#else
                dialogue.NodeLinks = new List<S1Dialogue.NodeLinkData>();
#endif
            }

            dialogue.NodeLinks.Add(
                new S1Dialogue.NodeLinkData
                {
                    BaseDialogueOrBranchNodeGuid = baseNodeGuid,
                    BaseChoiceOrOptionGUID = baseChoiceGuid,
                    TargetNodeGuid = targetNodeGuid
                });
        }
    }
}
