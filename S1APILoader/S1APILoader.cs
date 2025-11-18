using System;
using System.IO;
using System.Reflection;
using MelonLoader;

[assembly: MelonInfo(typeof(S1APILoader.S1APILoader), "S1APILoader", "2.4.4", "KaBooMa")]

namespace S1APILoader
{
    public class S1APILoader : MelonPlugin
    {
        private const string BuildFolderName = "S1API";
            
        public override void OnPreModsLoaded()
        {
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
            NormalizeBuild(modsFolder, activeBuild, shouldBeEnabled: true, fileNamePattern: "S1API.{0}.MelonLoader.dll");
            NormalizeBuild(modsFolder, inactiveBuild, shouldBeEnabled: false, fileNamePattern: "S1API.{0}.MelonLoader.dll");
            
            // Normalize both builds in Plugins/S1API folder: keep exactly one file per build
            // - Active build: keep enabled dll (newest by assembly version or write time)
            // - Inactive build: keep disabled dll (newest by assembly version or write time)
            NormalizeBuild(s1apiPluginsFolder, activeBuild, shouldBeEnabled: true, fileNamePattern: "S1API.{0}.dll");
            NormalizeBuild(s1apiPluginsFolder, inactiveBuild, shouldBeEnabled: false, fileNamePattern: "S1API.{0}.dll");
            
            MelonLogger.Msg($"Successfully loaded S1API for {activeBuild}!");
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
                    MelonLogger.Warning($"Copied '{sourcePath}' to '{destinationPath}' but could not delete source. You may need to manually delete it.");
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