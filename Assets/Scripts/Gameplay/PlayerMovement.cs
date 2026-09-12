using System;
using Mirror;
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
        private PlayerView _playerView;

        private readonly LayerMask _groundLayer = LayerMask.GetMask("Ground");
        private readonly float _landingLockDuration = 0.85f;
        private bool _canJump = true;
        private float _jumpLockTimer = 0f;
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
            _isLocalPlayer = isLocalPlayer;

            _inputService.OnKeyboardMoveStart += HandleKeyboardMoveStart;
            _inputService.OnKeyboardMoveStop += HandleKeyboardMoveStop;
            _inputService.OnKeyboardJump += HandleKeyboardJump;
            _inputService.OnMouseLook += HandleMouseLook;
        }

        public void Tick()
        {
            if (!_isLocalPlayer || _playerView.playerRigidbody == null)
                return;

            _isGrounded = CheckGround(Constants.PlayerSettings.RigidbodyGroundCheckDistance);

            HandleLandingLock();

            if (_isMoving)
            {
                Vector3 move = _playerView.playerRigidbody.transform.TransformDirection(_moveDirection)
                               * (Constants.PlayerSettings.MoveSpeed * Time.deltaTime);
                _playerView.playerRigidbody.MovePosition(_playerView.playerRigidbody.position + move);

                PlayMoveAnimation(_moveDirection);
            }
            else if (!_isMoving && (_playerView.playerAnimator.GetFloat(MoveX) != 0 || _playerView.playerAnimator.GetFloat(MoveZ) != 0))
            {
                PlayMoveAnimation(new Vector3(0, 0, 0));
            }

            _playerView.playerAnimator.SetFloat(MoveY, _playerView.playerRigidbody.linearVelocity.y);
            _playerView.playerAnimator.SetBool(IsGrounded, CheckGround(Constants.PlayerSettings.AnimatorGroundCheckDistance));
        }

        #region Jump

        private void HandleKeyboardJump()
        {
            if (!_isLocalPlayer || !_isGrounded || !_canJump)
                return;

            _playerView.playerRigidbody.AddForce(
                Vector3.up * Constants.PlayerSettings.JumpForce,
                ForceMode.Impulse);
            
            _playerView.networkAnimator.SetTrigger(JumpTrigger); 
        }

        private bool CheckGround(float distance)
        {
            Vector3 origin = _playerView.playerRigidbody.position + Vector3.up * 0.05f;

            Vector3 sphereCenter = origin + Vector3.down * distance;

            bool grounded = Physics.CheckSphere(
                sphereCenter,
                Constants.PlayerSettings.GroundCheckDistance,
                _groundLayer
            );

            return grounded;
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
            if (!_isLocalPlayer)
                return;

            Vector2 targetLook = context.GetLookDelta() * Constants.PlayerSettings.MouseSensitivity;

            _currentLook = Vector2.SmoothDamp(_currentLook, targetLook, ref _lookVelocity, 0.05f);

            _xRotation -= _currentLook.y * Time.deltaTime;
            _xRotation = Mathf.Clamp(
                _xRotation,
                Constants.PlayerSettings.MinXRotation,
                Constants.PlayerSettings.MaxXRotation);

            _playerView.playerCamera.transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);

            _playerView.playerRigidbody.transform.Rotate(Vector3.up * _currentLook.x * Time.deltaTime);
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

            _currentLook = Vector2.zero;
            _lookVelocity = Vector2.zero;
        }
    }
}