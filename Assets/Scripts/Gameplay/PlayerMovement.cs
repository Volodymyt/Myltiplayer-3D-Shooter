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
        private bool _isMoving;
        private Vector3 _moveDirection;
        private PlayerView _playerView;
        private ActiveRagdoll _ragdoll;

        private readonly float _landingLockDuration = 0.55f;
        private bool _canJump = true;
        private float _jumpLockTimer;
        private bool _wasGroundedLastFrame = true;
        private bool _isGrounded;

        private float _xRotation;
        private Vector2 _currentLook;
        private Vector2 _lookVelocity;

        public PlayerMovement(InputService inputService)
        {
            _inputService = inputService;
        }

        public void Construct(PlayerView playerView, bool isLocalPlayer)
        {
            _playerView = playerView;
            _ragdoll = playerView.activeRagdoll;
            _isLocalPlayer = isLocalPlayer;

            if (_ragdoll == null)
                Debug.LogError("PlayerMovement: in PlayerView Active Ragdoll not found", playerView);

            _inputService.OnKeyboardMoveStart += HandleKeyboardMoveStart;
            _inputService.OnKeyboardMoveStop += HandleKeyboardMoveStop;
            _inputService.OnKeyboardJump += HandleKeyboardJump;
            _inputService.OnMouseLook += HandleMouseLook;
        }

        private bool CanControl => _isLocalPlayer && _playerView != null && _ragdoll != null;

        public void Tick()
        {
            if (!CanControl)
                return;

            if (_ragdoll.IsDead)
            {
                _ragdoll.SetMoveInput(Vector3.zero);
                return;
            }

            _isGrounded = _ragdoll.IsGrounded;
            HandleLandingLock();

            Vector3 worldMove = Quaternion.Euler(0f, _ragdoll.Yaw, 0f) * _moveDirection;
            _ragdoll.SetMoveInput(_isMoving ? worldMove * Constants.PlayerSettings.MoveSpeed : Vector3.zero);

            PlayMoveAnimation(_isMoving ? _moveDirection : Vector3.zero);

            _playerView.playerAnimator.SetFloat(MoveY, _ragdoll.Velocity.y);
            _playerView.playerAnimator.SetBool(IsGrounded, _isGrounded);
        }

        #region Jump

        private void HandleKeyboardJump()
        {
            if (!CanControl || _ragdoll.IsDead || !_isGrounded || !_canJump)
                return;

            if (_ragdoll.Jump())
                _playerView.networkAnimator.SetTrigger(JumpTrigger);
        }

        private void HandleLandingLock()
        {
            bool landedThisFrame = _isGrounded && !_wasGroundedLastFrame;

            if (landedThisFrame)
            {
                _canJump = false;
                _jumpLockTimer = _landingLockDuration;
            }

            if (_jumpLockTimer > 0f)
            {
                _jumpLockTimer -= Time.deltaTime;
                _canJump = _jumpLockTimer <= 0f;
            }

            _wasGroundedLastFrame = _isGrounded;
        }

        #endregion

        private void PlayMoveAnimation(Vector3 moveDirection)
        {
            float currentX = _playerView.playerAnimator.GetFloat(MoveX);
            float currentY = _playerView.playerAnimator.GetFloat(MoveZ);

            float smoothX = Mathf.Lerp(currentX, moveDirection.x, Time.deltaTime * 10f);
            float smoothY = Mathf.Lerp(currentY, moveDirection.z, Time.deltaTime * 10f);

            _playerView.playerAnimator.SetFloat(MoveX, smoothX);
            _playerView.playerAnimator.SetFloat(MoveZ, smoothY);
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
            if (!CanControl)
                return;

            Vector2 targetLook = context.GetLookDelta() * Constants.PlayerSettings.MouseSensitivity;

            _currentLook = Vector2.SmoothDamp(_currentLook, targetLook, ref _lookVelocity, 0.05f);

            _xRotation -= _currentLook.y * Time.deltaTime;
            _xRotation = Mathf.Clamp(
                _xRotation,
                Constants.PlayerSettings.MinXRotation,
                Constants.PlayerSettings.MaxXRotation);

            _playerView.playerCamera.transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);

            _ragdoll.Yaw += _currentLook.x * Time.deltaTime;
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
            _isLocalPlayer = false;
            _playerView = null;
            _ragdoll = null;

            _currentLook = Vector2.zero;
            _lookVelocity = Vector2.zero;
        }
    }
}