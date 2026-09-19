#if (IL2CPPMELON)
using S1EntityFramework = Il2CppScheduleOne.EntityFramework;
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
using S1Storage = Il2CppScheduleOne.Storage;
#elif MONOMELON
using S1EntityFramework = ScheduleOne.EntityFramework;
using S1ItemFramework = ScheduleOne.ItemFramework;
using S1Storage = ScheduleOne.Storage;
#endif
using S1API.Items;

namespace S1API.Internal.Building
{
    internal sealed class FurnitureComposition
    {
        internal FurnitureComposition(
            S1ItemFramework.BuildableItemDefinition templateDefinition,
            S1EntityFramework.BuildableItem builtItem,
            S1Storage.StoredItem storedItem,
            Equippable equippable)
        {
            TemplateDefinition = templateDefinition;
            BuiltItem = builtItem;
            StoredItem = storedItem;
            Equippable = equippable;
        }

        internal S1ItemFramework.BuildableItemDefinition TemplateDefinition { get; }
        internal S1EntityFramework.BuildableItem BuiltItem { get; }
        internal S1Storage.StoredItem StoredItem { get; }
        internal Equippable Equippable { get; }
    }
}
