#if IL2CPPMELON
using Il2CppInterop.Runtime;
using NativeAction = Il2CppSystem.Action;
using S1Jukebox = Il2CppScheduleOne.ObjectScripts.Jukebox;
#elif MONOMELON
using NativeAction = System.Action;
using S1Jukebox = ScheduleOne.ObjectScripts.Jukebox;
#endif

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using S1API.Internal.Utils;
using S1API.Logging;
using UnityEngine;

namespace S1API.Audio
{
    /// <summary>
    /// Provides managed access to a placed native jukebox.
    /// </summary>
    /// <remarks>
    /// This wrapper exposes only tracks configured by the game. Controls use the native jukebox
    /// methods, including its existing authority, replication, and persistence behavior.
    /// </remarks>
    public sealed class Jukebox
    {
        private static readonly IReadOnlyList<JukeboxTrack> EmptyTracks =
            new ReadOnlyCollection<JukeboxTrack>(Array.Empty<JukeboxTrack>());
        private static readonly Log Logger = new Log("Jukebox");

        private Action<JukeboxState>? _stateChangedHandlers;
        private NativeAction? _nativeStateChangedDispatcher;

        /// <summary>
        /// INTERNAL: The native jukebox component.
        /// </summary>
        internal readonly S1Jukebox S1Jukebox;

        /// <summary>
        /// INTERNAL: Creates a wrapper for a native jukebox component.
        /// </summary>
        /// <param name="jukebox">The native jukebox component.</param>
        internal Jukebox(S1Jukebox jukebox)
        {
            S1Jukebox = jukebox ?? throw new ArgumentNullException(nameof(jukebox));
        }

        /// <summary>
        /// Gets the jukebox component on a game object.
        /// </summary>
        /// <param name="gameObject">The game object to inspect.</param>
        /// <returns>A managed jukebox wrapper, or <see langword="null"/> when no jukebox is attached.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="gameObject"/> is <c>null</c> or destroyed.</exception>
        public static Jukebox? FromGameObject(GameObject gameObject)
        {
            if (gameObject == null)
                throw new ArgumentNullException(nameof(gameObject));

            S1Jukebox? jukebox = gameObject.GetComponent<S1Jukebox>();
            return jukebox == null ? null : JukeboxManager.Wrap(jukebox);
        }

        /// <summary>
        /// Gets the placed-object identifier assigned by the game.
        /// </summary>
        public string GUID =>
            S1Jukebox.GUID.ToString();

        /// <summary>
        /// Gets the game object that owns the native jukebox.
        /// </summary>
        public GameObject GameObject =>
            S1Jukebox.gameObject;

        /// <summary>
        /// Gets an immutable snapshot of tracks configured on this jukebox.
        /// </summary>
        public IReadOnlyList<JukeboxTrack> Tracks =>
            SnapshotTracks();

        /// <summary>
        /// Gets the current native track, or <see langword="null"/> when none is selected.
        /// </summary>
        public JukeboxTrack? CurrentTrack =>
            SnapshotCurrentTrack();

        /// <summary>
        /// Gets the current native volume value.
        /// </summary>
        public int Volume =>
            S1Jukebox.CurrentVolume;

        /// <summary>
        /// Gets the native volume normalized by the game's maximum volume.
        /// </summary>
        public float NormalizedVolume =>
            S1Jukebox.NormalizedVolume;

        /// <summary>
        /// Gets whether the jukebox is playing.
        /// </summary>
        public bool IsPlaying =>
            S1Jukebox.IsPlaying;

        /// <summary>
        /// Gets the elapsed time of the current track in seconds.
        /// </summary>
        public float CurrentTrackTime =>
            S1Jukebox.CurrentTrackTime;

        /// <summary>
        /// Gets the current position in the native playback queue.
        /// </summary>
        public int CurrentTrackOrderIndex =>
            S1Jukebox.CurrentTrackOrderIndex;

        /// <summary>
        /// Gets whether the native playback queue is shuffled.
        /// </summary>
        public bool Shuffle =>
            S1Jukebox.Shuffle;

        /// <summary>
        /// Gets the native repeat mode.
        /// </summary>
        public JukeboxRepeatMode RepeatMode =>
            (JukeboxRepeatMode)(int)S1Jukebox.RepeatMode;

        /// <summary>
        /// Gets whether the jukebox synchronizes with other native jukeboxes.
        /// </summary>
        public bool Sync =>
            S1Jukebox.Sync;

        /// <summary>
        /// Gets an immutable snapshot of the jukebox's current state.
        /// </summary>
        public JukeboxState State =>
            new JukeboxState(
                Volume,
                NormalizedVolume,
                IsPlaying,
                CurrentTrackTime,
                CurrentTrackOrderIndex,
                Shuffle,
                RepeatMode,
                Sync,
                CurrentTrack);

        /// <summary>
        /// Occurs when the native jukebox reports a state change.
        /// </summary>
        public event Action<JukeboxState> OnStateChanged
        {
            add
            {
                if (value == null)
                    return;

                _stateChangedHandlers += value;
                try
                {
                    EnsureStateChangedHook();
                }
                catch
                {
                    _stateChangedHandlers -= value;
                    throw;
                }
            }
            remove
            {
                if (value == null)
                    return;

                _stateChangedHandlers -= value;
                if (_stateChangedHandlers == null)
                    RemoveStateChangedHook();
            }
        }

