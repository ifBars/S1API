namespace S1API.Entities.Equippables
{
    /// <summary>
    /// Type-safe wrapper for AvatarEquippable Resources paths.
    /// Use predefined static values (e.g. <see cref="Phone_Lowered"/>, <see cref="Beer"/>, <see cref="Flashlight"/>)
    /// or <see cref="Custom"/> for mod-specific items.
    /// </summary>
    /// <remarks>
    /// <para>Pass to schedule action builders:</para>
    /// <list type="bullet">
    ///   <item><see cref="Entities.Schedule.LocationBasedActionSpecBuilder.WithItem"/> — item to hold on arrival</item>
    ///   <item><see cref="Entities.Schedule.LocationBasedActionSpecBuilder.WithDrink"/> — drink to use on arrival</item>
    /// </list>
    /// <para>Pass to prefab builder methods:</para>
    /// <list type="bullet">
    ///   <item><see cref="NPCPrefabBuilder.EnsureItemHolding(EquippablePath)"/> — prefab-level default held item</item>
    ///   <item><see cref="NPCPrefabBuilder.EnsureDrinking(EquippablePath)"/> — prefab-level default drink</item>
    ///   <item><see cref="NPCPrefabBuilder.EnsureGraffiti(EquippablePath)"/> — spray paint equippable</item>
    /// </list>
    /// <para>Strings are implicitly converted to <see cref="EquippablePath"/>, so raw string paths continue to work.</para>
    /// </remarks>
    public readonly struct EquippablePath
    {
        /// <summary>
        /// The Resources path (relative to Resources folder, no extension).
        /// </summary>
        public string ResourcePath { get; }

        private EquippablePath(string path) => ResourcePath = path ?? string.Empty;

        /// <summary>
        /// Creates a path for a custom mod equippable.
        /// </summary>
        /// <param name="resourcePath">Resources path (e.g. "MyMod/Items/MyItem").</param>
        public static EquippablePath Custom(string resourcePath) => new(resourcePath);

        /// <summary>
        /// Implicit conversion from string for custom paths and existing constants (e.g. <see cref="Misc.Beer"/>).
        /// </summary>
        public static implicit operator EquippablePath(string path) => new(path ?? string.Empty);

        /// <summary>
        /// Returns the Resources path.
        /// </summary>
        public override string ToString() => ResourcePath;

        #region Avatar/Equippables (Misc)

        /// <summary>Baton (police).</summary>
        public static readonly EquippablePath Baton = new(Misc.Baton);

        /// <summary>Beer bottle (drink).</summary>
        public static readonly EquippablePath Beer = new(Misc.Beer);

        /// <summary>Coffee cup (drink).</summary>
        public static readonly EquippablePath Coffee = new(Misc.Coffee);

        /// <summary>Cuke energy drink.</summary>
        public static readonly EquippablePath Cuke = new(Misc.Cuke);

        /// <summary>Hammer tool.</summary>
        public static readonly EquippablePath Hammer = new(Misc.Hammer);

        /// <summary>Marijuana joint.</summary>
        public static readonly EquippablePath Joint = new(Misc.Joint);

        /// <summary>Phone (lowered).</summary>
        public static readonly EquippablePath Phone_Lowered = new(Misc.Phone_Lowered);

        /// <summary>Phone (raised).</summary>
        public static readonly EquippablePath Phone_Raised = new(Misc.Phone_Raised);

        /// <summary>Smoking pipe.</summary>
        public static readonly EquippablePath Pipe = new(Misc.Pipe);

        /// <summary>Trash bag.</summary>
        public static readonly EquippablePath TrashBag = new(Misc.TrashBag);

        #endregion

        #region Avatar/Equippables (Weapons)

        /// <summary>Broken bottle melee weapon.</summary>
        public static readonly EquippablePath BrokenBottle = new(Weapon.BrokenBottle);

        /// <summary>Knife melee weapon.</summary>
        public static readonly EquippablePath Knife = new(Weapon.Knife);

        /// <summary>M1911 pistol.</summary>
        public static readonly EquippablePath M1911 = new(Weapon.M1911);

        /// <summary>Pump shotgun.</summary>
        public static readonly EquippablePath PumpShotgun = new(Weapon.PumpShotgun);

        /// <summary>Revolver pistol.</summary>
        public static readonly EquippablePath Revolver = new(Weapon.Revolver);

        /// <summary>Taser weapon.</summary>
        public static readonly EquippablePath Taser = new(Weapon.Taser);

        #endregion

        #region Tools

        /// <summary>Flashlight tool.</summary>
        public static readonly EquippablePath Flashlight = new("Tools/Flashlight/Flashlight_AvatarEquippable");

        /// <summary>Trash grabber tool.</summary>
        public static readonly EquippablePath TrashGrabber = new("Tools/TrashGrabber/TrashGrabber_AvatarEquippable");

        /// <summary>Watering can tool.</summary>
        public static readonly EquippablePath WateringCan = new("Tools/WateringCan/WateringCan_AvatarEquippable");

        /// <summary>Trimmers tool.</summary>
        public static readonly EquippablePath Trimmers = new("Tools/Trimmers/Trimmers_AvatarEquippable");

        #endregion

        #region Drinks (for EnsureDrinking / WithDrink)

        /// <summary>Energy drink (ingredients).</summary>
        public static readonly EquippablePath EnergyDrink = new("ingredients/energydrink/EnergyDrink_AvatarEquippable");

        #endregion

        #region Graffiti

        /// <summary>Spray paint (for EnsureGraffiti).</summary>
        public static readonly EquippablePath SprayPaint = new("Weapons/SprayPaint/SprayPaint_AvatarEquippable");

        #endregion
    }
}
