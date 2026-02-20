using System;
using System.IO;
using System.Linq;
using System.Reflection;
using MelonLoader;
using MelonLoader.Utils;

[assembly: MelonInfo(typeof(S1APILoader.S1APILoader), "S1APILoader", "2.5.0", "KaBooMa & Bars")]

namespace S1APILoader
{
    public class S1APILoader : MelonPlugin
    {
        private const string BuildFolderName = "S1API";
        private const string ProblematicMelonVersion = "0.7.1";
        private const int InteropFixRevision = 1;
        private const string InteropMarkerFileName = "S1API.InteropFix.marker";
        private static bool _earlyInteropFixAttempted;

        public override void OnApplicationEarlyStart()
        {
            TryApplyEarlyIl2CppInteropFix();

            string? pluginsFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (pluginsFolder == null)
                throw new Exception("Failed to identify plugins folder.");

            string modsFolder = Path.GetFullPath(Path.Combine(pluginsFolder, "../Mods"));
            string s1apiPluginsFolder = Path.Combine(pluginsFolder, BuildFolderName);

            string activeBuild = MelonUtils.IsGameIl2Cpp() ? "Il2Cpp" : "Mono";
            string inactiveBuild = !MelonUtils.IsGameIl2Cpp() ? "Il2Cpp" : "Mono";

            MelonLogger.Msg($"Loading S1API for {activeBuild}...");

            // Normalize both builds in Mods folder: keep exactly one file per build
            // - Active build: keep enabled dll (newest by assembly version or write time)
            // - Inactive build: keep disabled dll (newest by assembly version or write time)
            NormalizeBuild(modsFolder, activeBuild, shouldBeEnabled: true,
                fileNamePattern: "S1API.{0}.MelonLoader.dll");
            NormalizeBuild(modsFolder, inactiveBuild, shouldBeEnabled: false,
                fileNamePattern: "S1API.{0}.MelonLoader.dll");

            // Normalize both builds in Plugins/S1API folder: keep exactly one file per build
            // - Active build: keep enabled dll (newest by assembly version or write time)
            // - Inactive build: keep disabled dll (newest by assembly version or write time)
            NormalizeBuild(s1apiPluginsFolder, activeBuild, shouldBeEnabled: true, fileNamePattern: "S1API.{0}.dll");
            NormalizeBuild(s1apiPluginsFolder, inactiveBuild, shouldBeEnabled: false, fileNamePattern: "S1API.{0}.dll");

            MelonLogger.Msg($"Successfully loaded S1API for {activeBuild}!");
        }

        private static void TryApplyEarlyIl2CppInteropFix()
        {
            if (_earlyInteropFixAttempted)
                return;

            _earlyInteropFixAttempted = true;

            if (!MelonUtils.IsGameIl2Cpp())
                return;

            string? melonVersion = GetMelonLoaderVersion();
            if (string.IsNullOrEmpty(melonVersion) ||
                !melonVersion.StartsWith(ProblematicMelonVersion, StringComparison.OrdinalIgnoreCase))
                return;

            MelonLogger.Warning("[S1APILoader] MelonLoader 0.7.1 detected. Applying early Il2CppInterop workaround...");
            RegisterAssemblyResolveFallback();
            InstallIl2CppInteropFixes();
            TryForceAssemblyRegenerationIfNeeded(melonVersion);
        }

        private static string? GetMelonLoaderVersion()
        {
            try
            {
                Assembly melonAssembly = typeof(MelonPlugin).Assembly;
                Type? buildInfoType = melonAssembly.GetType("MelonLoader.Properties.BuildInfo");
                PropertyInfo? versionProp = buildInfoType?.GetProperty("VersionNumber",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                object? value = versionProp?.GetValue(null);
                if (value != null)
                    return value.ToString();

                Version? version = melonAssembly.GetName().Version;
                if (version != null)
                    return $"{version.Major}.{version.Minor}.{version.Build}";
            }
            catch
            {
            }

            return null;
        }

        /// <summary>
        /// When Assembly.Load("Il2CppAssembly-CSharp") fails, the CLR fires AssemblyResolve.
        /// We return the already-loaded "Assembly-CSharp" from GetAssemblies() - we never call Assembly.Load.
        /// </summary>
        private static void RegisterAssemblyResolveFallback()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
            {
                string? requestedName = new AssemblyName(args.Name).Name;
                if (string.IsNullOrEmpty(requestedName) || !requestedName.StartsWith("Il2Cpp", StringComparison.Ordinal))
                    return null;

                string strippedName = requestedName.Substring("Il2Cpp".Length);
                if (string.IsNullOrEmpty(strippedName))
                    return null;

                return AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => string.Equals(a.GetName().Name, strippedName, StringComparison.OrdinalIgnoreCase));
            };
        }

