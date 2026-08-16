namespace S1API.Interaction
{
    /// <summary>
    /// Selects the native input action used to start an interaction.
    /// </summary>
    public enum InteractionPromptInput
    {
        /// <summary>The configured keyboard or controller interaction action.</summary>
        Interact,

        /// <summary>The configured primary-click action.</summary>
        PrimaryClick
    }
}
