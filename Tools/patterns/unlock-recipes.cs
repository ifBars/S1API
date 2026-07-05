// PATTERN: Unlock Products - Discover every known product
// USAGE: Call once (e.g. in GameLifecycle/OnInitializeMelon after load)
// REQUIRES: using S1Toolkit.Api;

foreach (string id in Api.Product.GetProductList())
    Api.Product.DiscoverProduct(id);
Api.UI.Notify("Products", "All products discovered!");
