// PATTERN: Full Health & Energy - Keep both topped up
// USAGE: Register once in OnInitializeMelon()
// REQUIRES: using S1Toolkit; using S1Toolkit.Api;

S1.Every(1f, () =>
{
    Api.Player.SetHealth(100);
    Api.Player.SetEnergy(100);
});
