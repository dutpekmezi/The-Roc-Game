using UnityEngine;

namespace Game.Systems
{
    public class PlayerSystem
    {
        private PlayerData playerData;

        public PlayerSystem(PlayerData playerData)
        {
            this.playerData = playerData;
        }
    }
}