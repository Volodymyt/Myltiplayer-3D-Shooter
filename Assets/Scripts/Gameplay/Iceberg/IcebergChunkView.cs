using UnityEngine;

namespace Gameplay.Iceberg
{
    [RequireComponent(typeof(Rigidbody), typeof(MeshFilter), typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]
    public class IcebergChunkView : MonoBehaviour
    {
        public enum ChunkState { Solid, Warning, Falling, Gone }

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly Color WarningColor = new Color(0.35f, 0.55f, 0.8f);

        [SerializeField] private Rigidbody body;
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshCollider meshCollider;
        [SerializeField] private MeshRenderer meshRenderer;

        [SerializeField] private float shakeStrength = 0.06f;
        [SerializeField] private float maxTilt = 35f;
        [SerializeField] private float tiltSpeed = 25f;
        [SerializeField] private float sinkAcceleration = 1.5f;
        [SerializeField] private float driftSpeed = 0.4f;
        [SerializeField] private float goneDepth = 8f;

        public int Index { get; private set; }
        public ChunkState State { get; private set; } = ChunkState.Solid;

        private MaterialPropertyBlock _block;
        private Color _baseColor;
        private Vector3 _startPosition;
        private Quaternion _startRotation;
        private Vector3 _outward;
        private float _warningDuration;
        private float _timer;

        private void Reset()
        {
            body = GetComponent<Rigidbody>();
            meshFilter = GetComponent<MeshFilter>();
            meshCollider = GetComponent<MeshCollider>();
            meshRenderer = GetComponent<MeshRenderer>();
        }

        public void Setup(int index, Mesh mesh, Mesh colliderMesh, Vector3 localCenter, Vector3 outward)
        {
            Index = index;
            name = $"Chunk {index}";

            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.None;

            transform.localPosition = localCenter;

            body.position = transform.position;
            body.rotation = transform.rotation;

            meshFilter.sharedMesh = mesh;
            meshCollider.sharedMesh = colliderMesh;

            Material material = meshRenderer.sharedMaterial;
            Color iceColor = material != null && material.HasProperty(BaseColorId)
                ? material.GetColor(BaseColorId)
                : Color.white;

            _baseColor = iceColor;
            _block = new MaterialPropertyBlock();

            _outward = outward;
            _startPosition = transform.position;
            _startRotation = transform.rotation;
        }

        public void PlayBreak(float warningDuration)
        {
            if (State != ChunkState.Solid) return;

            body.interpolation = RigidbodyInterpolation.Interpolate;

            State = ChunkState.Warning;
            _warningDuration = warningDuration;
            _timer = 0f;
        }

        public void HideImmediately()
        {
            State = ChunkState.Gone;
            gameObject.SetActive(false);
        }

        private void FixedUpdate()
        {
            if (State == ChunkState.Warning)
                UpdateWarning();
            else if (State == ChunkState.Falling)
                UpdateFalling();
        }

        private void UpdateWarning()
        {
            _timer += Time.fixedDeltaTime;
            float progress = Mathf.Clamp01(_timer / _warningDuration);

            Vector3 shake = Random.insideUnitSphere * shakeStrength * progress;
            body.MovePosition(_startPosition + shake);

            SetColor(Color.Lerp(_baseColor, WarningColor, progress));

            if (progress >= 1f)
            {
                State = ChunkState.Falling;
                _timer = 0f;
            }
        }

        private void UpdateFalling()
        {
            _timer += Time.fixedDeltaTime;

            Vector3 tiltAxis = Vector3.Cross(Vector3.up, _outward);
            float angle = Mathf.Min(maxTilt, _timer * tiltSpeed);
            body.MoveRotation(Quaternion.AngleAxis(angle, tiltAxis) * _startRotation);

            float sink = 0.5f * sinkAcceleration * _timer * _timer;
            Vector3 drift = _outward * (driftSpeed * _timer);
            body.MovePosition(_startPosition + drift + Vector3.down * sink);

            if (sink > goneDepth)
            {
                State = ChunkState.Gone;
                gameObject.SetActive(false);
            }
        }

        private void SetColor(Color color)
        {
            _block.SetColor(BaseColorId, color);
            meshRenderer.SetPropertyBlock(_block);
        }
    }
}