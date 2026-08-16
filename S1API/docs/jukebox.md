# Jukeboxes

`S1API.Audio` provides managed access to placed native jukeboxes. Use `JukeboxManager.GetAll()` to discover active jukeboxes, or `Jukebox.FromGameObject()` when a placement or interaction already provides the object.

```csharp
using S1API.Audio;

Jukebox? jukebox = Jukebox.FromGameObject(placedObject);
if (jukebox == null)
    return;

foreach (JukeboxTrack track in jukebox.Tracks)
    MelonLoader.MelonLogger.Msg($"{track.Index}: {track.Name} by {track.Artist}");

jukebox.SelectTrack(0);
jukebox.TogglePlay();
jukebox.SetVolume(4);
```

`Jukebox.State` and `OnStateChanged` provide immutable managed state snapshots. The wrapper exposes only tracks already configured by the game. It does not register custom audio or create new music network payloads.

Pass an index obtained from `Jukebox.Tracks` to `SelectTrack`. An index that does not identify a configured track throws `ArgumentOutOfRangeException` before native state is changed. `SetVolume` and `ChangeVolume` retain the game's native 0-through-8 clamping behavior.

All controls call the native jukebox methods. The game's authority, replication, synchronization, and save behavior remain in effect. Test host and client behavior for the game version and runtime that a mod supports.
