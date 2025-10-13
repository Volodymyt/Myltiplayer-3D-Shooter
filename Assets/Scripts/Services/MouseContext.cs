using UnityEngine;

namespace Services
{
    public class MouseContext
    {
        private readonly PlayerInputActions _inputActions;

        public MouseContext(PlayerInputActions inputActions)
        {
            _inputActions = inputActions;
        }

        public Vector2 GetLookDelta()
        {
            return _inputActions.Mouse.Look.ReadValue<Vector2>();
        }
    }
}