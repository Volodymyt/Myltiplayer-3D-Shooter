using System;
using Mirror;
using UnityEngine;

namespace Gameplay
{
    public class PlayerView : NetworkBehaviour
    {
        public static event Action<PlayerView> LocalPlayerStarted;
        public event Action<SpearView> SpearSpawned;

        public Camera playerCamera;
        public Animator playerAnimator;
        public Rigidbody playerRigidbody;
        public NetworkAnimator networkAnimator;
        public Transform spearThrowPoint;
        public GroundChecker groundChecker;

        private void Start()
        {
            if (!isLocalPlayer)
                playerCamera.enabled = false;
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            LocalPlayerStarted?.Invoke(this);
        }

        [Command]
        public void CmdSpawnSpear()
        {
            var prefab = Resources.Load<GameObject>(Constants.SpearPath);
            if (prefab == null)
            {
                Debug.LogError($"Spear prefab not found at path: {Constants.SpearPath}");
                return;
            }

            GameObject spearInstance = Instantiate(prefab, spearThrowPoint.position, spearThrowPoint.rotation);
            var spearView = spearInstance.GetComponentInChildren<SpearView>();
            spearView.ServerInit(netId);
            NetworkServer.Spawn(spearInstance, connectionToClient); 

            TargetOnSpearSpawned(connectionToClient, spearView.netId);
        }

        [TargetRpc]
        private void TargetOnSpearSpawned(NetworkConnectionToClient target, uint spearNetId)
        {
            if (!NetworkClient.spawned.TryGetValue(spearNetId, out NetworkIdentity spearIdentity))
            {
                Debug.LogWarning($"TargetOnSpearSpawned: spear with netId {spearNetId} not found in NetworkClient.spawned");
                return;
            }

            var spearView = spearIdentity.GetComponentInChildren<SpearView>();
            if (spearView == null)
            {
                Debug.LogWarning("TargetOnSpearSpawned: SpearView component not found on spawned spear");
                return;
            }

            SpearSpawned?.Invoke(spearView);
        }
    }
}