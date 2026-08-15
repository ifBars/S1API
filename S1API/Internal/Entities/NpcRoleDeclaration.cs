using S1API.Entities;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace S1API.Internal.Entities
{
    internal readonly struct NpcRoleDeclaration
    {
        internal bool IsPhysical { get; }
        internal bool IsCustomer { get; }
        internal bool IsDealer { get; }
        internal bool IsSupplier { get; }

        internal NpcRootRole RootRole =>
            IsSupplier
                ? NpcRootRole.Supplier
                : IsDealer
                    ? NpcRootRole.Dealer
                    : NpcRootRole.Plain;

        internal NpcRoleDeclaration(
            bool isPhysical,
            bool isCustomer,
            bool isDealer,
            bool isSupplier)
        {
            IsPhysical = isPhysical;
            IsCustomer = isCustomer;
            IsDealer = isDealer;
            IsSupplier = isSupplier;
        }

        internal NpcRoleDeclaration WithCompatibilityRoles(
            bool isCustomer,
            bool isDealer,
            bool isSupplier) =>
            new NpcRoleDeclaration(
                IsPhysical,
                IsCustomer || isCustomer,
                IsDealer || isDealer,
                IsSupplier || isSupplier);

        internal NpcRoleDeclaration Validate(Type npcType)
        {
            string typeName = npcType.FullName ?? npcType.Name;
            if (IsDealer && IsSupplier)
            {
                throw new InvalidOperationException(
                    $"Custom NPC type '{typeName}' cannot be both a dealer and a supplier root.");
            }

            if (IsSupplier && !IsPhysical)
            {
                throw new InvalidOperationException(
                    $"Custom supplier type '{typeName}' must override IsPhysical to return true.");
            }

            return this;
        }
    }

    internal static class NpcRoleDeclarationResolver
    {
        private static readonly Dictionary<Type, NpcRoleDeclaration> TypeDeclarations =
            new Dictionary<Type, NpcRoleDeclaration>();
        private static readonly object DeclarationLock = new object();

        internal static NpcRoleDeclaration GetDeclaredProperties(Type npcType)
        {
            if (npcType == null)
                throw new ArgumentNullException(nameof(npcType));
            if (!typeof(NPC).IsAssignableFrom(npcType))
            {
                throw new ArgumentException(
                    $"Type '{npcType.FullName}' does not derive from {typeof(NPC).FullName}.",
                    nameof(npcType));
            }

            lock (DeclarationLock)
            {
                if (TypeDeclarations.TryGetValue(npcType, out var declaration))
                    return declaration;

                var npc = (NPC)FormatterServices.GetUninitializedObject(npcType);
                declaration = new NpcRoleDeclaration(
                    ReadProperty(npc, npcType, nameof(NPC.IsPhysical), value => value.IsPhysical),
                    ReadProperty(npc, npcType, nameof(NPC.IsCustomer), value => value.IsCustomer),
                    ReadProperty(npc, npcType, nameof(NPC.IsDealer), value => value.IsDealer),
                    ReadProperty(npc, npcType, nameof(NPC.IsSupplier), value => value.IsSupplier));
                TypeDeclarations[npcType] = declaration;
                return declaration;
            }
        }

        private static bool ReadProperty(
            NPC npc,
            Type npcType,
            string propertyName,
            Func<NPC, bool> read)
        {
            try
            {
                return read(npc);
            }
            catch (Exception ex)
            {
                string typeName = npcType.FullName ?? npcType.Name;
                throw new InvalidOperationException(
                    $"Custom NPC type '{typeName}' could not evaluate declarative property " +
                    $"'{propertyName}' from an uninitialized instance. NPC role properties must " +
                    "be stable, side-effect-free values that do not depend on constructor or field initialization.",
                    ex);
            }
        }
    }
}
