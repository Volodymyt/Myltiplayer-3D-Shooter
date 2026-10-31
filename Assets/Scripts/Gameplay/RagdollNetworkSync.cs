using Mirror;
using UnityEngine;

namespace Gameplay
{
    [RequireComponent(typeof(ActiveRagdoll))]
    public class RagdollNetworkSync : NetworkBehaviour
    {
        [SerializeField] private ActiveRagdoll ragdoll;
        [SerializeField] private float sendRate = 30f;
        [SerializeField] private float maxExtrapolation = 0.2f;

        [SerializeField] private float pushRange = 1.3f;
        [SerializeField] private float pushStrength = 1.2f;
        [SerializeField] private float pushSendRate = 15f;

        private readonly System.Collections.Generic.Dictionary<ActiveRagdoll, float> _pushCooldowns =
            new System.Collections.Generic.Dictionary<ActiveRagdoll, float>();

        private float _sendTimer;

        private bool _hasState;
        private Vector3 _pos;
        private Quaternion _rot;
        private Vector3 _vel;
        private float _yaw;
        private double _receivedAt;

        private void Awake()
        {
            if (ragdoll == null) ragdoll = GetComponent<ActiveRagdoll>();
        }

        public override void OnStartClient() => ragdoll.SetAuthority(isOwned);
        public override void OnStartAuthority() => ragdoll.SetAuthority(true);
        public override void OnStopAuthority() => ragdoll.SetAuthority(false);

        private void FixedUpdate()
        {
            if (ragdoll == null || ragdoll.IsDead) return;

            if (isOwned)
            {
                DetectPushes();

                _sendTimer += Time.fixedDeltaTime;
                if (_sendTimer < 1f / sendRate) return;
                _sendTimer = 0f;

                var hips = ragdoll.Hips;
                CmdSendState(hips.position, hips.rotation, hips.linearVelocity, ragdoll.Yaw);
            }
            else if (_hasState)
            {
                float dt = Mathf.Min((float)(NetworkTime.time - _receivedAt), maxExtrapolation);
                ragdoll.ApplyRemoteState(_pos + _vel * dt, _rot, _yaw);
            }
        }

        [Command(channel = Channels.Unreliable)]
        private void CmdSendState(Vector3 pos, Quaternion rot, Vector3 vel, float yaw)
        {
            if (isServerOnly) Store(pos, rot, vel, yaw);
            RpcState(pos, rot, vel, yaw);
        }

        [ClientRpc(channel = Channels.Unreliable, includeOwner = false)]
        private void RpcState(Vector3 pos, Quaternion rot, Vector3 vel, float yaw) => Store(pos, rot, vel, yaw);

        private void Store(Vector3 pos, Quaternion rot, Vector3 vel, float yaw)
        {
            _pos = pos;
            _rot = rot;
            _vel = vel;
            _yaw = yaw;
            _receivedAt = NetworkTime.time;
            _hasState = true;
        }

        private void DetectPushes()
        {
            Vector3 intent = ragdoll.MoveInput;
            if (intent.sqrMagnitude < 0.01f || ragdoll.Strength < 0.3f) return;

            Vector3 myPos = ragdoll.Hips.position;
            float dt = Time.fixedDeltaTime;

            foreach (var other in ActiveRagdoll.All)
            {
                if (other == null || other == ragdoll || other.IsDead || other.Hips == null) continue;

                if (_pushCooldowns.TryGetValue(other, out float cd) && cd > 0f)
                {
                    _pushCooldowns[other] = cd - dt;
                    continue;
                }

                Vector3 toOther = other.Hips.position - myPos;
                toOther.y = 0f;
                float dist = toOther.magnitude;
                if (dist > pushRange || dist < 0.001f) continue;

                Vector3 dir = toOther / dist;
                float approach = Vector3.Dot(intent, dir);
                if (approach <= 0.1f) continue;

                float closeness = Mathf.Lerp(1.5f, 1f, dist / pushRange);
                Vector3 push = dir * (approach * pushStrength * closeness);

                if (other.TryGetComponent(out NetworkIdentity target))
                    CmdPush(target.netId, push);

                _pushCooldowns[other] = 1f / Mathf.Max(1f, pushSendRate);
            }
        }

        [Command(channel = Channels.Unreliable)]
        private void CmdPush(uint targetNetId, Vector3 velocity)
        {
            if (!NetworkServer.spawned.TryGetValue(targetNetId, out NetworkIdentity target)) return;
            if (!target.TryGetComponent(out RagdollNetworkSync targetSync)) return;
            if (velocity.magnitude > 20f) return;

            if (targetSync.connectionToClient != null)
                targetSync.TargetPush(targetSync.connectionToClient, velocity);
        }

        [TargetRpc(channel = Channels.Unreliable)]
        private void TargetPush(NetworkConnectionToClient conn, Vector3 velocity) => ragdoll.ReceivePush(velocity);

        public void Kill(Vector3 impulse = default)
        {
            if (isServer) RpcDie(impulse);
            else if (isOwned) CmdDie(impulse);
        }

        [Command]
        private void CmdDie(Vector3 impulse) => RpcDie(impulse);

        [ClientRpc]
        private void RpcDie(Vector3 impulse) => ragdoll.Die(impulse);
    }
}
