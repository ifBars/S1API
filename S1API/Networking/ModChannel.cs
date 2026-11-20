using System;
using System.Collections.Generic;
using System.Reflection;
using S1API.Entities;
using S1API.Internal.Networking;
using S1API.Logging;

namespace S1API.Networking
{
    /// <summary>
    /// A named co-op message channel. Clients send to the server, the server sends to every client or to one player.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Messages are strings. Serialize your own data into the payload, for example as JSON or as fields joined by a
    /// separator. Use a channel id that names your mod, such as <c>"author.modname"</c>, so channels of different
    /// mods never meet.
    /// </para>
    /// <para>
    /// On the host, <see cref="SendToServer"/> is handled locally and <see cref="SendToClients"/> also reaches the
    /// host's own <see cref="OnClientReceive"/>. In single player every send is handled locally.
    /// </para>
    /// <para>
    /// Each handler runs in its own try/catch. A handler that throws is logged with the channel and its mod; the
    /// other handlers still run.
    /// </para>
    /// </remarks>
    public sealed class ModChannel
    {
        /// <summary>
        /// The longest payload, in characters, that a send accepts. Longer payloads are rejected with an error log.
        /// </summary>
        public const int MaxPayloadChars = 60000;

        private static readonly Log Logger = new Log("ModChannel");
        private static readonly object Gate = new object();
        private static readonly Dictionary<string, ModChannel> Channels = new Dictionary<string, ModChannel>(StringComparer.Ordinal);
        private static readonly HashSet<string> ReportedUnknown = new HashSet<string>(StringComparer.Ordinal);

        private bool _reportedNoServerHandler;
        private bool _reportedNoClientHandler;

        private ModChannel(string id)
        {
            Id = id;
        }

        /// <summary>
        /// The channel id passed to <see cref="Get"/>.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Raised on the server when a client (or the host itself) sent a message with <see cref="SendToServer"/>.
        /// The first argument is the sending player, or null when the sender could not be resolved.
        /// </summary>
        public event Action<Player?, string>? OnServerReceive;

        /// <summary>
        /// Raised on a client (the host included) when the server sent a message with <see cref="SendToClients"/>
        /// or <see cref="SendTo"/>.
        /// </summary>
        public event Action<string>? OnClientReceive;

        /// <summary>
        /// Returns the channel with this id, created on first use. Every call with the same id returns the same channel.
        /// </summary>
        /// <param name="id">A non-empty id without the character '|', for example <c>"author.modname"</c>.</param>
        /// <exception cref="ArgumentException">The id is empty or contains '|'.</exception>
        public static ModChannel Get(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("A channel id must not be empty.", nameof(id));
            if (id.IndexOf(ModChannelFrame.Separator) >= 0)
                throw new ArgumentException($"A channel id must not contain '{ModChannelFrame.Separator}': {id}", nameof(id));

            lock (Gate)
            {
                if (!Channels.TryGetValue(id, out ModChannel? channel))
                {
                    channel = new ModChannel(id);
                    Channels[id] = channel;
                }

                return channel;
            }
        }

        /// <summary>
        /// Sends a message to the server. On the host it is handled right away, with the host as sender.
        /// </summary>
        /// <param name="payload">The message; null is sent as an empty string.</param>
        public void SendToServer(string payload)
        {
            if (TryFrame(ModChannelFrame.ToServer, payload, "SendToServer", out string frame, out string text))
                ModChannelTransport.SendToServer(this, frame, text);
        }

        /// <summary>
        /// Server only: sends a message to every client, the host's own client included.
        /// </summary>
        /// <param name="payload">The message; null is sent as an empty string.</param>
        public void SendToClients(string payload)
        {
            if (TryFrame(ModChannelFrame.ToClients, payload, "SendToClients", out string frame, out string text))
                ModChannelTransport.SendToClients(this, frame, text);
        }

