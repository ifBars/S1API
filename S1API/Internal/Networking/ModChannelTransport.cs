#if (IL2CPPMELON)
using S1Quests = Il2CppScheduleOne.Quests;
using S1InstanceFinder = Il2CppFishNet.InstanceFinder;
using S1NetworkConnection = Il2CppFishNet.Connection.NetworkConnection;
#elif MONOMELON
using S1Quests = ScheduleOne.Quests;
using S1InstanceFinder = FishNet.InstanceFinder;
using S1NetworkConnection = FishNet.Connection.NetworkConnection;
#endif

using System;
#if MONOMELON
using System.Reflection;
using HarmonyLib;
#endif
using S1API.Entities;
using S1API.Logging;
using S1API.Networking;

namespace S1API.Internal.Networking
{
    /// <summary>
    /// INTERNAL: Carries <see cref="ModChannel"/> frames on the vanilla quest state RPC pair of <c>QuestManager</c>.
    /// </summary>
    /// <remarks>
    /// Client to server: <c>SendQuestState</c>, a ServerRpc. Server to clients: <c>ReceiveQuestState</c>, an
    /// ObserversRpc with a null connection and a TargetRpc with a connection. Both RPCs exist in every co-op session
    /// and take a free string, so no FishNet serializer or extra network object is needed.
    /// </remarks>
    internal static class ModChannelTransport
    {
        private static readonly Log Logger = new Log("ModChannel");
#if MONOMELON
        private static MethodInfo? _receiveQuestState;
#endif

        /// <summary>
        /// The connection whose ServerRpc is being read right now. Set around the vanilla reader, null otherwise.
        /// </summary>
        internal static S1NetworkConnection? CurrentSender;

        internal static bool IsServer
        {
            get
            {
                try
                {
                    return S1InstanceFinder.IsServer;
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Network role could not be read; treated as client. {ex.Message}");
                    return false;
                }
            }
        }

        private static bool IsClient
        {
            get
            {
                try
                {
                    return S1InstanceFinder.IsClient;
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Network role could not be read; treated as no client. {ex.Message}");
                    return false;
                }
            }
        }

        internal static void SendToServer(ModChannel channel, string frame, string payload)
        {
            if (IsServer)
            {
                channel.DeliverToServer(Player.Local, payload);
                return;
            }

            S1Quests.QuestManager? manager = Manager(channel, "SendToServer");
            if (manager == null)
                return;

            try
            {
                manager.SendQuestState(frame, S1Quests.EQuestState.Inactive);
                Logger.Debug($"Channel '{channel.Id}': sent {payload.Length} characters to the server.");
            }
            catch (Exception ex)
            {
                ModChannel.Fail($"Channel '{channel.Id}': SendToServer failed. {ex}");
            }
        }

        internal static void SendToClients(ModChannel channel, string frame, string payload)
        {
            if (!IsServer)
            {
                ModChannel.Warn($"Channel '{channel.Id}': SendToClients is server only; the message is dropped.");
                return;
            }

            if (IsClient)
                channel.DeliverToClient(payload);

            S1Quests.QuestManager? manager = Manager(channel, "SendToClients");
            if (manager == null)
                return;

            try
            {
                ReceiveQuestState(manager, null, frame);
                Logger.Debug($"Channel '{channel.Id}': sent {payload.Length} characters to all clients.");
            }
            catch (Exception ex)
            {
                ModChannel.Fail($"Channel '{channel.Id}': SendToClients failed. {ex}");
            }
        }

        internal static void SendTo(ModChannel channel, Player player, string frame, string payload)
        {
            if (!IsServer)
            {
                ModChannel.Warn($"Channel '{channel.Id}': SendTo is server only; the message is dropped.");
                return;
            }

            if (player.IsLocal)
            {
                channel.DeliverToClient(payload);
                return;
            }

            S1NetworkConnection? connection = player.S1Player != null ? player.S1Player.Owner : null;
            if (connection == null || !connection.IsActive)
            {
                ModChannel.Warn($"Channel '{channel.Id}': player '{player.Name}' has no active connection; the message is dropped.");
                return;
            }

            S1Quests.QuestManager? manager = Manager(channel, "SendTo");
            if (manager == null)
                return;

            try
            {
                ReceiveQuestState(manager, connection, frame);
                Logger.Debug($"Channel '{channel.Id}': sent {payload.Length} characters to '{player.Name}'.");
            }
            catch (Exception ex)
            {
                ModChannel.Fail($"Channel '{channel.Id}': SendTo '{player.Name}' failed. {ex}");
            }
        }

        /// <summary>
        /// Handles a frame that reached one of the patched RPC methods. <paramref name="via"/> names the method.
        /// </summary>
        internal static void Receive(ModChannelFrame frame, string via)
        {
            bool server = IsServer;
            if (frame.IsToServer)
            {
                // A client sees its own ServerRpc run locally, and may see a server frame relayed by vanilla.
                if (!server)
                    return;

                if (!ModChannel.TryFind(frame.ChannelId, out ModChannel? channel) || channel == null)
                    return;

                Player? sender = PlayerOf(CurrentSender);
                if (sender == null)
                    ModChannel.Warn($"Channel '{frame.ChannelId}': sender of a message via {via} is unknown; it is delivered with a null sender.");
                Logger.Debug($"Channel '{frame.ChannelId}': {frame.Payload.Length} characters from '{sender?.Name ?? "unknown"}' via {via}.");
                channel.DeliverToServer(sender, frame.Payload);
                return;
            }

            // The host already delivered its own SendToClients and SendTo locally.
            if (server)
                return;

            if (!ModChannel.TryFind(frame.ChannelId, out ModChannel? target) || target == null)
                return;

            Logger.Debug($"Channel '{frame.ChannelId}': {frame.Payload.Length} characters from the server via {via}.");
            target.DeliverToClient(frame.Payload);
        }

        private static Player? PlayerOf(S1NetworkConnection? connection)
        {
            if (connection == null)
                return null;

            int clientId = connection.ClientId;
            foreach (Player player in Player.All.ToArray())
            {
                S1NetworkConnection? owner = player.S1Player != null ? player.S1Player.Owner : null;
                if (owner != null && owner.ClientId == clientId)
                    return player;
            }

            return null;
        }

        private static S1Quests.QuestManager? Manager(ModChannel channel, string operation)
        {
            S1Quests.QuestManager? manager = S1Quests.QuestManager.Instance;
            if (manager == null)
                ModChannel.Warn($"Channel '{channel.Id}': {operation} needs a loaded game (no QuestManager); the message is dropped.");
            return manager;
        }

        private static void ReceiveQuestState(S1Quests.QuestManager manager, S1NetworkConnection? connection, string frame)
        {
#if (IL2CPPMELON)
            manager.ReceiveQuestState(connection, frame, S1Quests.EQuestState.Inactive);
#else
            _receiveQuestState ??= AccessTools.Method(typeof(S1Quests.QuestManager), "ReceiveQuestState")
                ?? throw new MissingMethodException("QuestManager.ReceiveQuestState");
            _receiveQuestState.Invoke(manager, new object?[] { connection, frame, S1Quests.EQuestState.Inactive });
#endif
        }
    }
}
