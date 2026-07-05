// PATTERN: NPC Friend - Set all NPCs to max relationship
// USAGE: Register the hotkey once in OnInitializeMelon()
// REQUIRES: using S1Toolkit; using S1Toolkit.Api; using UnityEngine;

S1.OnKey(KeyCode.F10, () =>
{
    foreach (string npc in Api.NPC.GetAllNpcNames())
        Api.NPC.SetRelationship(npc, 5f); // 5 = loyal (max)
    Api.UI.Notify("NPCs", "Everyone is your friend now!");
});
