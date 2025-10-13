using Mirror;
using UnityEngine;

namespace Gameplay
{
    public class Player : NetworkBehaviour
    {
        [SerializeField] private Camera playerCamera;

        private void Start()
        {
            if (!isLocalPlayer)
                playerCamera.enabled = false;
        }
    }
}