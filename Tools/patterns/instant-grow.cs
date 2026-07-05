// PATTERN: Harvest Ready Plants - Harvest every plant that is ready
// USAGE: Register the hotkey once in OnInitializeMelon()
// REQUIRES: using S1Toolkit; using S1Toolkit.Api; using UnityEngine;

S1.OnKey(KeyCode.F9, () =>
{
    int harvested = 0;
    foreach (string pot in Api.Growing.GetGrowContainers())
    {
        if (Api.Growing.IsReadyToHarvest(pot))
        {
            Api.Growing.Harvest(pot);
            harvested++;
        }
    }
    Api.UI.Notify("Growing", $"Harvested {harvested} plants!");
});
