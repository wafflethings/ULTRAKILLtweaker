using FallFactory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace ULTRAKILLtweaker
{
    public class SliderSetting : Setting
    {
        public Slider slider { get; private set; }
        public Text text { get; private set; }
        public float min { get; private set; }
        public float max { get; private set; }
        public bool forceWhole { get; private set; }
        public string displayAs { get; private set; }

        public SliderSetting(string SettingID, GameObject sliderobj, float mini, float maxi, float settingDefaultValue, bool whole = false, string display = "{0}")
        {
            ID = SettingID;
            slider = sliderobj.GetComponent<Slider>();
            text = sliderobj.ChildByName("Amount").GetComponent<Text>();
            min = mini;
            max = maxi;

            defaultValue = settingDefaultValue;

            slider.wholeNumbers = whole;
            forceWhole = whole;

            displayAs = display;

            slider.minValue = min;
            slider.maxValue = max;
        }

        public string GetDisplay()
        {
            return string.Format(displayAs, Math.Round(slider.value, 2));
        }

        public override void SetDisplay()
        {
            if(MainClass.Instance.SettingsInit)
                text.text = GetDisplay();
        }

        public override void UpdateValue()
        {
            value = Math.Round(slider.value, 2);
        }

        public override void SetValue()
        {
            slider.value = (float)Math.Round((double)Convert.ToSingle(value), 2);
        }
    }

    public class ToggleSetting : Setting
    {
        public Toggle toggle { get; private set; }

        public ToggleSetting(string SettingID, GameObject toggleobj, bool settingDefaultValue)
        {
            ID = SettingID;
            toggle = toggleobj.GetComponent<Toggle>();
            defaultValue = settingDefaultValue;
        }

        public override void UpdateValue()
        {
            value = toggle.isOn;
        }

        public override void SetValue()
        {
            toggle.isOn = Convert.ToBoolean(value);
        }
    }

    public class ArtifactSetting : Setting
    {
        public Toggle toggle { get; private set; }
        public string Description;
        public string Name;
        public bool DisableCG;

        public ArtifactSetting(string SettingID, GameObject toggleobj, bool disablecg, bool settingDefaultValue, string name = "Placeholder Plugin", string desc = "Placeholder Description")
        {
            ID = SettingID;
            toggle = toggleobj.GetComponent<Toggle>();
            defaultValue = settingDefaultValue;
            Description = desc;
            Name = name;
            DisableCG = disablecg;

            ArtifactHover ah = toggle.gameObject.AddComponent<ArtifactHover>();
            ah.me = this;
            
            if (toggleobj.ChildByName("Extra Settings") != null)
            {
                toggleobj.ChildByName("Extra Settings").AddComponent<HudOpenEffect>();
                toggleobj.ChildByName("Extra Settings").ChildByName("STUFF").ChildByName("Close").GetComponent<Button>().onClick.AddListener(() =>
                {
                    foreach (Setting set in SettingRegistry.settings)
                    {
                        if (set.GetType() == typeof(ArtifactSetting))
                        {
                            ((ArtifactSetting)set).toggle.gameObject.SetActive(true);
                        }
                    }

                    toggleobj.ChildByName("Extra Settings").SetActive(false);
                    toggle.enabled = true;
                });

                toggleobj.ChildByName("Extra Settings").SetActive(false);
            }
        }

        public override void UpdateValue()
        {
            value = toggle.isOn;
        }

        public override void SetValue()
        {
            toggle.isOn = Convert.ToBoolean(value);
        }
    }

    public class Setting
    {
        public string ID;
        public object value;
        public object defaultValue;

        public virtual void UpdateValue()
        {

        }

        public virtual void SetDisplay()
        {

        }

        public virtual void SetValue()
        {

        }
    }

    public class SettingRegistry 
    {
        public static Dictionary<string, Setting> idToSetting = new Dictionary<string, Setting>();
        public static List<Setting> settings = new List<Setting>();
        public static string kelPath = Path.Combine(Utils.GameDirectory(), @"BepInEx\UKMM Mods\ULTRAKILLtweaker\settings.kel");
        public static char split = 'ඞ';

        public static void Read()
        {
            Debug.Log($"Attempting to read file at {kelPath}.");
            Validate();

            foreach(string line in File.ReadLines(kelPath))
            {
                string[] Data = line.Split(split);
                idToSetting[Data[0]].value = Data[1];
                idToSetting[Data[0]].SetValue();
            }
        }

        public static void Save()
        {
            string text = "";

            foreach (Setting setting in settings)
            {
                setting.UpdateValue();
                text += $"{setting.ID}{split}{setting.value}\n";
                Debug.Log($"{setting.ID} saved as value {setting.value}.");
            }

            File.WriteAllText(kelPath, text);
        }

        public static void Validate(bool Reset = false)
        {
            if (!File.Exists(kelPath) || Reset)
            {
                Debug.Log($"File not found, setting to defaults.");
                string text = "";

                foreach(Setting setting in settings)
                {
                    text += $"{setting.ID}{split}{setting.defaultValue}\n";
                    Debug.Log($"{setting.ID} set to value {setting.defaultValue}.");
                }

                File.WriteAllText(kelPath, text);
            }
        }
    }
}
