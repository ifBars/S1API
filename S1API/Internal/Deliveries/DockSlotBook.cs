using System;
using System.Collections.Generic;

namespace S1API.Internal.Deliveries
{
    /// <summary>What <see cref="DockSlotBook{TDock}.Apply"/> did to a property's dock list.</summary>
    internal enum DockSlotChange
    {
        /// <summary>The slot already held the reported dock, or the registration has no slot yet.</summary>
        None,

        /// <summary>The reported dock was written into the registration's slot.</summary>
        Attached,

        /// <summary>The slot lost its live dock and now holds a placeholder.</summary>
        Released,

        /// <summary>The registration uses a dock that S1API did not add; the dock list is unchanged.</summary>
        Shared,

        /// <summary>The reported dock already sits in another slot; the registration keeps its own slot.</summary>
        Conflict,
    }

    /// <summary>What <see cref="DockSlotBook{TDock}.Release"/> did when a registration was disposed.</summary>
    internal enum DockSlotRelease
    {
        /// <summary>The registration never had a slot.</summary>
        NoSlot,

        /// <summary>The slot keeps its index and holds a placeholder until the same dock returns.</summary>
        Reserved,

        /// <summary>Another registration still uses the slot's dock.</summary>
        StillUsed,

        /// <summary>The dock was not added by S1API and stays where it is.</summary>
        NotOwned,
    }

    /// <summary>
    /// Keeps the index of every mod-registered loading dock stable for the game session.
    /// </summary>
    /// <remarks>
    /// Deliveries store their dock as an index into <c>Property.LoadingDocks</c>, and saves keep that index. A slot
    /// that S1API added therefore never moves: it keeps its index across scene reloads, also while its provider still
    /// returns null, and after its registration is disposed. A slot without a live dock holds a placeholder, so
    /// vanilla code that walks the array never reads a null entry. A reserved slot is reused only by the dock it held
    /// before (same object or same identity key). This class makes no Unity calls; the runtime injects dock comparison,
    /// liveness and placeholder creation.
    /// </remarks>
    internal sealed class DockSlotBook<TDock> where TDock : class
    {
        private readonly Dictionary<string, List<Slot>> _byCode = new Dictionary<string, List<Slot>>(StringComparer.OrdinalIgnoreCase);
        private readonly List<TDock> _placeholders = new List<TDock>();
        private readonly HashSet<string> _restoredThisScene = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Func<TDock, TDock, bool> _same;
        private readonly Func<TDock, bool> _isAlive;
        private readonly Action<string> _info;
        private readonly Action<string> _warn;
        private string? _save;

        internal DockSlotBook(Func<TDock, TDock, bool> same, Func<TDock, bool> isAlive, Action<string> info, Action<string> warn)
        {
            _same = same;
            _isAlive = isAlive;
            _info = info;
            _warn = warn;
        }

        /// <summary>
        /// Puts every slot S1API owns for <paramref name="code"/> back into <paramref name="docks"/>: its live dock
        /// when this scene has one, otherwise a placeholder. True when the list changed.
        /// </summary>
        internal bool Reserve(string code, List<TDock?> docks, Func<int, TDock> placeholder)
        {
            if (!_byCode.TryGetValue(code, out List<Slot>? slots) || slots.Count == 0)
                return false;

            bool changed = false;
            int restored = 0;
            var displaced = new List<Slot>();
            slots.Sort((a, b) => a.Index.CompareTo(b.Index));
            foreach (Slot slot in slots)
            {
                if (!slot.Owned)
                    continue;

                TDock? current = slot.Index < docks.Count ? docks[slot.Index] : null;
                if (current != null && !IsPlaceholder(current) && !IsSlotDock(slot, current))
                {
                    displaced.Add(slot);
                    continue;
                }

                // Neither a live dock nor a placeholder in this scene: the first reservation after a reload.
                if (slot.Live == null && slot.Placeholder == null)
                    restored++;
                if (slot.Live != null && !_isAlive(slot.Live))
                    slot.Live = null;
                if (Place(slot, docks, slot.Live, placeholder))
                    changed = true;
            }

            foreach (Slot slot in displaced)
            {
                int previous = slot.Index;
                slot.Index = docks.Count;
                slot.Live = null;
                slot.Placeholder = null;
                Place(slot, docks, null, placeholder);
                changed = true;
                _warn($"Loading dock slot #{previous + 1} of '{code}' is taken by a dock S1API did not add; that registration moves to dock #{slot.Index + 1}.");
            }

            if (restored > 0 && _restoredThisScene.Add(code))
                _info($"Loading dock slots of '{code}' restored: {restored} reserved slot(s) keep their dock numbers.");
            return changed;
        }

