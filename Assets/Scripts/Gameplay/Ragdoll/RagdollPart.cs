using UnityEngine;

namespace Gameplay
{
    public class RagdollPart : MonoBehaviour
    {
        public ActiveRagdoll Owner { get; private set; }
        public int Index { get; private set; }
        public Rigidbody Body { get; private set; }
        public bool IsVisual { get; private set; }

        public void Init(ActiveRagdoll owner, int index, Rigidbody body, bool isVisual = false)
        {
            Owner = owner;
            Index = index;
            Body = body;
            IsVisual = isVisual;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (Owner != null)
                Owner.OnPartCollision(this, collision);
        }
    }
}