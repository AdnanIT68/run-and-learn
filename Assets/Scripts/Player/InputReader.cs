using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace FootballRunner.Player
{
    /// <summary>
    /// One place that answers "is the player asking to jump?", so gameplay code
    /// never cares whether that came from a keyboard, a mouse or a finger.
    /// This project uses the Input System package, so the legacy Input class
    /// is unavailable.
    /// </summary>
    public static class InputReader
    {
        private static readonly List<RaycastResult> RaycastHits = new List<RaycastResult>(8);
        private static PointerEventData _pointerData;
        private static EventSystem _pointerEventSystem;

        public static bool JumpPressed()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard != null &&
                (keyboard.spaceKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame))
            {
                return true;
            }

            Touchscreen touchscreen = Touchscreen.current;

            if (touchscreen != null && touchscreen.primaryTouch.press.wasPressedThisFrame)
            {
                return !IsPointerOverUI(touchscreen.primaryTouch.position.ReadValue());
            }

            Mouse mouse = Mouse.current;

            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                return !IsPointerOverUI(mouse.position.ReadValue());
            }

            return false;
        }

        /// <summary>Held state, used to allow shorter hops when the player taps briefly.</summary>
        public static bool JumpHeld()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard != null && (keyboard.spaceKey.isPressed || keyboard.upArrowKey.isPressed))
            {
                return true;
            }

            Touchscreen touchscreen = Touchscreen.current;

            if (touchscreen != null && touchscreen.primaryTouch.press.isPressed)
            {
                return true;
            }

            Mouse mouse = Mouse.current;
            return mouse != null && mouse.leftButton.isPressed;
        }

        /// <summary>
        /// Stops a tap on the pause button from also making the player jump.
        /// Raycasting the EventSystem is more reliable with the Input System than
        /// IsPointerOverGameObject(touchId), which often returns false on Android.
        /// </summary>
        private static bool IsPointerOverUI(Vector2 screenPosition)
        {
            EventSystem eventSystem = EventSystem.current;

            if (eventSystem == null)
            {
                return false;
            }

            if (_pointerData == null || _pointerEventSystem != eventSystem)
            {
                _pointerData = new PointerEventData(eventSystem);
                _pointerEventSystem = eventSystem;
            }

            _pointerData.Reset();
            _pointerData.position = screenPosition;
            RaycastHits.Clear();
            eventSystem.RaycastAll(_pointerData, RaycastHits);
            return RaycastHits.Count > 0;
        }
    }
}
