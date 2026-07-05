// PATTERN: Infinite Money - Adds money every second
// USAGE: Register once in OnInitializeMelon()
// REQUIRES: using S1Toolkit; using S1Toolkit.Api;

S1.Every(1f, () => Api.Money.Add(1000));
