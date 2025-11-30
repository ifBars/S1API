#if (IL2CPPMELON)
using S1Customization = Il2CppScheduleOne.AvatarFramework.Customization;
using Il2CppCollectionsGeneric = Il2CppSystem.Collections.Generic;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Customization = ScheduleOne.AvatarFramework.Customization;
#endif

using System.Collections.Generic;
using UnityEngine;

namespace S1API.Avatar
{
    /// <summary>
    /// Simplified avatar settings format used by the character creator.
    /// Provides a more user-friendly interface for basic character customization compared to the full AvatarSettings.
    /// </summary>
    public sealed class BasicAvatarSettings
    {
        /// <summary>
        /// INTERNAL: Reference to the game BasicAvatarSettings instance.
        /// </summary>
        internal readonly S1Customization.BasicAvatarSettings S1BasicAvatarSettings;

        /// <summary>
        /// INTERNAL: Constructor to create a wrapper from a game BasicAvatarSettings instance.
        /// </summary>
        /// <param name="settings">The game BasicAvatarSettings instance to wrap.</param>
        internal BasicAvatarSettings(S1Customization.BasicAvatarSettings settings)
        {
            S1BasicAvatarSettings = settings;
        }

        /// <summary>
        /// Creates a new BasicAvatarSettings instance.
        /// </summary>
        public static BasicAvatarSettings Create()
        {
            var settings = ScriptableObject.CreateInstance<S1Customization.BasicAvatarSettings>();
            settings.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return new BasicAvatarSettings(settings);
        }

        /// <summary>
        /// Gender value (0 = male, 1 = female).
        /// </summary>
        public int Gender
        {
            get => S1BasicAvatarSettings?.Gender ?? 0;
            set { if (S1BasicAvatarSettings != null) S1BasicAvatarSettings.Gender = value; }
        }

        /// <summary>
        /// Weight value (typically 0.0 to 1.0).
        /// </summary>
        public float Weight
        {
            get => S1BasicAvatarSettings?.Weight ?? 0.5f;
            set { if (S1BasicAvatarSettings != null) S1BasicAvatarSettings.Weight = value; }
        }

        /// <summary>
        /// Skin color.
        /// </summary>
        public Color SkinColor
        {
            get => S1BasicAvatarSettings?.SkinColor ?? new Color(0.6f, 0.5f, 0.4f);
            set { if (S1BasicAvatarSettings != null) S1BasicAvatarSettings.SkinColor = value; }
        }

        /// <summary>
        /// Hair style path/identifier.
        /// </summary>
        public string HairStyle
        {
            get => S1BasicAvatarSettings?.HairStyle ?? string.Empty;
            set { if (S1BasicAvatarSettings != null) S1BasicAvatarSettings.HairStyle = value ?? string.Empty; }
        }

        /// <summary>
        /// Hair color.
        /// </summary>
        public Color HairColor
        {
            get => S1BasicAvatarSettings?.HairColor ?? Color.black;
            set { if (S1BasicAvatarSettings != null) S1BasicAvatarSettings.HairColor = value; }
        }

        /// <summary>
        /// Mouth layer path/identifier.
        /// </summary>
        public string Mouth
        {
            get => S1BasicAvatarSettings?.Mouth ?? string.Empty;
            set { if (S1BasicAvatarSettings != null) S1BasicAvatarSettings.Mouth = value ?? string.Empty; }
        }

        /// <summary>
        /// Facial hair layer path/identifier.
        /// </summary>
        public string FacialHair
        {
            get => S1BasicAvatarSettings?.FacialHair ?? string.Empty;
            set { if (S1BasicAvatarSettings != null) S1BasicAvatarSettings.FacialHair = value ?? string.Empty; }
        }

        /// <summary>
        /// Facial details layer path/identifier (e.g., scars, makeup).
        /// </summary>
        public string FacialDetails
        {
            get => S1BasicAvatarSettings?.FacialDetails ?? string.Empty;
            set { if (S1BasicAvatarSettings != null) S1BasicAvatarSettings.FacialDetails = value ?? string.Empty; }
        }

        /// <summary>
        /// Facial details intensity (0.0 to 1.0).
        /// </summary>
        public float FacialDetailsIntensity
        {
            get => S1BasicAvatarSettings?.FacialDetailsIntensity ?? 0.0f;
            set { if (S1BasicAvatarSettings != null) S1BasicAvatarSettings.FacialDetailsIntensity = value; }
        }

