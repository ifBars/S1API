// PATTERN: Spawn Vehicle - Spawn a vehicle at a position
// USAGE: Register the hotkey once in OnInitializeMelon()
// REQUIRES: using S1Toolkit; using S1Toolkit.Api; using UnityEngine;

S1.OnKey(KeyCode.F5, () =>
{
    Api.Vehicle.Spawn("shitbox", 0, 1, 5); // vehicle code, x, y, z
    Api.UI.Notify("Vehicle", "Vehicle spawned!");
});
