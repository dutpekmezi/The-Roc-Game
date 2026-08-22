using UnityEngine;

namespace GameLift.ObjectFlowAnimator
{
    [CreateAssetMenu(fileName = "DestinationAction", menuName = "Game Lift/UIFlowAnimator/Destination Action", order = 1)]
    public class DestinationActionData : ScriptableObject
    {
        public float speed;
        public Vector2 speedRandX;
        public Vector2 speedRandY;

        public Vector3 scale;

        public float spawnDelayFactor;

        public bool bounceBack;
    }
}
