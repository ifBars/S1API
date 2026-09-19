#if IL2CPPMELON
using S1Cutscene = Il2CppScheduleOne.Cutscenes.Cutscene;
using S1CutsceneCamera = Il2CppScheduleOne.Cutscenes.CutsceneCamera;
using S1PlayerCamera = Il2CppScheduleOne.PlayerScripts.PlayerCamera;
using S1PlayerSingleton = Il2CppScheduleOne.DevUtilities.PlayerSingleton<Il2CppScheduleOne.PlayerScripts.PlayerCamera>;
#elif MONOMELON
using S1Cutscene = ScheduleOne.Cutscenes.Cutscene;
using S1CutsceneCamera = ScheduleOne.Cutscenes.CutsceneCamera;
using S1PlayerCamera = ScheduleOne.PlayerScripts.PlayerCamera;
using S1PlayerSingleton = ScheduleOne.DevUtilities.PlayerSingleton<ScheduleOne.PlayerScripts.PlayerCamera>;
#endif

using System;
using S1API.Cutscenes;
using S1API.Logging;
using UnityEngine;

namespace S1API.Internal.Cutscenes
{
    internal sealed class NativeCutsceneAdapter
    {
        private static readonly Log Logger = new Log("Cutscenes");
        private GameObject? _host;
        private GameObject? _cameraControlObject;
        private S1Cutscene? _cutscene;
        private string? _nativeStateName;

        internal bool TryBegin(
            string id,
            string name,
            Vector3 initialPosition,
            Quaternion initialRotation,
            float? fov,
            out CutsceneCamera? camera)
        {
            camera = null;

            try
            {
                _host = new GameObject($"S1API_Cutscene_{SanitizeName(id)}");
                _cameraControlObject = new GameObject($"S1API_CutsceneCamera_{SanitizeName(id)}");
                _cameraControlObject.transform.SetPositionAndRotation(initialPosition, initialRotation);

                _cutscene = _host.AddComponent<S1Cutscene>();
                _cutscene.Name = name;
                var nativeCamera = _cameraControlObject.AddComponent<S1CutsceneCamera>();
                Utils.ReflectionUtils.TrySetFieldOrProperty(nativeCamera, "_controlFoV", fov.HasValue);
                if (fov.HasValue)
                {
                    Utils.ReflectionUtils.TrySetFieldOrProperty(nativeCamera, "_fov", fov.Value);
                }
                _cutscene.SetActiveCameraControl(nativeCamera);

                _nativeStateName = $"Cutscene ({name})";
                _cutscene.Play();
                if (!_cutscene.IsPlaying)
                {
                    throw new InvalidOperationException("The native cutscene did not enter its playing state.");
                }

                camera = new CutsceneCamera(_cameraControlObject.transform);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Warning(
                    $"Could not start cutscene '{id}': {ex.GetType().Name}: {ex.Message}");
                TryEnd(out _);
                return false;
            }
        }

        internal bool TryEnd(out Exception? exception)
        {
            exception = null;

            try
            {
                if (_cutscene != null && _cutscene.IsPlaying)
                {
                    _cutscene.InvokeEnd();
                }
            }
            catch (Exception ex)
            {
                exception = ex;
                TryForceCameraRestore();
            }
            finally
            {
                if (_host != null)
                {
                    UnityEngine.Object.Destroy(_host);
                }

                if (_cameraControlObject != null)
                {
                    UnityEngine.Object.Destroy(_cameraControlObject);
                }

                _cutscene = null;
                _host = null;
                _cameraControlObject = null;
                _nativeStateName = null;
            }

            return exception == null;
        }

        private void TryForceCameraRestore()
        {
            try
            {
                if (!S1PlayerSingleton.InstanceExists)
                {
                    return;
                }

                S1PlayerCamera playerCamera = S1PlayerSingleton.Instance;
                if (!string.IsNullOrEmpty(_nativeStateName))
                {
                    playerCamera.RemoveActiveUIElement(_nativeStateName);
                }

                playerCamera.StopTransformOverride(
                    0f,
                    reenableCameraLook: true,
                    returnToOriginalRotation: false);
                playerCamera.StopFOVOverride(0f);
            }
            catch
            {
                // Native InvokeEnd is the authoritative cleanup path; this is only a final best effort.
            }
        }

        private static string SanitizeName(string value)
        {
            char[] characters = value.ToCharArray();
            for (int i = 0; i < characters.Length; i++)
            {
                if (!char.IsLetterOrDigit(characters[i]) && characters[i] != '_' && characters[i] != '-')
                {
                    characters[i] = '_';
                }
            }

            return new string(characters);
        }
    }
}