        /// <summary>
        /// Eyeball color.
        /// </summary>
        public Color EyeballColor
        {
            get => S1BasicAvatarSettings?.EyeballColor ?? new Color(0.2f, 0.4f, 0.6f);
            set { if (S1BasicAvatarSettings != null) S1BasicAvatarSettings.EyeballColor = value; }
        }

        /// <summary>
        /// Upper eye lid resting position (0.0 to 1.0).
        /// </summary>
        public float UpperEyeLidRestingPosition
        {
            get => S1BasicAvatarSettings?.UpperEyeLidRestingPosition ?? 0.5f;
            set { if (S1BasicAvatarSettings != null) S1BasicAvatarSettings.UpperEyeLidRestingPosition = value; }
        }

        /// <summary>
        /// Lower eye lid resting position (0.0 to 1.0).
        /// </summary>
        public float LowerEyeLidRestingPosition
        {
            get => S1BasicAvatarSettings?.LowerEyeLidRestingPosition ?? 0.5f;
            set { if (S1BasicAvatarSettings != null) S1BasicAvatarSettings.LowerEyeLidRestingPosition = value; }
        }

        /// <summary>
        /// Pupil dilation value (typically 0.0 to 1.0).
        /// </summary>
        public float PupilDilation
        {
            get => S1BasicAvatarSettings?.PupilDilation ?? 1.0f;
            set { if (S1BasicAvatarSettings != null) S1BasicAvatarSettings.PupilDilation = value; }
        }

        /// <summary>
        /// Eyebrow scale value.
        /// </summary>
        public float EyebrowScale
        {
            get => S1BasicAvatarSettings?.EyebrowScale ?? 1.0f;
            set { if (S1BasicAvatarSettings != null) S1BasicAvatarSettings.EyebrowScale = value; }
        }

        /// <summary>
        /// Eyebrow thickness value.
        /// </summary>
        public float EyebrowThickness
        {
            get => S1BasicAvatarSettings?.EyebrowThickness ?? 1.0f;
            set { if (S1BasicAvatarSettings != null) S1BasicAvatarSettings.EyebrowThickness = value; }
        }

        /// <summary>
        /// Eyebrow resting height value.
        /// </summary>
        public float EyebrowRestingHeight
        {
            get => S1BasicAvatarSettings?.EyebrowRestingHeight ?? 0.0f;
            set { if (S1BasicAvatarSettings != null) S1BasicAvatarSettings.EyebrowRestingHeight = value; }
        }

        /// <summary>
        /// Eyebrow resting angle value.
        /// </summary>
        public float EyebrowRestingAngle
        {
            get => S1BasicAvatarSettings?.EyebrowRestingAngle ?? 0.0f;
            set { if (S1BasicAvatarSettings != null) S1BasicAvatarSettings.EyebrowRestingAngle = value; }
        }

        /// <summary>
        /// Top clothing layer path/identifier.
        /// </summary>
        public string Top
        {
            get => S1BasicAvatarSettings?.Top ?? string.Empty;
            set { if (S1BasicAvatarSettings != null) S1BasicAvatarSettings.Top = value ?? string.Empty; }
        }

        /// <summary>
        /// Top clothing color.
        /// </summary>
        public Color TopColor
        {
            get => S1BasicAvatarSettings?.TopColor ?? Color.white;
            set { if (S1BasicAvatarSettings != null) S1BasicAvatarSettings.TopColor = value; }
        }

        /// <summary>
        /// Bottom clothing layer path/identifier.
        /// </summary>
        public string Bottom
        {
            get => S1BasicAvatarSettings?.Bottom ?? string.Empty;
            set { if (S1BasicAvatarSettings != null) S1BasicAvatarSettings.Bottom = value ?? string.Empty; }
        }

        /// <summary>
        /// Bottom clothing color.
        /// </summary>
        public Color BottomColor
        {
            get => S1BasicAvatarSettings?.BottomColor ?? Color.white;
            set { if (S1BasicAvatarSettings != null) S1BasicAvatarSettings.BottomColor = value; }
        }

        /// <summary>
        /// Shoes accessory path/identifier.
        /// </summary>
        public string Shoes
        {
            get => S1BasicAvatarSettings?.Shoes ?? string.Empty;
            set { if (S1BasicAvatarSettings != null) S1BasicAvatarSettings.Shoes = value ?? string.Empty; }
        }

        /// <summary>
        /// Shoes color.
        /// </summary>
        public Color ShoesColor
        {
            get => S1BasicAvatarSettings?.ShoesColor ?? Color.white;
            set { if (S1BasicAvatarSettings != null) S1BasicAvatarSettings.ShoesColor = value; }
        }

