#if (IL2CPPMELON)
using S1UICustomization = Il2CppScheduleOne.UI.CharacterCustomization;
using S1Avatar = Il2CppScheduleOne.Avatar;
using S1CoreAvatar = Il2CppScheduleOne.Core.Avatar;
using S1PlayerScripts = Il2CppScheduleOne.PlayerScripts;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
#elif MONOMELON
using S1UICustomization = ScheduleOne.UI.CharacterCustomization;
using S1Avatar = ScheduleOne.Avatar;
using S1CoreAvatar = ScheduleOne.Core.Avatar;
using S1PlayerScripts = ScheduleOne.PlayerScripts;
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using HarmonyLib;
using MelonLoader;
using S1API.Avatar;
using S1API.Logging;
using S1API.Shops;
using UnityEngine;
using UnityEngine.Events;

namespace S1API.Internal.Shops
{
    /// <summary>
    /// INTERNAL: Clones one option button per registered <see cref="CustomizationShops"/> entry into the vanilla
    /// shop categories, and removes them again.
    /// </summary>
    /// <remarks>
    /// While a category's Awake is about to run, the clone is parented next to the existing options so the vanilla
    /// Awake discovers, sorts and wires it like its own. For a category that is already built, the clone is wired to the
    /// category by hand and added to its option array.
    /// </remarks>
    internal static class CustomizationShopBuilder
    {
        private const string ClonePrefix = "S1API_Custom_";

        private static readonly Log Logger = new Log("CustomizationShops");
        private static readonly List<TrackedCategory> Categories = new List<TrackedCategory>();
        private static readonly List<BuiltOption> Built = new List<BuiltOption>();

        private sealed class TrackedCategory
        {
            internal CustomizationShop Shop;
            internal S1UICustomization.CharacterCustomizationCategory Category = null!;
            internal int InstanceId;
        }

        private sealed class BuiltOption
        {
            internal CustomizationShops.Entry Entry = null!;
            internal int CategoryId;
            internal S1UICustomization.CharacterCustomizationCategory Category = null!;
            internal S1UICustomization.CharacterCustomizationOption Option = null!;
        }

