using System;
using System.Reflection;
using S1API.Entities;
using S1API.Vehicles;
using UnityEngine;

namespace S1API.Tests.Entities;

public sealed class NPCAwarenessApiTests
{
#if MONOMELON
    [Fact]
    public void NoiseSnapshotCapturesManagedValues()
    {
        var origin = new Vector3(1f, 2f, 3f);
        var snapshot = new NPCNoiseEvent(
            origin,
            24f,
            NPCNoiseType.Gunshot,
            source: null,
            originInSewer: true);

        Assert.Equal(origin, snapshot.Origin);
        Assert.Equal(24f, snapshot.Range);
        Assert.Equal(NPCNoiseType.Gunshot, snapshot.Type);
        Assert.Null(snapshot.Source);
        Assert.True(snapshot.OriginInSewer);
    }
#endif

    [Theory]
    [InlineData(nameof(NPCNoiseEvent.Origin), typeof(Vector3))]
    [InlineData(nameof(NPCNoiseEvent.Range), typeof(float))]
    [InlineData(nameof(NPCNoiseEvent.Type), typeof(NPCNoiseType))]
    [InlineData(nameof(NPCNoiseEvent.Source), typeof(GameObject))]
    [InlineData(nameof(NPCNoiseEvent.OriginInSewer), typeof(bool))]
    public void NoiseSnapshotPropertiesAreReadOnly(string propertyName, Type propertyType)
    {
        PropertyInfo? property = typeof(NPCNoiseEvent).GetProperty(propertyName);

        Assert.NotNull(property);
        Assert.Equal(propertyType, property!.PropertyType);
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
