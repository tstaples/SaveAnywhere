using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley;
using Microsoft.Xna.Framework;

namespace SaveAnywhere
{
    [Serializable]
    public struct GameData
    {
        public int timeOfDay;
    }

    [Serializable]
    public struct PlayerData
    {
        public string currentLocation;
        public Vector2 position;
        public int facingDirection;
        public float stamina;
        public int health;
    }

    [Serializable]
    public class SaveData
    {
        public GameData gameData = new GameData();
        public PlayerData playerData = new PlayerData();
    }
}
