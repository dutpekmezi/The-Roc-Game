using Game.Installers;
using UnityEngine;
using Utils.Logger;
using Utils.LogicTimer;
using Utils.Pools;
using Utils.ObjectFlowAnimator;

namespace Game.Systems
{
    public class Collectable : MonoBehaviour
    {
        [SerializeField] protected ParticleSystem collectParticle;
        [SerializeField] protected CollectableConfig collectableConfig;

        private bool isCollected = false;

        private CollectableSystem collectableSystem;

        private const int DefaultPoolCapacity = 25;
        private const int DefaultPoolPreload = 1;
        private Pool particlePool;

        public CollectableConfig CollectableConfig => collectableConfig;

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

                collectableSystem.Collect(this);

                FlyGold();
            }
        }

        private void FlyGold()
        {
            if (particlePool == null)
            {
                InitializePool(DefaultPoolPreload, DefaultPoolCapacity, collectParticle);
            }

            var instance = Pools.Instance.Spawn(collectParticle, transform.position, Quaternion.identity, GameInstaller.Instance.GameObjectsParent);
            Pools.Instance.Despawn(instance.gameObject, instance.main.duration);


            var playerTransform = PlayerSystem.Instance.GetPlayerTransform();
            Vector2 playerScreenPos = Camera.main.WorldToScreenPoint(playerTransform.position);
            Vector2 endScreenPos = playerScreenPos;

            if (GameCanvas.Instance != null)
            {
                endScreenPos = RectTransformUtility.WorldToScreenPoint(Camera.main, GameInstaller.Instance.CollectableFlyDestination.position);
            }

            UIFlowAnimator.Instance.AddNewDestinationAction(
                startScreenPos: Camera.main.WorldToScreenPoint(transform.position),
                endScreenPos: endScreenPos,
                sprite: collectableConfig != null ? collectableConfig.Icon : null,
                parent: GameInstaller.Instance.Canvas.transform as RectTransform,
                particleCount: 1
            );
        }

        private void InitializePool(int preload, int capacity, ParticleSystem collectParticle)
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
