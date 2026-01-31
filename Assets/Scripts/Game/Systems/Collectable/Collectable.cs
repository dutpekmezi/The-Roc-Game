using UnityEngine;
using Utils.Currency;
using Utils.LogicTimer;
using Utils.Pools;

namespace Game.Systems
{
    public class Collectable : MonoBehaviour
    {
        //[SerializeField] private CurrencyConfig

        private bool isCollected = false;

        private CollectableSystem collectableSystem;

        public void Init(CollectableSystem collectableSystem)
        {
            isCollected = false;
            this.collectableSystem = collectableSystem;
        }

        public void Tick()
        {
            if (!isCollected)
            {
                transform.position += Vector3.left * ObstacleSystem.Instance.ObstacleSettings.moveSpeed * LogicTimer.FixedDelta;

                if (transform.position.x <= ObstacleSystem.Instance.ObstacleSettings.destroyX)
                {
                    collectableSystem.DespawnCollectable(this);
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision != null && collision.GetComponent<PlayerController>())
            {
                isCollected = true;
                collectableSystem.DespawnCollectable(this);
            }
        }
    }
}