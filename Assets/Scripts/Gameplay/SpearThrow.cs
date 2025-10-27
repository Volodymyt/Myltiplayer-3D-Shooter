using System;
using Services;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace Gameplay
{
    public class SpearThrow : IDisposable, ITickable
    {
        private readonly InputService _inputService;
        private readonly SpearFactory _spearFactory;

        private readonly LayerMask _playerLayer = LayerMask.GetMask("Player");

        private Transform _throwPoint;
        private Transform _currentSpear;
        private Camera _playerCamera;
        private LineRenderer _trajectoryLine;

        private const int TrajectoryPoints = 50;
        private const float TimeStep = 0.1f;
        private const float HalfAccelerationFactor = 0.5f;

        private float _respawnTimer;
        private bool _isThrowing;
        private bool _isWaitingForRespawn;

        public SpearThrow(InputService inputService, SpearFactory spearFactory)
        {
            _inputService = inputService;
            _spearFactory = spearFactory;
        }

        public void Construct(Transform throwPoint, Camera playerCamera)
        {
            _throwPoint = throwPoint;
            _playerCamera = playerCamera;

            SpawnNewSpear();
            CreateTrajectoryLine();

            _inputService.OnMouseLeftButtonDown += HandleSpearThrowStart;
            _inputService.OnMouseLeftButtonUp += HandleSpearThrowStop;
        }

        public void Tick()
        {
            HandleRespawnTimer();

            if (_isThrowing && _currentSpear != null)
                DrawTrajectory();
            else
                ClearTrajectory();
        }

        private void HandleRespawnTimer()
        {
            if (!_isWaitingForRespawn) return;

            _respawnTimer -= Time.deltaTime;
            if (_respawnTimer <= 0f)
            {
                SpawnNewSpear();
                _isWaitingForRespawn = false;
            }
        }

        #region Trajectory

        private void CreateTrajectoryLine()
        {
            _trajectoryLine = new GameObject("SpearTrajectory").AddComponent<LineRenderer>();
            _trajectoryLine.startWidth = 0.1f;
            _trajectoryLine.endWidth = 0.035f;
            _trajectoryLine.material = new Material(Shader.Find("Sprites/Default"));

            var color = new Color(1f, 1f, 0f, 0.3f);
            _trajectoryLine.startColor = color;
            _trajectoryLine.endColor = color;
            _trajectoryLine.positionCount = 0;
        }

        private void DrawTrajectory()
        {
            Vector3 arcedDir = CalculateThrowDirection();

            Vector3 startPos = _throwPoint.position;
            Vector3 startVelocity = arcedDir * Constants.PlayerSettings.ThrowForce;

            Vector3[] points = new Vector3[TrajectoryPoints];
            for (int i = 0; i < TrajectoryPoints; i++)
            {
                float t = i * TimeStep;
                points[i] = startPos + startVelocity * t + HalfAccelerationFactor * Physics.gravity * (t * t);
            }

            _trajectoryLine.positionCount = TrajectoryPoints;
            _trajectoryLine.SetPositions(points);
        }

        private void ClearTrajectory()
        {
            if (_trajectoryLine != null)
                _trajectoryLine.positionCount = 0;
        }

        #endregion

        #region Throw Handling

        private void HandleSpearThrowStart()
        {
            if (_isWaitingForRespawn || _currentSpear == null)
                return;

            _isThrowing = true;
        }

        private void HandleSpearThrowStop()
        {
            if (!_isThrowing || _currentSpear == null)
                return;

            _isThrowing = false;
            ThrowSpear();
        }

        private void ThrowSpear()
        {
            _currentSpear.SetParent(null);

            Rigidbody rigidbody = _currentSpear.GetComponentInChildren<Rigidbody>();
            if (rigidbody == null)
            {
                Debug.LogError("Spear prefab is missing a Rigidbody component!");
                return;
            }

            rigidbody.isKinematic = false;

            Vector3 arcedDir = CalculateThrowDirection();

            _currentSpear.forward = arcedDir;
            rigidbody.linearVelocity = arcedDir * Constants.PlayerSettings.ThrowForce;

            _currentSpear = null;
            _isWaitingForRespawn = true;
            _respawnTimer = Constants.PlayerSettings.RespawnDelay;
        }

        private void SpawnNewSpear()
        {
            _currentSpear = _spearFactory.CreateSpear();
            _currentSpear.SetParent(_throwPoint);
            _currentSpear.localPosition = Vector3.zero;
            _currentSpear.localRotation = Quaternion.identity;
        }

        #endregion

        private Vector3 CalculateThrowDirection()
        {
            Ray ray = _playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            Vector3 direction =
                Physics.Raycast(ray, out RaycastHit hit, 100f, ~_playerLayer, QueryTriggerInteraction.Ignore)
                    ? (hit.point - _throwPoint.position).normalized
                    : _playerCamera.transform.forward;

            return Quaternion.AngleAxis(-Constants.PlayerSettings.ThrowAngle, _playerCamera.transform.right) *
                   direction;
        }

        public void Dispose()
        {
            if (_inputService != null)
            {
                _inputService.OnMouseLeftButtonDown -= HandleSpearThrowStart;
                _inputService.OnMouseLeftButtonUp -= HandleSpearThrowStop;
            }

            if (_trajectoryLine != null)
                Object.Destroy(_trajectoryLine.gameObject);
        }
    }
}