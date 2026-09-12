using System;
using Services;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace Gameplay
{
    public class SpearThrow : IDisposable, ITickable
    {
        private static readonly int Throw = Animator.StringToHash("Throw");
        private static readonly int IsCharging = Animator.StringToHash("IsCharging");

        private readonly LayerMask _playerLayer = LayerMask.GetMask("Player");
        private readonly InputService _inputService;

        private Transform _currentSpear;
        private SpearView _currentSpearView;
        private LineRenderer _trajectoryLine;
        private PlayerView _playerView;

        private const int TrajectoryPoints = 50;
        private const int ThrowAnimationLayer = 1;
        private const float TimeStep = 0.1f;
        private const float HalfAccelerationFactor = 0.5f;

        private float _spearRespawnTimer;
        private bool _isThrowing;
        private bool _isWaitingForSpearRespawn;
        private bool _isAnimationCharging;

        public SpearThrow(InputService inputService)
        {
            _inputService = inputService;
        }

        public void Construct(PlayerView playerView)
        {
            _playerView = playerView;

            _playerView.SpearSpawned += HandleSpearSpawned;

            _playerView.CmdSpawnSpear();
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
                _playerView.CmdSpawnSpear();
                _isWaitingForSpearRespawn = false;
            }
        }

        private void HandleChargingAnimation()
        {
            if (!_isThrowing) return;

            var stateInfo =  _playerView.playerAnimator.GetCurrentAnimatorStateInfo(ThrowAnimationLayer);

            bool isInThrowState = stateInfo.shortNameHash == Throw;
            if (isInThrowState && stateInfo.normalizedTime >= 0.10f)
            {
                ThrowSpear();
                _isThrowing = false;
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

            Vector3 startPos = _playerView.spearThrowPoint.position;
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

            _playerView.playerAnimator.SetBool(IsCharging, true);
            _isThrowing = true;
        }

        private void HandleSpearThrowStop()
        {
            if (!_isThrowing) return;

            _playerView.playerAnimator.SetBool(IsCharging, false);
        }

        private void ThrowSpear()
        {
            if (_currentSpearView == null)
                return;

            Vector3 arcedDir = CalculateThrowDirection();
            Vector3 velocity = arcedDir * Constants.PlayerSettings.ThrowForce;

            _currentSpearView.CmdThrow(velocity);

            _currentSpear = null;
            _currentSpearView = null;
            _isWaitingForSpearRespawn = true;
            _spearRespawnTimer = Constants.PlayerSettings.RespawnDelay;
        }

        private void HandleSpearSpawned(SpearView spearView)
        {
            if (spearView == null) return;

            _currentSpear = spearView.transform;
            _currentSpearView = spearView;
        }

        private Vector3 CalculateThrowDirection()
        {
            Ray ray = _playerView.playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            Vector3 direction =
                Physics.Raycast(ray, out RaycastHit hit, 100f, ~_playerLayer, QueryTriggerInteraction.Ignore)
                    ? (hit.point - _playerView.spearThrowPoint.position).normalized
                    : _playerView.playerCamera.transform.forward;

            return Quaternion.AngleAxis(-Constants.PlayerSettings.ThrowAngle, _playerView.playerCamera.transform.right) *
                   direction;
        }

        public void Dispose()
        {
            if (_inputService != null)
            {
                _inputService.OnMouseLeftButtonDown -= HandleSpearThrowStart;
                _inputService.OnMouseLeftButtonUp -= HandleSpearThrowStop;
            }

            if (_playerView != null)
                _playerView.SpearSpawned -= HandleSpearSpawned;

            if (_trajectoryLine != null)
                Object.Destroy(_trajectoryLine.gameObject);

            _trajectoryLine = null;
            _playerView = null;
            _currentSpear = null;
            _currentSpearView = null;

            _isThrowing = false;
            _isWaitingForSpearRespawn = false;
            _spearRespawnTimer = 0f;
        }
    }
}