        /// <summary>
        /// Headwear accessory path/identifier.
        /// </summary>
        public string Headwear
        {
            get => S1BasicAvatarSettings?.Headwear ?? string.Empty;
            set { if (S1BasicAvatarSettings != null) S1BasicAvatarSettings.Headwear = value ?? string.Empty; }
        }

        /// <summary>
        /// Headwear color.
        /// </summary>
        public Color HeadwearColor
        {
            get => S1BasicAvatarSettings?.HeadwearColor ?? Color.white;
            set { if (S1BasicAvatarSettings != null) S1BasicAvatarSettings.HeadwearColor = value; }
        }

        /// <summary>
        /// Eyewear accessory path/identifier.
        /// </summary>
        public string Eyewear
        {
            get => S1BasicAvatarSettings?.Eyewear ?? string.Empty;
            set { if (S1BasicAvatarSettings != null) S1BasicAvatarSettings.Eyewear = value ?? string.Empty; }
        }

        /// <summary>
        /// Eyewear color.
        /// </summary>
        public Color EyewearColor
        {
            get => S1BasicAvatarSettings?.EyewearColor ?? Color.white;
            set { if (S1BasicAvatarSettings != null) S1BasicAvatarSettings.EyewearColor = value; }
        }

        /// <summary>
        /// Gets a list of tattoo layer paths.
        /// </summary>
        public List<string> GetTattoos()
        {
            var result = new List<string>();
            if (S1BasicAvatarSettings?.Tattoos == null)
                return result;

            for (int i = 0; i < S1BasicAvatarSettings.Tattoos.Count; i++)
            {
                result.Add(S1BasicAvatarSettings.Tattoos[i] ?? string.Empty);
            }

            return result;
        }

        /// <summary>
        /// Sets the tattoo layer paths.
        /// </summary>
        /// <param name="tattoos">List of tattoo layer paths.</param>
        public void SetTattoos(List<string> tattoos)
        {
            if (S1BasicAvatarSettings == null)
                return;

#if (IL2CPPMELON)
            var list = new Il2CppCollectionsGeneric.List<string>();
#else
            var list = new List<string>();
#endif

            if (tattoos != null)
            {
                foreach (var tattoo in tattoos)
                {
                    if (!string.IsNullOrWhiteSpace(tattoo))
                    {
                        list.Add(tattoo);
                    }
                }
            }

            S1BasicAvatarSettings.Tattoos = list;
        }

        /// <summary>
        /// Adds a tattoo layer path to the tattoos list.
        /// </summary>
        /// <param name="tattooPath">The tattoo layer path to add.</param>
        public void AddTattoo(string tattooPath)
        {
            if (S1BasicAvatarSettings == null || string.IsNullOrWhiteSpace(tattooPath))
                return;

            if (S1BasicAvatarSettings.Tattoos == null)
            {
#if (IL2CPPMELON)
                S1BasicAvatarSettings.Tattoos = new Il2CppCollectionsGeneric.List<string>();
#else
                S1BasicAvatarSettings.Tattoos = new List<string>();
#endif
            }

            S1BasicAvatarSettings.Tattoos.Add(tattooPath);
        }

        /// <summary>
        /// Converts this BasicAvatarSettings to a full AvatarSettings instance.
        /// </summary>
        /// <returns>A new AvatarSettings instance with the converted settings.</returns>
        public AvatarSettings ToAvatarSettings()
        {
            if (S1BasicAvatarSettings == null)
                return AvatarSettings.Create();

            var s1AvatarSettings = S1BasicAvatarSettings.GetAvatarSettings();
            return new AvatarSettings(s1AvatarSettings);
        }

        /// <summary>
        /// Sets a field value by name using reflection.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="fieldName">The name of the field.</param>
        /// <param name="value">The value to set.</param>
        public void SetValue<T>(string fieldName, T value)
        {
            if (S1BasicAvatarSettings == null || string.IsNullOrWhiteSpace(fieldName))
                return;

            S1BasicAvatarSettings.SetValue(fieldName, value);
        }

        /// <summary>
        /// Gets a field value by name using reflection.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="fieldName">The name of the field.</param>
        /// <returns>The field value, or default(T) if not found.</returns>
        public T GetValue<T>(string fieldName)
        {
            if (S1BasicAvatarSettings == null || string.IsNullOrWhiteSpace(fieldName))
                return default(T);

            return S1BasicAvatarSettings.GetValue<T>(fieldName);
        }
    }
}
