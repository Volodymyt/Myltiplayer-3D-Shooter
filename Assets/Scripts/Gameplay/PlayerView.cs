using System;
using Gameplay.Iceberg;
using Mirror;
using UnityEngine;

namespace Gameplay
{
    public class PlayerView : NetworkBehaviour
    {
        public static event Action<PlayerView> LocalPlayerStarted;
        public event Action<SpearView> SpearSpawned;
        public event Action<int> HealthChanged;

        public Camera playerCamera;
        public Animator playerAnimator;
        public ActiveRagdoll activeRagdoll;
        public NetworkAnimator networkAnimator;
        public Transform spearThrowPoint;

        [SyncVar(hook = nameof(OnHealthChanged))]
        private int _health;
        
        public int Health => _health;

        private void Start()
        {
            if (!isLocalPlayer)
                playerCamera.enabled = false;
            
            if (!IcebergView.IsReady)
            {
                activeRagdoll.SetFrozen(true);
                IcebergView.Ready += HandleIcebergReady;
            }
        }

        public override void OnStartServer()
        {
            ChangeHealth(Constants.PlayerSettings.MaxHealth);
        }

        [Server]
        public void ChangeHealth(int newHealth)
        {
            _health = newHealth;
        }
        
        private void OnHealthChanged(int oldHealth, int newHealth)
        {
            HealthChanged?.Invoke(newHealth);
        }
        
        private void HandleIcebergReady()
        {
            IcebergView.Ready -= HandleIcebergReady;
            activeRagdoll.SetFrozen(false);
        }

        private void OnDestroy() => IcebergView.Ready -= HandleIcebergReady;

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            LocalPlayerStarted?.Invoke(this);
        }

        public static PlayerView FromCollider(Collider collider)
        {
            if (collider == null) return null;

            var part = collider.GetComponentInParent<RagdollPart>();
            if (part != null && part.Owner != null)
                return part.Owner.GetComponent<PlayerView>();

            return collider.GetComponentInParent<PlayerView>();
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