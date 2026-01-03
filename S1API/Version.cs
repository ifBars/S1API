using MelonLoader;
using MelonLoader.Utils;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace S1API
{
    /// <summary>
    /// Version checking functionality for MelonLoader compatibility.
    /// Credits: estonia___ and k073l (S1 Modding Discord)
    /// </summary>
    public static class VersionChecker
    {
        private const string PROBLEMATIC_VERSION = "0.7.1";
        private const string RECOMMENDED_VERSION_1 = "0.7.0";
        private const string RECOMMENDED_VERSION_2 = "0.7.2-nightly";

        /// <summary>
        /// Checks the current MelonLoader version and warns the user if it's a known problematic version.
        /// </summary>
        public static void CheckMelonLoaderVersion()
        {
            try
            {
                var melonVersion = GetMelonLoaderVersion();

                if (string.IsNullOrEmpty(melonVersion))
                {
                    MelonLogger.Warning("[S1API VersionChecker] Could not determine MelonLoader version!");
                    return;
                }

                MelonLogger.Msg("========================================");
                MelonLogger.Msg($"[S1API VersionChecker] MelonLoader Version Detected: {melonVersion}");
                MelonLogger.Msg("========================================");

                // Check if version matches problematic version (0.7.1.x)
                if (IsProblematicVersion(melonVersion))
                {
                    ShowBigWarning(melonVersion);
                }
                else
                {
                    MelonLogger.Msg("[S1API VersionChecker] Your MelonLoader version appears to be compatible!");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[S1API VersionChecker] Version check failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the MelonLoader version string.
        /// Tries to get SemVersion from BuildInfo first, falls back to Assembly version.
        /// </summary>
        private static string GetMelonLoaderVersion()
        {
            try
            {
                var melonAssembly = typeof(MelonMod).Assembly;

                // Try to get SemVersion from BuildInfo (MelonLoader 0.6.0+)
                var buildInfoType = melonAssembly.GetType("MelonLoader.Properties.BuildInfo");
                var versionProp = buildInfoType?.GetProperty("VersionNumber");

                if (versionProp?.GetValue(null) != null)
                {
                    // If SemVersion is available, convert to string
                    var semVer = versionProp.GetValue(null);
                    return semVer.ToString() ?? GetVersionFromAssembly(melonAssembly);
                }

                return GetVersionFromAssembly(melonAssembly);
            }
            catch
            {
                // Fallback: search loaded assemblies
                try
                {
                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        var name = assembly.GetName().Name;
                        if (name != null && (name.Equals("MelonLoader") || name.Equals("MelonLoader.Core")))
                        {
                            return GetVersionFromAssembly(assembly);
                        }
                    }
                }
                catch
                {
                    // Ignore
                }

                return null;
            }
        }

        /// <summary>
        /// Extracts version string from assembly version.
        /// </summary>
        private static string GetVersionFromAssembly(Assembly assembly)
        {
            var v = assembly.GetName().Version;
            if (v != null)
            {
                // Format as Major.Minor.Patch (ignore revision)
                return $"{v.Major}.{v.Minor}.{v.Build}";
            }
            return null;
        }

        /// <summary>
        /// Checks if the version matches the problematic version (0.7.1.x).
        /// </summary>
        private static bool IsProblematicVersion(string version)
        {
            if (string.IsNullOrEmpty(version))
                return false;

            // Check if version starts with "0.7.1" (matches 0.7.1, 0.7.1.0, 0.7.1.x, etc.)
            return version.StartsWith(PROBLEMATIC_VERSION, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Displays a prominent warning about the problematic MelonLoader version.
        /// </summary>
        /// <param name="detectedVersion">Detected MelonLoader version string.</param>
        private static void ShowBigWarning(string detectedVersion)
        {
            var warning = $"""

                       ╔════════════════════════════════════════════════════════════════════════╗
                       ║                                                                        ║
                       ║                        !!! URGENT WARNING !!!                          ║
                       ║                                                                        ║
                       ║                YOU ARE USING MELONLOADER VERSION {detectedVersion,-21} ║
                       ║                                                                        ║
                       ║      This version is KNOWN TO HAVE CRITICAL ISSUES and may cause:      ║
                       ║                                                                        ║
                       ║                - Game crashes and unexpected behavior                  ║
                       ║                - Mod incompatibility and loading failures              ║
                       ║                - Performance issues and memory leaks                   ║
                       ║                - Random errors and instability                         ║
                       ║                                                                        ║
                       ║           PLEASE UPDATE IMMEDIATELY to one of these versions:          ║
                       ║                                                                        ║
                       ║                ► {RECOMMENDED_VERSION_1,-27} (Stable Release)          ║
                       ║                ► {RECOMMENDED_VERSION_2,-27} (Latest Nightly)          ║
                       ║                                                                        ║
                       ║      Download: https://melonwiki.xyz/#/?id=automated-installation      ║
                       ║                                                                        ║
                       ╚════════════════════════════════════════════════════════════════════════╝
                       """;

            MelonLogger.Error(warning);

            MelonLogger.Error($"[S1API VersionChecker] DETECTED PROBLEMATIC MELONLOADER VERSION: {detectedVersion}");
            MelonLogger.Error($"[S1API VersionChecker] PLEASE UPDATE TO {RECOMMENDED_VERSION_1} OR {RECOMMENDED_VERSION_2}");

            MelonLogger.Error(string.Concat(Enumerable.Repeat(Environment.NewLine, 2)));
            MelonLogger.Warning(
                $"[S1API VersionChecker] IMPORTANT: This message will be available in logs for your reference at \n{Path.Combine(MelonEnvironment.MelonLoaderLogsDirectory, "Latest.log")}");
        }
    }
}
