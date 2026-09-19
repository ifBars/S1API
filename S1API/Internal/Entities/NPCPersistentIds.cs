using System;
using System.Security.Cryptography;
using System.Text;

namespace S1API.Internal.Entities
{
    /// <summary>
    /// Creates stable native identifiers for custom NPCs that participate in persisted game systems.
    /// </summary>
    internal static class NPCPersistentIds
    {
        internal static bool TryGetGuid(string? npcId, out Guid guid)
        {
            if (string.IsNullOrWhiteSpace(npcId))
            {
                guid = Guid.Empty;
                return false;
            }

            byte[] hash;
            using (SHA256 algorithm = SHA256.Create())
            {
                string value = $"S1API.NPC:v1:{npcId.Trim().ToLowerInvariant()}";
                hash = algorithm.ComputeHash(Encoding.UTF8.GetBytes(value));
            }

            var guidBytes = new byte[16];
            Array.Copy(hash, guidBytes, guidBytes.Length);
            guidBytes[6] = (byte)((guidBytes[6] & 0x0f) | 0x50);
            guidBytes[8] = (byte)((guidBytes[8] & 0x3f) | 0x80);
            guid = new Guid(guidBytes);
            return true;
        }
    }
}
