using System;
using Services;
using UnityEngine;
using Zenject;

namespace Gameplay
{
    public class SpearThrow : IDisposable, ITickable
    {
        private readonly InputService _inputService;
        private readonly SpearFactory _spearFactory;

        private Transform _throwPoint;
        private Transform _currentSpear;

        private float _throwForce = 20f;
        private float _respawnDelay = 2f;     // delay before new spear appears
        private float _respawnTimer;
        private bool _isThrowing;
        private bool _isWaitingForRespawn;

        public SpearThrow(InputService inputService, SpearFactory spearFactory)
        {
            _inputService = inputService;
            _spearFactory = spearFactory;
        }

        public void Construct(Transform throwPoint)
        {
            _throwPoint = throwPoint;
            _currentSpear = _spearFactory.CreateSpear();
            _currentSpear.SetParent(_throwPoint);
            _currentSpear.localPosition = Vector3.zero;
            _currentSpear.localRotation = Quaternion.identity;

            _inputService.OnMouseLeftButtonDown += HandleSpearThrowStart;
            _inputService.OnMouseLeftButtonUp += HandleSpearThrowStop;
        }

        public void Tick()
        {
            // Handle spear respawn after throw
            if (_isWaitingForRespawn)
            {
                _respawnTimer -= Time.deltaTime;
                if (_respawnTimer <= 0f)
                {
                    SpawnNewSpear();
                    _isWaitingForRespawn = false;
                }
            }

            // Optional: if you want a "charge" mechanic when holding
            if (_isThrowing && _currentSpear != null)
            {
                // Here you could add a charge timer, animation, etc.
            }
        }

        private void HandleSpearThrowStart()
        {
            if (_currentSpear == null || _isWaitingForRespawn)
                return;

            _isThrowing = true;
            Debug.Log("Spear Throw Started");
        }

        private void HandleSpearThrowStop()
        {
            if (!_isThrowing || _currentSpear == null)
                return;

            _isThrowing = false;
            Debug.Log("Spear Thrown");
            ReleaseSpear();
        }

        private void ReleaseSpear()
        {
            _currentSpear.SetParent(null);

            Rigidbody rb = _currentSpear.GetComponentInChildren<Rigidbody>();
            if (rb == null)
            {
                Debug.LogError("Spear prefab is missing a Rigidbody component!");
                return;
            }

            rb.isKinematic = false;
            rb.linearVelocity = -_throwPoint.forward * _throwForce;

            _currentSpear = null;
            _isWaitingForRespawn = true;
            _respawnTimer = _respawnDelay;
        }

        private void SpawnNewSpear()
        {
            _currentSpear = _spearFactory.CreateSpear();
            _currentSpear.SetParent(_throwPoint);
            _currentSpear.localPosition = Vector3.zero;
            _currentSpear.localRotation = Quaternion.identity;
        }

        public void Dispose()
        {
            if (_inputService != null)
            {
                _inputService.OnMouseLeftButtonDown -= HandleSpearThrowStart;
                _inputService.OnMouseLeftButtonUp -= HandleSpearThrowStop;
            }
        }
    }
}