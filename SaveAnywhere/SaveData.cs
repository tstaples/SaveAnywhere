using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
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
        public bool swimming;
    }

    [Serializable]
    public class SaveData
    {
        public GameData gameData = new GameData();
        public PlayerData playerData = new PlayerData();
    }

    /*
    - field name
    - object it belongs to
    - base class logic to get the field: prob needs to search public, private etc.
    - base logic to assign it which simply assigns it
    - overrideable custom logic to do things like warping the player (not needed if very few cases)

    - need to be able to serialize non-serializable complex objects
        - might be able to generically s/d collections http://forum.unity3d.com/threads/c-serializing-a-complex-object.196591/
    */

    public class SerializeableObject<DataType, InstanceType>
    {
        public static BinaryFormatter formatter;
        public string fieldName;
        public DataType data;
        public InstanceType instance;

        public SerializeableObject(string fieldName, InstanceType instance = default(InstanceType))
        {
            this.fieldName = fieldName;
            this.instance = instance;
        }

        public virtual DataType GetData()
        {
            FieldInfo fieldInfo = typeof(InstanceType).GetField(fieldName, 
                  BindingFlags.Instance 
                | BindingFlags.NonPublic
                | BindingFlags.Public
                | BindingFlags.Static
                );
            return (DataType)fieldInfo.GetValue(instance);
        }

        public virtual bool Serialize(Stream s)
        {
            formatter.Serialize(s, data);
            return true;
        }

        public virtual bool Deserialize(Stream s)
        {
            data = (DataType)formatter.Deserialize(s);
            return (data != null);
        }
    }
}
