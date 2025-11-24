using System;
using Mirror;
using UnityEngine;

namespace Gameplay
{
    public class SpearView : NetworkBehaviour
    {
        [SerializeField] private Rigidbody spearRigidbody;
        [SerializeField] private Transform emptyParent;
        [SerializeField] private Collider spearCollider;

        private Transform _spearContainer;
        private uint _ownerNetId;
        private bool _isStuck;

        public void Init(uint ownerNetId, Transform spearContainer)
        {
            _ownerNetId = ownerNetId;
            _spearContainer = spearContainer;
        }

        private void FixedUpdate()
        {
            if (_isStuck) return;

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
            if (_isStuck) return;

            PlayerView player = other.transform.root.GetComponent<PlayerView>();
            if (player != null)
            {
                if (player.netId == _ownerNetId)
                {
                    return;
                }
            }

            StuckIntoSurface();
        }

        private void StuckIntoSurface()
        {
            _isStuck = true;

            spearCollider.isTrigger = false;
            spearRigidbody.linearVelocity = Vector3.zero;
            spearRigidbody.isKinematic = true;
            emptyParent.transform.SetParent(_spearContainer, true);
            gameObject.layer = LayerMask.NameToLayer("Ground");
        }
    }
}