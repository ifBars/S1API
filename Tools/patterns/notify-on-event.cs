// PATTERN: Notify on Event - Show a notification when something happens
// USAGE: Register once in OnInitializeMelon()
// REQUIRES: using S1Toolkit.Api;

Api.Police.OnArrestStart += () =>
    Api.UI.Notify("Police", "You are being arrested!");

Api.Events.OnItemSold += (id, qty) =>
    Api.UI.Notify("Sale", $"Sold {qty}x {id}");

Api.Time.OnDayPass += () =>
    Api.UI.Notify("Time", $"New day: {Api.Time.GetDayName()}");
