using S1API.Items;
using UnityEngine;

namespace S1API.Tests.Items;

#pragma warning disable CS0618
internal static class ItemCreatorApiCompileFixture
{
    internal static void CompileLegacyAndCurrentCallers(Sprite? icon, Equippable? equippable)
    {
        _ = ItemCreator.CreateItem(
            "examplemod:legacy-nine",
            "Legacy Nine",
            "Legacy positional call without an equippable.",
            ItemCategory.Tools,
            1,
            10f,
            0.5f,
            LegalStatus.Legal,
            icon);

        _ = ItemCreator.CreateItem(
            "examplemod:legacy-ten",
            "Legacy Ten",
            "Legacy positional call with an equippable.",
            ItemCategory.Tools,
            1,
            10f,
            0.5f,
            LegalStatus.Legal,
            icon,
            equippable);

        _ = ItemCreator.CreateItem(
            "examplemod:current-named-icon",
            "Current Named Icon",
            "Current call that names the icon parameter.",
            ItemCategory.Tools,
            legalStatus: LegalStatus.Legal,
            icon: icon);

        _ = ItemCreator.CreateItem(
            "examplemod:current-rank",
            "Current Rank",
            "Current positional rank-gating call.",
            ItemCategory.Tools,
            1,
            10f,
            0.5f,
            LegalStatus.Legal,
            false,
            null);
    }
}
#pragma warning restore CS0618
