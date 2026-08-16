namespace S1API.Interaction
{
    /// <summary>
    /// Selects the native visual and interaction state shown by an interaction prompt.
    /// </summary>
    public enum InteractionPromptState
    {
        /// <summary>Shows the normal input prompt.</summary>
        Default,

        /// <summary>Shows the native invalid prompt and prevents interaction start.</summary>
        Invalid,

        /// <summary>Hides the prompt and prevents interaction selection.</summary>
        Disabled,

        /// <summary>Shows the message without an input icon.</summary>
        Label
    }
}
