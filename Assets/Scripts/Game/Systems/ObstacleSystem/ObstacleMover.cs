using Game.Systems;
using UnityEngine;
using Utils.LogicTimer;

namespace Game.Systems
{
    public class ObstacleMover : MonoBehaviour
    {
        private ObstacleSettings obstacleSettings;
        private ObstacleSystem obstacleSystem;

        private bool isDestroyed = false;

        public void Init(ObstacleSystem obstacleSystem)
        {
            this.obstacleSettings = obstacleSystem.ObstacleSettings;
            this.obstacleSystem = obstacleSystem;
            isDestroyed = false;
        }
        public void Tick()
        {
            if (!isDestroyed)
            {
                transform.position += Vector3.left * obstacleSettings.moveSpeed * LogicTimer.FixedDelta;

                if (transform.position.x <= obstacleSettings.destroyX)
                {
                    obstacleSystem.DespawnObstacle(this);
                    isDestroyed = true;
                }
            }
        }
    }
}