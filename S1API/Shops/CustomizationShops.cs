using System;
using System.Collections.Generic;
using S1API.Avatar;
using S1API.Internal.Shops;
using S1API.Logging;

namespace S1API.Shops
{
    /// <summary>
    /// Lists mod-defined options in the character customization shops.
    /// </summary>
    /// <remarks>
    /// An option registered before a shop is built is cloned in while the shop builds. An option registered later
    /// is added to the shop that already exists. Disposing the handle removes it again.
    /// </remarks>
    public static class CustomizationShops
    {
        private static readonly Log Logger = new Log("CustomizationShops");
        private static readonly object Gate = new object();
        private static readonly List<Entry> Entries = new List<Entry>();

        /// <summary>
        /// INTERNAL: Receives log lines (level, message) instead of the MelonLoader log in unit tests.
        /// </summary>
        internal static Action<string, string>? LogSinkForTesting;

        /// <summary>
        /// INTERNAL: Skips touching live shop UI (used by unit tests, where no Unity runtime exists).
        /// </summary>
        internal static bool SkipLiveShopsForTesting;

        /// <summary>
        /// Lists an option in a customization shop.
        /// </summary>
        /// <param name="shop">The shop to list the option in.</param>
        /// <param name="spec">The option. It is copied; later changes to <paramref name="spec"/> have no effect.</param>
        /// <returns>
        /// A handle that removes the option when disposed, also from a shop that is already built.
        /// If the option cannot be listed (unsupported shop, same avatar object already registered for the category)
        /// a warning is logged and a handle that does nothing is returned.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="spec"/> is null.</exception>
        /// <exception cref="ArgumentException">Category, Label or AvatarObjectId is empty, or Price is negative.</exception>
        public static IDisposable AddOption(CustomizationShop shop, CustomizationOptionSpec spec)
        {
            if (spec == null)
                throw new ArgumentNullException(nameof(spec));
            if (string.IsNullOrWhiteSpace(spec.Category))
                throw new ArgumentException("Category is required.", nameof(spec));
            if (string.IsNullOrWhiteSpace(spec.Label))
                throw new ArgumentException("Label is required.", nameof(spec));
            if (string.IsNullOrWhiteSpace(spec.AvatarObjectId))
                throw new ArgumentException("AvatarObjectId is required.", nameof(spec));
            if (spec.Price < 0f)
                throw new ArgumentException("Price must not be negative.", nameof(spec));

            if (shop != CustomizationShop.Tattoo && shop != CustomizationShop.Barber)
            {
                Write("warn", $"AddOption for shop {shop} is not supported (only Tattoo and Barber); '{spec.Label}' is not listed.");
                return NoopHandle.Instance;
            }

            if (!(spec.Icon is null))
                Write("warn", $"Option '{spec.Label}': Icon is ignored, the vanilla option button has no icon slot.");

            var entry = new Entry(shop, spec);
            lock (Gate)
            {
                foreach (var existing in Entries)
                {
                    if (existing.Shop == shop
                        && string.Equals(existing.Category, entry.Category, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(existing.AvatarObjectId, entry.AvatarObjectId, StringComparison.OrdinalIgnoreCase))
                    {
                        Write("warn", $"Option '{spec.Label}' ({spec.AvatarObjectId}) is already registered for {shop}/{spec.Category}; the second registration is ignored.");
                        return NoopHandle.Instance;
                    }
                }
                Entries.Add(entry);
            }

            Write("info", $"Registered option '{entry.Label}' ({entry.AvatarObjectId}) for {shop}/{entry.Category}.");
            if (!SkipLiveShopsForTesting)
                CustomizationShopBuilder.OnEntryAdded(entry);
            return new Handle(entry);
        }

        /// <summary>
        /// INTERNAL: The active entries of one shop.
        /// </summary>
        internal static List<Entry> EntriesFor(CustomizationShop shop)
        {
            var result = new List<Entry>();
            lock (Gate)
            {
                foreach (var entry in Entries)
                    if (entry.Shop == shop)
                        result.Add(entry);
            }
            return result;
        }

        /// <summary>
        /// INTERNAL: Number of active entries (unit tests).
        /// </summary>
        internal static int ActiveCountForTesting
        {
            get { lock (Gate) return Entries.Count; }
        }

        /// <summary>
        /// INTERNAL: Drops all entries (unit tests).
        /// </summary>
        internal static void ResetForTesting()
        {
            lock (Gate) Entries.Clear();
        }

        private static void Remove(Entry entry)
        {
            bool removed;
            lock (Gate) removed = Entries.Remove(entry);
            if (!removed)
                return;

            Write("info", $"Removed option '{entry.Label}' ({entry.AvatarObjectId}) from {entry.Shop}/{entry.Category}.");
            if (!SkipLiveShopsForTesting)
                CustomizationShopBuilder.OnEntryRemoved(entry);
        }

        internal static void Write(string level, string message)
        {
            if (LogSinkForTesting != null)
            {
                LogSinkForTesting(level, message);
                return;
            }

            switch (level)
            {
                case "error": Logger.Error(message); break;
                case "warn": Logger.Warning(message); break;
                default: Logger.Msg(message); break;
            }
        }

        /// <summary>
        /// INTERNAL: One registered option, copied from its spec.
        /// </summary>
        internal sealed class Entry
        {
            internal Entry(CustomizationShop shop, CustomizationOptionSpec spec)
            {
                Shop = shop;
                Category = spec.Category.Trim();
                Label = spec.Label;
                Price = spec.Price;
                AvatarObjectId = spec.AvatarObjectId.Trim();
                Apply = spec.Apply;
            }

            internal CustomizationShop Shop { get; }
            internal string Category { get; }
            internal string Label { get; }
            internal float Price { get; }
            internal string AvatarObjectId { get; }
            internal Action<BasicAvatarSettings>? Apply { get; }
        }

        private sealed class Handle : IDisposable
        {
            private Entry? _entry;

            internal Handle(Entry entry) => _entry = entry;

            public void Dispose()
            {
                var entry = _entry;
                _entry = null;
                if (entry != null)
                    Remove(entry);
            }
        }

        private sealed class NoopHandle : IDisposable
        {
            internal static readonly NoopHandle Instance = new NoopHandle();

            public void Dispose()
            {
            }
        }
    }
}
