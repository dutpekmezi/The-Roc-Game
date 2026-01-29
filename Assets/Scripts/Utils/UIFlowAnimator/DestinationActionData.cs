using UnityEngine;

namespace Utils.ObjectFlowAnimator
{
    [CreateAssetMenu(fileName = "DestinationAction", menuName = "ObjectFlow/Destination Action", order = 1)]
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
