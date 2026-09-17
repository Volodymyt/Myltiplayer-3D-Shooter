using System;
using Mirror;
using UnityEngine;

namespace Gameplay
{
    public class SpearView : NetworkBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Rigidbody spearRigidbody;
        [SerializeField] private Transform emptyParent;
        [SerializeField] private Collider spearCollider;
        [SerializeField] private Transform spearTip;

        [Header("Tuning")]
        [SerializeField] private float stickDepth = 0.25f;
        [SerializeField] private float visualSmoothTime = 0.12f;
        [SerializeField] private float maxVisualCorrection = 2f;
        [SerializeField] private float maxOriginDesync = 2f;   
        [SerializeField] private float maxThrowTimeRewind = 0.3f; 

        [SyncVar(hook = nameof(OnOwnerChanged))]
        private uint _ownerNetId;

        [SyncVar(hook = nameof(OnHeldChanged))]
        private bool _isHeld = true;

        private Vector3 _throwStartPos;
        private Vector3 _throwVelocity;
        private double _throwTime;

        private Vector3 _bodyLocalPos;
        private Quaternion _bodyLocalRot;
        private Vector3 _tipLocalPos;

        private Vector3 _visualOffset;
        private Vector3 _prevTipPos;

        private Transform _spearContainer;
        private bool _isFlying;
        private bool _isStuck;

        private readonly RaycastHit[] _hitBuffer = new RaycastHit[16];

        public Vector3 RootPosition => emptyParent.position;

        private void Awake()
        {
            _bodyLocalPos = emptyParent.InverseTransformPoint(spearRigidbody.transform.position);
            _bodyLocalRot = Quaternion.Inverse(emptyParent.rotation) * spearRigidbody.transform.rotation;
            _tipLocalPos = spearTip != null
                ? emptyParent.InverseTransformPoint(spearTip.position)
                : _bodyLocalPos;

            spearRigidbody.isKinematic = true;
            spearRigidbody.interpolation = RigidbodyInterpolation.None;
            spearRigidbody.useGravity = false;
        }

        public void ServerInit(uint ownerNetId) => _ownerNetId = ownerNetId;

        public override void OnStartClient()
        {
            base.OnStartClient();
            _spearContainer = GameplayMediator.SpearContainer;
            if (_isHeld) AttachToOwnerHand();
        }

        private void OnOwnerChanged(uint oldId, uint newId) => AttachToOwnerHand();

        private void OnHeldChanged(bool oldVal, bool newVal)
        {
            if (newVal) AttachToOwnerHand();
            else if (emptyParent.parent != null) emptyParent.SetParent(null, true);
        }

        private void AttachToOwnerHand()
        {
            if (!_isHeld || _isFlying || _isStuck) return;
            if (!NetworkClient.spawned.TryGetValue(_ownerNetId, out NetworkIdentity ownerIdentity)) return;

            var playerView = ownerIdentity.GetComponent<PlayerView>();
            if (playerView == null || playerView.spearThrowPoint == null) return;

            spearCollider.enabled = false;

            emptyParent.SetParent(playerView.spearThrowPoint, true);
            emptyParent.localPosition = Vector3.zero;
            emptyParent.localRotation = Quaternion.Euler(180, 0, 0);
        }

        #region Throw

        public double PredictThrow(Vector3 origin, Vector3 velocity)
        {
            double throwTime = NetworkTime.time;

            if (isOwned && !_isFlying && !_isStuck)
                BeginFlight(velocity, origin, throwTime);

            return throwTime;
        }

        [Command]
        public void CmdThrow(Vector3 clientOrigin, Vector3 velocity, double clientThrowTime)
        {
            if (!_isHeld) return;
            _isHeld = false;

            Vector3 serverOrigin = emptyParent.position;
            Vector3 startPos = Vector3.Distance(serverOrigin, clientOrigin) <= maxOriginDesync
                ? clientOrigin
                : serverOrigin;

            double now = NetworkTime.time;
            double throwTime = Math.Max(now - maxThrowTimeRewind, Math.Min(clientThrowTime, now));

            BeginFlight(velocity, startPos, throwTime);
            RpcBeginFlight(velocity, startPos, throwTime);
        }

        [ClientRpc]
        private void RpcBeginFlight(Vector3 velocity, Vector3 startPos, double throwTime) =>
            BeginFlight(velocity, startPos, throwTime);

