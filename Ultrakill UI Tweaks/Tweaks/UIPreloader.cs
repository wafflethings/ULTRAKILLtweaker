using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ULTRAKILLtweaker.Tweaks
{
    public class UIPreloader
    {
        public static Dictionary<string, GameObject> Settings = new Dictionary<string, GameObject>()
        {
            { "Page", null },
            { "Setting", null },
            { "Toggle", null },
            { "Subsetting - Slider", null },
            { "Subsetting - Toggle", null },
            { "Subsetting - Comment", null },

            { "DicerollCanvas", null },
            { "DPS", null },
            { "GrindCanvas", null },
            { "Info", null },
            { "Speedometer", null },
            { "Weapons", null }
        };

        public static void LoadSettingElements()
        {
            foreach (var setting in Settings.ToList())
            {
                if(setting.Value == null)
                    Settings[setting.Key] = MainClass.UIBundle.LoadAsset<GameObject>(setting.Key); 
            }
        }
    }
}