        private static void InstallIl2CppInteropFixes()
        {
            try
            {
                Assembly melonAssembly = typeof(MelonPlugin).Assembly;
                Type? fixesType = melonAssembly.GetType("MelonLoader.Fixes.Il2CppInteropFixes");
                MethodInfo? installMethod =
                    fixesType?.GetMethod("Install", BindingFlags.NonPublic | BindingFlags.Static);
                if (installMethod == null)
                {
                    MelonLogger.Warning("[S1APILoader] Could not find Il2CppInteropFixes.Install().");
                    return;
                }

                installMethod.Invoke(null, null);
                MelonLogger.Msg("[S1APILoader] Early Il2CppInterop fixes applied.");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[S1APILoader] Failed to apply early Il2CppInterop fixes: {ex.Message}");
            }
        }

        private static void TryForceAssemblyRegenerationIfNeeded(string melonVersion)
        {
            try
            {
                string markerPath = GetInteropMarkerPath();

                if (IsMarkerValid(markerPath, melonVersion))
                {
                    MelonLogger.Msg("[S1APILoader] Interop fix marker found. Skipping assembly regeneration.");
                    return;
                }

                if (!HasPreGeneratedIl2CppAssemblies())
                {
                    MelonLogger.Msg(
                        "[S1APILoader] No pre-generated Il2Cpp assemblies found. MelonLoader will generate them normally.");
                    WriteMarker(markerPath, melonVersion);
                    return;
                }

                MelonLogger.Warning("[S1APILoader] Pre-generated Il2Cpp assemblies detected. Forcing regeneration...");

                if (ForceRunIl2CppAssemblyGenerator())
                {
                    WriteMarker(markerPath, melonVersion);
                    MelonLogger.Msg("[S1APILoader] Il2Cpp assembly regeneration completed successfully.");
                }
                else
                {
                    MelonLogger.Warning(
                        "[S1APILoader] Il2Cpp assembly regeneration may have failed. Will retry on next launch.");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[S1APILoader] Error during assembly regeneration check: {ex.Message}");
            }
        }

        private static string GetInteropMarkerPath()
        {
            string userDataDir = MelonEnvironment.UserDataDirectory;
            if (!Directory.Exists(userDataDir))
                Directory.CreateDirectory(userDataDir);

            return Path.Combine(userDataDir, InteropMarkerFileName);
        }

        private static bool IsMarkerValid(string markerPath, string melonVersion)
        {
            if (!File.Exists(markerPath))
                return false;

            try
            {
                string[] lines = File.ReadAllLines(markerPath);
                if (lines.Length < 2)
                    return false;

                string? storedVersion = null;
                int storedRevision = -1;

                foreach (string line in lines)
                {
                    if (line.StartsWith("melonloader=", StringComparison.OrdinalIgnoreCase))
                        storedVersion = line.Substring("melonloader=".Length).Trim();
                    else if (line.StartsWith("fixrevision=", StringComparison.OrdinalIgnoreCase))
                    {
                        string revStr = line.Substring("fixrevision=".Length).Trim();
                        int.TryParse(revStr, out storedRevision);
                    }
                }

                if (string.IsNullOrEmpty(storedVersion))
                    return false;

                bool versionMatch = storedVersion.Equals(melonVersion, StringComparison.OrdinalIgnoreCase);
                bool revisionMatch = storedRevision >= InteropFixRevision;

                return versionMatch && revisionMatch;
            }
            catch
            {
                return false;
            }
        }

        private static void WriteMarker(string markerPath, string melonVersion)
        {
            try
            {
                string content =
                    $"melonloader={melonVersion}{Environment.NewLine}fixrevision={InteropFixRevision}{Environment.NewLine}timestamp={DateTime.UtcNow:O}";
                File.WriteAllText(markerPath, content);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[S1APILoader] Failed to write interop fix marker: {ex.Message}");
            }
        }

        private static bool HasPreGeneratedIl2CppAssemblies()
        {
            try
            {
                string il2cppAssembliesDir = MelonEnvironment.Il2CppAssembliesDirectory;
                if (!Directory.Exists(il2cppAssembliesDir))
                    return false;

                string[] dllFiles =
                    Directory.GetFiles(il2cppAssembliesDir, "Il2Cpp*.dll", SearchOption.TopDirectoryOnly);
                return dllFiles.Length > 0;
            }
            catch
            {
                return false;
            }
        }

        private static bool ForceRunIl2CppAssemblyGenerator()
        {
            Assembly melonAssembly = typeof(MelonPlugin).Assembly;

            try
            {
                Type? loaderConfigType = melonAssembly.GetType("MelonLoader.LoaderConfig");
                PropertyInfo? currentProp =
                    loaderConfigType?.GetProperty("Current", BindingFlags.Public | BindingFlags.Static);
                object? configCurrent = currentProp?.GetValue(null);

                if (configCurrent != null)
                {
                    PropertyInfo? unityEngineProp = configCurrent.GetType()
                        .GetProperty("UnityEngine", BindingFlags.Public | BindingFlags.Instance);
                    object? unityEngineConfig = unityEngineProp?.GetValue(configCurrent);

                    if (unityEngineConfig != null)
                    {
                        PropertyInfo? forceRegenProp = unityEngineConfig.GetType()
                            .GetProperty("ForceRegeneration", BindingFlags.Public | BindingFlags.Instance);
                        if (forceRegenProp != null && forceRegenProp.CanWrite)
                        {
                            forceRegenProp.SetValue(unityEngineConfig, true);
                            MelonLogger.Msg(
                                "[S1APILoader] Set LoaderConfig.Current.UnityEngine.ForceRegeneration=true");
                        }
                        else
                        {
                            MelonLogger.Warning("[S1APILoader] ForceRegeneration property is read-only or not found.");
                        }
                    }
                    else
                    {
                        MelonLogger.Warning("[S1APILoader] UnityEngine config is null.");
                    }
                }
                else
                {
                    MelonLogger.Warning("[S1APILoader] LoaderConfig.Current is null.");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[S1APILoader] Error setting ForceRegeneration via LoaderConfig: {ex.Message}");
            }

            return true;
        }

        private static void NormalizeBuild(string folder, string build, bool shouldBeEnabled, string fileNamePattern)
        {
            // Ensure folder exists
            if (!Directory.Exists(folder))
                return; // Folder doesn't exist, nothing to do

            string fileName = string.Format(fileNamePattern, build);
            string enabledPath = Path.Combine(folder, fileName);
            string disabledPath = $"{enabledPath}.disabled";

            bool enabledExists = File.Exists(enabledPath);
            bool disabledExists = File.Exists(disabledPath);

            if (!enabledExists && !disabledExists)
                return; // Nothing to do for this build

            if (shouldBeEnabled)
            {
                // We want this build to be enabled
                if (enabledExists && !disabledExists)
                {
                    // Already enabled, nothing to do
                    return;
                }

                // We need to ensure it's enabled
                string? sourcePath = null;
                if (disabledExists)
                    sourcePath = disabledPath;
                if (enabledExists)
                    sourcePath = ChooseNewest(sourcePath, enabledPath);

                if (sourcePath == null)
                    return;

                // If source is disabled, move it to enabled
                if (StringComparer.OrdinalIgnoreCase.Equals(sourcePath, disabledPath))
                {
                    SafeDelete(enabledPath);
                    SafeMoveReplace(disabledPath, enabledPath);
                }
                // Remove any leftover disabled duplicate if we have an enabled version
                else if (disabledExists)
                {
                    SafeDelete(disabledPath);
                }
            }
            else
            {
                // We want this build to be disabled
                if (!enabledExists && disabledExists)
                {
                    // Already disabled, nothing to do
                    return;
                }

                // We need to ensure it's disabled
                if (enabledExists)
                {
                    // Delete any existing disabled version first (in case it's older)
                    SafeDelete(disabledPath);
                    // Move the enabled file to disabled
                    bool moveSucceeded = SafeMoveReplace(enabledPath, disabledPath);

                    // Verify the disable operation succeeded
                    if (!moveSucceeded || File.Exists(enabledPath))
                    {
                        MelonLogger.Warning($"Failed to disable '{enabledPath}'. File may be locked or in use.");
                        // Try alternative: copy and delete
                        if (File.Exists(enabledPath))
                        {
                            TryCopyAndDelete(enabledPath, disabledPath);
                        }
                    }
                }
            }
        }

        private static string ChooseNewest(string? currentBestPath, string candidatePath)
        {
            if (currentBestPath == null)
                return candidatePath;

            Version? bestVersion = TryGetAssemblyVersion(currentBestPath);
            Version? candidateVersion = TryGetAssemblyVersion(candidatePath);

            if (bestVersion != null && candidateVersion != null)
                return candidateVersion > bestVersion ? candidatePath : currentBestPath;

            DateTime bestTime = GetSafeWriteTimeUtc(currentBestPath);
            DateTime candidateTime = GetSafeWriteTimeUtc(candidatePath);
            return candidateTime > bestTime ? candidatePath : currentBestPath;
        }

        private static Version? TryGetAssemblyVersion(string path)
        {
            try
            {
                return AssemblyName.GetAssemblyName(path).Version;
            }
            catch
            {
                return null;
            }
        }

        private static DateTime GetSafeWriteTimeUtc(string path)
        {
            try
            {
                return File.GetLastWriteTimeUtc(path);
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        private static bool SafeMoveReplace(string sourcePath, string destinationPath)
        {
            try
            {
                if (File.Exists(destinationPath))
                    File.Delete(destinationPath);
                File.Move(sourcePath, destinationPath);
                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"Failed to move '{sourcePath}' to '{destinationPath}': {ex.Message}");
                return false;
            }
        }

        private static void TryCopyAndDelete(string sourcePath, string destinationPath)
        {
            try
            {
                if (File.Exists(destinationPath))
                    File.Delete(destinationPath);
                File.Copy(sourcePath, destinationPath, overwrite: true);
                // Try to delete the source, but don't fail if it's locked
                try
                {
                    File.Delete(sourcePath);
                }
                catch
                {
                    MelonLogger.Warning(
                        $"Copied '{sourcePath}' to '{destinationPath}' but could not delete source. You may need to manually delete it.");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"Failed to copy '{sourcePath}' to '{destinationPath}': {ex.Message}");
            }
        }

        private static void SafeDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"Failed to delete '{path}': {ex.Message}");
            }
        }
    }
}