        private void BeginFlight(Vector3 velocity, Vector3 startPos, double throwTime)
        {
            if (_isFlying && _throwTime == throwTime) return;

            if (emptyParent.parent != null)
                emptyParent.SetParent(null, true);

            _throwStartPos = startPos;
            _throwVelocity = velocity;
            _throwTime = throwTime;
            _isFlying = true;
            _isStuck = false;

            spearRigidbody.isKinematic = true;
            spearCollider.enabled = false;

            Vector3 predicted = EvaluatePosition(FlightTime());
            _visualOffset = Vector3.ClampMagnitude(emptyParent.position - predicted, maxVisualCorrection);

            ApplyFlightPose();
            _prevTipPos = TipPosition(emptyParent.position, emptyParent.rotation);
        }

        #endregion

        #region Flight

        private void FixedUpdate()
        {
            if (!_isFlying || _isStuck) return;

            float decay = visualSmoothTime <= 0f
                ? 1f
                : Mathf.Clamp01(Time.fixedDeltaTime / visualSmoothTime);
            
            _visualOffset = Vector3.Lerp(_visualOffset, Vector3.zero, decay);

            ApplyFlightPose();

            Vector3 tipNow = TipPosition(emptyParent.position, emptyParent.rotation);

            if (TryGetHit(_prevTipPos, tipNow, out RaycastHit hit))
            {
                Quaternion rootRot = emptyParent.rotation;
                Vector3 dir = (tipNow - _prevTipPos).normalized;
                Vector3 tipTarget = hit.point + dir * stickDepth;
                Vector3 rootPos = tipTarget - rootRot * _tipLocalPos;

                ApplyStuck(rootPos, rootRot);

                if (isServer)
                    RpcStuck(rootPos, rootRot);

                return;
            }

            _prevTipPos = tipNow;
        }

        private void ApplyFlightPose()
        {
            float t = FlightTime();

            Vector3 pos = EvaluatePosition(t) + _visualOffset;
            Vector3 velocity = _throwVelocity + Physics.gravity * t;

            Quaternion rot = velocity.sqrMagnitude > 0.01f
                ? Quaternion.LookRotation(-velocity.normalized)
                : emptyParent.rotation;

            emptyParent.SetPositionAndRotation(pos, rot);
        }

        private float FlightTime()
        {
            double elapsed = NetworkTime.time - _throwTime;
            return elapsed < 0d ? 0f : (float)elapsed;
        }

        private Vector3 EvaluatePosition(float t) =>
            _throwStartPos + _throwVelocity * t + 0.5f * Physics.gravity * (t * t);

        private Vector3 TipPosition(Vector3 rootPos, Quaternion rootRot) =>
            rootPos + rootRot * _tipLocalPos;

        private bool TryGetHit(Vector3 from, Vector3 to, out RaycastHit hit)
        {
            hit = default;

            Vector3 delta = to - from;
            float distance = delta.magnitude;
            if (distance < 0.0001f) return false;

            int count = Physics.RaycastNonAlloc(
                from,
                delta / distance,
                _hitBuffer, distance, 
                ~0,
                QueryTriggerInteraction.Ignore);

            float best = float.MaxValue;
            bool found = false;

            for (int i = 0; i < count; i++)
            {
                RaycastHit candidate = _hitBuffer[i];

                if (candidate.collider.transform.IsChildOf(emptyParent)) continue;

                var player = candidate.collider.transform.root.GetComponent<PlayerView>();
                if (player != null && player.netId == _ownerNetId) continue;

                if (candidate.distance < best)
                {
                    best = candidate.distance;
                    hit = candidate;
                    found = true;
                }
            }

            return found;
        }

        #endregion

        #region Stick

        [ClientRpc]
        private void RpcStuck(Vector3 rootPos, Quaternion rootRot) => ApplyStuck(rootPos, rootRot);

        private void ApplyStuck(Vector3 rootPos, Quaternion rootRot)
        {
            _isStuck = true;
            _isFlying = false;
            _visualOffset = Vector3.zero;

            spearRigidbody.isKinematic = true;

            emptyParent.SetPositionAndRotation(rootPos, rootRot);
            spearRigidbody.transform.SetPositionAndRotation(
                rootPos + rootRot * _bodyLocalPos,
                rootRot * _bodyLocalRot);

            spearCollider.enabled = true;
            spearCollider.isTrigger = false;

            if (_spearContainer != null)
                emptyParent.SetParent(_spearContainer, true);

            SetLayerRecursively(emptyParent.gameObject, LayerMask.NameToLayer("Ground"));

            Physics.SyncTransforms();
        }

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            if (layer < 0) return;

            go.layer = layer;
            for (int i = 0; i < go.transform.childCount; i++)
                SetLayerRecursively(go.transform.GetChild(i).gameObject, layer);
        }

        #endregion
    }
}