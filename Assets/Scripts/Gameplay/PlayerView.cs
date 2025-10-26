using Mirror;
using UnityEngine;

namespace Gameplay
{
    public class PlayerView : NetworkBehaviour
    {
        public Camera playerCamera;
        public Animator playerAnimator;
        public Rigidbody playerRigidbody;
        public Collider playerCollider;
        public NetworkIdentity playerNetworkIdentity;
        public Transform spearThrowTransform;

        private void Start()
        {
            if (!isLocalPlayer)
                playerCamera.enabled = false;
        }
    }
}