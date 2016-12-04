using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.Xna.Framework;

namespace SaveAnywhere.SaveData
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    class NativeClassDataWrapperAttribute : Attribute
    {
        public string ClassName { get; set; }
    }

    class BaseData
    {

    }

    [NativeClassDataWrapper(ClassName = "GameLocation")]
    class GameLocationData : BaseData
    {
        public string name { get; set; }
    }

    [NativeClassDataWrapper(ClassName = "Farmer")]
    class FarmerData : BaseData
    {
        public Vector2 position { get; set; }
        public GameLocationData currentLocation { get; set; } = new GameLocationData();
        public int facingDirection { get; set; }
        public float stamina { get; set; }
        public int health { get; set; }
        public bool swimming { get; set; }
    }

    [NativeClassDataWrapper(ClassName = "Game1")]
    class GameData : BaseData
    {
        //public Dictionary<string, object> data { get; set; } = new Dictionary<string, object>();
        public int timeOfDay { get; set; }
        public FarmerData player { get; set; } = new FarmerData();
    }
}
