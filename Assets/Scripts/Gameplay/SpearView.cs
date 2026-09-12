using Mirror;
using UnityEngine;

namespace Gameplay
{
    public class SpearView : NetworkBehaviour
    {
        [SerializeField] private Rigidbody spearRigidbody;
        [SerializeField] private Transform emptyParent;
        [SerializeField] private Collider spearCollider;

        [SyncVar(hook = nameof(OnOwnerChanged))]
        private uint _ownerNetId;

        [SyncVar(hook = nameof(OnHeldChanged))]
        private bool _isHeld = true;

        private Transform _spearContainer;
        private bool _isStuck;

        public void ServerInit(uint ownerNetId) => _ownerNetId = ownerNetId;

        public override void OnStartClient()
        {
            base.OnStartClient();
            _spearContainer = GameplayMediator.SpearContainer;
            if (_isHeld) AttachToOwnerHand();
        }

        private void OnOwnerChanged(uint oldId, uint newId) => AttachToOwnerHand();

        private void OnHeldChanged(bool oldValue, bool newValue)
        {
            if (newValue) AttachToOwnerHand();
            else emptyParent.transform.SetParent(null, true);
        }

        private void AttachToOwnerHand()
        {
            if (!_isHeld) return;
            if (!NetworkClient.spawned.TryGetValue(_ownerNetId, out NetworkIdentity ownerIdentity)) return;

            var playerView = ownerIdentity.GetComponent<PlayerView>();
            if (playerView == null || playerView.spearThrowPoint == null) return;

            emptyParent.transform.SetParent(playerView.spearThrowPoint, true);
            emptyParent.transform.localPosition = Vector3.zero;
            emptyParent.transform.localRotation = Quaternion.Euler(180, 0, 0);
        }

        [Command]
        public void CmdThrow(Vector3 velocity)
        {
            if (!_isHeld) return;
            _isHeld = false;
            ApplyThrowPhysics(velocity);
            RpcApplyThrow(velocity);
        }

        [ClientRpc]
        private void RpcApplyThrow(Vector3 velocity) => ApplyThrowPhysics(velocity);

        private void ApplyThrowPhysics(Vector3 velocity)
        {
            spearRigidbody.isKinematic = false;
            emptyParent.transform.forward = -velocity.normalized;
            spearRigidbody.linearVelocity = velocity;

            spearCollider.enabled = true;
            spearCollider.isTrigger = true;
        }

        private void FixedUpdate()
        {
            if (_isStuck || _isHeld) return;

            if (spearRigidbody.linearVelocity.sqrMagnitude > 0.1f)
            {
                transform.forward = Vector3.Lerp(
                    transform.forward,
                    -spearRigidbody.linearVelocity.normalized,
                    Time.fixedDeltaTime * 10f);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isServer || _isStuck) return;

            PlayerView player = other.transform.root.GetComponent<PlayerView>();
            if (player != null && player.netId == _ownerNetId) return;

            _isStuck = true;
            RpcStuckIntoSurface();
        }

        [ClientRpc]
        private void RpcStuckIntoSurface()
        {
            _isStuck = true;
            spearCollider.isTrigger = false;
            spearRigidbody.linearVelocity = Vector3.zero;
            spearRigidbody.isKinematic = true;

            if (emptyParent != null && _spearContainer != null)
                emptyParent.transform.SetParent(_spearContainer, true);
            else
                Debug.LogWarning("SpearView: emptyParent or _spearContainer is not assigned!");

            gameObject.layer = LayerMask.NameToLayer("Ground");
        }
    }
}