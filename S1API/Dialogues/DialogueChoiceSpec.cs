using System;

namespace S1API.Dialogues
{
    /// <summary>
    /// Describes a dialogue choice that S1API adds to a vanilla NPC menu each time the menu is built.
    /// </summary>
    /// <remarks>
    /// Register a spec with <see cref="DialogueChoices.Add"/>. All properties are read when the menu opens,
    /// so a mod can change <see cref="Text"/> or the callbacks after registration.
    /// </remarks>
    public sealed class DialogueChoiceSpec
    {
        /// <summary>
        /// Unique label of the choice. Used to find it again when it is chosen or checked.
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Text shown for the choice.
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Name of the conversation the node belongs to, for example <c>Realtor_SellProperty</c>.
        /// Null matches every conversation of the NPC.
        /// </summary>
        public string? ConversationName { get; set; }

        /// <summary>
        /// Label of a dialogue node in the same conversation to show after the choice.
        /// Null ends the dialogue after <see cref="OnChosen"/>, unless <see cref="OnChosen"/> started another one.
        /// </summary>
        public string? TargetNodeLabel { get; set; }

        /// <summary>
        /// Label of the choice to insert after. When it is missing from the menu, the choice is appended.
        /// </summary>
        public string? InsertAfterLabel { get; set; }

        /// <summary>
        /// Inserts the choice before the last one, which most vanilla menus use for the farewell.
        /// Ignored when <see cref="InsertAfterLabel"/> is found.
        /// </summary>
        public bool InsertBeforeLastChoice { get; set; }

        /// <summary>
        /// Evaluated each time the menu is built. Null always shows the choice.
        /// </summary>
        public Func<bool>? IsShown { get; set; }

        /// <summary>
        /// Evaluated when the player hovers or picks the choice. Null always enables it.
        /// </summary>
        public Func<bool>? IsEnabled { get; set; }

        /// <summary>
        /// Reason the game shows while <see cref="IsEnabled"/> returns false.
        /// </summary>
        public string? DisabledReason { get; set; }

        /// <summary>
        /// Called when the player picks the choice.
        /// </summary>
        public Action? OnChosen { get; set; }
    }
}
