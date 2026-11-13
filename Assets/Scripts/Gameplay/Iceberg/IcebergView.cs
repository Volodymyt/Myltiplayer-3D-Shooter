using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace Gameplay.Iceberg
{
    public class IcebergView : NetworkBehaviour
    {
        public static event Action<IcebergView> ServerStarted; 
        public static event Action<IcebergView> ClientStarted;
        public event Action<int> ChunkBreakReceived;

        [SerializeField] private IcebergShapeSettings shape = new IcebergShapeSettings();


        [SyncVar] private int _seed;
        [SyncVar] private int _checksum;
        private readonly SyncList<Vector2> _seedPoints = new SyncList<Vector2>();
        private readonly SyncList<int> _brokenChunks = new SyncList<int>();

        public IcebergShapeSettings Shape => shape;
        public Transform ChunksRoot => transform;

        public int Seed => _seed;
        public int Checksum => _checksum;
        public IEnumerable<int> BrokenChunks => _brokenChunks;
        public List<Vector2> GetSeedPoints() => new List<Vector2>(_seedPoints);

        public override void OnStartServer()
        {
            base.OnStartServer();
            ServerStarted?.Invoke(this);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            ClientStarted?.Invoke(this);
        }

        [Server]
        public void ServerSetup(int seed, List<Vector2> seedPoints, int checksum)
        {
            _seed = seed;
            _checksum = checksum;

            _seedPoints.Clear();
            foreach (Vector2 point in seedPoints)
                _seedPoints.Add(point);

            _brokenChunks.Clear();
        }

        [Server]
        public void ServerBreakChunk(int index)
        {
            _brokenChunks.Add(index); 
            RpcBreakChunk(index);
        }

        [ClientRpc]
        private void RpcBreakChunk(int index) => ChunkBreakReceived?.Invoke(index);
    }
}