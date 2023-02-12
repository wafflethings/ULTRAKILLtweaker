using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ULTRAKILLtweaker.Tweaks.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ULTRAKILLtweaker.Tweaks.Handlers.Impl
{
    public class CGUtils : TweakHandler
    {
        private Harmony harmony = new Harmony("waffle.ultrakill.UKtweaker.cgutils");

        private GameObject Panel;
        private Text Waves;
        private Text Enemies;
        private Text Time;
        private Text Kills;
        private Text Style;

        private GameObject WaveNum;

        private StatsManager sm;

        public override void OnTweakEnabled()
        {
            base.OnTweakEnabled();
        }

        public override void OnTweakDisabled()
        {
            base.OnTweakDisabled();
            harmony.UnpatchSelf();
        }

        public void Create()
        {
            if (SceneManager.GetActiveScene().name == "Endless")
            {
                sm = StatsManager.Instance;
                WaveNum = GameObject.Find("Wave Number");

                Panel = Instantiate(UIElement.Settings["GrindCanvas"]);

                Waves = Panel.ChildByName("Panel").ChildByName("WaveCount").GetComponent<Text>();
                Enemies = Panel.ChildByName("Panel").ChildByName("EnemyCount").GetComponent<Text>();
                Time = Panel.ChildByName("Panel").ChildByName("Time").GetComponent<Text>();
                Kills = Panel.ChildByName("Panel").ChildByName("Kills").GetComponent<Text>();
                Style = Panel.ChildByName("Panel").ChildByName("Style").GetComponent<Text>();

                Waves.text = "0";
                Enemies.text = "0";
                Time.text = "00:00.000";
                Kills.text = "0";
                Style.text = "0";
            }
        }

        public void Update()
        {
            if (SceneManager.GetActiveScene().name == "Endless")
            {
                if (WaveNum != null)
                {
                    GameObject panel = WaveNum.transform.parent.gameObject;
                    Waves.text = WaveNum.GetComponent<Text>().text;
                    Enemies.text = panel.ChildByName("Enemies Left Number").GetComponent<Text>().text;
                    Kills.text = sm.kills + "";
                    Style.text = sm.stylePoints + "";
                    string joe = TimeSpan.FromSeconds(sm.seconds).ToString();
                    if (joe.StartsWith("00:"))
                    {
                        Time.text = joe.Substring(3, joe.Length - 7);
                    }
                    else
                    {
                        Time.text = joe.Substring(0, joe.Length - 4);
                    }
                }
                else
                {
                    sm = StatsManager.Instance;
                    WaveNum = GameObject.Find("Wave Number");
                }
            } 
        }

        public override void OnSceneLoad(Scene scene, LoadSceneMode mode)
        {
            Create();
        }
    }
}
