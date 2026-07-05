// PATTERN: Complete Quests - Finish all active quests instantly
// USAGE: Register the hotkey once in OnInitializeMelon()
// REQUIRES: using S1Toolkit; using S1Toolkit.Api; using UnityEngine;

S1.OnKey(KeyCode.F4, () =>
{
    foreach (string guid in Api.Quest.GetActiveQuests())
        Api.Quest.CompleteQuest(guid);
    Api.UI.Notify("Quests", "All active quests completed!");
});
