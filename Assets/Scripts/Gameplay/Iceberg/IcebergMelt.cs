using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Gameplay.Iceberg
{
    public class IcebergMelt : ITickable, IDisposable
    {
        public event Action<int> ChunkBreakRequested;

        private IcebergData _data;
        private bool[] _broken;     
        private float[] _scores;   
        private float _nextBreakTime;
        private bool _isActive;

        public void Construct(IcebergData data, int seed)
        {
            _data = data;
            _broken = new bool[data.ChunkCount];
            _scores = new float[data.ChunkCount];

            var random = new System.Random(seed);

            for (int i = 0; i < data.ChunkCount; i++)
            {
                if (data.ChunkMeshes[i] == null)
                {
                    _broken[i] = true; 
                    continue;
                }

                Vector3 center = data.ChunkCenters[i];
                float distance = new Vector2(center.x, center.z).magnitude;
                _scores[i] = distance + (float)random.NextDouble() * Constants.IcebergMeltSettings.Randomness;
            }

            _nextBreakTime = Time.time + Constants.IcebergMeltSettings.StartDelay;
            _isActive = true;
        }

        public void Tick()
        {
            if (!_isActive || Time.time < _nextBreakTime)
                return;

            _nextBreakTime = Time.time + Constants.IcebergMeltSettings.TimeBetweenBreaks;
            BreakNext();
        }

        private void BreakNext()
        {
            int next = -1;

            for (int i = 1; i < _broken.Length; i++)
            {
                if (_broken[i]) continue;

                if (next == -1 || _scores[i] > _scores[next])
                    next = i;
            }

            if (next == -1)
            {
                _isActive = false;
                Debug.Log("IcebergMelt: залишився тільки центральний шматок.");
                return;
            }

            Break(next);
            BreakCutOffChunks();
        }

        private void Break(int index)
        {
            _broken[index] = true;
            ChunkBreakRequested?.Invoke(index);
        }

        private void BreakCutOffChunks()
        {
            if (_broken[0]) return;

            var reached = new bool[_broken.Length];
            var queue = new Queue<int>();

            reached[0] = true;
            queue.Enqueue(0);

            while (queue.Count > 0)
            {
                int i = queue.Dequeue();

                foreach (int n in _data.ChunkNeighbors[i])
                {
                    if (reached[n] || _broken[n]) continue;

                    reached[n] = true;
                    queue.Enqueue(n);
                }
            }

            for (int i = 1; i < _broken.Length; i++)
            {
                if (!_broken[i] && !reached[i])
                    Break(i);
            }
        }

        public void Dispose()
        {
            _isActive = false;
            _data = null;
            _broken = null;
            _scores = null;
        }
    }
}