        /// <summary>
        /// Reconciles what a registration's provider returned with its slot. Call <see cref="Reserve"/> first.
        /// </summary>
        /// <param name="code">The property code.</param>
        /// <param name="user">The registration.</param>
        /// <param name="dock">The provider result, or null while the dock is unavailable.</param>
        /// <param name="identity">A key that names the same dock again after a reload, or null when it has none.</param>
        /// <param name="docks">The property's dock list; changed in place.</param>
        /// <param name="placeholder">Creates the stand-in for an empty slot at the given index.</param>
        /// <param name="index">The registration's slot index, or -1.</param>
        internal DockSlotChange Apply(string code, object user, TDock? dock, string? identity, List<TDock?> docks, Func<int, TDock> placeholder, out int index)
        {
            Slot? slot = SlotOf(code, user);
            index = slot?.Index ?? -1;

            if (dock == null)
            {
                if (slot == null)
                    return DockSlotChange.None;
                slot.Users[user] = null;
                if (!slot.Owned)
                    return DockSlotChange.None;
                TDock? other = LiveDockOf(slot);
                if (!Place(slot, docks, other, placeholder))
                    return DockSlotChange.None;
                return other == null ? DockSlotChange.Released : DockSlotChange.Attached;
            }

            int existing = IndexOf(docks, dock);
            if (slot != null)
            {
                if (existing == slot.Index)
                {
                    slot.Users[user] = dock;
                    if (slot.Owned)
                        slot.Live = dock;
                    Remember(slot, dock, identity);
                    return DockSlotChange.None;
                }

                if (existing >= 0 || !slot.Owned)
                    return DockSlotChange.Conflict;

                slot.Users[user] = dock;
                Remember(slot, dock, identity);
                return Place(slot, docks, dock, placeholder) ? DockSlotChange.Attached : DockSlotChange.None;
            }

            if (existing >= 0)
            {
                Slot shared = SlotAt(code, existing) ?? AddSlot(code, existing, owned: false);
                shared.Users[user] = dock;
                Remember(shared, dock, identity);
                index = existing;
                return shared.Owned ? DockSlotChange.None : DockSlotChange.Shared;
            }

            Slot target = ReservedFor(code, dock, identity) ?? AddSlot(code, docks.Count, owned: true);
            target.Users[user] = dock;
            Remember(target, dock, identity);
            Place(target, docks, dock, placeholder);
            index = target.Index;
            return DockSlotChange.Attached;
        }

        /// <summary>
        /// Removes a disposed registration from its slot. An owned slot keeps its index; without a remaining user it
        /// holds a placeholder. <paramref name="docks"/> is null while the property is not loaded.
        /// </summary>
        internal DockSlotRelease Release(string code, object user, List<TDock?>? docks, Func<int, TDock> placeholder, out int index)
        {
            Slot? slot = SlotOf(code, user);
            index = slot?.Index ?? -1;
            if (slot == null)
                return DockSlotRelease.NoSlot;

            slot.Users.Remove(user);
            if (!slot.Owned)
            {
                if (slot.Users.Count == 0)
                    _byCode[code].Remove(slot);
                return DockSlotRelease.NotOwned;
            }

            TDock? other = LiveDockOf(slot);
            if (docks != null)
                Place(slot, docks, other, placeholder);
            else
                slot.Live = other;
            return slot.Users.Count > 0 ? DockSlotRelease.StillUsed : DockSlotRelease.Reserved;
        }

        /// <summary>Forgets this scene's dock objects. Slots, their indexes and their users stay.</summary>
        internal void ResetScene()
        {
            foreach (List<Slot> slots in _byCode.Values)
            {
                foreach (Slot slot in slots)
                {
                    slot.Live = null;
                    slot.Placeholder = null;
                    foreach (object user in new List<object>(slot.Users.Keys))
                        slot.Users[user] = null;
                }
            }

            _placeholders.Clear();
            _restoredThisScene.Clear();
        }

        /// <summary>
        /// Drops every slot when <paramref name="save"/> differs from the save the slots belong to, so another save
        /// does not inherit reserved dock numbers or placeholders. An empty key (no save loaded, or a co-op client)
        /// keeps the slots. True when the slots were dropped.
        /// </summary>
        internal bool UseSave(string? save)
        {
            if (string.IsNullOrEmpty(save))
                return false;
            if (_save == null)
            {
                _save = save;
                return false;
            }

            if (string.Equals(_save, save, StringComparison.OrdinalIgnoreCase))
                return false;

            int count = 0;
            foreach (List<Slot> slots in _byCode.Values)
                count += slots.Count;
            _byCode.Clear();
            _placeholders.Clear();
            _restoredThisScene.Clear();
            _save = save;
            _info($"Another save is loaded: {count} loading dock slot(s) of the previous save dropped.");
            return true;
        }

