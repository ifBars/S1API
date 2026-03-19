using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using S1API.Internal.Utils;
#if IL2CPPMELON
using Il2CppTMPro;
using Il2CppScheduleOne.Money;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using TMPro;
using ScheduleOne.Money;
#endif

namespace S1API.Property
{
    /// <summary>
    /// Provides an abstraction for interacting with in game player properties.
    /// </summary>
    public class PropertyWrapper : BaseProperty
    {
        /// <summary>
        /// A readonly backing field encapsulating the core property instance
        /// used within the PropertyWrapper class. This field provides access
        /// to the underlying implementation of the property functionalities
        /// and is leveraged across multiple overriden members to delegate
        /// operations to the actual property instance.
        /// </summary>
#if IL2CPPMELON
        internal readonly Il2CppScheduleOne.Property.Property InnerProperty;
#else
        internal readonly ScheduleOne.Property.Property InnerProperty;
#endif
        /// <summary>
        /// A wrapper class that extends the functionality of <see cref="BaseProperty"/>
        /// and acts as a bridge to interact with an inner property implementation
        /// from the ScheduleOne.Property namespace.
        /// </summary>
#if IL2CPPMELON
        public PropertyWrapper(Il2CppScheduleOne.Property.Property property)
#else
        public PropertyWrapper(ScheduleOne.Property.Property property)
#endif
        {
            InnerProperty = property;
        }

        /// <summary>
        /// Gets the name of the property.
        /// Represents the underlying property name as defined by its implementation.
        /// </summary>
        public override string PropertyName => InnerProperty.PropertyName;

        /// <summary>
        /// Gets the unique code representing this property. This code serves as an identifier
        /// for distinguishing the property in the system and is typically defined in the internal
        /// implementation of the property.
        /// </summary>
        public override string PropertyCode => InnerProperty.PropertyCode;

