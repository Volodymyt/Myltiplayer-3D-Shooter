using System.Collections.Generic;
using UnityEngine;

namespace Gameplay
{
    public class GroundChecker : MonoBehaviour
    {
        [SerializeField] private LayerMask groundLayer;

        private readonly HashSet<Collider> _contacts = new HashSet<Collider>();

        public bool IsGrounded { get; private set; }

        private void Reset() => GetComponent<Collider>().isTrigger = true;

        private void OnTriggerEnter(Collider other)
        {
            if (IsGround(other))
            {
                _contacts.Add(other);
                IsGrounded = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!_contacts.Remove(other)) return;

            IsGrounded = _contacts.Count > 0;
        }

        private void FixedUpdate()
        {
            if (_contacts.Count == 0) return;

            _contacts.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);
            IsGrounded = _contacts.Count > 0;
        }

        private void OnDisable()
        {
            _contacts.Clear();
            IsGrounded = false;
        }

        private bool IsGround(Collider other) =>
            (groundLayer.value & (1 << other.gameObject.layer)) != 0;
    }
}
