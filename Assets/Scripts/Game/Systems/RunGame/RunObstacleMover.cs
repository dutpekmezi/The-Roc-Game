using UnityEngine;
using Utils.LogicTimer;

namespace Game.Systems
{
    public class RunObstacleMover : MonoBehaviour
    {
        [SerializeField] private bool overrideSpeed = false;
        [SerializeField] private bool overridePosition = false;

        [SerializeField] private float _speed = 4f;
        [SerializeField] private float _yPosition = 0f;

        private RunObstacleSystem obstacleSystem;
        private float moveSpeed;
        private float destroyX;
        private bool isActive;

        public void Init(RunObstacleSystem system, float speed, float despawnX)
        {
            obstacleSystem = system;
            moveSpeed = overrideSpeed ? _speed : speed;
            destroyX = despawnX;
            isActive = true;
            if (overridePosition) transform.position = new Vector2(transform.position.x, _yPosition);
        }

        public void Tick()
        {
            if (!isActive)
            {
                return;
            }

            transform.position += Vector3.left * moveSpeed * LogicTimer.FixedDelta;

            if (transform.position.x <= destroyX)
            {
                isActive = false;
                obstacleSystem?.DespawnObstacle(this);
            }
        }

        public void OnDespawn()
        {
            isActive = false;
            obstacleSystem = null;
        }
    }
}
