using System;
using S1API.Internal.Abstraction;
using UnityEngine;
using UnityEngine.Events;

#if (IL2CPPMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Misc = Il2CppScheduleOne.Misc;
using S1Interaction = Il2CppScheduleOne.Interaction;
#else
using S1Interaction = ScheduleOne.Interaction;
using S1Misc = ScheduleOne.Misc;
#endif

namespace S1API.Misc
{
    /// <summary>
    /// Wrapper for ScheduleOne ModularSwitch component.
    /// Provides a clean API without exposing game types directly.
    /// </summary>
    public sealed class ModularSwitch
    {
        private readonly S1Misc.ModularSwitch _switch;

        #region Internal Access

        /// <summary>
        /// Gets the underlying ScheduleOne ModularSwitch component.
        /// </summary>
        internal S1Misc.ModularSwitch Switch =>
            _switch;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets whether the switch is currently on.
        /// </summary>
        public bool IsOn
        {
            get => _switch.isOn;
            set => _switch.isOn = value;
        }

        private string _onMessage = "Switch Off";
        private string _offMessage = "Switch On";
        private bool _hoverListenerAdded = false;

        #endregion

        #region Events

        /// <summary>
        /// Event fired when the switch is toggled on or off.
        /// </summary>
        public event Action<bool>? OnToggled;

        /// <summary>
        /// Event fired when the switch is turned on.
        /// </summary>
        public event Action? OnSwitchedOn;

        /// <summary>
        /// Event fired when the switch is turned off.
        /// </summary>
        public event Action? OnSwitchedOff;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new wrapper from a GameObject containing a ModularSwitch component.
        /// </summary>
        /// <param name="gameObject">GameObject containing a ModularSwitch component.</param>
        public ModularSwitch(GameObject gameObject)
        {
            _switch = gameObject.GetComponent<S1Misc.ModularSwitch>();
            if (_switch == null)
            {
                throw new ArgumentException($"GameObject '{gameObject.name}' does not contain a ModularSwitch component");
            }

            SubscribeToEvents();
        }

        #endregion

        #region Private Methods

        private void SubscribeToEvents()
        {
#if !MONOMELON
            _switch.onToggled += new System.Action<bool>(HandleToggled);
#else
            _switch.onToggled += HandleToggled;
#endif
            EventHelper.AddListener(HandleSwitchedOn, _switch.switchedOn);
            EventHelper.AddListener(HandleSwitchedOff, _switch.switchedOff);
        }

        private void HandleToggled(bool isOn) =>
            OnToggled?.Invoke(isOn);

        private void HandleSwitchedOn() =>
            OnSwitchedOn?.Invoke();

        private void HandleSwitchedOff() =>
            OnSwitchedOff?.Invoke();

        #endregion

        #region Public Methods

        /// <summary>
        /// Updates the interaction message based on current switch state.
        /// </summary>
        /// <param name="messageWhenOn">Message to display when switch is On (e.g. "Lock Doors").</param>
        /// <param name="messageWhenOff">Message to display when switch is Off (e.g. "Unlock Doors").</param>
        public void SetInteractionMessages(string messageWhenOn, string messageWhenOff)
        {
            _onMessage = messageWhenOn;
            _offMessage = messageWhenOff;

            if (!_hoverListenerAdded)
            {
                S1Interaction.InteractableObject intObj = (S1Interaction.InteractableObject)Internal.Utils.ReflectionUtils.TryGetFieldOrProperty(_switch, "intObj");
                if (intObj != null)
                {
                    Action hoverHandler = () =>
                    {
                        if (_switch.isOn)
                        {
                            intObj.SetMessage(_onMessage);
                        }
                        else
                        {
                            intObj.SetMessage(_offMessage);
                        }
                    };
                    EventHelper.AddListener(hoverHandler, intObj.onHovered);
                    _hoverListenerAdded = true;
                }
            }
        }

        /// <summary>
        /// Turns the switch on programmatically.
        /// </summary>
        public void SwitchOn() =>
            _switch.SwitchOn();

        /// <summary>
        /// Turns the switch off programmatically.
        /// </summary>
        public void SwitchOff() =>
            _switch.SwitchOff();

        #endregion
    }
}
