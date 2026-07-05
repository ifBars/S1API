// PATTERN: No Police - Clear the warrant whenever the player is wanted
// USAGE: Register once in OnInitializeMelon()
// REQUIRES: using S1Toolkit; using S1Toolkit.Api;

S1.Every(1f, () =>
{
    if (Api.Police.IsWanted())
        Api.Police.ClearWarrant();
});
