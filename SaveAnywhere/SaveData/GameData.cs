using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.Xna.Framework;
using StardewValley.Menus;
using Microsoft.Xna.Framework.Graphics;

namespace SaveAnywhere.SaveData
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    class NativeClassDataAttribute : Attribute
    {
        public string ClassName { get; set; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    class NativePropertyDataAttribute : Attribute
    {
        // TODO: allow setting callback for custom pre/post load so it doesn't have to be done in one big method.

        // Should this property be ignored during generic serialization.
        // Set this to true for complex types that can't be serialized normally and must be done manually.
        //public bool SerializeManually { get; set; } = false;

        // TODO: change to IsComplexCollection or something if regular collections work fine
        public bool IsCollection { get; set; } = false;
        public bool CollectionUsesWrapperTypes { get; set; } = false;
    }

    abstract class BaseData
    {
        // TODO: maybe convenience methods for getting attribute data (if accessible from here)
        // ie. bool ContainsNativeWrapperFields()
    }

    [NativeClassData(ClassName = "ClickableTextureComponent")]
    class ClickableTextureComponentData : BaseData
    {
        public float baseScale { get; set; }
        public bool drawShadow { get; set; }
        public string hoverText { get; set; }
        public Rectangle sourceRect { get; set; }
        public Texture2D texture { get; set; }
    }

    [NativeClassData(ClassName = "Buff")]
    class BuffData : BaseData
    {
        public int millisecondsDuration { get; set; }
        public string description { get; set; }
        public string source { get; set; }
        public int total { get; set; }
        public int sheetIndex { get; set; }
        public int which { get; set; }
        public Color glow { get; set; }
        public int[] buffAttributes { get; set; }
    }

    [NativeClassData(ClassName = "BuffDisplay")]
    class BuffsDisplayData : BaseData
    {
        public BuffData food { get; set; } = new BuffData();
        public BuffData drink { get; set; } = new BuffData();

        [NativePropertyData(IsCollection = true, CollectionUsesWrapperTypes = true)]
        public List<BuffData> otherBuffs { get; set; } = new List<BuffData>();

        // need an attribute to tell the serializer we're using a wrapper class for one of the types
        //[NativePropertyData(IsCollection = true, CollectionUsesWrapperTypes = true)]
        //public Dictionary<ClickableTextureComponentData, BuffData> buffs { get; set; } = new Dictionary<ClickableTextureComponentData, BuffData>();
    }

    [NativeClassData(ClassName = "GameLocation")]
    class GameLocationData : BaseData
    {
        public string name { get; set; }
    }

    [NativeClassData(ClassName = "Farmer")]
    class FarmerData : BaseData
    {
        public Vector2 position { get; set; }
        public GameLocationData currentLocation { get; set; } = new GameLocationData();
        public int facingDirection { get; set; }
        public float stamina { get; set; }
        public int health { get; set; }
        public bool swimming { get; set; }
    }

    [NativeClassData(ClassName = "Game1")]
    class GameData : BaseData
    {
        public int timeOfDay { get; set; }
        public FarmerData player { get; set; } = new FarmerData();
        public BuffsDisplayData buffsDisplay { get; set; } = new BuffsDisplayData();
    }
}
