# Generic Custom Products

`CustomProductDefinitionBuilder` registers the first generic S1API product
definition that does not belong to a native weed, methamphetamine, cocaine, or
shroom family. Use it for same-mod products such as tablets, powders, or other
fixed definitions that should not enter native mix generation.

## Register before save restoration

Create the logical kind once, then build the definition during
`GameLifecycle.OnPreLoad`. Every host and client must run the same mod and
register the same stable IDs before native item save data is restored.

```csharp
using System;
using S1API.Console;
using S1API.Items;
using S1API.Lifecycle;
using S1API.Products;
using S1API.Properties;

ProductKind focusTabletKind = new ProductKindBuilder(
        "example.mod:focus-tablet")
    .WithCompatibilityDrugType(DrugType.MDMA)
    .Build();

CustomProductDefinition? focusTablet = null;

GameLifecycle.OnPreLoad += () =>
{
    if (focusTablet != null)
        return;

    var representationTemplate =
        ItemManager.GetDefinition("ogkush") as ProductDefinition;
    var baggie = ProductPopulator.GetPackaging("baggie");

    if (representationTemplate == null)
    {
        throw new InvalidOperationException(
            "Cannot register example.mod:products/focus-tablet: " +
            "the representation template 'ogkush' is unavailable during OnPreLoad.");
    }

    if (baggie == null)
    {
        throw new InvalidOperationException(
            "Cannot register example.mod:products/focus-tablet: " +
            "the required packaging 'baggie' is unavailable during OnPreLoad.");
    }

    focusTablet = CustomProductItemCreator
        .CreateBuilder(
            "example.mod:products/focus-tablet",
            focusTabletKind)
        .WithName("Focus Tablet")
        .WithDescription("A fixed, same-mod generic product.")
        .WithProductPrice(175f)
        .WithProperties(Property.Focused)
        .WithLegalStatus(LegalStatus.Illegal)
        .WithBaseAddictiveness(0.2f)
        .WithDefaultQuality(Quality.Standard)
        .WithValidPackaging(baggie)
        .WithRepresentationsFrom(representationTemplate)
        .WithEffectDurations(
            playerSeconds: 120,
            npcSeconds: 180)
        .Build();

    ConsoleItemAliases.Register(
        alias: "focus-tablet",
        canonicalItemId: focusTablet.ProductId);
};
```

Don't want to type the namespace in the console? Keep the durable namespaced
item ID and register the short name with `ConsoleItemAliases` after `Build()`.
The example above accepts both `give focus-tablet` and
`give example.mod:products/focus-tablet`. The alias is a local console
convenience only: created items still use the canonical ID, and aliases are not
saved, networked, or included in compatibility manifests.

The compatibility drug type remains optional metadata. A generic product needs
a native execution representation: use compatibility metadata when it is
appropriate, or explicitly select `WithNativeMixerMap(...)` for a logical kind
that has no base-game enum. Neither option changes the logical kind's identity.

