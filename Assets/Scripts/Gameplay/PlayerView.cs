using Mirror;
using UnityEngine;
using UnityEngine.Serialization;

namespace Gameplay
{
    public class PlayerView : NetworkBehaviour
    {
        public Camera playerCamera;
        public Animator playerAnimator;
        public Rigidbody playerRigidbody;
        public Collider playerCollider;
        public NetworkIdentity playerNetworkIdentity;
        public Transform spearThrowPoint;

        private void Start()
        {
            if (!isLocalPlayer)
                playerCamera.enabled = false;
        }
    }
}