#if (IL2CPPMELON)
using S1Economy = Il2CppScheduleOne.Economy;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Economy = ScheduleOne.Economy;
#endif

namespace S1API.Economy
{
    /// <summary>
    /// Type of dealer behavior for NPCs.
    /// </summary>
    public enum DealerType
    {
        /// <summary>
        /// Dealer that works for the player, selling products to assigned customers.
        /// </summary>
        PlayerDealer = 0,
        
        /// <summary>
        /// Dealer that works for the cartel, independent of player control.
        /// </summary>
        CartelDealer = 1
    }
}

