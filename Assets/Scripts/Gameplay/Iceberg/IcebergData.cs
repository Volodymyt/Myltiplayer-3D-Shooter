using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Iceberg
{
    public class IcebergData
    {
        public List<Vector2> SeedPoints { get; set; }  
        public Mesh[] ChunkMeshes { get; set; }            
        public Mesh[] ChunkColliderMeshes { get; set; }
        public Vector3[] ChunkCenters { get; set; }
        public HashSet<int>[] ChunkNeighbors { get; set; } 

        public int Checksum { get; set; }                   

        public float SteepestSlope { get; set; }        
        public Mesh SteepMesh { get; set; }            

        public int ChunkCount => ChunkMeshes.Length;

        public void Destroy()
        {
            foreach (Mesh mesh in ChunkMeshes)
                DestroyMesh(mesh);

            if (ChunkColliderMeshes != null)
                foreach (Mesh mesh in ChunkColliderMeshes)
                    DestroyMesh(mesh);

            DestroyMesh(SteepMesh);
        }

        private static void DestroyMesh(Mesh mesh)
        {
            if (mesh == null) return;

            if (Application.isPlaying)
                Object.Destroy(mesh);
            else
                Object.DestroyImmediate(mesh);
        }
    }
}