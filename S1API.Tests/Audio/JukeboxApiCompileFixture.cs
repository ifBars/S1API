using System;
using System.Collections.Generic;
using S1API.Audio;
using UnityEngine;

namespace S1API.Tests.Audio;

internal static class JukeboxApiCompileFixture
{
    internal static void InspectAndControl(GameObject gameObject, string guid)
    {
        Jukebox? jukebox = Jukebox.FromGameObject(gameObject);
        IReadOnlyList<Jukebox> allJukeboxes = JukeboxManager.GetAll();
        Jukebox? byGuid = JukeboxManager.GetByGUID(guid);
        _ = allJukeboxes;
        _ = byGuid;

        if (jukebox == null)
            return;

        IReadOnlyList<JukeboxTrack> tracks = jukebox.Tracks;
        JukeboxTrack? currentTrack = jukebox.CurrentTrack;
        JukeboxState state = jukebox.State;
        Action<JukeboxState> handler = changedState =>
        {
            _ = changedState.CurrentTrack;
        };
        jukebox.OnStateChanged += handler;
        jukebox.OnStateChanged -= handler;

        _ = jukebox.GUID;
        _ = jukebox.GameObject;
        _ = tracks;
        _ = currentTrack;
        _ = currentTrack?.Index;
        _ = currentTrack?.Name;
        _ = currentTrack?.Artist;
        _ = state.Volume;
        _ = state.NormalizedVolume;
        _ = state.IsPlaying;
        _ = state.CurrentTrackTime;
        _ = state.CurrentTrackOrderIndex;
        _ = state.Shuffle;
        _ = state.RepeatMode;
        _ = state.Sync;
        _ = jukebox.NormalizedVolume;
        _ = jukebox.IsPlaying;
        _ = jukebox.CurrentTrackTime;
        _ = jukebox.CurrentTrackOrderIndex;
        _ = jukebox.Shuffle;
        _ = jukebox.RepeatMode;
        _ = jukebox.Sync;
        jukebox.TogglePlay();
        jukebox.PreviousTrack();
        jukebox.NextTrack();
        jukebox.ChangeVolume(1);
        jukebox.SetVolume(4);
        jukebox.ToggleShuffle();
        jukebox.ToggleRepeatMode();
        jukebox.ToggleSync();
        jukebox.SelectTrack(0);
    }
}
