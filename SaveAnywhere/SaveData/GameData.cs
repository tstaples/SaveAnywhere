using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace SaveAnywhere.SaveData
{
    class GameLocationData
    {
        public string name { get; set; }
    }

    class FarmerData
    {
        public Vector2 position { get; set; }
        public GameLocationData currentLocation { get; set; } = new GameLocationData();
        public int facingDirection { get; set; }
        public float stamina { get; set; }
        public int health { get; set; }
        public bool swimming { get; set; }
    }

    class GameData
    {
        public int timeOfDay { get; set; }
        public FarmerData player { get; set; } = new FarmerData();
    }
}
