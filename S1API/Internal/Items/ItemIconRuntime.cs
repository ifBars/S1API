#if IL2CPPMELON
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
using S1UI = Il2CppScheduleOne.UI;
using S1UIShop = Il2CppScheduleOne.UI.Shop;
#elif MONOMELON
using S1ItemFramework = ScheduleOne.ItemFramework;
using S1UI = ScheduleOne.UI;
using S1UIShop = ScheduleOne.UI.Shop;
#endif

using System;
using S1API.Logging;
using UnityEngine;

namespace S1API.Internal.Items
{
    /// <summary>
    /// Keeps already-bound native UI synchronized with late item icon changes.
    /// </summary>
    internal static class ItemIconRuntime
    {
        private static readonly Log Logger = new Log("ItemIconRuntime");

        internal static void SetIcon(
            S1ItemFramework.ItemDefinition definition,
            Sprite icon)
        {
            definition.Icon = icon;
            StoredItemPlaceholder.RefreshIcon(definition.ID, icon);
            RefreshVisibleUis(definition);
        }

        internal static void RefreshVisibleUis(
            S1ItemFramework.ItemDefinition definition)
        {
            if (definition == null)
                return;

            RefreshBoundItemSlots(definition.ID);
            RefreshBoundShopListings(definition.ID, definition.Icon);
        }

        private static void RefreshBoundItemSlots(string itemId)
        {
            try
            {
                S1UI.ItemSlotUI[] slotUis =
                    UnityEngine.Object.FindObjectsOfType<S1UI.ItemSlotUI>(
                        includeInactive: true);
                for (int index = 0; index < slotUis.Length; index++)
                {
                    S1UI.ItemSlotUI slotUi = slotUis[index];
                    if (slotUi == null ||
                        !MatchesItemId(
                            slotUi.assignedSlot?.ItemInstance?.ID,
                            itemId))
                    {
                        continue;
                    }

                    slotUi.UpdateUI();
                }
            }
            catch (Exception exception)
            {
                Logger.Warning(
                    $"Could not refresh bound item slots for " +
                    $"'{itemId}': {exception.Message}");
            }
        }

        private static void RefreshBoundShopListings(
            string itemId,
            Sprite icon)
        {
            try
            {
                S1UIShop.ListingUI[] listingUis =
                    UnityEngine.Object.FindObjectsOfType<S1UIShop.ListingUI>(
                        includeInactive: true);
                for (int index = 0; index < listingUis.Length; index++)
                {
                    S1UIShop.ListingUI listingUi = listingUis[index];
                    if (listingUi == null ||
                        listingUi.Icon == null ||
                        !MatchesItemId(
                            listingUi.Listing?.Item?.ID,
                            itemId))
                    {
                        continue;
                    }

                    listingUi.Icon.sprite = icon;
                }
            }
            catch (Exception exception)
            {
                Logger.Warning(
                    $"Could not refresh bound shop listings for " +
                    $"'{itemId}': {exception.Message}");
            }
        }

        internal static bool MatchesItemId(
            string? candidateItemId,
            string? targetItemId) =>
            !string.IsNullOrWhiteSpace(candidateItemId) &&
            !string.IsNullOrWhiteSpace(targetItemId) &&
            string.Equals(
                candidateItemId,
                targetItemId,
                StringComparison.OrdinalIgnoreCase);
    }
}
