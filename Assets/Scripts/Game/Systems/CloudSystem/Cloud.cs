using UnityEngine;
using Utils.LogicTimer;

namespace Game.Systems
{
    public class Cloud : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        private CloudGeneratorSystem cloudGeneratorSystem;
        private float moveSpeed;
        private bool isActive;

        public void Init(
            CloudGeneratorSystem generatorSystem,
            Sprite sprite,
            float speed,
            float scale,
            int sortingOrder,
            Color tint)
        {
            cloudGeneratorSystem = generatorSystem;
            moveSpeed = speed;
            isActive = true;

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = sprite;
                spriteRenderer.sortingOrder = sortingOrder;
                spriteRenderer.color = tint;
            }

            transform.localScale = Vector3.one * scale;
        }

        public void Tick()
        {
            if (!isActive || cloudGeneratorSystem?.CloudsConfig == null)
            {
                return;
            }

            transform.position += Vector3.left * moveSpeed * LogicTimer.FixedDelta;

            if (transform.position.x <= cloudGeneratorSystem.CloudsConfig.destroyX)
            {
                isActive = false;
                cloudGeneratorSystem.DespawnCloud(this);
            }
        }

        public void OnDespawn()
        {
            isActive = false;
            cloudGeneratorSystem = null;
        }
    }
}
