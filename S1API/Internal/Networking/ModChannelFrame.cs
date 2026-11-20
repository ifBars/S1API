using System;

namespace S1API.Internal.Networking
{
    /// <summary>
    /// INTERNAL: Wire format of a <c>ModChannel</c> message, carried in the quest GUID string
    /// of the vanilla quest state RPCs: <c>s1api|&lt;direction&gt;|&lt;channel&gt;|&lt;payload&gt;</c>.
    /// </summary>
    /// <remarks>
    /// The direction is <see cref="ToServer"/> or <see cref="ToClients"/>, so a side can drop frames that a vanilla
    /// relay delivers to the wrong role. Real quest GUIDs never start with the prefix and pass through untouched.
    /// </remarks>
    internal readonly struct ModChannelFrame
    {
        internal const string Prefix = "s1api|";
        internal const char Separator = '|';
        internal const char ToServer = 's';
        internal const char ToClients = 'c';

        private ModChannelFrame(char direction, string channelId, string payload)
        {
            Direction = direction;
            ChannelId = channelId;
            Payload = payload;
        }

        internal char Direction { get; }

        internal string ChannelId { get; }

        internal string Payload { get; }

        internal bool IsToServer => Direction == ToServer;

        internal static string Encode(char direction, string channelId, string payload) =>
            Prefix + direction + Separator + channelId + Separator + payload;

        internal static bool IsFrame(string? value) =>
            value != null && value.StartsWith(Prefix, StringComparison.Ordinal);

        /// <summary>
        /// Decodes a frame. Returns false for anything else, a real quest GUID among them.
        /// </summary>
        internal static bool TryDecode(string? value, out ModChannelFrame frame)
        {
            frame = default;
            if (!IsFrame(value))
                return false;

            string text = value!;
            int directionAt = Prefix.Length;
            if (text.Length < directionAt + 2 || text[directionAt + 1] != Separator)
                return false;

            char direction = text[directionAt];
            if (direction != ToServer && direction != ToClients)
                return false;

            int idStart = directionAt + 2;
            int idEnd = text.IndexOf(Separator, idStart);
            if (idEnd <= idStart)
                return false;

            frame = new ModChannelFrame(direction, text.Substring(idStart, idEnd - idStart), text.Substring(idEnd + 1));
            return true;
        }
    }
}
