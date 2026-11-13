using System;
using UnityEngine;

namespace Gameplay.Iceberg
{
    [Serializable]
    public class IcebergShapeSettings
    {
        [Range(1, 128)] public int resolution = 128;
        [Min(1f)] public float size = 30f;
        [Min(0f)] public float heightScale = 2f;
        [Min(0.1f)] public float hillSize = 15f;
        [Range(1, 6)] public int octaves = 4;
        [Range(0f, 1f)] public float persistence = 0.4f;
        [Min(1f)] public float radius = 11f;
        [Min(0f)] public float edgeWobble = 3f;
        [Min(0.1f)] public float edgeWobbleSize = 8f;
        [Min(0.01f)] public float cliffWidth = 1f;
        public float iceHeight = 1.5f;
        [Min(0f)] public float seaDepth = 3f;
        [Range(2, 60)] public int chunkCount = 20;
        [Range(0f, 60f)] public float maxWalkableSlope = 17f;
    }
}