using Game.Installers;
using System;
using UnityEngine;
using Utils.Logger;
using Utils.LogicTimer;
using Utils.Pools;
using Utils.ObjectFlowAnimator;

namespace Game.Systems
{
    public class Collectable : MonoBehaviour
    {
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
                InitializePool(DefaultPoolPreload, DefaultPoolCapacity, collectableSystem.CollectableSettings.collectParticle);
            }

            var instance = Pools.Instance.Spawn(collectableSystem.CollectableSettings.collectParticle, transform.position, Quaternion.identity, GameInstaller.Instance.GameObjectsParent);
            Pools.Instance.Despawn(instance.gameObject, instance.main.duration);


            Func<Vector3> endScreenPosProvider = () =>
            {
                if (GameCanvas.Instance != null)
                {
                    return GetScreenPoint(GameInstaller.Instance.Canvas, GameInstaller.Instance.CollectableFlyDestination);
                }

                var playerTransform = PlayerSystem.Instance.GetPlayerTransform();
                return Camera.main.WorldToScreenPoint(playerTransform.position);
            };

            UIFlowAnimator.Instance.AddNewDestinationAction(
               startScreenPos: Camera.main.WorldToScreenPoint(transform.position),
               endScreenPosProvider: endScreenPosProvider,
               sprite: collectableConfig != null ? collectableConfig.Icon : null,
               parent: GameInstaller.Instance.Canvas.transform as RectTransform,
               particleCount: 1,
               startDelay: collectableSystem.CollectableSettings.flyGoldStartDelay
           );
        }

        private static Vector2 GetScreenPoint(Canvas canvas, RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return Vector2.zero;
            }

            Camera camera = null;

            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                camera = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
            }

            return RectTransformUtility.WorldToScreenPoint(camera, rectTransform.position);
        }

        private void InitializePool(int preload, int capacity, UnityEngine.ParticleSystem collectParticle)
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
