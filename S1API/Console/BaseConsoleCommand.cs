using System.Collections.Generic;

namespace S1API.Console
{
    /// <summary>
    /// Abstract base class for creating console commands.
    /// Inherit from this base class to auto-register a custom command in the API.
    /// </summary>
    public abstract class BaseConsoleCommand
    {
        /// <summary>
        /// The command word that triggers this console command.
        /// </summary>
        public abstract string CommandWord { get; }

        /// <summary>
        /// A brief description of what the command does.
        /// </summary>
        public abstract string CommandDescription { get; }

        /// <summary>
        /// An example of how to use the command.
        /// </summary>
        public abstract string ExampleUsage { get; }

        /// <summary>
        /// Executes the command with the provided arguments.
        /// </summary>
        /// <param name="args">The list of arguments passed to the command.</param>
        public abstract void ExecuteCommand(List<string> args);
    }
}