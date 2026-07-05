// PATTERN: Time Control - Set the exact time of day (host only)
// USAGE: Register the hotkeys once in OnInitializeMelon()
// REQUIRES: using S1Toolkit; using S1Toolkit.Api; using UnityEngine;

S1.OnKey(KeyCode.F7, () => Api.Time.SetTime(720)); // noon (minutes since midnight)
S1.OnKey(KeyCode.F8, () => Api.Time.SetTime(0));   // midnight
