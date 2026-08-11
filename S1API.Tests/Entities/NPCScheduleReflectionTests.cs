#if IL2CPPMELON
using NativeNpc = Il2CppScheduleOne.NPCs.NPC;
using NativeStayInBuildingAction = Il2CppScheduleOne.NPCs.Schedules.NPCEvent_StayInBuilding;
#elif MONOMELON
using NativeNpc = ScheduleOne.NPCs.NPC;
using NativeStayInBuildingAction = ScheduleOne.NPCs.Schedules.NPCEvent_StayInBuilding;
#endif

using System.Reflection;
using S1API.Internal.Patches;
using S1API.Internal.Utils;

namespace S1API.Tests.Entities;

public sealed class NPCScheduleReflectionTests
{
    private const BindingFlags InstanceMemberFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    [Fact]
    public void ScheduleActionNpcLookup_ResolvesTheRuntimeNativeMember()
    {
        var action = TestObjectFactory.CreateUninitialized<NativeStayInBuildingAction>();
        var npc = TestObjectFactory.CreateUninitialized<NativeNpc>();

        SetRuntimeNpcMember(action, npc);

        Assert.Same(npc, NPCPatches.GetScheduleActionNpc(action));
    }

    [Fact]
    public void ScheduleActionNpcLookup_ReturnsNullWhenTheNativeMemberIsUnset()
    {
        var action = TestObjectFactory.CreateUninitialized<NativeStayInBuildingAction>();

        Assert.Null(NPCPatches.GetScheduleActionNpc(action));
    }

    [Fact]
    public void FieldOrPropertyLookup_ReturnsNullForMissingAndNullMembers()
    {
        var fixture = new ReflectionFixture();

        Assert.Null(ReflectionUtils.TryGetFieldOrProperty(fixture, "missing"));
        Assert.Null(ReflectionUtils.TryGetFieldOrProperty(fixture, nameof(ReflectionFixture.NullValue)));
    }

    private static void SetRuntimeNpcMember(
        NativeStayInBuildingAction action,
        NativeNpc npc)
    {
#if IL2CPPMELON
        PropertyInfo? property = FindProperty(action.GetType(), "npc");
        Assert.NotNull(property);
        Assert.True(property!.CanWrite);
        property.SetValue(action, npc);
#elif MONOMELON
        FieldInfo? field = FindField(action.GetType(), "npc");
        Assert.NotNull(field);
        field!.SetValue(action, npc);
#endif
    }

    private static FieldInfo? FindField(Type type, string memberName)
    {
        while (type != typeof(object))
        {
            FieldInfo? field = type.GetField(memberName, InstanceMemberFlags);
            if (field != null)
                return field;

            type = type.BaseType!;
        }

        return null;
    }

    private static PropertyInfo? FindProperty(Type type, string memberName)
    {
        while (type != typeof(object))
        {
            PropertyInfo? property = type.GetProperty(memberName, InstanceMemberFlags);
            if (property != null)
                return property;

            type = type.BaseType!;
        }

        return null;
    }

    private sealed class ReflectionFixture
    {
        private object? NullValue => null;
    }
}
