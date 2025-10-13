using System;
using UnityEngine.InputSystem;

namespace Services
{
    public class InputService : IDisposable
    {
        public event Action<KeyboardContext> OnKeyboardMoveStart;
        public event Action<KeyboardContext> OnKeyboardMoveStop;
        public event Action OnKeyboardJump;

        public event Action<MouseContext> OnMouseLook;

        private readonly PlayerInputActions _inputActions;
        private KeyboardContext _keyboardContext;
        private MouseContext _mouseContext;

        public InputService(PlayerInputActions inputActions)
        {
            _inputActions = inputActions;
        }

        public void Construct()
        {
            _keyboardContext = new KeyboardContext(_inputActions);
            _mouseContext = new MouseContext(_inputActions);

            _inputActions.Keyboard.Move.performed += HandleKeyboardMoveStarted;
            _inputActions.Keyboard.Move.canceled += HandleKeyboardMoveCanceled;
            _inputActions.Keyboard.Jump.performed += HandleJump;
            _inputActions.Keyboard.Enable();

            _inputActions.Mouse.Look.performed += HandleMouseLook;
            _inputActions.Mouse.Enable();
        }

        private void HandleKeyboardMoveStarted(InputAction.CallbackContext context)
            => OnKeyboardMoveStart?.Invoke(_keyboardContext);

        private void HandleKeyboardMoveCanceled(InputAction.CallbackContext context)
            => OnKeyboardMoveStop?.Invoke(_keyboardContext);
        
        private void HandleJump(InputAction.CallbackContext context)
            => OnKeyboardJump?.Invoke();

        private void HandleMouseLook(InputAction.CallbackContext context)
            => OnMouseLook?.Invoke(_mouseContext);

        public void Dispose()
        {
            if (_inputActions == null) return;

            _inputActions.Keyboard.Move.performed -= HandleKeyboardMoveStarted;
            _inputActions.Keyboard.Move.canceled -= HandleKeyboardMoveCanceled;
            _inputActions.Keyboard.Jump.performed -= HandleJump;
            _inputActions.Mouse.Look.performed -= HandleMouseLook;

            _inputActions.Keyboard.Disable();
            _inputActions.Mouse.Disable();
        }
    }
}