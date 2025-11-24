using System;
using Mirror;
using Services;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace Gameplay
{
    public class SpearThrow : IDisposable, ITickable
    {
        private static readonly int Throw = Animator.StringToHash("Throw");

        private readonly InputService _inputService;
        private readonly GenericFactory _genericFactory;

        private readonly LayerMask _playerLayer = LayerMask.GetMask("Player");
        private Animator _playerAnimator;
        private Transform _throwPoint;
        private Transform _currentSpear;
        private Transform _spearContainer;
        private Camera _playerCamera;
        private LineRenderer _trajectoryLine;

        private const int TrajectoryPoints = 50;
        private const int ThrowAnimationLayer = 1;
        private const float TimeStep = 0.1f;
        private const float HalfAccelerationFactor = 0.5f;
        private const float ChargePausePoint = 0.25f;

        private uint _playerNetworkID;
        private float _spearRespawnTimer;
        private bool _isThrowing;
        private bool _isWaitingForSpearRespawn;
        private bool _isAnimationCharging;

        public SpearThrow(InputService inputService, GenericFactory genericFactory)
        {
            _inputService = inputService;
            _genericFactory = genericFactory;
        }

        public void Construct(PlayerView playerView, Transform spearContainer)
        {
            _throwPoint = playerView.spearThrowPoint;
            _playerCamera = playerView.playerCamera;
            _playerAnimator = playerView.playerAnimator;
            _playerNetworkID = playerView.GetComponent<NetworkIdentity>().netId;
            _spearContainer = spearContainer;

            SpawnNewSpear();
            CreateTrajectoryLine();

            _inputService.OnMouseLeftButtonDown += HandleSpearThrowStart;
            _inputService.OnMouseLeftButtonUp += HandleSpearThrowStop;
        }

        public void Tick()
        {
            HandleRespawnTimer();
            HandleChargingAnimation();

            if (_isThrowing && _currentSpear != null)
                DrawTrajectory();
            else
                ClearTrajectory();
        }

        private void HandleRespawnTimer()
        {
            if (!_isWaitingForSpearRespawn) return;

            _spearRespawnTimer -= Time.deltaTime;
            if (_spearRespawnTimer <= 0f)
            {
                SpawnNewSpear();
                _isWaitingForSpearRespawn = false;
            }
        }

        private void HandleChargingAnimation()
        {
            if (!_isThrowing) return;

            var stateInfo = _playerAnimator.GetCurrentAnimatorStateInfo(ThrowAnimationLayer);

            if (_isAnimationCharging)
            {
                if (stateInfo.normalizedTime >= ChargePausePoint)
                {
                    _playerAnimator.Play(Throw, ThrowAnimationLayer, ChargePausePoint);
                    _playerAnimator.Update(0f);
                }
            }
            else
            {
                if (stateInfo.normalizedTime >= 0.36f)
                {
                    ThrowSpear();
                    _isThrowing = false;
                }
            }
        }

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

        private void HandleSpearThrowStart()
        {
            if (_isWaitingForSpearRespawn || _currentSpear == null)
                return;

            _playerAnimator.Play(Throw, ThrowAnimationLayer, 0f);
            _playerAnimator.Update(0f);
            _isThrowing = true;
            _isAnimationCharging = true;
        }

        private void HandleSpearThrowStop()
        {
            if (!_isThrowing)
                return;

            _isAnimationCharging = false;
            _playerAnimator.speed = 1f;
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

            _currentSpear.forward = -arcedDir;
            rigidbody.linearVelocity = arcedDir * Constants.PlayerSettings.ThrowForce;

            Collider spearCollider = _currentSpear.GetComponentInChildren<Collider>();
            spearCollider.enabled = true;
            spearCollider.isTrigger = true;

            _currentSpear = null;
            _isWaitingForSpearRespawn = true;
            _spearRespawnTimer = Constants.PlayerSettings.RespawnDelay;
        }

        private void SpawnNewSpear()
        {
            _currentSpear = _genericFactory.Create<Transform>(Constants.SpearPath);
            _currentSpear.SetParent(_throwPoint);
            _currentSpear.localPosition = Vector3.zero;
            _currentSpear.transform.localRotation = Quaternion.Euler(180, 0, 0);
            
            var spearView = _currentSpear.GetComponentInChildren<SpearView>();
            spearView.Init(_playerNetworkID, _spearContainer);
        }

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