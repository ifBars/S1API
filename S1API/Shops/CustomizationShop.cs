namespace S1API.Shops
{
    /// <summary>
    /// The character customization shops a mod can list options in.
    /// </summary>
    public enum CustomizationShop
    {
        /// <summary>
        /// The tattoo parlour. Supported.
        /// </summary>
        Tattoo = 0,

        /// <summary>
        /// The barbershop (hair style and beard categories). Supported.
        /// </summary>
        Barber = 1,

        /// <summary>
        /// The clothing shop. It is a different UI without option categories, so
        /// <see cref="CustomizationShops.AddOption"/> logs a warning and lists nothing.
        /// </summary>
        Clothing = 2
    }
}
