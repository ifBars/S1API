using System;
using System.Reflection;
using S1API.Entities;
using S1API.Vehicles;

namespace S1API.Tests.Entities;

public sealed class NPCAwarenessApiTests
{
    [Theory]
    [InlineData(nameof(NPCNoiseEvent.Origin), "UnityEngine.Vector3")]
    [InlineData(nameof(NPCNoiseEvent.Range), "System.Single")]
    [InlineData(nameof(NPCNoiseEvent.Type), "S1API.Entities.NPCNoiseType")]
    [InlineData(nameof(NPCNoiseEvent.Source), "UnityEngine.GameObject")]
    [InlineData(nameof(NPCNoiseEvent.OriginInSewer), "System.Boolean")]
    public void NoiseSnapshotPropertiesAreReadOnly(string propertyName, string propertyTypeName)
    {
        PropertyInfo? property = typeof(NPCNoiseEvent).GetProperty(propertyName);

        Assert.NotNull(property);
        Assert.Equal(propertyTypeName, property!.PropertyType.FullName);
        Assert.False(property.CanWrite);
    }

    [Fact]
    public void NoiseTypesRetainNativeValues()
    {
        Assert.Equal(0, (int)NPCNoiseType.Footstep);
        Assert.Equal(1, (int)NPCNoiseType.Gunshot);
        Assert.Equal(2, (int)NPCNoiseType.Explosion);
    }

    [Theory]
    [InlineData(nameof(NPC.OnNoticedDrugDealing), typeof(Player))]
    [InlineData(nameof(NPC.OnNoticedGeneralCrime), typeof(Player))]
    [InlineData(nameof(NPC.OnNoticedPettyCrime), typeof(Player))]
    [InlineData(nameof(NPC.OnNoticedPlayerViolatingCurfew), typeof(Player))]
    [InlineData(nameof(NPC.OnNoticedSuspiciousPlayer), typeof(Player))]
    [InlineData(nameof(NPC.OnGunshotHeard), typeof(NPCNoiseEvent))]
    [InlineData(nameof(NPC.OnExplosionHeard), typeof(NPCNoiseEvent))]
    [InlineData(nameof(NPC.OnHitByCar), typeof(LandVehicle))]
    public void AwarenessEventsExposeManagedArguments(string eventName, Type argumentType)
    {
        EventInfo? eventInfo = typeof(NPC).GetEvent(eventName);

        Assert.NotNull(eventInfo);
        Assert.Equal(typeof(Action<>).MakeGenericType(argumentType), eventInfo!.EventHandlerType);
    }
}

internal static class NPCAwarenessApiCompileFixture
{
    internal static void SubscribeAndUnsubscribe(NPC npc)
    {
        Action<Player?> playerHandler = _ => { };
        Action<NPCNoiseEvent?> noiseHandler = _ => { };
        Action<LandVehicle?> vehicleHandler = _ => { };

        npc.OnNoticedDrugDealing += playerHandler;
        npc.OnNoticedGeneralCrime += playerHandler;
        npc.OnNoticedPettyCrime += playerHandler;
        npc.OnNoticedPlayerViolatingCurfew += playerHandler;
        npc.OnNoticedSuspiciousPlayer += playerHandler;
        npc.OnGunshotHeard += noiseHandler;
        npc.OnExplosionHeard += noiseHandler;
        npc.OnHitByCar += vehicleHandler;

        npc.OnNoticedDrugDealing -= playerHandler;
        npc.OnNoticedGeneralCrime -= playerHandler;
        npc.OnNoticedPettyCrime -= playerHandler;
        npc.OnNoticedPlayerViolatingCurfew -= playerHandler;
        npc.OnNoticedSuspiciousPlayer -= playerHandler;
        npc.OnGunshotHeard -= noiseHandler;
        npc.OnExplosionHeard -= noiseHandler;
        npc.OnHitByCar -= vehicleHandler;
    }
}
