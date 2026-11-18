using UnityEngine;

namespace S1API.Entities
{
    /// <summary>
    /// Data of an NPC that is about to initialize, as passed to <see cref="NPC.OnInitializing"/>.
    /// </summary>
    public sealed class NpcInitializingArgs
    {
        internal NpcInitializingArgs(string id, Sprite? mugshot)
        {
            Id = id;
            Mugshot = mugshot;
        }

        /// <summary>The id of the NPC.</summary>
        public string Id { get; }

        /// <summary>
        /// The mugshot the NPC initializes with. Set it to replace the mugshot; the game reads it after all handlers ran.
        /// </summary>
        public Sprite? Mugshot { get; set; }
    }
}
