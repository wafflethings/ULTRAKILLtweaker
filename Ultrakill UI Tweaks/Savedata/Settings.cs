﻿
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ULTRAKILLtweaker.Components;
using ULTRAKILLtweaker.Tweaks.Handlers;
using ULTRAKILLtweaker.Tweaks.UIElements;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ULTRAKILLtweaker
{
    public class SliderSubsetting : Setting
    {
        public Slider slider { get; private set; }
        public InputField input { get; private set; }
        public float min { get; private set; }
        public float max { get; private set; }
        public bool forceWhole { get; private set; }
        public string displayAs { get; private set; }

        public SliderSubsetting(string SettingID, GameObject sliderobj, float mini, float maxi, float settingDefaultValue, bool whole = false, string display = "{0}") : base()
        {
            ID = SettingID;

            self = sliderobj;
            slider = sliderobj.ChildByName("Slider").GetComponent<Slider>();
            input = sliderobj.ChildByName("InputField").GetComponent<InputField>();

            min = mini;
            max = maxi;

            defaultValue = settingDefaultValue;

            slider.wholeNumbers = whole;
            forceWhole = whole;

            displayAs = display;

            slider.minValue = min;
            slider.maxValue = max;

            slider.onValueChanged.AddListener((val) =>
            {
                int precision = 2;

                if (Input.GetKey(KeyCode.LeftControl))
                    precision = 1;

                if (val <= max)
                {
                    value = (float)Math.Round((double)Convert.ToSingle(val), precision);

                    input.text = string.Format(displayAs, value);

                    input.textComponent.color = Color.white;
                }
            });

            input.onEndEdit.AddListener((val) =>
            {
                val = Regex.Replace(val, @"[^\d-.]", "");

                try
                {
                    float numVal = Convert.ToSingle(val);

                    if (numVal < min) 
                        numVal = min;

                    input.text = string.Format(displayAs, numVal);
                    slider.value = numVal;
                    value = numVal;

                    if (numVal > max)
                        input.textComponent.color = Color.red;
                    else
                        input.textComponent.color = Color.white;

                } catch
                {
                    input.text = string.Format(displayAs, ((float)value).ToString());
                }
            });
        }

        public override void SetValue()
        {
            slider.value = Convert.ToSingle(value);
            input.text = string.Format(displayAs, value.ToString());

            if (slider.value > max)
                input.textComponent.color = Color.red;
        }
    }

    public class ToggleSetting : Setting
    {
        public Toggle toggle { get; private set; }
        public UIElement uie;

        public ToggleSetting(string SettingID, UIElement toggleobj, bool settingDefaultValue) : base()
        {
            ID = SettingID;
            self = toggleobj.Self;
            uie = toggleobj;
            toggle = toggleobj.Self.ChildByName("Toggle").GetComponent<Toggle>();
            defaultValue = settingDefaultValue;

            toggle.onValueChanged.AddListener((val) =>
            {
                value = val.ToString();
            });
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
        public GameObject self;

        public Setting()
        {
            SettingRegistry.settings.Add(this);
        }

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
        public static string kelPath = Path.Combine(Utils.GameDirectory(), @"BepInEx\UMM Mods\ULTRAKILLtweaker\settings.kel");
        public static char split = 'ඞ';
        public static string CurrentFile;

        public static void Read()
        {
            Debug.Log($"Attempting to read file at {kelPath}.");
            Validate();

            CurrentFile = "";

            foreach(string line in File.ReadLines(kelPath))
            {
                string[] Data = line.Split(split);
                if (idToSetting.ContainsKey(Data[0]))
                {
                    idToSetting[Data[0]].value = Data[1];
                    idToSetting[Data[0]].SetValue();
                    CurrentFile += line + "\n";
                }
            }
        }

        public static void Save()
        {
            string text = "";

            Debug.Log("Saving!");

            foreach (Setting setting in settings)
            {
                setting.UpdateValue();

                if (!CurrentFile.Contains(setting.ID))
                    setting.value = setting.defaultValue;

                text += $"{setting.ID}{split}{setting.value}\n";

                if(MainClass.IDToType.ContainsKey(setting.ID))
                {
                    TweakHandler handler = MainClass.TypeToHandler[MainClass.IDToType[setting.ID]];
                    bool Value = Convert.ToBoolean(((ToggleSetting)setting).value);

                    if (Value && !handler.WasEnabled)
                        handler.OnTweakEnabled();
                    else if (!Value && handler.WasEnabled)
                        handler.OnTweakDisabled();
                }
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
                }

                File.WriteAllText(kelPath, text);
            }
        }
    }
}