        /// <summary>Removes every slot. Only for tests.</summary>
        internal void Clear()
        {
            _byCode.Clear();
            _placeholders.Clear();
            _restoredThisScene.Clear();
            _save = null;
        }

        internal bool IsPlaceholder(TDock dock)
        {
            foreach (TDock placeholder in _placeholders)
            {
                if (_same(placeholder, dock))
                    return true;
            }

            return false;
        }

        internal int IndexOf(string code, object user) => SlotOf(code, user)?.Index ?? -1;

        private bool Place(Slot slot, List<TDock?> docks, TDock? live, Func<int, TDock> placeholder)
        {
            bool changed = false;
            while (docks.Count < slot.Index)
            {
                docks.Add(NewPlaceholder(placeholder, docks.Count));
                changed = true;
            }

            if (docks.Count == slot.Index)
            {
                docks.Add(null);
                changed = true;
            }

            TDock target = live ?? (slot.Placeholder ??= NewPlaceholder(placeholder, slot.Index));
            slot.Live = live;
            TDock? current = docks[slot.Index];
            if (current != null && _same(current, target))
                return changed;

            docks[slot.Index] = target;
            return true;
        }

        private TDock NewPlaceholder(Func<int, TDock> placeholder, int index)
        {
            TDock created = placeholder(index);
            _placeholders.Add(created);
            return created;
        }

        private bool IsSlotDock(Slot slot, TDock dock) =>
            (slot.Live != null && _same(slot.Live, dock)) || (slot.Placeholder != null && _same(slot.Placeholder, dock));

        private TDock? LiveDockOf(Slot slot)
        {
            foreach (TDock? dock in slot.Users.Values)
            {
                if (dock != null && _isAlive(dock))
                    return dock;
            }

            return null;
        }

        private int IndexOf(List<TDock?> docks, TDock dock)
        {
            for (int i = 0; i < docks.Count; i++)
            {
                TDock? entry = docks[i];
                if (entry != null && _same(entry, dock))
                    return i;
            }

            return -1;
        }

        private Slot? SlotOf(string code, object user)
        {
            if (!_byCode.TryGetValue(code, out List<Slot>? slots))
                return null;
            foreach (Slot slot in slots)
            {
                if (slot.Users.ContainsKey(user))
                    return slot;
            }

            return null;
        }

        private Slot? SlotAt(string code, int index)
        {
            if (!_byCode.TryGetValue(code, out List<Slot>? slots))
                return null;
            foreach (Slot slot in slots)
            {
                if (slot.Index == index)
                    return slot;
            }

            return null;
        }

        /// <summary>An owned slot without users that last held this dock, matched by object or identity.</summary>
        private Slot? ReservedFor(string code, TDock dock, string? identity)
        {
            if (!_byCode.TryGetValue(code, out List<Slot>? slots))
                return null;
            foreach (Slot slot in slots)
            {
                if (!slot.Owned || slot.Users.Count > 0)
                    continue;
                bool sameObject = slot.LastDock != null && _same(slot.LastDock, dock);
                bool sameGuid = !string.IsNullOrEmpty(identity) && string.Equals(slot.Identity, identity, StringComparison.OrdinalIgnoreCase);
                if (sameObject || sameGuid)
                    return slot;
            }

            return null;
        }

        private Slot AddSlot(string code, int index, bool owned)
        {
            if (!_byCode.TryGetValue(code, out List<Slot>? slots))
            {
                slots = new List<Slot>();
                _byCode[code] = slots;
            }

            var slot = new Slot(index, owned);
            slots.Add(slot);
            return slot;
        }

        private static void Remember(Slot slot, TDock dock, string? identity)
        {
            slot.LastDock = dock;
            if (!string.IsNullOrEmpty(identity))
                slot.Identity = identity;
        }

        private sealed class Slot
        {
            internal Slot(int index, bool owned)
            {
                Index = index;
                Owned = owned;
            }

            internal int Index { get; set; }

            /// <summary>True when S1API added the slot; false for a dock that was already in the array.</summary>
            internal bool Owned { get; }

            /// <summary>Registrations using the slot and the dock each provider last returned in this scene.</summary>
            internal Dictionary<object, TDock?> Users { get; } = new Dictionary<object, TDock?>();

            internal TDock? Live { get; set; }
            internal TDock? Placeholder { get; set; }
            internal TDock? LastDock { get; set; }
            internal string? Identity { get; set; }
        }
    }
}
