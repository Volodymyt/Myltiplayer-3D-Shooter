using System;
using Services;
using UnityEngine;
using Zenject;

namespace Gameplay
{
    public class PlayerMovement : IDisposable, ITickable
    {
        private static readonly int MoveX = Animator.StringToHash("MoveX");
        private static readonly int MoveZ = Animator.StringToHash("MoveZ");
        private static readonly int MoveY = Animator.StringToHash("MoveY");
        private static readonly int JumpTrigger = Animator.StringToHash("JumpTrigger");
        private static readonly int IsGrounded = Animator.StringToHash("IsGrounded");

        private readonly InputService _inputService;

        private bool _isLocalPlayer;
        private bool _isMoving = false;
        private Vector3 _moveDirection;
        private Rigidbody _playerRigidbody;
        private Animator _playerAnimator;

        private readonly LayerMask _groundLayer = LayerMask.GetMask("Ground");
        private bool _isGrounded;
        private Collider _playerCollider;

        private float _xRotation;
        private Vector2 _currentLook;
        private Vector2 _lookVelocity;
        private Transform _cameraTransform;
        private bool _isGrounded2;

        public PlayerMovement(InputService inputService)
        {
            _inputService = inputService;
        }

        public void Construct(PlayerView player, bool isLocalPlayer)
        {
            _playerRigidbody = player.playerRigidbody;
            _playerCollider = player.playerCollider;
            _playerAnimator = player.playerAnimator;

            _cameraTransform = player.playerCamera.transform;
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
            
            _isGrounded2 = Physics.Raycast(
                _playerRigidbody.position + Vector3.up * 0.05f,
                Vector3.down,
                _playerCollider.bounds.extents.y + 0.5f,
                _groundLayer);
            
            Debug.Log(_isGrounded);
            
            if (_isMoving)
            {
                Vector3 move = _playerRigidbody.transform.TransformDirection(_moveDirection)
                               * (Constants.PlayerSettings.MoveSpeed * Time.deltaTime);
                _playerRigidbody.MovePosition(_playerRigidbody.position + move);

                PlayMoveAnimation(_moveDirection);
            }
            else if (!_isMoving && _playerAnimator.GetFloat(MoveX) != 0 || _playerAnimator.GetFloat(MoveZ) != 0)
            {
                PlayMoveAnimation(new Vector3(0, 0, 0));
            }
            
            _playerAnimator.SetFloat(MoveY, _playerRigidbody.linearVelocity.y);
            _playerAnimator.SetBool(IsGrounded, _isGrounded2);
        }

        private void PlayMoveAnimation(Vector3 moveDirection)
        {
            float currentX = _playerAnimator.GetFloat(MoveX);
            float currentY = _playerAnimator.GetFloat(MoveZ);

            float smoothX = Mathf.Lerp(currentX, moveDirection.x, Time.deltaTime * 10f);
            float smoothY = Mathf.Lerp(currentY, moveDirection.z, Time.deltaTime * 10f);

            _playerAnimator.SetFloat(MoveX, smoothX);
            _playerAnimator.SetFloat(MoveZ, smoothY);
        }

        private void HandleKeyboardJump()
        {
            if (!_isLocalPlayer || !_isGrounded)
                return;
            
            _playerRigidbody.AddForce(
                Vector3.up * Constants.PlayerSettings.JumpForce,
                ForceMode.Impulse);
            
            _playerAnimator.SetTrigger(JumpTrigger);
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