using System;
using System.Collections.Generic;
using MelonLoader;
using UnityEngine;
using S1API.Logging;
#if IL2CPPMELON
using Il2CppInterop.Runtime;
#endif

namespace S1API.Internal.Phone
{
    /// <summary>
    /// Compatibility component for the HomeScreen scroll behaviour. When the real AppIcons
    /// container is moved under Viewport, this stub stays as a direct child of HomeScreen
    /// so that Find("AppIcons") still returns a valid object for third-party mods.
    /// 
    /// To support mods that call GetChild(), the stub copies children FROM
    /// the real container on Start. Mods can then get/modify these copies. Modified copies
    /// are moved to the real container in LateUpdate so they appear in the scroll view.
    /// </summary>
#if IL2CPPMELON
    [RegisterTypeInIl2Cpp]
#endif
    internal sealed class AppIconsRedirect : MonoBehaviour
    {
#if IL2CPPMELON
        /// <summary>
        /// IL2CPP constructor required for RegisterTypeInIl2Cpp (called by Il2Cpp side).
        /// </summary>
        public AppIconsRedirect(IntPtr ptr) : base(ptr) { }
#endif

        internal Transform _realAppIcons;
        private bool _initialized;
        private int _lastMirroredRealCount = -1;
        private readonly HashSet<int> _stubMirrorIconIds = new HashSet<int>();
        private static readonly Log Logger = new Log("AppIconsRedirect");

        #region Unity lifecycle

        private void Start()
        {
            InitializeStubIcons();
        }

        private void LateUpdate()
        {
            if (_realAppIcons == null)
                return;

            if (!_initialized && _realAppIcons.childCount > 0)
            {
                InitializeStubIcons();
                return;
            }

            bool movedAnyExternalIcons = false;
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (_stubMirrorIconIds.Contains(child.gameObject.GetInstanceID()))
                    continue;

                child.SetParent(_realAppIcons, true);
                movedAnyExternalIcons = true;
            }

            // Keep a stable mirror in the stub. Only rebuild when data actually changed.
            if (_initialized && (movedAnyExternalIcons || _realAppIcons.childCount != _lastMirroredRealCount))
            {
                RepopulateStub();
            }
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Copies icons from real container to stub so mods can access them via GetChild().
        /// </summary>
        private void InitializeStubIcons()
        {
            if (_realAppIcons == null)
            {
                Logger.Warning("[AppIconsRedirect] InitializeStubIcons skipped: _realAppIcons is null.");
                return;
            }
            if (_initialized)
                return;

            _initialized = true;
            RepopulateStub();
        }

        /// <summary>
        /// Copies icons from real back to stub so mods can access them via GetChild().
        /// Called after draining when mods may run in later frames (e.g. coroutines).
        /// </summary>
        private void RepopulateStub()
        {
            if (_realAppIcons == null)
                return;

            DestroyMirrorIconsFromStub();
            _stubMirrorIconIds.Clear();

            int realCount = _realAppIcons.childCount;
            for (int i = 0; i < realCount; i++)
            {
                Transform realIcon = _realAppIcons.GetChild(i);
                GameObject copy = Instantiate(realIcon.gameObject, transform);
                copy.name = realIcon.name;
                _stubMirrorIconIds.Add(copy.GetInstanceID());
            }

            _lastMirroredRealCount = _realAppIcons.childCount;
        }

        private void DestroyMirrorIconsFromStub()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (_stubMirrorIconIds.Contains(child.gameObject.GetInstanceID()))
                    UnityEngine.Object.Destroy(child.gameObject);
            }
        }

        #endregion
    }
}
