using System;
using Mirror;
using Services;
using UnityEngine;

namespace Gameplay.Iceberg
{
    public class IcebergMediator : IDisposable
    {
        private readonly IcebergBuilder _builder;
        private readonly IcebergMelt _melt;
        private readonly GenericFactory _genericFactory;

        private readonly IAssetProviderService _assetProviderService;

        private GameObject _icebergPrefab;
        private IcebergView _view;
        private IcebergData _data;
        private IcebergChunkView[] _chunks = Array.Empty<IcebergChunkView>();

        public IcebergMediator(
            IcebergBuilder builder,
            IcebergMelt melt,
            GenericFactory genericFactory,
            IAssetProviderService assetProviderService)
        {
            _builder = builder;
            _melt = melt;
            _genericFactory = genericFactory;
            _assetProviderService = assetProviderService;
        }

        public void Construct()
        {
            _icebergPrefab = _assetProviderService.LoadAssetFromResources<GameObject>(Constants.IcebergPath);

            if (_icebergPrefab != null)
                NetworkClient.RegisterPrefab(_icebergPrefab);

            IcebergView.ServerStarted += HandleServerStarted;
            IcebergView.ClientStarted += HandleClientStarted;
            _melt.ChunkBreakRequested += HandleChunkBreakRequested;
        }

        public void SpawnIceberg()
        {
            if (!NetworkServer.active) return;

            var view = _genericFactory.Create<IcebergView>(Constants.IcebergPath);
            NetworkServer.Spawn(view.gameObject);
        }

        private void HandleServerStarted(IcebergView view)
        {
            _view = view;

            int seed = UnityEngine.Random.Range(1, int.MaxValue);

            _data = _builder.Build(view.Shape, seed);
            view.ServerSetup(seed, _data.SeedPoints, _data.Checksum);

            _melt.Construct(_data, seed); 
        }

        private void HandleChunkBreakRequested(int index) => _view.ServerBreakChunk(index);

        private void HandleClientStarted(IcebergView view)
        {
            _view = view;

            if (_data == null)
                _data = _builder.Build(view.Shape, view.Seed, view.GetSeedPoints());

            if (_data.Checksum != view.Checksum)
            {
                Debug.LogError($"IcebergMediator: iceberg did not match the host " +
                               $"(checksum {_data.Checksum}, host has {view.Checksum})");
            }

            CreateChunks();

            foreach (int index in view.BrokenChunks)
            {
                if (IsValidChunk(index))
                    _chunks[index].HideImmediately();
            }

            view.ChunkBreakReceived += HandleChunkBreakReceived;
            IcebergView.NotifyReady();
        }

        private void CreateChunks()
        {
            _chunks = new IcebergChunkView[_data.ChunkCount];

            for (int i = 0; i < _data.ChunkCount; i++)
            {
                if (_data.ChunkMeshes[i] == null) continue;

                var chunk = _genericFactory.Create<IcebergChunkView>(Constants.IcebergChunkPath, _view.ChunksRoot);

                Vector3 center = _data.ChunkCenters[i];
                Vector3 outward = _view.ChunksRoot.TransformDirection(new Vector3(center.x, 0f, center.z).normalized);

                chunk.Setup(i, _data.ChunkMeshes[i], center, outward);

                _chunks[i] = chunk;
            }
        }

        private void HandleChunkBreakReceived(int index)
        {
            if (IsValidChunk(index))
                _chunks[index].PlayBreak(Constants.IcebergMeltSettings.WarningTime);
        }

        private bool IsValidChunk(int index) =>
            index >= 0 && index < _chunks.Length && _chunks[index] != null;

        public void Dispose()
        {
            IcebergView.ServerStarted -= HandleServerStarted;
            IcebergView.ClientStarted -= HandleClientStarted;
            _melt.ChunkBreakRequested -= HandleChunkBreakRequested;

            if (_view != null)
                _view.ChunkBreakReceived -= HandleChunkBreakReceived;

            _melt.Dispose();
            IcebergView.ResetReady();

            if (_icebergPrefab != null)
                NetworkClient.UnregisterPrefab(_icebergPrefab);

            _icebergPrefab = null;
            _data?.Destroy();

            _data = null;
            _view = null;
            _chunks = Array.Empty<IcebergChunkView>();
        }
    }
}
