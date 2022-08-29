using BepInEx;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FallFactory
{
    static class Utils
    {
        public static string GameDirectory()
        {
            string path = Application.dataPath;
            if (Application.platform == RuntimePlatform.OSXPlayer)
            {
                path = Utility.ParentDirectory(path, 2);
            }
            else if (Application.platform == RuntimePlatform.WindowsPlayer)
            {
                path = Utility.ParentDirectory(path, 1);
            }

            return path;
        }

        public static Vector3 Mult(this Vector3 from, Vector3 vec)
        {
            return new Vector3(from.x * vec.x, from.y * vec.y, from.z * vec.z);
        }

        public static void Resize(this GameObject from, float amount, Vector3 direction)
        {
            from.transform.position += direction * amount / 2; // Move the object in the direction of scaling, so that the corner on ther side stays in place
            from.transform.localScale += direction * amount; // Scale object in the specified direction
        }

        public static GameObject ChildByName(this GameObject from, string name)
        {
            List<GameObject> children = new List<GameObject>();
            int count = 0;
            while (count < from.transform.childCount)
            {
                children.Add(from.transform.GetChild(count).gameObject);
                count++;
            }

            if(count == 0)
            {
                return null;
            }

            for (int i = 0; i < children.Count; i++)
            {
                if (children[i].name == name)
                {
                    return children[i];
                }
            }
            return null;
        }

        public static List<GameObject> ChildrenList(this GameObject from)
        {
            List<GameObject> children = new List<GameObject>();
            int count = 0;
            while (count < from.transform.childCount)
            {
                children.Add(from.transform.GetChild(count).gameObject);
                count++;
            }

            return children;
        }

        public static T GetFieldValue<T>(this object obj, string name)
        {
            // Set the flags so that private and public fields from instances will be found
            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var field = obj.GetType().GetField(name, bindingFlags);
            return (T)field?.GetValue(obj);
        }
    }

    public class CMDUtils
    {
        public static string Clear = "\x1b[0m";
        public static string Red = "\x1b[31m";
        public static string Dim = "\x1b[2m";
        public static string Underline = "\x1b[4m";
        public static string Blink = "\x1b[5m";
        public static string Reverse = "\x1b[7m";
        public static string Hidden = "\x1b[8m";
        public static string Strike = "\x1b[9m";
    }
}