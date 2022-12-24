using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ULTRAKILLtweaker
{
    public class Times
    {
        public static string kelPath = Path.Combine(Utils.GameDirectory(), @"BepInEx\UMM Mods\ULTRAKILLtweaker\times.kel");
        public static char split = 'ඞ';
        public static Dictionary<string, float> SceneToTime = new Dictionary<string, float>();
        public static Dictionary<string, float> SceneToKills = new Dictionary<string, float>();
        public static Dictionary<string, float> SceneToStyle = new Dictionary<string, float>();

        public static void Load()
        {
            SceneToTime.Clear();
            SceneToKills.Clear();
            SceneToStyle.Clear();

            if (File.Exists(kelPath))
            {
                foreach (string s in File.ReadAllLines(kelPath))
                {
                    string thing = s.Split(split)[1];
                    SceneToTime.Add(s.Split(split)[0], Convert.ToSingle(thing.Split(',')[0]));
                    SceneToKills.Add(s.Split(split)[0], Convert.ToSingle(thing.Split(',')[1]));
                    SceneToStyle.Add(s.Split(split)[0], Convert.ToSingle(thing.Split(',')[2]));
                }
            }
            else
            {
                Save();
            }
        }

        public static void Save()
        {
            string text = "";
            foreach (KeyValuePair<string, float> kvp in SceneToTime)
            {
                text += $"{kvp.Key}{split}{kvp.Value},{SceneToKills[kvp.Key]},{SceneToStyle[kvp.Key]}\n";
            }
            File.WriteAllText(kelPath, text);
        }
    }
}