        /// <summary>
        /// Runs before a category's Awake: tracks the category and clones the registered options into it.
        /// </summary>
        internal static void OnCategoryAwake(S1UICustomization.CharacterCustomizationCategory category)
        {
            try
            {
                if (category == null)
                {
                    Logger.Warning("CharacterCustomizationCategory.Awake ran without a category instance; nothing listed.");
                    return;
                }

                CustomizationShop? shop = ResolveShop(category);
                if (shop == null)
                    return;

                Prune();
                int id = category.GetInstanceID();
                if (!IsTracked(id))
                    Categories.Add(new TrackedCategory { Shop = shop.Value, Category = category, InstanceId = id });

                int added = 0;
                foreach (var entry in CustomizationShops.EntriesFor(shop.Value))
                {
                    if (!CategoryMatches(category, entry))
                        continue;
                    if (BuildOption(category, entry, late: false))
                        added++;
                }

                if (added > 0)
                {
                    MoveNonOptionsLast(category);
                    Logger.Msg($"Listed {added} option(s) in {shop.Value} category '{category.CategoryName}'.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to list options in a customization category: {ex}");
            }
        }

        /// <summary>
        /// Adds a newly registered entry to every already-built matching category.
        /// </summary>
        internal static void OnEntryAdded(CustomizationShops.Entry entry)
        {
            try
            {
                Prune();
                int added = 0;
                foreach (var tracked in Categories.ToArray())
                {
                    if (tracked.Shop != entry.Shop || !CategoryMatches(tracked.Category, entry))
                        continue;
                    if (HasBuilt(entry, tracked.InstanceId))
                        continue;
                    if (BuildOption(tracked.Category, entry, late: true))
                    {
                        MoveNonOptionsLast(tracked.Category);
                        added++;
                    }
                }

                if (added == 0)
                    Logger.Msg($"Option '{entry.Label}' will be listed when the {entry.Shop} shop builds category '{entry.Category}'.");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to add option '{entry.Label}' to a built shop: {ex}");
            }
        }

        /// <summary>
        /// Removes the clones of an entry from every shop that was built with it.
        /// </summary>
        internal static void OnEntryRemoved(CustomizationShops.Entry entry)
        {
            try
            {
                for (int i = Built.Count - 1; i >= 0; i--)
                {
                    var built = Built[i];
                    if (built.Entry != entry)
                        continue;

                    Built.RemoveAt(i);
                    if (built.Option == null)
                    {
                        Logger.Msg($"Option '{entry.Label}' was already gone with its shop.");
                        continue;
                    }

                    if (built.Category != null)
                        SetOptions(built.Category, GetOptions(built.Category).FindAll(o => o != null && o != built.Option));

                    built.Option.gameObject.SetActive(false);
                    UnityEngine.Object.Destroy(built.Option.gameObject);
                    Logger.Msg($"Removed option '{entry.Label}' from {entry.Shop} category '{entry.Category}'.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to remove option '{entry.Label}' from a built shop: {ex}");
            }
        }

        private static CustomizationShop? ResolveShop(S1UICustomization.CharacterCustomizationCategory category)
        {
            if (category.GetComponentInParent<S1UICustomization.TattooShopUI>() != null)
                return CustomizationShop.Tattoo;
            if (category.GetComponentInParent<S1UICustomization.BarbershopUI>() != null)
                return CustomizationShop.Barber;
            return null;
        }

        private static bool CategoryMatches(S1UICustomization.CharacterCustomizationCategory category, CustomizationShops.Entry entry) =>
            CustomizationShops.KeyOf(category.CategoryName) == entry.CategoryKey;

        private static bool IsTracked(int instanceId)
        {
            foreach (var tracked in Categories)
                if (tracked.InstanceId == instanceId)
                    return true;
            return false;
        }

        private static bool HasBuilt(CustomizationShops.Entry entry, int categoryId)
        {
            foreach (var built in Built)
                if (built.Entry == entry && built.CategoryId == categoryId && built.Option != null)
                    return true;
            return false;
        }

        // Shop scenes can unload; destroyed categories and options are dropped from the bookkeeping.
        private static void Prune()
        {
            Categories.RemoveAll(c => c.Category == null);
            Built.RemoveAll(b => b.Option == null);
        }

        private static bool BuildOption(S1UICustomization.CharacterCustomizationCategory category, CustomizationShops.Entry entry, bool late)
        {
            var existing = category.GetComponentsInChildren<S1UICustomization.CharacterCustomizationOption>(true);
            S1UICustomization.CharacterCustomizationOption? template = null;
            for (int i = 0; i < existing.Length; i++)
            {
                var option = existing[i];
                if (option == null || option.name.StartsWith(ClonePrefix, StringComparison.Ordinal))
                    continue;
                if (option.AvatarObject != null
                    && string.Equals(option.AvatarObject.Id, entry.AvatarObjectId, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Warning($"Option '{entry.Label}': category '{category.CategoryName}' already lists avatar object '{entry.AvatarObjectId}'; not added again.");
                    return false;
                }

                if (template == null || (!template.gameObject.activeSelf && option.gameObject.activeSelf))
                    template = option;
            }

            if (template == null)
            {
                Logger.Warning($"Option '{entry.Label}': category '{category.CategoryName}' has no option to clone; not added.");
                return false;
            }

            if (!S1Avatar.AvatarObjectLibrary.TryGetAvatarObjectById(entry.AvatarObjectId, out S1CoreAvatar.AvatarObject avatarObject)
                || avatarObject == null)
            {
                Logger.Error($"Option '{entry.Label}': avatar object '{entry.AvatarObjectId}' is not in the avatar object library; not added.");
                return false;
            }

            Transform parent = template.transform.parent;
            var clone = UnityEngine.Object.Instantiate(template.gameObject, parent, false);
            clone.transform.localScale = Vector3.one;
            clone.name = ClonePrefix + entry.AvatarObjectId;

            var cloneOption = clone.GetComponent<S1UICustomization.CharacterCustomizationOption>();
            if (cloneOption == null)
            {
                UnityEngine.Object.Destroy(clone);
                Logger.Error($"Option '{entry.Label}': the cloned shop button has no option component; not added.");
                return false;
            }

            cloneOption.AvatarObject = avatarObject;
            cloneOption.Price = entry.Price;
            cloneOption.RequireLevel = false;
            ApplyLabels(cloneOption, entry);
            if (!clone.activeSelf)
                clone.SetActive(true);

            // The option's own Awake rewrites its labels from the avatar object; when that Awake has not run yet
            // (the shop is closed) the labels are set again once it has.
            if (!clone.activeInHierarchy)
                MelonCoroutines.Start(ReapplyLabelsAfterAwake(cloneOption, entry));

            if (entry.Apply != null)
                cloneOption.onSelect.AddListener((UnityAction)(() => InvokeApply(entry)));

            if (late)
                WireToCategory(category, cloneOption);

            Built.Add(new BuiltOption
            {
                Entry = entry,
                CategoryId = category.GetInstanceID(),
                Category = category,
                Option = cloneOption
            });
            Logger.Msg($"Added option '{entry.Label}' ({entry.AvatarObjectId}, {FormatPrice(entry.Price)}) to {entry.Shop} category '{category.CategoryName}'{(late ? " (shop already built)" : string.Empty)}.");
            return true;
        }

        private static void ApplyLabels(S1UICustomization.CharacterCustomizationOption option, CustomizationShops.Entry entry)
        {
            if (option.NameLabel != null)
                option.NameLabel.text = entry.Label;
            if (option.PriceLabel != null)
                option.PriceLabel.text = FormatPrice(entry.Price);
        }

        private static IEnumerator ReapplyLabelsAfterAwake(S1UICustomization.CharacterCustomizationOption option, CustomizationShops.Entry entry)
        {
            while (option != null && !option.gameObject.activeInHierarchy)
                yield return new WaitForSeconds(0.25f);

            yield return null;
            if (option == null)
                yield break;

            ApplyLabels(option, entry);
        }

        // Matches the "$250" the vanilla option shows (MoneyManager.FormatAmount); formatted locally with the
        // invariant culture because the modded runtime can run globalization-invariant.
        private static string FormatPrice(float price) =>
            price > 0f ? "$" + Mathf.RoundToInt(price).ToString("N0", CultureInfo.InvariantCulture) : "Free";

        private static void InvokeApply(CustomizationShops.Entry entry)
        {
            try
            {
                var appearance = S1PlayerScripts.Player.Local != null ? S1PlayerScripts.Player.Local.CurrentAppearance : null;
                if (appearance == null)
                {
                    Logger.Warning($"Option '{entry.Label}' was selected but the local player has no appearance; Apply skipped.");
                    return;
                }

                entry.Apply!(BasicAvatarSettings.FromPlayerAppearance(appearance));
                Logger.Msg($"Apply ran for option '{entry.Label}'.");
            }
            catch (Exception ex)
            {
                Logger.Error($"Apply of option '{entry.Label}' threw: {ex}");
            }
        }

        // The vanilla Awake lists a spacer after the options and sorts options assuming they are contiguous; options
        // appended after the spacer would split the list.
        private static void MoveNonOptionsLast(S1UICustomization.CharacterCustomizationCategory category)
        {
            var options = category.GetComponentsInChildren<S1UICustomization.CharacterCustomizationOption>(true);
            if (options.Length == 0)
                return;

            Transform content = options[0].transform.parent;
            if (content == null)
                return;

            var others = new List<Transform>();
            for (int i = 0; i < content.childCount; i++)
            {
                Transform child = content.GetChild(i);
                if (child.GetComponent<S1UICustomization.CharacterCustomizationOption>() == null)
                    others.Add(child);
            }

            foreach (var child in others)
                child.SetAsLastSibling();
        }

        // Late path: the category's Awake already ran, so repeat what it does for an option.
        private static void WireToCategory(S1UICustomization.CharacterCustomizationCategory category, S1UICustomization.CharacterCustomizationOption option)
        {
            option.onSelect.AddListener((UnityAction)(() => CallCategory(category, "OptionSelected", option)));
            option.onDeselect.AddListener((UnityAction)(() => CallCategory(category, "OptionDeselected", option)));
            option.onPurchase.AddListener((UnityAction)(() => CallCategory(category, "OptionPurchased", option)));

            var options = GetOptions(category);
            options.Add(option);
            SetOptions(category, options);
        }

        private static void CallCategory(S1UICustomization.CharacterCustomizationCategory category, string method, S1UICustomization.CharacterCustomizationOption option)
        {
            try
            {
                var info = AccessTools.Method(typeof(S1UICustomization.CharacterCustomizationCategory), method,
                    new[] { typeof(S1UICustomization.CharacterCustomizationOption) });
                if (info == null)
                {
                    Logger.Error($"CharacterCustomizationCategory.{method} was not found; option '{option.Name}' is not wired.");
                    return;
                }

                info.Invoke(category, new object[] { option });
            }
            catch (Exception ex)
            {
                Logger.Error($"CharacterCustomizationCategory.{method} failed for option '{option.Name}': {ex}");
            }
        }

        private static List<S1UICustomization.CharacterCustomizationOption> GetOptions(S1UICustomization.CharacterCustomizationCategory category)
        {
            var result = new List<S1UICustomization.CharacterCustomizationOption>();
#if (IL2CPPMELON)
            var array = category.options;
#else
            var array = AccessTools.Field(typeof(S1UICustomization.CharacterCustomizationCategory), "options")
                .GetValue(category) as S1UICustomization.CharacterCustomizationOption[];
#endif
            if (array == null)
                return result;
            for (int i = 0; i < array.Length; i++)
                if (array[i] != null)
                    result.Add(array[i]);
            return result;
        }

        private static void SetOptions(S1UICustomization.CharacterCustomizationCategory category, List<S1UICustomization.CharacterCustomizationOption> options)
        {
#if (IL2CPPMELON)
            category.options = new Il2CppReferenceArray<S1UICustomization.CharacterCustomizationOption>(options.ToArray());
#else
            AccessTools.Field(typeof(S1UICustomization.CharacterCustomizationCategory), "options")
                .SetValue(category, options.ToArray());
#endif
        }
    }
}
