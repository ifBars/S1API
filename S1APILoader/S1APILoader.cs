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

            string activeBuild = MelonUtils.IsGameIl2Cpp() ? "Il2Cpp" : "Mono";
            string inactiveBuild = !MelonUtils.IsGameIl2Cpp() ? "Il2Cpp" : "Mono";
            
            MelonLogger.Msg($"Loading S1API for {activeBuild}...");
            
            // Normalize both builds: keep exactly one file per build
            // - Active build: keep enabled dll (newest by assembly version or write time)
            // - Inactive build: keep disabled dll (newest by assembly version or write time)
            NormalizeBuild(modsFolder, activeBuild, shouldBeEnabled: true);
            NormalizeBuild(modsFolder, inactiveBuild, shouldBeEnabled: false);
            
            MelonLogger.Msg($"Successfully loaded S1API for {activeBuild}!");
        }

        private static void NormalizeBuild(string modsFolder, string build, bool shouldBeEnabled)
        {
            string enabledPath = Path.Combine(modsFolder, $"S1API.{build}.MelonLoader.dll");
            string disabledPath = $"{enabledPath}.disabled";

            bool enabledExists = File.Exists(enabledPath);
            bool disabledExists = File.Exists(disabledPath);

            string? newestPath = null;
            if (enabledExists)
                newestPath = enabledPath;
            if (disabledExists)
                newestPath = ChooseNewest(newestPath, disabledPath);

            if (newestPath == null)
                return; // Nothing to do for this build

            if (shouldBeEnabled)
            {
                // Ensure newest is enabled
                if (!StringComparer.OrdinalIgnoreCase.Equals(newestPath, enabledPath))
                {
                    SafeDelete(enabledPath);
                    SafeMoveReplace(newestPath, enabledPath);
                }
                // Remove any leftover disabled duplicate
                SafeDelete(disabledPath);
            }
            else
            {
                // Ensure newest is disabled
                if (!StringComparer.OrdinalIgnoreCase.Equals(newestPath, disabledPath))
                {
                    SafeDelete(disabledPath);
                    SafeMoveReplace(newestPath, disabledPath);
                }
                // Remove any leftover enabled duplicate
                SafeDelete(enabledPath);
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

        private static void SafeMoveReplace(string sourcePath, string destinationPath)
        {
            try
            {
                if (File.Exists(destinationPath))
                    File.Delete(destinationPath);
                File.Move(sourcePath, destinationPath);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"Failed to move '{sourcePath}' to '{destinationPath}': {ex.Message}");
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