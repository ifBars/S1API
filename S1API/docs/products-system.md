# Products system

Use the product APIs to work with existing products or register fixed,
mod-owned product definitions. S1API keeps product identity, save data, and
network behavior explicit so a visual or catalog addition does not silently
change gameplay state.

## Choose the product path

- **Read or create instances of an existing product:** start with
  [Products API](products-api.md).
- **Create a marijuana-family variant through the native game path:** use
  [Native weed variants](weed-variants.md).
- **Create a fixed product outside the native drug families:** use
  [Generic custom products](generic-custom-products.md).
- **Group mod-owned products under a durable logical ID:** use
  [Logical product kinds](product-kinds.md).
- **Fill a storage, reward, or test inventory with product instances:** use
  [ProductPopulator](products-populator.md).
- **Register runtime additives for growing:** use
  [Runtime additives](runtime-additives.md).

## Product concepts

`ProductDefinition` describes a product type. `ProductInstance` is one stack
or item of that product. A product definition can have properties, a price,
legal status, valid packaging, and presentation data.

Use `ProductManager.DiscoveredProducts` only for products discovered in the
current save. A custom definition is registered separately, then discovery and
listing are explicit host-side actions after loading.

## Registration order

Register custom product kinds, profiles, and definitions during
`GameLifecycle.OnPreLoad`. Register the same stable IDs on every peer before
native save restoration. Defer discovery, Product Manager listing, and shop
inventory changes until `GameLifecycle.OnLoadComplete`.

Do not use a display name as an ID. Published product and product-kind IDs are
durable, namespaced, and case-insensitive.

## Presentation and packaging

Generic products borrow the game's interaction scaffolding from a native
representation template. Add a `ProductPresentationProfile` only when the
product needs mod-owned loose, held, station, functional-product, or icon
visuals. Add a packaging-content profile when a filled baggie, jar, or brick
needs mod-owned contents.

The [presentation workbench](presentation-workbench.md) is a local authoring
tool for registered product and item visuals. It does not persist edits or
transfer assets between peers.

## Product Manager and shops

`ProductKind` establishes logical identity. `ProductKindMetadata` optionally
adds a display name, color, icon, aliases, and an S1API-managed Product Manager
section. It does not create a definition, discover a product, list it, or add
it to a shop.

Discovery, listing, and shop stock are independent actions. This lets a mod
register a product before save loading without forcing it into a player's
catalog or a vendor inventory.

## Customer preferences

Customers use `DrugType`, property tokens, affinities, and standards when they
choose orders. Configure that behavior through the NPC builders in
[Customer behavior](customer-behavior.md), not through product registration.

## Read next

1. Read [Products API](products-api.md) for wrappers, instances, packaging, and
   effect callbacks.
2. Choose [Native weed variants](weed-variants.md) or
   [Generic custom products](generic-custom-products.md) for registration.
3. Add [Logical product kinds](product-kinds.md) only when another system needs
   a durable category or Product Manager metadata.
