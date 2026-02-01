using UnityEngine;
using Utils.Currency;
using Utils.Logger;
using Utils.LogicTimer;
using Utils.Pools;

namespace Game.Systems
{
    public class Collectable : MonoBehaviour
    {
        [SerializeField] protected ParticleSystem collectParticle;

        private bool isCollected = false;

        private CollectableSystem collectableSystem;

        private Pool particlePool;

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

                FlyGold();

                collectableSystem.DespawnCollectable(this);
            }
        }

        private void FlyGold()
        {
            var instance = Pools.Instance.Spawn(collectParticle, transform.position, Quaternion.identity);
            Pools.Instance.Despawn(instance.gameObject, instance.main.duration);

            //ResolveServices.CurrencyService.ModifyCurrency("gold", 1, false);
            //RewardManager.Instance.Gold += 1;

            var player = PlayerSystem.Instance.GetPlayerEntity();
            Vector2 playerScreenPos = Camera.main.WorldToScreenPoint(player.transform.position);

            UIAnimationService.AddNewDestinationAction(
                startScreenPos: Camera.main.WorldToScreenPoint(transform.position),
                endScreenPos: playerScreenPos,
                sprite: ResolveServices.CurrencyService.GetCurrencyConfig("gold").currencySprite,
                parent: ResolveServices.WindowService.Canvas.transform as RectTransform,
                particleCount: 1
            );
        }

        private void InitializePool(int preload, int capacity, Collectable collectablePrefab)
        {
            if (collectParticle == null)
            {
                GameLogger.LogWarning("Collectable cannot initialize pool without a collect particle prefab.");
                return;
            }

            if (capacity > 0)
            {
                particlePool = Pools.Instance.InitializePool(collectParticle.gameObject, preload, capacity);
            }
            else
            {
                particlePool = Pools.Instance.InitializePool(collectParticle.gameObject, preload);
            }
        }
    }
}