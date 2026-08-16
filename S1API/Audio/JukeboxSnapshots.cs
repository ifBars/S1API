using System;

namespace S1API.Audio
{
    /// <summary>
    /// A track configured on a native jukebox.
    /// </summary>
    public sealed class JukeboxTrack
    {
        internal JukeboxTrack(int index, string name, string artist)
        {
            Index = index;
            Name = name ?? string.Empty;
            Artist = artist ?? string.Empty;
        }

        /// <summary>
        /// Gets the index to pass to <see cref="Jukebox.SelectTrack"/>.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Gets the track name configured by the game.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the artist name configured by the game.
        /// </summary>
        public string Artist { get; }
    }

    /// <summary>
    /// The repeat modes supported by native jukeboxes.
    /// </summary>
    public enum JukeboxRepeatMode
    {
        /// <summary>
        /// The queue does not repeat.
        /// </summary>
        None = 0,

        /// <summary>
        /// The queue repeats after its final track.
        /// </summary>
        RepeatQueue = 1,

        /// <summary>
        /// The current track repeats.
        /// </summary>
        RepeatTrack = 2
    }

    /// <summary>
    /// An immutable snapshot of a jukebox's current native state.
    /// </summary>
    public sealed class JukeboxState
    {
        internal JukeboxState(
            int volume,
            float normalizedVolume,
            bool isPlaying,
            float currentTrackTime,
            int currentTrackOrderIndex,
            bool shuffle,
            JukeboxRepeatMode repeatMode,
            bool sync,
            JukeboxTrack? currentTrack)
        {
            Volume = volume;
            NormalizedVolume = normalizedVolume;
            IsPlaying = isPlaying;
            CurrentTrackTime = currentTrackTime;
            CurrentTrackOrderIndex = currentTrackOrderIndex;
            Shuffle = shuffle;
            RepeatMode = repeatMode;
            Sync = sync;
            CurrentTrack = currentTrack;
        }

        /// <summary>
        /// Gets the current native volume value.
        /// </summary>
        public int Volume { get; }

        /// <summary>
        /// Gets the native volume normalized by the game's maximum volume.
        /// </summary>
        public float NormalizedVolume { get; }

        /// <summary>
        /// Gets whether the jukebox is playing.
        /// </summary>
        public bool IsPlaying { get; }

        /// <summary>
        /// Gets the elapsed time of the current track in seconds.
        /// </summary>
        public float CurrentTrackTime { get; }

        /// <summary>
        /// Gets the current position in the native playback queue.
        /// </summary>
        public int CurrentTrackOrderIndex { get; }

        /// <summary>
        /// Gets whether the native playback queue is shuffled.
        /// </summary>
        public bool Shuffle { get; }

        /// <summary>
        /// Gets the native repeat mode.
        /// </summary>
        public JukeboxRepeatMode RepeatMode { get; }

        /// <summary>
        /// Gets whether the jukebox synchronizes with other native jukeboxes.
        /// </summary>
        public bool Sync { get; }

        /// <summary>
        /// Gets the current track, or <see langword="null"/> when the native jukebox has no current track.
        /// </summary>
        public JukeboxTrack? CurrentTrack { get; }
    }
}
