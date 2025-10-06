using UnityEngine;

namespace Services
{
    public record KeyboardContext
    {
        private readonly PlayerInputActions _inputActions;

        public KeyboardContext(PlayerInputActions inputActions)
        {
            _inputActions = inputActions;
        }

        public Vector3 GetMoveDirection()
        {
            return _inputActions.Keyboard.Move.ReadValue<Vector3>();
        }
    }
}