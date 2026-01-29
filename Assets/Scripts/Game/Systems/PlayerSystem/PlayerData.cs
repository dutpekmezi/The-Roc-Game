using UnityEngine;

namespace Game.Systems
{
    [CreateAssetMenu(fileName = "PlayerData", menuName = "Game/Player/Player Data")]
    public class PlayerData : ScriptableObject
    {
        public float flapForce = 5.5f;
        public float maxUpAngle = 25f;
        public float maxDownAngle = -75f;
        public float rotationSpeed = 6f;
    }
}