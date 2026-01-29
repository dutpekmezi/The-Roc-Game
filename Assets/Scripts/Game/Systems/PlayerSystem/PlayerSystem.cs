using UnityEngine;
using Utils.Pools;

namespace Game.Systems
{
    public class PlayerSystem
    {
        private PlayerData playerData;

        public PlayerSystem(PlayerData playerData)
        {
            this.playerData = playerData;
        }

        public PlayerController CreatePlayer()
        {
            //var instance = Spawn();
            return null;
        }
    }
}