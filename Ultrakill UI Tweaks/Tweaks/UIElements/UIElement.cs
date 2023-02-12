using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace ULTRAKILLtweaker.Tweaks.UIElements
{
    public class UIElement
    {
        public Metadata meta;
        public GameObject Self;

        public static Dictionary<string, GameObject> Settings = new Dictionary<string, GameObject>()
        {
            { "Page", null },
            { "Mutator", null },
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
                if (setting.Value == null)
                    Settings[setting.Key] = MainClass.UIBundle.LoadAsset<GameObject>(setting.Key);
            }
        }

        public UIElement(LayoutGroup lg, Metadata meta)
        {
            Self = GameObject.Instantiate(Settings[AssetName()]);
            Self.GetComponent<RectTransform>().SetParent(lg.transform);

            RectTransform rt = lg.GetComponent<RectTransform>();
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rt.rect.height + Offset());
            Self.transform.localScale = Vector3.one;

            // Set name, desc, stuff
        }

        public virtual string AssetName()
        {
            return "";
        }

        public virtual int Offset()
        {
            return 80;
        }
    }
}
