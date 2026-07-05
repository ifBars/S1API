// PATTERN: Kill All NPCs - Eliminate every registered NPC
// USAGE: Register the hotkey once in OnInitializeMelon()
// REQUIRES: using S1Toolkit; using S1Toolkit.Api; using UnityEngine;

S1.OnKey(KeyCode.F12, () =>
{
    foreach (string npc in Api.NPC.GetAllNpcNames())
        Api.Combat.Kill(npc);
    Api.UI.Notify("NPCs", "All NPCs eliminated!");
});