        /// <summary>
        /// Toggles playback using the native jukebox control.
        /// </summary>
        public void TogglePlay() =>
            S1Jukebox.TogglePlay();

        /// <summary>
        /// Selects the previous track using the native jukebox control.
        /// </summary>
        public void PreviousTrack() =>
            S1Jukebox.Back();

        /// <summary>
        /// Selects the next track using the native jukebox control.
        /// </summary>
        public void NextTrack() =>
            S1Jukebox.Next();

        /// <summary>
        /// Changes the volume using the native jukebox control.
        /// </summary>
        /// <param name="change">The change to apply to the current native volume.</param>
        public void ChangeVolume(int change) =>
            S1Jukebox.ChangeVolume(change);

        /// <summary>
        /// Sets the volume and requests native replication.
        /// </summary>
        /// <param name="volume">The native volume value to set.</param>
        public void SetVolume(int volume) =>
            S1Jukebox.SetVolume(volume, replicate: true);

        /// <summary>
        /// Toggles native queue shuffling.
        /// </summary>
        public void ToggleShuffle() =>
            S1Jukebox.ToggleShuffle();

        /// <summary>
        /// Cycles the native repeat mode.
        /// </summary>
        public void ToggleRepeatMode() =>
            S1Jukebox.ToggleRepeatMode();

        /// <summary>
        /// Toggles native jukebox synchronization.
        /// </summary>
        public void ToggleSync() =>
            S1Jukebox.ToggleSync();

        /// <summary>
        /// Selects a configured native track.
        /// </summary>
        /// <param name="trackIndex">The <see cref="JukeboxTrack.Index"/> of the configured track.</param>
        public void SelectTrack(int trackIndex) =>
            S1Jukebox.PlayTrack(trackIndex);

        internal void Cleanup()
        {
            RemoveStateChangedHook();
            _stateChangedHandlers = null;
        }

        private IReadOnlyList<JukeboxTrack> SnapshotTracks()
        {
            var nativeTracks = S1Jukebox.TrackList;
            if (nativeTracks == null || nativeTracks.Length == 0)
                return EmptyTracks;

            var tracks = new List<JukeboxTrack>(nativeTracks.Length);
            for (int index = 0; index < nativeTracks.Length; index++)
            {
                var nativeTrack = nativeTracks[index];
                if (nativeTrack != null)
                {
                    tracks.Add(new JukeboxTrack(
                        index,
                        nativeTrack.TrackName,
                        nativeTrack.ArtistName));
                }
            }

            return tracks.Count == 0
                ? EmptyTracks
                : new ReadOnlyCollection<JukeboxTrack>(tracks);
        }

        private JukeboxTrack? SnapshotCurrentTrack()
        {
            var nativeTrack = S1Jukebox.currentTrack;
            if (nativeTrack == null)
                return null;

            var nativeTracks = S1Jukebox.TrackList;
            if (nativeTracks != null)
            {
                for (int index = 0; index < nativeTracks.Length; index++)
                {
                    if (nativeTracks[index] == nativeTrack)
                    {
                        return new JukeboxTrack(
                            index,
                            nativeTrack.TrackName,
                            nativeTrack.ArtistName);
                    }
                }
            }

            return null;
        }

        private void EnsureStateChangedHook()
        {
            if (_nativeStateChangedDispatcher != null)
                return;

            NativeAction dispatcher = CreateNativeStateChangedDispatcher();
#if IL2CPPMELON
            S1Jukebox.onStateChanged = S1Jukebox.onStateChanged == null
                ? dispatcher
                : Il2CppSystem.Delegate.Combine(S1Jukebox.onStateChanged, dispatcher)
                    .Cast<NativeAction>();
#else
            S1Jukebox.onStateChanged += dispatcher;
#endif
            _nativeStateChangedDispatcher = dispatcher;
        }

        private void RemoveStateChangedHook()
        {
            if (_nativeStateChangedDispatcher == null)
                return;

#if IL2CPPMELON
            Il2CppSystem.Delegate? remaining = Il2CppSystem.Delegate.Remove(
                S1Jukebox.onStateChanged,
                _nativeStateChangedDispatcher);
            S1Jukebox.onStateChanged = remaining?.Cast<NativeAction>();
#else
            S1Jukebox.onStateChanged -= _nativeStateChangedDispatcher;
#endif
            _nativeStateChangedDispatcher = null;
        }

        private NativeAction CreateNativeStateChangedDispatcher()
        {
#if IL2CPPMELON
            return DelegateSupport.ConvertDelegate<NativeAction>(NotifyStateChanged)
                ?? throw new InvalidOperationException("Could not create the native jukebox state-change delegate.");
#else
            return NotifyStateChanged;
#endif
        }

        private void NotifyStateChanged()
        {
            Action<JukeboxState>? handlers = _stateChangedHandlers;
            if (handlers == null)
                return;

            JukeboxState state = State;
            foreach (Delegate callback in handlers.GetInvocationList())
            {
                try
                {
                    ((Action<JukeboxState>)callback)(state);
                }
                catch (Exception exception)
                {
                    Logger.Warning($"A Jukebox.OnStateChanged subscriber failed: {exception.Message}");
                }
            }
        }
    }
}
