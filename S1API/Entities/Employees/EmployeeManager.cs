#if IL2CPPMELON
using S1Employees = Il2CppScheduleOne.Employees;
#elif MONOMELON
using S1Employees = ScheduleOne.Employees;
#endif
using System.Collections.Generic;
using S1API.Avatar;
using UnityEngine;

namespace S1API.Entities.Employees
{
    /// <summary>
    /// Provides methods for managing employee appearances and related data.
    /// </summary>
    public static class EmployeeManager
    {
        private static readonly Logging.Log Logger = new("EmployeeManager");

        /// <summary>
        /// Gets an employee appearance by index.
        /// </summary>
        /// <param name="male">Whether to choose from male employee appearance pool</param>
        /// <param name="index">The index of the appearance to retrieve</param>
        /// <returns>An <see cref="EmployeeAppearance"/> representing the employee appearance at the specified index if successful; otherwise, null.</returns>
        public static EmployeeAppearance? GetAppearance(bool male, int index)
        {
            if (!S1Employees.EmployeeManager.InstanceExists)
            {
                Logger.Error("EmployeeManager instance does not exist; cannot get appearance");
                return null;
            }

            return new EmployeeAppearance(S1Employees.EmployeeManager.Instance.GetAppearance(male, index));
        }

        /// <summary>
        /// Gets a random employee appearance.
        /// </summary>
        /// <param name="male">Whether to choose from male employee appearance pool</param>
        /// <param name="index">The index of the appearance that was retrieved</param>
        /// <param name="settings">The avatar settings of the appearance that was retrieved</param>
        /// <returns>True if an appearance was successfully retrieved; otherwise, false.</returns>
        public static bool GetRandomAppearance(bool male, out int index, out AvatarSettings? settings)
        {
            if (!S1Employees.EmployeeManager.InstanceExists)
            {
                settings = null;
                index = -1;
                Logger.Error("EmployeeManager instance does not exist; cannot get random appearance");
                return false;
            }

            S1Employees.EmployeeManager.Instance.GetRandomAppearance(male, out var i, out var avatarSettings);
            index = i;
            settings = new AvatarSettings(avatarSettings);
            return true;
        }
    }

    /// <summary>
    /// Represents an employee appearance, including avatar settings and mugshot sprite.
    /// </summary>
    public class EmployeeAppearance
    {
        /// <summary>
        /// INTERNAL: The underlying employee appearance from the base game.
        /// </summary>
        internal S1Employees.EmployeeManager.EmployeeAppearance S1EmployeeAppearance;

        /// <summary>
        /// Gets the avatar settings associated with this employee appearance.
        /// </summary>
        public AvatarSettings Settings => new(S1EmployeeAppearance.Settings);

        /// <summary>
        /// Gets the mugshot sprite associated with this employee appearance.
        /// </summary>
        public Sprite Mugshot => S1EmployeeAppearance.Mugshot;

        /// <summary>
        /// INTERNAL: Initializes a new instance of the EmployeeAppearance class with the specified base game type appearance.
        /// </summary>
        internal EmployeeAppearance(S1Employees.EmployeeManager.EmployeeAppearance s1EmployeeAppearance)
        {
            S1EmployeeAppearance = s1EmployeeAppearance;
        }
    }
}