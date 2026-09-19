# Logical Product Kinds

`ProductKind` gives mods a stable logical identity for product categories without allocating native enum values or waiting for `ProductManager`.

## Register a product kind

Use a namespaced ID so unrelated mods cannot collide:

```csharp
using S1API.Products;

ProductKind mdma = new ProductKindBuilder("examplemod:mdma")
    .WithCompatibilityDrugType(DrugType.MDMA)
    .Build();
```

IDs use `<namespace>:<name>` form. Comparisons are case-insensitive, and the name may contain `/` for optional grouping.

`CompatibilityDrugType` is optional metadata for consumers that still need a vanilla category. It is not the product kind's identity and does not create a native product definition or add support to native systems.

## Look up registered kinds

```csharp
ProductKind? mdma = ProductKindRegistry.Get("EXAMPLEMOD:MDMA");

if (ProductKindRegistry.TryGet("examplemod:mdma", out ProductKind? found))
{
    // Use the immutable logical product kind.
}

IReadOnlyCollection<ProductKind> allKinds = ProductKindRegistry.All;
```

Registry snapshots and product kinds are read-only. Registrations remain available across scene and save transitions for the lifetime of the process.

## Duplicate registrations

Building the same case-insensitive ID with the same compatibility metadata returns the original `ProductKind`. Building that ID with different metadata throws an `InvalidOperationException` that identifies the conflict.

This makes per-load setup calls safe when they repeat an equivalent registration while rejecting two mods that claim the same logical ID differently.

## Add optional presentation and Product Manager metadata

Product-kind identity and UI registration are separate. Register immutable
metadata only when the kind needs a user-facing name, color, search aliases, or
its own Product Manager section:

```csharp
using S1API.Products;
using UnityEngine;

Sprite mdmaIcon = LoadModOwnedMdmaIcon();

ProductKindMetadata mdmaMetadata =
    new ProductKindMetadataBuilder(mdma)
        .WithDisplayName("MDMA")
        .WithColor(new Color(0.84f, 0.24f, 0.72f))
        .WithIcon(mdmaIcon)
        .WithSortOrder(50)
        .WithSearchAliases("ecstasy", "molly")
        .WithProductManagerVisibility()
        .Build();
```

Visible sections require both an icon and a compatibility drug type. S1API
clones a native Product Manager section, replaces only its heading metadata,
and keeps vanilla sections and their order unchanged. Custom sections are
ordered by `SortOrder`, display name, then stable kind ID. Equivalent repeated
registrations return the first immutable metadata instance; conflicting
registrations fail without replacing it.

Search aliases are case-insensitive for matching and deduplication, but their
input order is preserved in `SearchAliases`. Because that ordered list is part
of the immutable registration snapshot, repeating a kind with the same aliases
in a different order is a conflicting registration. Keep registration calls
deterministic across lifecycle paths and peers.

When reusing a generated product icon for the section, register the product's
`ProductPresentationProfile` before building its definition. Icon capture runs
during loading and replaces the representation-template fallback on the native
definition. Register `ProductKindMetadata` with that generated sprite after it
becomes available; registering metadata does not itself queue or regenerate an
icon. S1API then refreshes existing S1API-managed product and favourite entries
from the definition's current icon.

The generated sprite is available through the custom definition's `Icon`
property after capture completes. If metadata setup runs earlier, retain the
template icon reference and defer metadata registration until `Icon` is
non-null and no longer that reference. Do not copy or reload the generated PNG:
pass the generated `Sprite` instance directly to `WithIcon`.
The sprite must still be a live Unity object when metadata is built; destroyed
sprites are rejected as missing icons.

When the compatibility type has no serialized native metadata row, S1API adds
the missing name and color without changing existing native rows. MDMA and
heroin metadata that share a native compatibility type must agree on that
native-facing name and color; logical kinds backed by an existing vanilla type
may still use distinct custom section presentation.

Product Manager sections are reconciled on registration, app startup and open,
and save-load lifecycle transitions. Stale or duplicate S1API-owned sections
are removed. Discovered entries are routed by logical `ProductKind`, including
when multiple logical kinds share a compatibility type. Favourites remain in
the native Favourites section, and listed state remains on each native entry.

The current native Product Manager has category panels but no text-search
control. Consumers can use `MatchesSearch` to match a kind ID, display name, or
alias:

```csharp
IEnumerable<ProductKindMetadata> matches =
    ProductKindMetadataRegistry.All.Where(metadata =>
        metadata.MatchesSearch("molly"));
```

Omit `WithProductManagerVisibility()` to retain searchable metadata without
showing custom products of that kind in Product Manager. Visibility does not
discover or list a definition.

## Keep definition and catalog actions explicit

Keep these operations separate:

1. `ProductKindBuilder.Build()` registers logical identity.
2. `ProductKindMetadataBuilder.Build()` registers optional presentation and
   Product Manager behavior.
3. `CustomProductDefinitionBuilder.Build()` constructs and registers a
   definition.
4. `CustomProductDefinition.Discover()` and `SetListed()` explicitly change
   the authoritative catalog state after loading.

Metadata registration does not create a product definition, discover it, list
it, add it to a shop, or change save and network payloads.
