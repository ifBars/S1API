using System;

namespace S1API.Items
{
    /// <summary>
    /// Represents how a clothing item is applied to the avatar.
    /// Mirrors ScheduleOne.Clothing.EClothingApplicationType.
    /// </summary>
    [Obsolete("Use S1API.Items.Clothing.ClothingApplicationType instead")]
    public enum ClothingApplicationType
    {
        /// <summary>Applied as a body layer (flat texture on body mesh).</summary>
        BodyLayer = 0,
        /// <summary>Applied as a face layer (flat texture on face mesh).</summary>
        FaceLayer = 1,
        /// <summary>Applied as a 3D accessory (separate mesh).</summary>
        Accessory = 2
    }
}

