using System;
using System.Collections.Generic;
using MelonLoader;
#if (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1ConsoleCommand = ScheduleOne.Console.ConsoleCommand;

#elif (IL2CPPMELON)
using S1ConsoleCommand = Il2CppScheduleOne.Console.ConsoleCommand;
#endif

#if (IL2CPPMELON || IL2CPPBEPINEX)
using Il2CppInterop.Runtime.Injection;
#endif

namespace S1API.Console
{
    /// <summary>
    /// Wraps a <see cref="BaseConsoleCommand"/> instance to adapt it to the <see cref="S1ConsoleCommand"/> expected by the base console system.
    /// </summary>
    [RegisterTypeInIl2Cpp]
    internal class ConsoleCommandWrapper : S1ConsoleCommand
    {
        private readonly BaseConsoleCommand _impl;

#if IL2CPPMELON
        /// <summary>
        /// Constructor called by the IL2CPP runtime.
        /// </summary>
        /// <param name="ptr">Pointer to the native IL2CPP object.</param>
        public ConsoleCommandWrapper(IntPtr ptr) : base(ptr) { }

        /// <summary>
        /// Creates a new instance wrapping a user-defined <see cref="BaseConsoleCommand"/>.
        /// </summary>
        /// <param name="impl">The user-defined command implementation.</param>
        public ConsoleCommandWrapper(BaseConsoleCommand impl)
            : base(ClassInjector.DerivedConstructorPointer<ConsoleCommandWrapper>())
        {
            ClassInjector.DerivedConstructorBody(this);
            _impl = impl;
        }

        /// <summary>
        /// Executes the wrapped command with IL2CPP argument conversion.
        /// </summary>
        /// <param name="args">List of IL2CPP strings.</param>
        public override void Execute(Il2CppSystem.Collections.Generic.List<string> args)
        {
            var managedArgs = new System.Collections.Generic.List<string>(args.Count);
            foreach (var arg in args)
                managedArgs.Add(arg);

            _impl.ExecuteCommand(managedArgs);
        }

#else
        /// <summary>
        /// Creates a new instance wrapping a user-defined <see cref="BaseConsoleCommand"/>.
        /// </summary>
        /// <param name="impl">The user-defined command implementation.</param>
        public ConsoleCommandWrapper(BaseConsoleCommand impl)
        {
            _impl = impl;
        }

        /// <summary>
        /// Executes the wrapped command with managed argument list.
        /// </summary>
        /// <param name="args">List of managed strings.</param>
        public override void Execute(System.Collections.Generic.List<string> args)
        {
            _impl.ExecuteCommand(args);
        }
#endif

        /// <summary>
        /// Gets the command word from the wrapped command.
        /// </summary>
        public override string CommandWord => _impl.CommandWord;

        /// <summary>
        /// Gets the command description from the wrapped command.
        /// </summary>
        public override string CommandDescription => _impl.CommandDescription;

        /// <summary>
        /// Gets the example usage string from the wrapped command.
        /// </summary>
        public override string ExampleUsage => _impl.ExampleUsage;
    }
}