#if (IL2CPPMELON)
using S1Quests = Il2CppScheduleOne.Quests;
using S1NetworkConnection = Il2CppFishNet.Connection.NetworkConnection;
#elif MONOMELON
using S1Quests = ScheduleOne.Quests;
using S1NetworkConnection = FishNet.Connection.NetworkConnection;
#endif

using System;
using System.Reflection;
using HarmonyLib;
using S1API.Internal.Networking;
using S1API.Logging;

namespace S1API.Internal.Patches
{
    /// <summary>
    /// INTERNAL: Finds the generated FishNet RPC methods of <c>QuestManager</c> that carry
    /// <c>ModChannel</c> frames.
    /// </summary>
    /// <remarks>
    /// The generated names end in a hash (<c>RpcLogic___SendQuestState_4117703421</c>), so they are matched by
    /// prefix. A missing method disables only its patch and logs an error; the game and other patches keep running.
    /// </remarks>
    internal static class ModChannelPatches
    {
        private static readonly Log Logger = new Log("ModChannel");

        internal static MethodBase? Find(string namePrefix)
        {
            try
            {
                foreach (MethodInfo method in AccessTools.GetDeclaredMethods(typeof(S1Quests.QuestManager)))
                {
                    if (method.Name.StartsWith(namePrefix, StringComparison.Ordinal))
                        return method;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"QuestManager methods could not be listed while looking for {namePrefix}: {ex}");
                return null;
            }

            Logger.Error($"QuestManager.{namePrefix}* not found; ModChannel messages on this path do not arrive.");
            return null;
        }
    }

    /// <summary>
    /// INTERNAL: Remembers which connection sent the ServerRpc being read, so the server knows the sender.
    /// </summary>
    [HarmonyPatch]
    internal static class ModChannelServerReaderPatch
    {
        private static MethodBase? _target;

        private static bool Prepare()
        {
            _target ??= ModChannelPatches.Find("RpcReader___Server_SendQuestState");
            return _target != null;
        }

        private static MethodBase TargetMethod() => _target!;

        // RpcReader___Server_SendQuestState(PooledReader reader, Channel channel, NetworkConnection conn).
        [HarmonyPrefix]
        private static void Prefix(S1NetworkConnection __2) => ModChannelTransport.CurrentSender = __2;

        // A finalizer runs even when the reader throws, so the sender never leaks into the next frame.
        [HarmonyFinalizer]
        private static void Finalizer() => ModChannelTransport.CurrentSender = null;
    }

    /// <summary>
    /// INTERNAL: Takes frames out of <c>SendQuestState</c> on the server before the vanilla quest lookup.
    /// </summary>
    [HarmonyPatch]
    internal static class ModChannelSendLogicPatch
    {
        private static MethodBase? _target;

        private static bool Prepare()
        {
            _target ??= ModChannelPatches.Find("RpcLogic___SendQuestState");
            return _target != null;
        }

        private static MethodBase TargetMethod() => _target!;

        // RpcLogic___SendQuestState(string guid, EQuestState state).
        [HarmonyPrefix]
        private static bool Prefix(string __0)
        {
            if (!ModChannelFrame.TryDecode(__0, out ModChannelFrame frame))
                return true;

            ModChannelTransport.Receive(frame, "SendQuestState");
            return false;
        }
    }

    /// <summary>
    /// INTERNAL: Takes frames out of <c>ReceiveQuestState</c> on the clients before the vanilla quest lookup.
    /// </summary>
    /// <remarks>
    /// On IL2CPP the server can reach this method with a client's frame too: the reader may run the inlined body of
    /// <c>RpcLogic___SendQuestState</c>, which relays through <c>ReceiveQuestState</c>. The frame's direction
    /// routes it either way.
    /// </remarks>
    [HarmonyPatch]
    internal static class ModChannelReceiveLogicPatch
    {
        private static MethodBase? _target;

        private static bool Prepare()
        {
            _target ??= ModChannelPatches.Find("RpcLogic___ReceiveQuestState");
            return _target != null;
        }

        private static MethodBase TargetMethod() => _target!;

        // RpcLogic___ReceiveQuestState(NetworkConnection conn, string guid, EQuestState state).
        [HarmonyPrefix]
        private static bool Prefix(string __1)
        {
            if (!ModChannelFrame.TryDecode(__1, out ModChannelFrame frame))
                return true;

            ModChannelTransport.Receive(frame, "ReceiveQuestState");
            return false;
        }
    }
}
