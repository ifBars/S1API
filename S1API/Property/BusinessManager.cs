
using System.Collections.Generic;
#if (MONOMELON || IL2CPPBEPINEX || MONOBEPINEX)
using S1Property = ScheduleOne.Property;

#elif IL2CPPMELON
using S1Property = Il2CppScheduleOne.Property;
#endif

namespace S1API.Property
{
    /// <summary>
    /// Provides methods for managing and retrieving business property data within the application.
    /// </summary>
    public static class BusinessManager
    {
        /// <summary>
        /// Retrieves a list of all business properties available.
        /// </summary>
        /// <returns>A list of <see cref="BusinessWrapper"/> objects representing all available business properties.</returns>
        public static List<BusinessWrapper> GetAllBusinesses()
        {
            var list = new List<BusinessWrapper>();
            foreach (var business in S1Property.Business.Businesses)
            {
                list.Add(new BusinessWrapper(business));
            }
            return list;
        }
        
        /// <summary>
        /// Retrieves a list of all business properties that are currently owned.
        /// </summary>
        /// <returns>A list of <see cref="BusinessWrapper"/> objects representing the owned business properties.</returns>
        public static List<BusinessWrapper> GetOwnedBusinesses()
        {
            var list = new List<BusinessWrapper>();
            foreach (var business in S1Property.Business.OwnedBusinesses)
            {
                list.Add(new BusinessWrapper(business));
            }
            return list;
        }
        
        /// <summary>
        /// Finds a business property with the given name from the list of available business properties.
        /// </summary>
        /// <param name="name">The name of the business property to search for.</param>
        /// <returns>A <see cref="BusinessWrapper"/> representing the business property with the specified name if found; otherwise, null.</returns>
        public static BusinessWrapper? FindBusinessByName(string name)
        {
            foreach (var business in S1Property.Business.Businesses)
            {
                if (business.PropertyName == name)
                {
                    return new BusinessWrapper(business);
                }
            }
            return null;
        }
    }   
}