        /// <summary>
        /// Gets the price of the property.
        /// </summary>
        /// <remarks>
        /// The price represents a floating-point value that denotes the monetary
        /// value or cost associated with the property. This property is both
        /// readable and writable, allowing for dynamic adjustments to the property's price.
        /// </remarks>
        public override float Price
        {
            get => InnerProperty.Price;
            set
            {
                InnerProperty.Price = value;
                UpdatePriceDisplay(value);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the property is currently owned.
        /// </summary>
        /// <remarks>
        /// This property reflects the ownership status of the property. Returns true if the property
        /// is owned and false otherwise. The ownership status is based on the internal state of the
        /// wrapped property implementation.
        /// </remarks>
        public override bool IsOwned => InnerProperty.IsOwned;

        /// <summary>
        /// Represents the maximum number of employees that can be allocated to the property.
        /// This property is both readable and writable, allowing for dynamic configuration
        /// of employee capacity based on the property's current requirements or constraints.
        /// </summary>
        public override int EmployeeCapacity
        {
            get => InnerProperty.EmployeeCapacity;
            set => InnerProperty.EmployeeCapacity = value;
        }

        /// <summary>
        /// Marks the property as owned within the PropertyWrapper implementation.
        /// Updates the ownership status by delegating the operation to the underlying
        /// ScheduleOne.Property.Property instance.
        /// This is typically used to signify that the property has been acquired or purchased.
        /// </summary>
        public override void SetOwned()
        {
            InnerProperty.SetOwned();
        }

        /// <summary>
        /// Determines whether a specified point lies within the boundary of the property.
        /// </summary>
        /// <param name="point">The point to check, specified as a Vector3 coordinate.</param>
        /// <returns>true if the point is within the property's boundary; otherwise, false.</returns>
        public override bool IsPointInside(Vector3 point)
        {
            return InnerProperty.DoBoundsContainPoint(point);
        }

        /// <summary>
        /// Gets the exterior spawn point position of the property.
        /// This is typically used for spawning outside the property.
        /// </summary>
        public Vector3 ExteriorSpawnPosition =>
            InnerProperty.SpawnPoint.position;

        /// <summary>
        /// Gets the interior spawn point position of the property.
        /// This is typically used for spawning inside the property.
        /// </summary>
        public Vector3 InteriorSpawnPosition =>
            InnerProperty.InteriorSpawnPoint.position;

        /// <summary>
        /// Gets the number of loading docks available at this property.
        /// </summary>
        public int LoadingDockCount =>
            InnerProperty.LoadingDockCount;

        /// <summary>
        /// Gets the default rotation value for the property.
        /// </summary>
        public float DefaultRotation =>
            InnerProperty.DefaultRotation;

        /// <summary>
        /// Gets a value indicating whether the property is available in the demo version of the game.
        /// </summary>
        public bool AvailableInDemo =>
            InnerProperty.AvailableInDemo;

        /// <summary>
        /// Gets or sets a value indicating whether the property's content is currently culled.
        /// Content culling is used to optimize performance by hiding property contents when far away.
        /// </summary>
        public bool IsContentCulled
        {
            get => InnerProperty.IsContentCulled;
            set => InnerProperty.SetContentCulled(value);
        }

        /// <summary>
        /// Gets the number of employees currently assigned to this property.
        /// </summary>
        public int EmployeeCount =>
            InnerProperty.Employees.Count;

        /// <summary>
        /// Gets the number of buildable items currently placed in this property.
        /// </summary>
        public int BuildableItemCount =>
            (ReflectionUtils.TryGetFieldOrProperty(InnerProperty, "BuildableItems") as IList)?.Count ?? 0;

        /// <summary>
        /// Gets the position of the NPC spawn point for this property.
        /// Returns Vector3.zero if no NPC spawn point is configured.
        /// </summary>
        public Vector3 NPCSpawnPosition =>
            InnerProperty.NPCSpawnPoint != null ? InnerProperty.NPCSpawnPoint.position : Vector3.zero;

        /// <summary>
        /// Gets the number of employee idle points configured for this property.
        /// </summary>
        public int EmployeeIdlePointCount =>
            InnerProperty.EmployeeIdlePoints?.Length ?? 0;

        /// <summary>
        /// Gets the position of a specific employee idle point by index.
        /// </summary>
        /// <param name="index">The zero-based index of the idle point.</param>
        /// <returns>The position of the idle point, or Vector3.zero if the index is invalid.</returns>
        public Vector3 GetEmployeeIdlePointPosition(int index)
        {
            if (InnerProperty.EmployeeIdlePoints != null &&
                index >= 0 &&
                index < InnerProperty.EmployeeIdlePoints.Length &&
                InnerProperty.EmployeeIdlePoints[index] != null)
            {
                return InnerProperty.EmployeeIdlePoints[index].position;
            }
            return Vector3.zero;
        }

        /// <summary>
        /// Gets the number of unassigned beds currently available in this property.
        /// </summary>
        /// <returns>The count of beds that do not have an assigned employee.</returns>
        public int GetUnassignedBedCount() =>
            InnerProperty.GetUnassignedBeds().Count;

        /// <summary>
        /// INTERNAL: Prefix used for locating property signs in the scene hierarchy.
        /// </summary>
        internal virtual string SignPrefix => "@Properties/";

        /// <summary>
        /// INTERNAL: Path to the whiteboard text object in the scene hierarchy for updating the property price display.
        /// </summary>
        internal virtual string WhiteboardPath =>
            $"Map/Container/RE Office/Interior/Whiteboard/PropertyListing {PropertyName}/Price";

        /// <summary>
        /// INTERNAL: Collection of potential paths to the sign text objects in the scene hierarchy for updating the property price display.
        /// </summary>
        internal virtual IEnumerable<string> SignPaths
        {
            get
            {
                var baseName = PropertyName.Replace(" ", "");
                yield return $"{SignPrefix}{baseName}/ForSaleSign/Price";
                yield return $"{SignPrefix}{PropertyName}/ForSaleSign/Price";
                yield return $"{SignPrefix}{baseName}/ForSaleSign (1)/Price";
                yield return $"{SignPrefix}{PropertyName}/ForSaleSign (1)/Price";
            }
        }

        /// <summary>
        /// INTERNAL: Updates the price display on both the whiteboard and property signs in the game world.
        /// Will not update property sign for Sweatshop property, as it uses a Texture based sign.
        /// </summary>
        /// <param name="price">The new price to display.</param>
        internal void UpdatePriceDisplay(float price)
        {
            var formattedPrice = MoneyManager.FormatAmount(price);

            // Whiteboard update
            TrySetTMPText(WhiteboardPath, formattedPrice, "whiteboard");

            // Sign updates
            foreach (var path in SignPaths)
            {
                if (TrySetTMPText(path, formattedPrice, "sign"))
                    break;
            }
        }

        /// <summary>
        /// INTERNAL: Attempts to find a TextMeshPro text component at the specified path and update its text to display the given price.
        /// </summary>
        /// <param name="path">The hierarchy path to the GameObject containing the TMP_Text component.</param>
        /// <param name="priceText">The formatted price text to set.</param>
        /// <param name="context">A string indicating the context (e.g., "whiteboard" or "sign") for logging purposes.</param>
        /// <returns>True if the text was successfully updated; otherwise, false.</returns>
        internal bool TrySetTMPText(string path, string priceText, string context)
        {
            var obj = GameObject.Find(path);
            if (obj != null)
            {
                var textComp = obj.GetComponent<TMP_Text>();
                if (textComp != null)
                {
                    textComp.text = priceText;
                    return true;
                }
                Debug.LogWarning($"TMP_Text not found on {context} price object.");
            }
            else
            {
                Debug.LogWarning($"{context} price object not found at path: {path}");
            }
            return false;
        }
    }
}