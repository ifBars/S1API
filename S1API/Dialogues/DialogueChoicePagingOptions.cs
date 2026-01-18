using System;

namespace S1API.Dialogues
{
    /// <summary>
    /// Configuration for <see cref="DialogueChoicePaging"/>.
    /// </summary>
    public sealed class DialogueChoicePagingOptions
    {
        /// <summary>
        /// The maximum number of choice UI entries the vanilla dialogue canvas can show at once.
        /// Default is 8 (vanilla prefab provides 8 entries).
        /// </summary>
        public int MaxVisibleChoices { get; set; } = 8;

        /// <summary>
        /// Number of real choices to show per page (reserves one slot for the "More" entry).
        /// Defaults to 7.
        /// </summary>
        public int ChoicesPerPage { get; set; } = 7;

        /// <summary>
        /// Whether the "More" entry cycles back to page 1 after the last page.
        /// Default is true.
        /// </summary>
        public bool WrapPages { get; set; } = true;

        /// <summary>
        /// Format string for the paging entry. Receives (currentPage, totalPages).
        /// Default: "More ({0}/{1})".
        /// </summary>
        public string MoreTextFormat { get; set; } = "More ({0}/{1})";

        internal DialogueChoicePagingOptions Normalize()
        {
            var normalized = new DialogueChoicePagingOptions
            {
                MaxVisibleChoices = MaxVisibleChoices,
                ChoicesPerPage = ChoicesPerPage,
                WrapPages = WrapPages,
                MoreTextFormat = MoreTextFormat
            };

            if (normalized.MaxVisibleChoices < 2)
                normalized.MaxVisibleChoices = 2;

            int maxChoicesPerPage = Math.Max(1, normalized.MaxVisibleChoices - 1);
            if (normalized.ChoicesPerPage < 1)
                normalized.ChoicesPerPage = 1;
            if (normalized.ChoicesPerPage > maxChoicesPerPage)
                normalized.ChoicesPerPage = maxChoicesPerPage;

            if (string.IsNullOrWhiteSpace(normalized.MoreTextFormat))
                normalized.MoreTextFormat = "More ({0}/{1})";

            return normalized;
        }
    }
}

