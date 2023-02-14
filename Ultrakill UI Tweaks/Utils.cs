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
using ULTRAKILLtweaker;
using UnityEngine;

namespace ULTRAKILLtweaker
{
    static class Utils
    {
        public static T GetSetting<T>(string ID)
        {
            if (typeof(T) == typeof(bool))
            {
                return (T)(object)Convert.ToBoolean(SettingRegistry.idToSetting[ID].value);
            }

            if (typeof(T) == typeof(float))
            {
                return (T)(object)Convert.ToSingle(SettingRegistry.idToSetting[ID].value);
            }

            return default(T);
        }

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

        public static string PathToName(this string from)
        {
            return from.Split('\\').Last();
        }

        public static string RemoveFileExt(this string from)
        {
            int len = from.Split('.').Last().Length + 1;
            return from.Substring(0, from.Length - len);
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

        public static void SetPrivate_Field<T>(T obj, string property, object val, bool log = false)
        {
            foreach(FieldInfo fi in obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
            {
                if(log)
                    Debug.Log($"FIELD COMP: {fi.Name} == {property}.");

                if(fi.Name == property)
                {
                    fi.SetValue(obj, val);
                    break;
                }
            }
        }

        public static void SetPrivate_Prop<T>(T obj, string property, object val, bool log = false)
        {
            foreach (PropertyInfo pi in obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.NonPublic))
            {
                if (log)
                    Debug.Log($"FIELD COMP: {pi.Name} == {property}.");

                if (pi.Name == property)
                {
                    pi.SetValue(obj, val);
                    break;
                }
            }
        }

        internal static object GetInstanceField(Type type, object instance, string fieldName)
        {
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Static;
            FieldInfo field = type.GetField(fieldName, bindFlags);
            return field.GetValue(instance);
        }

        public static List<GameObject> FindSceneObjects(string sceneName)
        {
            List<GameObject> objs = new List<GameObject>();
            foreach (GameObject obj in GameObject.FindObjectsOfType<GameObject>())
            {
                if (obj.scene.name == sceneName)
                {
                    objs.Add(obj);
                }
            }

            return objs;
        }

        public static GameObject PageContent(this GameObject from)
        {
            return from.ChildByName("Viewport").ChildByName("Content");
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
            foreach(var f in obj.GetType().GetFields(bindingFlags))
            {
                Debug.Log(f.Name);
            }
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