using System;

#if (IL2CPPMELON)
using S1GameInput = Il2CppScheduleOne.GameInput;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1GameInput = ScheduleOne.GameInput;
#endif

namespace S1API.Input
{
	/// <summary>
	/// Modder-facing facade over the base game's input state to keep S1API consumers decoupled
	/// from the underlying <c>ScheduleOne.GameInput</c> type across Mono/IL2CPP.
	/// </summary>
	public static class Controls
	{
		/// <summary>
		/// Gets or sets whether the player is currently typing in a UI field.
		/// When true, gameplay input should generally be ignored by systems listening for controls.
		/// </summary>
		public static bool IsTyping
		{
			get => S1GameInput.IsTyping;
			set => S1GameInput.IsTyping = value;
		}
	}
}


