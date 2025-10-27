using UnityEngine;

namespace Gameplay
{
    public class SpearFlight : MonoBehaviour
    {
        [SerializeField] private Rigidbody _rigidbody;

        private void FixedUpdate()
        {
            if (_rigidbody.linearVelocity.sqrMagnitude > 0.1f)
                transform.forward = Vector3.Lerp(transform.forward, _rigidbody.linearVelocity.normalized, Time.fixedDeltaTime * 10f);
        }
    }
}