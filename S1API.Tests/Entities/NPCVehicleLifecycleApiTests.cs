using System;
using System.Reflection;
using S1API.Entities;
using S1API.Vehicles;

namespace S1API.Tests.Entities;

public sealed class NPCVehicleLifecycleApiTests
{
    [Theory]
    [InlineData(nameof(NPC.OnEnterVehicle))]
    [InlineData(nameof(NPC.OnExitVehicle))]
    public void VehicleLifecycleEventsExposeManagedVehicleArguments(string eventName)
    {
        EventInfo? eventInfo = typeof(NPC).GetEvent(eventName);

        Assert.NotNull(eventInfo);
        Assert.Equal(typeof(Action<LandVehicle>), eventInfo!.EventHandlerType);
    }
}

internal static class NPCVehicleLifecycleApiCompileFixture
{
    internal static void SubscribeAndUnsubscribe(NPC npc)
    {
        Action<LandVehicle> handler = _ => { };

        npc.OnEnterVehicle += handler;
        npc.OnExitVehicle += handler;
        npc.OnEnterVehicle -= handler;
        npc.OnExitVehicle -= handler;
    }
}
