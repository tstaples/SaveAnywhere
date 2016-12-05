using System;
using System.Diagnostics;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using Newtonsoft.Json;
using System.IO;

namespace SaveAnywhere
{
    public class Utils
    {
        public static string ArrayToString<T>(T[] array)
        {
            string s = "";
            int i = 0;
            foreach (T item in array)
            {
                s += item.ToString() + ((++i < array.Length) ? ", " : "");
            }
            return s;
        }

        public static T[] ConcatArrays<T>(T[] a, T[] b)
        {
            T[] c = new T[a.Length + b.Length];
            Array.Copy(a, c, a.Length);
            Array.Copy(b, c, b.Length);
            return c;
        }

        public static int[] StringToIntArray(string[] array, int defaultVal=0)
        {
            int[] output = new int[array.Length];
            for (int i = 0; i < array.Length; ++i)
            {
                if (array[i] == null || !array[i].IsInt32())
                    continue;

                try
                {
                    output[i] = Int32.Parse(array[i]);
                }
                catch (Exception)
                {
                    output[i] = defaultVal;
                }
            }
            return output;
        }

        public static int GetTileSheetIndexFromID(int id)
        {
            if (id == 0)
                return 0;

            const int spriteSize = 16; // each sprite on this sheet is 16x16
            int x = (int)Math.Floor((float)(id / 24.0f));
            int y = id % spriteSize;
            return (y * spriteSize) + x;
        }

        public static int Clamp(int val, int min, int max)
        {
            return Math.Max(Math.Min(val, max), min);
        }

        public static T CheckNotNull<T>(T value, string name)
        {
            if (value == null)
            {
                throw new ArgumentNullException(name);
            }
            return value;
        }

        /// <summary>Save to a JSON file.</summary>
        /// <typeparam name="TModel">The model type.</typeparam>
        /// <param name="path">The file path relative to the mod directory.</param>
        /// <param name="model">The model to save.</param>
        public static void WriteJsonFile<TModel>(string path, TModel model)
            where TModel : class
        {
            // create directory if needed
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            // write file
            JsonSerializerSettings settings = new JsonSerializerSettings()
            {
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize
            };

            string json = JsonConvert.SerializeObject(model, Formatting.Indented/*, settings*/);
            File.WriteAllText(path, json);
        }

        /// <summary>Read a JSON file.</summary>
        /// <typeparam name="TModel">The model type.</typeparam>
        /// <param name="path">The file path relative to the mod directory.</param>
        /// <returns>Returns the deserialised model, or <c>null</c> if the file doesn't exist or is empty.</returns>
        public static TModel ReadJsonFile<TModel>(string path)
            where TModel : class
        {
            // read file
            string json;
            try
            {
                json = File.ReadAllText(path);
            }
            catch (FileNotFoundException)
            {
                return null;
            }

            JsonSerializerSettings settings = new JsonSerializerSettings()
            {
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize
            };

            // TODO: find a way to handle deserializing the lists.
            // Otherwise we might just have to wrap errthang D:
            // deserialise model
            TModel model = JsonConvert.DeserializeObject<TModel>(json, settings);

            return model;
        }

        public static void DeleteFile(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
