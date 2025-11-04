using System;
using System.Collections.Generic;
using S1API.Internal.Utils;

namespace S1API.Entities.Appearances.Base
{
    /// <summary>
    /// The base accessory appearance class
    /// </summary>
    /// <remarks>This is used to track the properties within the AvatarSettings</remarks>
    public class BaseAccessoryAppearance
    {
        /// <summary>
        /// Retrieves and caches all public <c>const string</c> fields defined in the specified type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">
        /// The type from which to retrieve constant string fields. Must inherit from <see cref="BaseAccessoryAppearance"/>.
        /// </typeparam>
        /// <returns>
        /// A list of constant string values defined in the type <typeparamref name="T"/>.
        /// </returns>
        /// <remarks>
        /// Uses reflection to gather constants and caches them for future calls to improve performance.
        /// </remarks>
        internal static List<string> GetConstPaths<T>() where T : BaseAccessoryAppearance
        {
            return ReflectionUtils.GetConstStringFields(typeof(T));
        }
    }
}
