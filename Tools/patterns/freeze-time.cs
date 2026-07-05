// PATTERN: Freeze Time - Hold the clock at the current minute (host only)
// USAGE: Register the hotkey once in OnInitializeMelon()
// REQUIRES: using S1Toolkit; using S1Toolkit.Api; using UnityEngine;

private static object _freezeLoop;

S1.OnKey(KeyCode.F11, () =>
{
    if (_freezeLoop == null)
    {
        int frozenMinute = Api.Time.GetCurrentMinute();
        _freezeLoop = S1.Every(5f, () => Api.Time.SetTime(frozenMinute));
        Api.UI.Notify("Time", "Time frozen!");
    }
    else
    {
        MelonLoader.MelonCoroutines.Stop(_freezeLoop);
        _freezeLoop = null;
        Api.UI.Notify("Time", "Time running again");
    }
});
