// PATTERN: Teleport - Teleport the player to specific coordinates
// USAGE: Register the hotkey once in OnInitializeMelon()
// REQUIRES: using S1Toolkit; using S1Toolkit.Api; using UnityEngine;

S1.OnKey(KeyCode.F6, () =>
{
    Api.Player.Teleport(100, 0, 50); // x, y, z world coordinates
    Api.UI.Notify("Teleport", "Teleported!");
});
