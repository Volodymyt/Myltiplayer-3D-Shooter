using System;
using Services;
using UnityEngine;
using Zenject;

namespace Gameplay
{
    public class PlayerMovement : IDisposable, ITickable
    {
        private readonly InputService _inputService;

        private readonly float _moveSpeed = 5f;
        private bool _isLocalPlayer;
        private bool _isMoving = false;
        private Vector3 _moveDirection;
        private Rigidbody _playerRigidbody;

        private readonly float _mouseSensitivity = 15f;
        private readonly float _minXRotation = -20;
        private readonly float _maxXRotation = 20;
        private float _xRotation;
        private Vector2 _currentLook;     
        private Vector2 _lookVelocity; 
        private Transform _cameraTransform;

        public PlayerMovement(InputService inputService)
        {
            _inputService = inputService;
        }

        public void Construct(Rigidbody playerRigidbody, Transform playerCamera, bool isLocalPlayer)
        {
            _playerRigidbody = playerRigidbody;
            _cameraTransform = playerCamera;
            _isLocalPlayer = isLocalPlayer;

            _inputService.OnKeyboardMoveStart += HandleKeyboardMoveStart;
            _inputService.OnKeyboardMoveStop += HandleKeyboardMoveStop;
            _inputService.OnMouseLook += HandleMouseLook;
        }

        public void Tick()
        {
            if (!_isLocalPlayer || _playerRigidbody == null)
                return;

            if (_isMoving)
            {
                Vector3 move = _playerRigidbody.transform.TransformDirection(_moveDirection) * _moveSpeed * Time.deltaTime;
                _playerRigidbody.MovePosition(_playerRigidbody.position + move);
            }
        }

        private void HandleKeyboardMoveStart(KeyboardContext context)
        {
            _moveDirection = context.GetMoveDirection();
            _isMoving = true;
        }

        private void HandleKeyboardMoveStop(KeyboardContext context)
        {
            _moveDirection = Vector3.zero;
            _isMoving = false;
        }

        private void HandleMouseLook(MouseContext context)
        {
            if (!_isLocalPlayer)
                return;

            Vector2 targetLook = context.GetLookDelta() * _mouseSensitivity;

            _currentLook = Vector2.SmoothDamp(_currentLook, targetLook, ref _lookVelocity, 0.05f);

            _xRotation -= _currentLook.y * Time.deltaTime;
            _xRotation = Mathf.Clamp(_xRotation, _minXRotation, _maxXRotation);
            _cameraTransform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);

            _playerRigidbody.transform.Rotate(Vector3.up * _currentLook.x * Time.deltaTime);
        }

        public void Dispose()
        {
            if (_inputService != null)
            {
                _inputService.OnKeyboardMoveStart -= HandleKeyboardMoveStart;
                _inputService.OnKeyboardMoveStop -= HandleKeyboardMoveStop;
                _inputService.OnMouseLook -= HandleMouseLook;
            }

            if (_inputService != null) _inputService.Dispose();

            _isMoving = false;
            _moveDirection = Vector3.zero;
            _playerRigidbody = null;
            _cameraTransform = null;
            _isLocalPlayer = false;
    
            _currentLook = Vector2.zero;
            _lookVelocity = Vector2.zero;
        }
    }
}