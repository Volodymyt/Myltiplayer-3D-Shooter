using Mirror;
using UnityEngine;

namespace Gameplay
{
    public class HealthSystem
    {
        public void TakeDamage(PlayerView playerView, int damage)
        {
            if (!NetworkServer.active || playerView == null || playerView.Health <= 0) return;

            int newHealth = Mathf.Max(0, playerView.Health - damage);
            playerView.ChangeHealth(newHealth);

            if (newHealth <= 0)
                Die();
        }

        private void Die()
        {
            if (!NetworkServer.active) return;

            Debug.Log("Die");
        }
    }
}