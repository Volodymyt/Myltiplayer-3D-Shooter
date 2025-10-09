using System;
using Services;
using UnityEngine;
using Zenject;

namespace Gameplay
{
    public class PlayerMovement : IDisposable, ITickable
    {
        private readonly InputService _inputService;

        private bool _isMoving = false;
        private bool _isLocalPlayer;
        private Vector3 _moveDirection;
        private Rigidbody _playerRigidbody;
        private float _moveSpeed = 5f;

        public PlayerMovement(InputService inputService)
        {
            _inputService = inputService;
        }

        public void Construct(Rigidbody playerRigidbody, bool isLocalPlayer)
        {
            _playerRigidbody = playerRigidbody;
            _isLocalPlayer = isLocalPlayer;

            _inputService.OnKeyboardMoveStart += HandleKeyboardMoveStart;
            _inputService.OnKeyboardMoveStop += HandleKeyboardMoveStop;
        }

        public void Tick()
        {
            if (_isMoving && _playerRigidbody != null && _isLocalPlayer)
            {
                Vector3 move = _moveDirection * _moveSpeed * Time.deltaTime;
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
        }

        public void Dispose()
        {
            if (_inputService != null)
            {
                _inputService.OnKeyboardMoveStart -= HandleKeyboardMoveStart;
                _inputService.OnKeyboardMoveStop -= HandleKeyboardMoveStop;
            }
            
            _isMoving = false;
            _moveDirection = Vector3.zero;
            _playerRigidbody = null;
        }
    }
}