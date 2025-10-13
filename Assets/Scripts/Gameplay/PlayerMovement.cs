using System;
using Services;
using UnityEngine;
using Zenject;

namespace Gameplay
{
    public class PlayerMovement : IDisposable, ITickable
    {
        private readonly InputService _inputService;
        
        private bool _isLocalPlayer;
        private bool _isMoving = false;
        private Vector3 _moveDirection;
        private Rigidbody _playerRigidbody;
        
        private readonly LayerMask _groundLayer = LayerMask.GetMask("Ground");
        private bool _isGrounded;
        private Collider _playerCollider;

        private float _xRotation;
        private Vector2 _currentLook;     
        private Vector2 _lookVelocity; 
        private Transform _cameraTransform;

        public PlayerMovement(InputService inputService)
        {
            _inputService = inputService;
        }

        public void Construct(GameObject playerRigidbody, Transform playerCamera, bool isLocalPlayer)
        {
            _playerRigidbody = playerRigidbody.GetComponent<Rigidbody>();
            _playerCollider = playerRigidbody.GetComponent<Collider>();
            
            _cameraTransform = playerCamera;
            _isLocalPlayer = isLocalPlayer;

            _inputService.OnKeyboardMoveStart += HandleKeyboardMoveStart;
            _inputService.OnKeyboardMoveStop += HandleKeyboardMoveStop;
            _inputService.OnKeyboardJump += HandleKeyboardJump;
            _inputService.OnMouseLook += HandleMouseLook;
        }

        public void Tick()
        {
            if (!_isLocalPlayer || _playerRigidbody == null)
                return;

            _isGrounded = Physics.Raycast(
                _playerRigidbody.position + Vector3.up * 0.05f,
                Vector3.down,
                _playerCollider.bounds.extents.y + 0.1f,
                _groundLayer);
            
            if (_isMoving)
            {
                Vector3 move = _playerRigidbody.transform.TransformDirection(_moveDirection)
                               * (Constants.PlayerSettings.MoveSpeed * Time.deltaTime);
                
                _playerRigidbody.MovePosition(_playerRigidbody.position + move);
            }
        }
        
        private void HandleKeyboardJump()
        {
            if (!_isLocalPlayer || !_isGrounded)
                return;

            _playerRigidbody.AddForce(
                Vector3.up * Constants.PlayerSettings.JumpForce,
                ForceMode.Impulse);
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

            Vector2 targetLook = context.GetLookDelta() * Constants.PlayerSettings.MouseSensitivity;

            _currentLook = Vector2.SmoothDamp(_currentLook, targetLook, ref _lookVelocity, 0.05f);

            _xRotation -= _currentLook.y * Time.deltaTime;
            _xRotation = Mathf.Clamp(
                _xRotation,
                Constants.PlayerSettings.MinXRotation,
                Constants.PlayerSettings.MaxXRotation);
            
            _cameraTransform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);

            _playerRigidbody.transform.Rotate(Vector3.up * _currentLook.x * Time.deltaTime);
        }

        public void Dispose()
        {
            if (_inputService != null)
            {
                _inputService.OnKeyboardMoveStart -= HandleKeyboardMoveStart;
                _inputService.OnKeyboardMoveStop -= HandleKeyboardMoveStop;
                _inputService.OnKeyboardJump -= HandleKeyboardJump;
                _inputService.OnMouseLook -= HandleMouseLook;
            }

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