        /// <summary>
        /// Server only: sends a message to one player's client.
        /// </summary>
        /// <param name="player">The receiving player.</param>
        /// <param name="payload">The message; null is sent as an empty string.</param>
        public void SendTo(Player player, string payload)
        {
            if (player == null)
            {
                Fail($"Channel '{Id}': SendTo needs a player; the message is dropped.");
                return;
            }

            if (TryFrame(ModChannelFrame.ToClients, payload, "SendTo", out string frame, out string text))
                ModChannelTransport.SendTo(this, player, frame, text);
        }

        internal bool TryFrame(char direction, string? payload, string operation, out string frame, out string text)
        {
            text = payload ?? string.Empty;
            frame = string.Empty;
            if (text.Length > MaxPayloadChars)
            {
                Fail($"Channel '{Id}': {operation} payload has {text.Length} characters, more than {MaxPayloadChars}; the message is dropped.");
                return false;
            }

            frame = ModChannelFrame.Encode(direction, Id, text);
            return true;
        }

        internal static bool TryFind(string id, out ModChannel? channel)
        {
            lock (Gate)
            {
                if (Channels.TryGetValue(id, out channel))
                    return true;

                if (ReportedUnknown.Add(id))
                    Warn($"Channel '{id}' received a message, but no mod here uses it. Is a mod missing on this side? Further messages on it are dropped without a log line.");
                return false;
            }
        }

        internal void DeliverToServer(Player? sender, string payload)
        {
            Action<Player?, string>? handlers = OnServerReceive;
            if (handlers == null)
            {
                if (!_reportedNoServerHandler)
                {
                    _reportedNoServerHandler = true;
                    Info($"Channel '{Id}' got a message for the server, but nothing handles OnServerReceive here.");
                }
                return;
            }

            foreach (Delegate handler in handlers.GetInvocationList())
            {
                try
                {
                    ((Action<Player?, string>)handler)(sender, payload);
                }
                catch (Exception ex)
                {
                    Fail($"Channel '{Id}': OnServerReceive handler of {OwnerOf(handler)} threw. {ex}");
                }
            }
        }

        internal void DeliverToClient(string payload)
        {
            Action<string>? handlers = OnClientReceive;
            if (handlers == null)
            {
                if (!_reportedNoClientHandler)
                {
                    _reportedNoClientHandler = true;
                    Info($"Channel '{Id}' got a message for the client, but nothing handles OnClientReceive here.");
                }
                return;
            }

            foreach (Delegate handler in handlers.GetInvocationList())
            {
                try
                {
                    ((Action<string>)handler)(payload);
                }
                catch (Exception ex)
                {
                    Fail($"Channel '{Id}': OnClientReceive handler of {OwnerOf(handler)} threw. {ex}");
                }
            }
        }

        /// <summary>
        /// INTERNAL: Removes every channel. Only for tests.
        /// </summary>
        internal static void ResetForTesting()
        {
            lock (Gate)
            {
                Channels.Clear();
                ReportedUnknown.Clear();
            }
        }

        /// <summary>
        /// INTERNAL: Receives log lines instead of MelonLoader while set. Only for tests, which run without MelonLoader.
        /// </summary>
        internal static Action<string>? LogSinkForTesting { get; set; }

        internal static void Info(string message)
        {
            if (LogSinkForTesting != null)
                LogSinkForTesting("info: " + message);
            else
                Logger.Msg(message);
        }

        internal static void Warn(string message)
        {
            if (LogSinkForTesting != null)
                LogSinkForTesting("warn: " + message);
            else
                Logger.Warning(message);
        }

        internal static void Fail(string message)
        {
            if (LogSinkForTesting != null)
                LogSinkForTesting("error: " + message);
            else
                Logger.Error(message);
        }

        private static string OwnerOf(Delegate handler)
        {
            Assembly? assembly = handler.Method?.DeclaringType?.Assembly;
            return assembly?.GetName().Name ?? "unknown mod";
        }
    }
}
