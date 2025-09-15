using System;
using System.Reflection;
using Newtonsoft.Json;
using S1API.Internal.Utils;

namespace S1API.Internal.Abstraction
{
    /// <summary>
    /// INTERNAL: JSON Converter to handle GUID referencing classes when saved and loaded.
    /// </summary>
    internal class GUIDReferenceConverter : JsonConverter
    {
        /// <summary>
        /// Returns true if the provided type implements
        /// <see cref="IGUIDReference"/> and can be converted.
        /// </summary>
        /// <param name="objectType">The type to check.</param>
        /// <returns>True when assignable to <see cref="IGUIDReference"/>.</returns>
        public override bool CanConvert(Type objectType) =>
            typeof(IGUIDReference).IsAssignableFrom(objectType);

        /// <summary>
        /// Writes the GUID backing value of an <see cref="IGUIDReference"/>
        /// instance; writes null if the value is not a reference.
        /// </summary>
        /// <param name="writer">JSON writer.</param>
        /// <param name="value">Reference instance.</param>
        /// <param name="serializer">JSON serializer.</param>
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is IGUIDReference reference)
            {
                writer.WriteValue(reference.GUID);
            }
            else
            {
                writer.WriteNull();
            }
        }

        /// <summary>
        /// Reads a GUID string and resolves the corresponding
        /// instance by invoking a non-public static
        /// <c>GetFromGUID(string guid)</c> method on the target type.
        /// </summary>
        /// <param name="reader">JSON reader positioned at the GUID value.</param>
        /// <param name="objectType">Target reference type.</param>
        /// <param name="existingValue">Existing value (ignored).</param>
        /// <param name="serializer">JSON serializer.</param>
        /// <returns>Resolved reference instance or null.</returns>
        /// <exception cref="Exception">Thrown if the type does not
        /// implement the expected <c>GetFromGUID</c> lookup method.</exception>
        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            string? guid = reader.Value?.ToString();
            if (string.IsNullOrEmpty(guid))
                return null;
            
            MethodInfo? getGUIDMethod = ReflectionUtils.GetMethod(objectType, "GetFromGUID", BindingFlags.NonPublic | BindingFlags.Static);
            if (getGUIDMethod == null)
                throw new Exception($"The type {objectType.Name} does not have a valid implementation of the GetFromGUID(string guid) method!");
            
            return getGUIDMethod.Invoke(null, new object[] { guid });
        }

        /// <summary>
        /// Indicates the converter supports reading JSON.
        /// </summary>
        public override bool CanRead => true;
    }
}