To give the logical kind a Product Manager section, separately register
[`ProductKindMetadata`](product-kinds.md#add-optional-presentation-and-product-manager-metadata).
This catalog metadata does not change definition construction, discovery, or
listing.

## Builder contract and defaults

- Product and product-kind IDs are durable, case-insensitive, and namespaced.
  Do not change a published ID or reuse it with another definition.
- `WithName`, `WithProductPrice`, and `WithRepresentationsFrom` are required.
- Prices follow the native product-manager policy: finite values are clamped to
  1 through 999 and rounded to the nearest integer.
- Description defaults to empty, legal status to `Illegal`, base addictiveness
  to `0`, quality to `Standard`, properties to none, and valid packaging to none.
- Up to eight distinct properties may be supplied. Every property must resolve
  to a vanilla or registered custom native effect.
- Packaging is deduplicated case-insensitively and stored in ascending capacity
  order. `CustomProductDefinition.CreatePackagedInstance` returns `null` for
  packaging outside that policy.
- When effect durations are omitted, they are borrowed from the representation
  template.
- A successful builder is immutable. Repeated `Build()` calls on that builder
  return the same wrapper. Another builder claiming the same ID fails
  deterministically.

The repeated-`Build()` guarantee applies only to that builder instance. As with
existing typed product wrappers, registry and Product Manager lookups may return
a different wrapper for the same native definition. Compare the stable ID or
native-backed item equality; do not use `ReferenceEquals` across lookup calls.

The template's icon, stored/held representations, functional product,
consumption animation, and item UI references are shared rather than cloned.
The builder does not export, embed, or redistribute those game assets. Its
station representation is deliberately not copied unless a presentation
profile supplies a station visual.

## Custom presentation profiles

`ProductPresentationProfile` lets a generic custom product use mod-owned visuals
without subclassing a native drug family. Register the profile by stable product
ID before building the definition:

```csharp
using S1API.Products;
using UnityEngine;

// Load once through S1API.AssetBundles, MAPI's embedded GLB loader, or another
// local mod-owned asset path. Providers should return this reusable prefab source.
GameObject pillVisual = LoadPillVisual();

ProductPresentationProfile pillProfile =
    new ProductPresentationProfileBuilder()
        .WithLooseVisual(() => pillVisual)
        .WithFunctionalProductConvexMeshColliders()
        .WithGeneratedIconFromLooseVisual(size: 512)
        .Require(
            ProductPresentationContext.Stored,
            ProductPresentationContext.Held,
            ProductPresentationContext.Station,
            ProductPresentationContext.FunctionalProduct)
        .Build();

ProductPresentationProfileRegistry.RegisterForProduct(
    ownerId: "example.mod",
    productId: "example.mod:products/focus-tablet",
    profile: pillProfile);
```

The loose visual is the deterministic default for the stored, held, station,
and functional-product contexts. Providers may author the desired root
transform directly. For reusable source objects, pass a
`ProductPresentationTransform` to set an explicit local position, Euler
rotation, and scale on S1API's cloned visual root:

```csharp
var pillPose =
    new ProductPresentationTransform(
        localPosition: Vector3.zero,
        localEulerAngles:
            (Quaternion.Euler(78f, 0f, -8f) *
             Quaternion.Euler(0f, 90f, 0f)).eulerAngles,
        localScale: Vector3.one * 0.06f);

ProductPresentationProfile profile =
    new ProductPresentationProfileBuilder()
        .WithLooseVisual(() => pillVisual, pillPose)
        .WithHeldVisual(() => heldPillVisual, heldPose)
        .WithStationVisual(() => stationPillVisual, stationPose)
        .WithIcon(() => pillIcon)
        .WithConsumptionPrefab(() => pillConsumeAnimationPrefab)
        .Build();
```

Visual providers return a `GameObject` prefab source. S1API clones the source
and preserves its root local position, rotation, and scale unless an explicit
presentation transform overrides them. A loose transform follows the loose
provider into fallback contexts; a context-specific provider and transform
take precedence. S1API also clones the representation template's native
stored-item, equippable, station-item, and functional-product scaffolds,
replacing only their family-specific visual setter. This keeps native storage
footprints, station modules, draggable behavior, first-person equip behavior,
and consumption wiring intact.

Compact or non-box-shaped products can opt into
`WithFunctionalProductConvexMeshColliders()`. S1API then disables the cloned
template's inherited colliders and builds convex colliders from the custom
functional visual's mesh filters. This is intended for dynamic loose-product
physics in packaging stations. Configure a functional-product visual or
loose-visual fallback before enabling it. Meshes that Unity cannot retain as
usable convex colliders fall back to a bounds-based box collider. The option is
disabled by default so existing profiles retain their original scaffold and
box-fallback collision behavior.

For runtime-imported GLB sources, keep one reusable source active beneath a
persistent root positioned outside the playable scene; providers should return
that source rather than importing the model again. Ensure its child renderers
are enabled. If a normal-mapped mesh has no tangent data, recalculate tangents
once after import so the native icon lighting can shade it correctly. These are
source-preparation requirements, not icon texture-import settings: generated
icons are ordinary runtime `Sprite` objects with transparent backgrounds.

The held context also creates a third-person `AvatarEquippable` under the
deterministic resource path
`S1API/ProductPresentation/{productId}/Held`. Every peer must register the same
product and profile locally before that path is received over the network.
S1API does not transmit the mesh, materials, textures, definition, or profile.
By default, this avatar equippable preserves the held visual and transform.
Use `WithAvatarHeldTransform(...)` when a shared model needs a different
third-person pose, or `WithAvatarHeldVisual(provider, transform)` when it also
needs a different source. These methods are additive; profiles that omit them
retain the existing held fallback.

Registered profiles can be tuned in game through the
[presentation workbench](presentation-workbench.md) by opening their stable
product ID.

An explicit consumption provider must return a prefab containing the native
`ProductConsumeAnimation` component. Generated icons reuse the base game's
`IconGenerator` through `S1API.Rendering.IconFactory`; explicit sprites can be
supplied with `WithIcon`. S1API preserves the representation template's icon
while capture is queued, waits for the native `@IconGenerator` rig, yields
through a complete render frame, and rejects transparent cold-start captures.
The loading screen stays open until queued product icons complete or reach the
bounded retry timeout. `MugshotGenerator` remains reserved for avatar/accessory
previews. The generated icon is the loose inventory icon only. Filled packaging
uses the separate packaging-content API below.
Before creating the sprite, S1API round-trips the native preview capture
through PNG into a non-mipmapped 32-bit alpha texture with bilinear filtering
and clamp wrapping. On the target Unity 2022.3 runtime, PNG decoding produces
an ARGB32 texture and uploads it without an additional `Apply()` call. This
normalization is required for reliable `UnityEngine.UI.Image` rendering; using
the native preview texture directly can appear as a solid gray rectangle even
when exporting that texture produces a valid transparent PNG. An empty native
capture is retried, while a deterministic PNG normalization failure stops and
preserves the template icon fallback.
Product Manager entries cache their sprite when initialized; S1API refreshes
S1API-managed product and favourite entries after generated-icon completion.
Mods should still register the presentation profile before building the
definition so the generation request is queued during loading.

Automatic icon fitting targets 72% of the native thumbnail camera by default.
Adjust the framing or preserve the authored scale with the additive overload:

```csharp
.WithGeneratedIconFromLooseVisual(
    size: 512,
    fitToCamera: true,
    cameraFill: 0.82f)
```

Set `fitToCamera: false` when the provider's scale is already authored for the
base-game thumbnail rig. By default, the loose presentation transform controls
icon rotation. Use an icon-only transform when the inventory view needs a
different angle without changing the world model:

```csharp
.WithGeneratedIconTransform(
    new ProductPresentationTransform(
        Vector3.zero,
        new Vector3(45f, 0f, 0f),
        Vector3.one))
```

The icon-only transform replaces the loose transform during capture;
`cameraFill` controls only automatic scale fitting. Direct
`IconFactory.GenerateIcon` and `GenerateIconSprite` overloads expose the same
framing controls.

Profiles may instead be registered by `ProductKind` with
`RegisterForProductKind`. A product-ID profile always wins over a kind profile,
and both key types are case-insensitive. The first owner wins; an owner may
repeat the same profile registration idempotently but cannot silently replace
it with another profile.

Missing optional providers preserve the original template reference. Provider
exceptions, null results, incompatible native scaffolds, and icon-rendering
failures also preserve that reference and log a warning. `Require(...)` changes
those conditions into an actionable registration error once the relevant
native scene service is available. Profiles and generated prefabs are retained
for the process lifetime and reapplied during pre-load and load-complete
restoration.

Presentation profiles affect only generic custom products registered with
`CustomProductDefinitionBuilder`. Vanilla definitions, native-family builders,
and legacy custom registrations are not modified.

## Filled packaging contents and composite icons

`ProductPackagingContentProfile` supplies the mod-owned content rendered inside
one game-owned packaging shell. Profiles are keyed by both product ID and
packaging ID, so a baggie and jar can use different counts and transforms:

```csharp
var pillInBaggie =
    new ProductPackagingContentProfileBuilder()
        .WithContent(() => pillVisualPrefab)
        .AddPlacement(
            new ProductPresentationTransform(
                new Vector3(0f, 0.01f, 0f),
                Vector3.zero,
                Vector3.one))
        .Build();

ProductPackagingContentProfileRegistry.Register(
    ownerId: "example.mod",
    productId: "example.mod:products/focus-tablet",
    packagingId: "baggie",
    profile: pillInBaggie);

var pillsInJar =
    new ProductPackagingContentProfileBuilder()
        .WithContent(() => pillVisualPrefab)
        .AddPlacements(
            jarPlacement1,
            jarPlacement2,
            jarPlacement3,
            jarPlacement4,
            jarPlacement5)
        .Build();

ProductPackagingContentProfileRegistry.Register(
    ownerId: "example.mod",
    productId: "example.mod:products/focus-tablet",
    packagingId: "jar",
    profile: pillsInJar);
```

Each placement creates one clone of the provider's prefab. If no placements are
specified, S1API creates one clone and preserves its authored local transform.
The same composite is used for filled stored items, equipped items, and the
packaged inventory icon.

S1API applies content to individual runtime objects and temporary icon-shell
clones. It does not mutate the shared vanilla `PackagingDefinition` prefabs.
Unregistered product/packaging pairs continue through the native visual and icon
paths unchanged. If a registered provider fails or returns `null`, S1API removes
any partial owned objects and preserves that same native fallback.

Generated composite icons are single-flight and cached once per registered pair
for the active `Main` or `Tutorial` scene. S1API owns both the generated
`Sprite` and `Texture2D`; it destroys them when that scene unloads and recreates
them lazily after a later load. Repeated calls reuse the scene cache and never
append entries to the game's serialized icon list.

Requests made through `ProductIconManager` enter a serialized render queue.
S1API preserves the native fallback for the current frame, waits for the shared
icon rig to settle, and then renders one registered product/packaging pair at a
time. Later lookups use the cached composite. This prevents adjacent baggie,
jar, or other packaging captures from appearing together during a render
transition.

Functional packing stations already parent real `FunctionalProduct` instances
into their game-owned packaging objects, so this profile intentionally does not
replace that interaction. Custom packaging definitions, packaging asset
networking, and extra packaging save data remain outside this API.

### Complete filled visuals and Brick Press support

Some packaging does not have a reusable shell with repeated contents. The
native `brick` packaging is a complete filled product form. Register one
complete mod-owned visual explicitly:

```csharp
var brick = ProductPopulator.GetPackaging("brick");
if (brick == null)
{
    throw new InvalidOperationException(
        "The native 'brick' packaging is unavailable during OnPreLoad.");
}

// Include brick when building the product:
// .WithValidPackaging(baggie, brick)

var pressedBrick =
    new ProductPackagingContentProfileBuilder()
        .WithCompleteFilledVisual(
            provider: () => modOwnedBrickPrefab,
            transform: new ProductPresentationTransform(
                Vector3.zero,
                Vector3.zero,
                Vector3.one))
        .Build();

ProductPackagingContentProfileRegistry.Register(
    ownerId: "example.mod",
    productId: "example.mod:products/focus-tablet",
    packagingId: "brick",
    profile: pressedBrick);
```

`WithCompleteFilledVisual(...)` creates exactly one clone in each filled stored,
equipped, and composite-icon context. The provider may return a mod-owned prefab,
an object loaded from the mod's asset bundle, or another reusable local source.
It must not return or modify the shared native packaging prefab.

If a mod wants the native brick geometry and wrapping without extracting or
redistributing them, it can instead select a presentation-only runtime scaffold:

```csharp
var pressedBrick =
    new ProductPackagingContentProfileBuilder()
        .WithNativeFilledVisualScaffold(
            template: ProductPackagingVisualTemplate.Marijuana,
            customize: clone =>
            {
                Renderer[] renderers =
                    clone.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                    renderers[i].sharedMaterial = modOwnedBrickMaterial;
            })
        .Build();
```

S1API resolves and clones the selected game-owned visual independently from the
active stored, equipped, or icon prefab, then invokes `customize` only with that
clone. The template does not change `ProductKind`, compatibility metadata,
native execution behavior, stable IDs, save data, or network data. Use only
mod-owned materials or other local presentation data in the callback.

The native Brick Press remains authoritative for conversion: it groups
stack-compatible unpackaged inputs, consumes 20 units, copies one product
instance, and applies packaging ID `brick`. S1API does not patch its batch size,
eligibility, task flow, player path, or NPC/server path. Adding `brick` through
`WithValidPackaging(...)` is the explicit product declaration and keeps ordinary
packaged-instance APIs consistent; registering the `brick` presentation profile
is the separate visual opt-in.

The 20 loose objects poured into the mould still use the product's
`ProductPresentationProfile` functional-product context. Presentation assets
are local per peer and are never saved or transmitted. Every participating peer
must register the same product/profile IDs and provide its own assets. If the
complete provider, selected runtime scaffold, or customization callback fails,
S1API removes the partial clone, logs the failure once for that pair and
operation, and preserves the native fallback.

## Loose and packaged instances

```csharp
ProductInstance loose =
    focusTablet.CreateInstance(
        quantity: 2,
        quality: Quality.Premium);

ProductInstance? packaged =
    focusTablet.CreatePackagedInstance(
        quantity: 1,
        packaging: baggie,
        quality: Quality.Standard);
```

Loose and packaged native item data use the product ID, quality, and packaging
ID. That data survives ordinary save/load and network serialization when the
same definition is registered before restoration on every peer.

## Discovery, listing, and shops are opt-in

`Build()` registers the definition and initial price only. It does not discover
the product, list it in Product Manager, add it to shops, or put it in the
native created-products list.

On the authoritative host/server, discovery is explicit:

```csharp
GameLifecycle.OnLoadComplete += () =>
{
    CustomProductDefinition definition = focusTablet ??
        throw new InvalidOperationException(
            "The custom product was not registered during OnPreLoad.");

    definition.Discover();
    definition.SetListed();
};
```

Definitions created during `OnPreLoad` must defer discovery and listing until
`OnLoadComplete`, when `ProductManager.Instance` and the network session are
available. Subscribe to these lifecycle events once; `SetListed` throws when
called before Product Manager initialization.

Pass `listForSale: true` to `Discover` only when discovery should also list the
product. Shop inventory remains separate; call the existing
`S1API.Shops.ShopManager` APIs explicitly after registration.

## Save descriptors and missing-mod recovery

Generic products are persisted separately at `Modded/CustomProducts.json`. The
file is versioned and records only stable IDs and bounded scalar provider data;
it never serializes Unity objects, prefab/asset references, textures, meshes,
delegates, or paths. S1API reads it before the vanilla Product Manager loader,
so the normal vanilla `ProductManager.json` restoration of discovery, listing,
favourites, prices, and inventory product IDs can resolve the definition first.
The provider recreates its logical kind, presentation/profile associations, and
packaging-content provider associations from local mod resources. Generic products
are never inserted into vanilla's four-family `createdProducts` arrays.

For fresh-process recovery, register a provider during your mod's normal early
initialization and associate it with each definition:

```csharp
sealed class FocusTabletProvider : ICustomProductSaveProvider
{
    public string ProviderId => "example:focus-tablet";
    public int MaximumDescriptorVersion => 1;

    public CustomProductDefinitionBuilder? Restore(CustomProductSaveDescriptor descriptor)
    {
        // Recreate the same builder, presentation profile, packaging-content profile,
        // and ProductKind metadata from descriptor.ProviderData and local mod resources.
        return CreateFocusTabletBuilder(descriptor.ProviderData);
    }
}

CustomProductSaveProviderRegistry.Register(new FocusTabletProvider());

// When initially creating the product:
builder.WithSaveProvider("example:focus-tablet", providerVersion: 1, providerData: "v1");
```

Provider IDs are case-insensitive and must remain stable. S1API deterministically
keeps the first descriptor for a case-insensitive product ID. A malformed,
oversized, duplicate, or unknown-format descriptor is skipped. A missing provider,
provider version that is too new, failed reconstruction, or returned renamed ID is
also skipped with an actionable warning; loading continues and S1API never attempts
an arbitrary ID migration. Reinstall the content mod/provider with the original
stable ID to restore the product. These warnings intentionally contain IDs only,
never local paths or personal data.

## Multiplayer compatibility manifest

When a host starts a multiplayer load, S1API establishes a manifest session,
then finalizes its snapshot immediately after the versioned save descriptors are
restored and before base product/inventory loaders run. It sends each joining
S1API client that host-authoritative manifest before the native player-data
request is allowed to deserialize inventory product instances. The client
responds only after comparing its locally registered definitions with the host
manifest. This also covers clients already joined before load, late joins, and
reconnects: registrations remain process-lifetime and the handshake is repeated
per connection without adding duplicate definitions, providers, presentation
profiles, or Product Manager entries.

The manifest is deterministic and bounded (at most 256 entries and 64 KiB). It
contains stable product/owner/kind IDs, the native compatibility drug type,
descriptor and provider versions, local provider availability,
representation-template and packaging IDs,
the resolved presentation-profile registration identity, and a compatibility
hash. The hash additionally covers the local descriptor's display/scalar fields
and provider-data digest, but raw provider data is never transmitted. It never
contains or downloads Unity objects, assets, bundles, sprites, meshes, prefabs,
delegates, arbitrary types, local paths, or save files.

The missing or incompatible-content policy is **reject**. S1API disconnects a
joining client whose local manifest is missing, malformed, too new, duplicated,
timed out, replayed with conflicting content, or incompatible with the host.
It does not create a placeholder or silently deserialize product data against a
different definition. The host also holds target player data until it receives
the matching acknowledgement, so an unmodded or incompatible peer cannot obtain
custom inventory state before validation.

When the host has no descriptor-backed custom products, no manifest packet or
player-data gate is added. Vanilla/no-custom-product sessions retain the native
network path. A client with local custom products still waits for a compatible
custom-product host manifest and fails closed rather than sending its custom
inventory request into an unvalidated session.

`CustomProductMultiplayer.MissingContentPolicy` exposes this fixed policy and
`CustomProductMultiplayer.GetCompatibilityManifestHash()` provides a safe local
diagnostic value. Neither exposes asset references, provider payloads, paths, or
personal information.

## Supported and unsupported behavior

Supported in this milestone:

- process-lifetime definition ownership and scene re-registration through the
  S1API custom-product lifecycle registry;
- stable loose and packaged item instances;
- fixed name, description, price, effects, legality, addictiveness, quality,
  packaging policy, borrowed consumption references, and optional mod-owned
  loose presentation profiles;
- same-mod save/load and native item network serialization; and
- explicit discovery, Product Manager listing, and existing shop integration.

Not supported:

- mixing, generated variants, native family conversion, or production-station
  recipes;
- custom packaging definitions, packaging asset networking, additional
  packaging-content save data;
- definition transfer to a peer without the defining mod; or
- automatic arbitrary renamed-ID migration or recovery of content assets from a
  removed mod.

Generic products remain non-mixable until they explicitly register a
`ProductMixingProfile`. See the mixing guide for the supported opt-in boundary.

## Compatibility

This API is additive. Existing `ProductDefinition`, `ProductInstance`,
`ProductDefinitionWrapper`, native-family builders, defaults, exception
behavior, save IDs, and network payloads are unchanged. The wrapper factory
selects `CustomProductDefinition` only for definitions registered with the new
builder metadata; all previous generic and native-family fallback behavior
remains intact. Presentation-profile registration is opt-in, and definitions
without a resolved profile keep the exact representation references selected by
`WithRepresentationsFrom`. Product-kind metadata registration is also opt-in;
it adds only native-facing presentation metadata and S1API-owned Product
Manager sections. It does not change product IDs, definition serialization,
save data, network payloads, discovery state, listing state, vanilla sections,
or native enum values.
