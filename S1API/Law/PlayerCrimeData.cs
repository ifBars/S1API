#if (IL2CPPMELON)
using S1PlayerScripts = Il2CppScheduleOne.PlayerScripts;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1PlayerScripts = ScheduleOne.PlayerScripts;
#endif

using UnityEngine;

namespace S1API.Law
{
    /// <summary>
    /// Manages a player's criminal record, wanted level, and police pursuit state.
    /// </summary>
    public sealed class PlayerCrimeData
    {
        /// <summary>
        /// INTERNAL: The underlying game crime data instance.
        /// </summary>
        internal readonly S1PlayerScripts.PlayerCrimeData S1CrimeData;

        /// <summary>
        /// INTERNAL: Constructor for creating crime data wrapper.
        /// </summary>
        internal PlayerCrimeData(S1PlayerScripts.PlayerCrimeData crimeData)
        {
            S1CrimeData = crimeData;
        }

        /// <summary>
        /// The current pursuit level (wanted level) of the player.
        /// </summary>
        public PursuitLevel CurrentPursuitLevel =>
            (PursuitLevel)S1CrimeData.CurrentPursuitLevel;

        /// <summary>
        /// The player's last known position to police.
        /// This is where officers will search if they lose sight of the player.
        /// </summary>
        public Vector3 LastKnownPosition =>
            S1CrimeData.LastKnownPosition;

        /// <summary>
        /// Time in seconds since police last saw the player.
        /// When this exceeds the search time for the current pursuit level, the pursuit ends.
        /// </summary>
        public float TimeSinceSighted =>
            S1CrimeData.TimeSinceSighted;

        /// <summary>
        /// Whether the player is subject to a body search by police.
        /// </summary>
        public bool BodySearchPending =>
            S1CrimeData.BodySearchPending;

        /// <summary>
        /// Whether the player has evaded arrest (escalated from Arresting to NonLethal).
        /// This flag affects the severity of charges.
        /// </summary>
        public bool EvadedArrest =>
            S1CrimeData.EvadedArrest;

        /// <summary>
        /// How long police will search for the player after losing sight, based on current pursuit level.
        /// </summary>
        /// <returns>Search time in seconds.</returns>
        public float GetSearchTime() =>
            S1CrimeData.GetSearchTime();

        /// <summary>
        /// Sets the player's pursuit level (wanted level).
        /// </summary>
        /// <param name="level">The pursuit level to set.</param>
        /// <remarks>
        /// Setting to None will clear all crimes and end the pursuit.
        /// This method will not work during tutorial mode.
        /// </remarks>
        public void SetPursuitLevel(PursuitLevel level)
        {
            S1CrimeData.SetPursuitLevel((S1PlayerScripts.PlayerCrimeData.EPursuitLevel)level);
        }

        /// <summary>
        /// Escalates the pursuit level to the next higher level.
        /// </summary>
        /// <remarks>
        /// Progression: None → Investigating → Arresting → NonLethal → Lethal.
        /// Escalating from Arresting to NonLethal marks the player as having evaded arrest.
        /// </remarks>
        public void Escalate()
        {
            S1CrimeData.Escalate();
        }

        /// <summary>
        /// De-escalates the pursuit level to the next lower level.
        /// </summary>
        /// <remarks>
        /// Progression: Lethal → NonLethal → Arresting → Investigating → None.
        /// De-escalating to None will clear all crimes.
        /// </remarks>
        public void Deescalate()
        {
            S1CrimeData.Deescalate();
        }

        /// <summary>
        /// Clears all crimes from the player's record.
        /// </summary>
        /// <remarks>
        /// This does not automatically end the pursuit or change the pursuit level.
        /// To fully clear wanted status, use <see cref="SetPursuitLevel"/> with <see cref="PursuitLevel.None"/>.
        /// </remarks>
        public void ClearCrimes()
        {
            S1CrimeData.ClearCrimes();
        }

        /// <summary>
        /// Records the player's current position as their last known position to police.
        /// </summary>
        /// <param name="resetTimeSinceSighted">If true, resets the time since sighted to 0 (police just saw the player).</param>
        public void RecordLastKnownPosition(bool resetTimeSinceSighted = true)
        {
            S1CrimeData.RecordLastKnownPosition(resetTimeSinceSighted);
        }
    }
}
