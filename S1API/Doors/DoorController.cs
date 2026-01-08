using System;
using S1API.Internal.Abstraction;
using UnityEngine;
using UnityEngine.Events;

#if (IL2CPPMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Doors = Il2CppScheduleOne.Doors;
#else
using S1Doors = ScheduleOne.Doors;
#endif

namespace S1API.Doors
{
    /// <summary>
    /// Wrapper for ScheduleOne DoorController component.
    /// Provides a clean API without exposing game types directly.
    /// </summary>
    public sealed class DoorController
    {
        private readonly S1Doors.DoorController _controller;

        #region Internal Access

        /// <summary>
        /// Gets the underlying ScheduleOne DoorController component.
        /// </summary>
        internal S1Doors.DoorController Controller =>
            _controller;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets door access level.
        /// </summary>
        public DoorAccess PlayerAccess
        {
            get => (DoorAccess)_controller.PlayerAccess;
            set => _controller.PlayerAccess = (S1Doors.EDoorAccess)value;
        }

        /// <summary>
        /// Gets whether the door is currently open.
        /// </summary>
        public bool IsOpen =>
            _controller.IsOpen;

        /// <summary>
        /// Gets or sets whether players can automatically open the door.
        /// </summary>
        public bool AutoOpenForPlayer
        {
            get => _controller.AutoOpenForPlayer;
            set => _controller.AutoOpenForPlayer = value;
        }

        /// <summary>
        /// Gets or sets whether NPCs can open the door.
        /// </summary>
        public bool OpenableByNPCs
        {
            get => (bool)Internal.Utils.ReflectionUtils.TryGetFieldOrProperty(_controller, "OpenableByNPCs");
            set => Internal.Utils.ReflectionUtils.TrySetFieldOrProperty(_controller, "OpenableByNPCs", value);
        }

        #endregion

        #region Events

        /// <summary>
        /// Event fired when the door is opened from a specific side.
        /// </summary>
        public event Action<DoorSide> OnDoorOpened;

        /// <summary>
        /// Event fired when the door is opened (any side).
        /// </summary>
        public event Action OnDoorOpenedAny;

        /// <summary>
        /// Event fired when the door is closed.
        /// </summary>
        public event Action OnDoorClosed;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new wrapper from a GameObject containing the DoorController component.
        /// </summary>
        /// <param name="gameObject">GameObject containing the DoorController component.</param>
        public DoorController(GameObject gameObject)
        {
            _controller = gameObject.GetComponent<S1Doors.DoorController>();
            if (_controller == null)
            {
                throw new ArgumentException($"GameObject '{gameObject.name}' does not contain a DoorController component");
            }

            SubscribeToEvents();
        }

        #endregion

        #region Private Methods

        private void SubscribeToEvents()
        {
            EventHelper.AddListener(HandleDoorOpened, _controller.onDoorOpened);
            EventHelper.AddListener(HandleDoorClosed, _controller.onDoorClosed);
        }

        private void HandleDoorOpened(S1Doors.EDoorSide side)
        {
            OnDoorOpened?.Invoke((DoorSide)side);
            OnDoorOpenedAny?.Invoke();
        }

        private void HandleDoorClosed() =>
            OnDoorClosed?.Invoke();

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets whether the door is open.
        /// </summary>
        /// <param name="open">True to open, false to close.</param>
        /// <param name="side">The side the door is being opened from.</param>
        public void SetIsOpen(bool open, DoorSide side) =>
            _controller.SetIsOpen(open, (S1Doors.EDoorSide)side);

        #endregion
    }
}
