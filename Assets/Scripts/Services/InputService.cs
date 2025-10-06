using System;
using UnityEngine.InputSystem;

namespace Services
{
    public class InputService : IDisposable
    {
        public event Action<KeyboardContext> OnKeyboardMoveStart;
        
        public event Action<KeyboardContext> OnKeyboardMoveStop;

        private readonly PlayerInputActions _inputActions;

        private KeyboardContext _keyboardContext;
        
        public InputService(PlayerInputActions inputActions)
        {
            _inputActions = inputActions;
        }
        
        public void Construct()
        {
            _keyboardContext = new KeyboardContext(_inputActions);

            _inputActions.Keyboard.Move.performed += HandleKeyboardMoveStarted;
            _inputActions.Keyboard.Move.canceled += HandleKeyboardMoveCanceled;
            _inputActions.Keyboard.Enable();
        }
        
        private void HandleKeyboardMoveStarted(InputAction.CallbackContext context) =>
            OnKeyboardMoveStart?.Invoke(_keyboardContext);

        private void HandleKeyboardMoveCanceled(InputAction.CallbackContext context) =>
            OnKeyboardMoveStop?.Invoke(_keyboardContext);
        
        public void Dispose()
        {
            if (_inputActions != null)
            {
                _inputActions.Keyboard.Move.performed -= HandleKeyboardMoveStarted;
                _inputActions.Keyboard.Move.canceled -= HandleKeyboardMoveCanceled;
                _inputActions.Keyboard.Disable();
            }
        }
    }
}