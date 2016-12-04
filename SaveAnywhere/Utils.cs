﻿using System;
using System.Diagnostics;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI;

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
    }
}
