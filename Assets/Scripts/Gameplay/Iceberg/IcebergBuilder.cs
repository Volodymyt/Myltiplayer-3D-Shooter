using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Iceberg
{
    public class IcebergBuilder
    {
        private IcebergShapeSettings _shape;
        private int _pointsPerSide;
        private float _step;
        private float _half;
        private float _offsetX;
        private float _offsetZ;

        private Vector3[] _vertices;
        private float[] _edgeMask;
        private bool[] _connected;

        public IcebergData Build(IcebergShapeSettings shape, int seed, List<Vector2> seedPoints = null)
        {
            _shape = shape;
            _pointsPerSide = shape.resolution + 1; 
            _step = shape.size / shape.resolution;  
            _half = shape.size / 2f;              

            var random = new System.Random(seed);
            _offsetX = random.Next(-1000, 1000);
            _offsetZ = random.Next(-1000, 1000);

            CreatePoints();
            int[] triangles = CreateTriangles();

            _connected = FindConnectedIce();
            triangles = KeepConnectedIce(triangles);

            Vector3[] normals = CalculateNormals(triangles);

            var data = new IcebergData();
            CheckSlopes(triangles, data);
            data.SeedPoints = seedPoints ?? CreateSeeds(random);
            BuildChunks(triangles, normals, data);

            return data;
        }

        private void CreatePoints()
        {
            _vertices = new Vector3[_pointsPerSide * _pointsPerSide];
            _edgeMask = new float[_pointsPerSide * _pointsPerSide];

            for (int z = 0; z < _pointsPerSide; z++)
            {
                for (int x = 0; x < _pointsPerSide; x++)
                {
                    int i = z * _pointsPerSide + x;

                    float worldX = x * _step - _half;
                    float worldZ = z * _step - _half;

                    float iceTop = _shape.iceHeight + SampleNoise(worldX, worldZ) * _shape.heightScale;

                    float mask = EdgeMask(worldX, worldZ);
                    _edgeMask[i] = mask;

                    float height = Mathf.Lerp(-_shape.seaDepth, iceTop, mask);
                    _vertices[i] = new Vector3(worldX, height, worldZ);
                }
            }
        }

        private int[] CreateTriangles()
        {
            int resolution = _shape.resolution;
            int[] triangles = new int[resolution * resolution * 6]; 
            int t = 0;

            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    int bl = z * _pointsPerSide + x;   
                    int br = bl + 1;                   
                    int tl = bl + _pointsPerSide;      
                    int tr = tl + 1;                   

                    triangles[t++] = bl;
                    triangles[t++] = tl;
                    triangles[t++] = tr;

                    triangles[t++] = bl;
                    triangles[t++] = tr;
                    triangles[t++] = br;
                }
            }

            return triangles;
        }

        private Vector3[] CalculateNormals(int[] triangles)
        {
            var normals = new Vector3[_vertices.Length];

            for (int t = 0; t < triangles.Length; t += 3)
            {
                int a = triangles[t];
                int b = triangles[t + 1];
                int c = triangles[t + 2];

                Vector3 faceNormal = Vector3.Cross(_vertices[b] - _vertices[a], _vertices[c] - _vertices[a]);

                normals[a] += faceNormal;
                normals[b] += faceNormal;
                normals[c] += faceNormal;
            }

            for (int i = 0; i < normals.Length; i++)
                normals[i] = normals[i].normalized; 

            return normals;
        }

        private float SampleNoise(float worldX, float worldZ)
        {
            float total = 0f;
            float amplitude = 1f;
            float frequency = 1f;
            float maxTotal = 0f;

            for (int o = 0; o < _shape.octaves; o++)
            {
                float nx = (worldX + _offsetX) / _shape.hillSize * frequency;
                float nz = (worldZ + _offsetZ) / _shape.hillSize * frequency;

                total += Mathf.PerlinNoise(nx, nz) * amplitude;
                maxTotal += amplitude;

                amplitude *= _shape.persistence;   
                frequency *= 2f;                 
            }

            return total / maxTotal;
        }

        private float EdgeMask(float worldX, float worldZ)
        {
            float distance = new Vector2(worldX, worldZ).magnitude;

            float wobble = Mathf.PerlinNoise(
                (worldX + _offsetZ) / _shape.edgeWobbleSize,
                (worldZ + _offsetX) / _shape.edgeWobbleSize);

            float edge = _shape.radius + (wobble - 0.5f) * 2f * _shape.edgeWobble;

            float t = Mathf.InverseLerp(edge - _shape.cliffWidth, edge, distance);
            return 1f - Mathf.SmoothStep(0f, 1f, t);
        }

        private bool[] FindConnectedIce()
        {
            var connected = new bool[_vertices.Length];
            var queue = new Queue<int>();

            int middle = _pointsPerSide / 2;
            int center = middle * _pointsPerSide + middle;

            if (_edgeMask[center] <= 0f)
            {
                return connected;
            }

            connected[center] = true;
            queue.Enqueue(center);

            while (queue.Count > 0)
            {
                int i = queue.Dequeue();
                int x = i % _pointsPerSide;
                int z = i / _pointsPerSide;

                Visit(x + 1, z);
                Visit(x - 1, z);
                Visit(x, z + 1);
                Visit(x, z - 1);
            }

            return connected;

            void Visit(int nx, int nz)
            {
                if (nx < 0 || nz < 0 || nx >= _pointsPerSide || nz >= _pointsPerSide) return;

                int n = nz * _pointsPerSide + nx;
                if (connected[n] || _edgeMask[n] <= 0f) return;

                connected[n] = true;
                queue.Enqueue(n);
            }
        }

        private int[] KeepConnectedIce(int[] triangles)
        {
            var kept = new List<int>();

            for (int t = 0; t < triangles.Length; t += 3)
            {
                int a = triangles[t];
                int b = triangles[t + 1];
                int c = triangles[t + 2];

                if (!_connected[a] && !_connected[b] && !_connected[c])
                    continue;

                kept.Add(a);
                kept.Add(b);
                kept.Add(c);
            }

            return kept.ToArray();
        }

        private bool IsOnMainIce(Vector2 point)
        {
            int x = Mathf.RoundToInt((point.x + _half) / _step);
            int z = Mathf.RoundToInt((point.y + _half) / _step);

            if (x < 0 || z < 0 || x >= _pointsPerSide || z >= _pointsPerSide) return false;

            return _connected[z * _pointsPerSide + x];
        }

        private void CheckSlopes(int[] triangles, IcebergData data)
        {
            float steepest = 0f;
            var steepTriangles = new List<int>();

            for (int t = 0; t < triangles.Length; t += 3)
            {
                if (_edgeMask[triangles[t]] < 0.999f ||
                    _edgeMask[triangles[t + 1]] < 0.999f ||
                    _edgeMask[triangles[t + 2]] < 0.999f)
                    continue;

                Vector3 a = _vertices[triangles[t]];
                Vector3 b = _vertices[triangles[t + 1]];
                Vector3 c = _vertices[triangles[t + 2]];

                float angle = Vector3.Angle(Vector3.Cross(b - a, c - a), Vector3.up);
                steepest = Mathf.Max(steepest, angle);

                if (angle > _shape.maxWalkableSlope)
                {
                    steepTriangles.Add(triangles[t]);
                    steepTriangles.Add(triangles[t + 1]);
                    steepTriangles.Add(triangles[t + 2]);
                }
            }

            data.SteepestSlope = Mathf.Round(steepest * 10f) / 10f;

            var steepMesh = new Mesh { name = "Iceberg steep areas" };
            steepMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            steepMesh.vertices = _vertices;
            steepMesh.triangles = steepTriangles.ToArray();
            steepMesh.RecalculateNormals();
            data.SteepMesh = steepMesh;
        }

        private List<Vector2> CreateSeeds(System.Random random)
        {
            var seeds = new List<Vector2> { Vector2.zero };

            float iceArea = Mathf.PI * _shape.radius * _shape.radius;
            float minDistance = Mathf.Sqrt(iceArea / _shape.chunkCount) * 0.7f;
            float range = _shape.radius + _shape.edgeWobble;

            for (int attempt = 0; attempt < 5000 && seeds.Count < _shape.chunkCount; attempt++)
            {
                var point = new Vector2(
                    (float)(random.NextDouble() * 2 - 1) * range,
                    (float)(random.NextDouble() * 2 - 1) * range);

                if (EdgeMask(point.x, point.y) < 1f) continue;
                if (!IsOnMainIce(point)) continue;

                bool tooClose = false;
                foreach (Vector2 other in seeds)
                {
                    if (Vector2.Distance(point, other) < minDistance)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose) continue;

                seeds.Add(point);
            }

            return seeds;
        }

        private static int NearestSeed(List<Vector2> seeds, Vector2 point)
        {
            int nearest = 0;
            float nearestDistance = float.MaxValue;

            for (int s = 0; s < seeds.Count; s++)
            {
                float distance = (seeds[s] - point).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = s;
                }
            }

            return nearest;
        }

        private void BuildChunks(int[] triangles, Vector3[] normals, IcebergData data)
        {
            int count = data.SeedPoints.Count;

            var chunkTriangles = new List<int>[count];
            for (int s = 0; s < count; s++)
                chunkTriangles[s] = new List<int>();

            var edgeOwner = new Dictionary<(int, int), int>();

            for (int t = 0; t < triangles.Length; t += 3)
            {
                int a = triangles[t];
                int b = triangles[t + 1];
                int c = triangles[t + 2];

                var center = new Vector2(
                    (_vertices[a].x + _vertices[b].x + _vertices[c].x) / 3f,
                    (_vertices[a].z + _vertices[b].z + _vertices[c].z) / 3f);

                int chunk = NearestSeed(data.SeedPoints, center);
                chunkTriangles[chunk].Add(a);
                chunkTriangles[chunk].Add(b);
                chunkTriangles[chunk].Add(c);

                edgeOwner[(a, b)] = chunk;
                edgeOwner[(b, c)] = chunk;
                edgeOwner[(c, a)] = chunk;
            }

            data.ChunkNeighbors = new HashSet<int>[count];
            for (int s = 0; s < count; s++)
                data.ChunkNeighbors[s] = new HashSet<int>();

            foreach (var pair in edgeOwner)
            {
                (int a, int b) = pair.Key;
                int chunk = pair.Value;

                if (edgeOwner.TryGetValue((b, a), out int other) && other != chunk)
                    data.ChunkNeighbors[chunk].Add(other);
            }

            unchecked 
            {
                int checksum = count;
                for (int s = 0; s < count; s++)
                    checksum = checksum * 31 + chunkTriangles[s].Count;
                data.Checksum = checksum;
            }

            data.ChunkMeshes = new Mesh[count];
            data.ChunkColliderMeshes = new Mesh[count];
            data.ChunkCenters = new Vector3[count];

            for (int s = 0; s < count; s++)
            {
                if (chunkTriangles[s].Count == 0) continue;

                data.ChunkMeshes[s] = MakeChunkMesh(s, chunkTriangles[s], normals,
                    out data.ChunkCenters[s], out Mesh colliderMesh);
                data.ChunkColliderMeshes[s] = colliderMesh;
            }
        }

        private Mesh MakeChunkMesh(int index, List<int> triangles, Vector3[] normals, out Vector3 center, out Mesh collider)
        {
            float bottom = -_shape.seaDepth;
            var usedPoints = new HashSet<int>(triangles);

            Vector3 middle = Vector3.zero;
            foreach (int p in usedPoints)
                middle += _vertices[p];
            middle /= usedPoints.Count;
            middle.y = (middle.y + bottom) / 2f;

            var vertices = new List<Vector3>();
            var chunkNormals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var chunkTriangles = new List<int>();

            int AddPoint(Vector3 position, Vector3 normal, Vector2 uv)
            {
                vertices.Add(position - middle);
                chunkNormals.Add(normal);
                uvs.Add(uv);
                return vertices.Count - 1;
            }

            var topIndex = new Dictionary<int, int>();
            foreach (int p in usedPoints)
                topIndex[p] = AddPoint(_vertices[p], normals[p], TopUv(_vertices[p]));

            foreach (int p in triangles)
                chunkTriangles.Add(topIndex[p]);

            var colliderMesh = new Mesh { name = $"Chunk {index} collider" };
            colliderMesh.SetVertices(vertices.GetRange(0, usedPoints.Count));
            colliderMesh.SetTriangles(new List<int>(chunkTriangles), 0);
            colliderMesh.RecalculateBounds();
            collider = colliderMesh;

            var edges = new HashSet<(int, int)>();
            for (int t = 0; t < triangles.Count; t += 3)
            {
                edges.Add((triangles[t], triangles[t + 1]));
                edges.Add((triangles[t + 1], triangles[t + 2]));
                edges.Add((triangles[t + 2], triangles[t]));
            }

            foreach ((int p, int q) in edges)
            {
                if (edges.Contains((q, p))) continue;

                Vector3 pTop = _vertices[p];
                Vector3 qTop = _vertices[q];
                if (pTop.y <= bottom + 0.01f && qTop.y <= bottom + 0.01f) continue;

                Vector3 pBottom = new Vector3(pTop.x, bottom, pTop.z);
                Vector3 qBottom = new Vector3(qTop.x, bottom, qTop.z);
                Vector3 wallNormal = Vector3.Cross(qTop - pTop, Vector3.up).normalized;

                int a = AddPoint(pTop, wallNormal, WallUv(pTop, wallNormal));
                int b = AddPoint(qTop, wallNormal, WallUv(qTop, wallNormal));
                int c = AddPoint(qBottom, wallNormal, WallUv(qBottom, wallNormal));
                int d = AddPoint(pBottom, wallNormal, WallUv(pBottom, wallNormal));

                chunkTriangles.Add(a); chunkTriangles.Add(c); chunkTriangles.Add(b);
                chunkTriangles.Add(a); chunkTriangles.Add(d); chunkTriangles.Add(c);
            }

            var bottomIndex = new Dictionary<int, int>();
            foreach (int p in usedPoints)
            {
                Vector3 bottomPoint = new Vector3(_vertices[p].x, bottom, _vertices[p].z);
                bottomIndex[p] = AddPoint(bottomPoint, Vector3.down, TopUv(bottomPoint));
            }

            for (int t = 0; t < triangles.Count; t += 3)
            {
                chunkTriangles.Add(bottomIndex[triangles[t]]);
                chunkTriangles.Add(bottomIndex[triangles[t + 2]]);
                chunkTriangles.Add(bottomIndex[triangles[t + 1]]);
            }

            center = middle;

            var mesh = new Mesh { name = $"Chunk {index}" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(chunkNormals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(chunkTriangles, 0);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Vector2 TopUv(Vector3 point) => new Vector2(point.x, point.z);

        private static Vector2 WallUv(Vector3 point, Vector3 wallNormal) =>
            Mathf.Abs(wallNormal.x) > Mathf.Abs(wallNormal.z)
                ? new Vector2(point.z, point.y)
                : new Vector2(point.x, point.y);
    }
}