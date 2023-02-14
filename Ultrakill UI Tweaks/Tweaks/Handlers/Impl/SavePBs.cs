﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ULTRAKILLtweaker.Components;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ULTRAKILLtweaker.Tweaks.Handlers.Impl
{
    public class SavePBs : TweakHandler
    {
        private Harmony harmony = new Harmony("waffle.ultrakill.UKtweaker.savepbs");
        private GameObject NewStat;
        protected static LevelStats ls;

        public void CreateStat()
        {
            if (SceneManager.GetActiveScene().name != "Endless")
            {
                GameObject stat = ls.gameObject;

                if (NewStat != null)
                    Destroy(NewStat);

                NewStat = Instantiate(stat);

                NewStat.transform.parent = stat.transform;
                NewStat.transform.position = new Vector3(1480 - 15 / Screen.width * 1920, stat.transform.position.y, stat.transform.position.z);
                NewStat.transform.localScale = stat.transform.localScale;
                NewStat.AddComponent<StatPBController>();
            }
        }

        public override void OnTweakEnabled()
        {
            base.OnTweakEnabled();
            harmony.PatchAll(typeof(PBPatches));
            StartCoroutine(CreateWhenSet());
        }

        public System.Collections.IEnumerator CreateWhenSet()
        {
            yield return null;

            while (ls == null)
                yield return null;

            CreateStat();
        }

        public override void OnTweakDisabled()
        {
            base.OnTweakDisabled();
            harmony.UnpatchSelf();

            if(NewStat != null)
                Destroy(NewStat);
        }

        public override void OnSceneLoad(Scene scene, LoadSceneMode mode)
        {
            ls = null;
        }

        public static class PBPatches
        {
            [HarmonyPatch(typeof(LevelStats), nameof(LevelStats.Start))]
            [HarmonyPostfix]
            static void SetLS(LevelStats __instance)
            {
                ls = __instance;
            }

            [HarmonyPatch(typeof(FinalRank), nameof(FinalRank.SetTime))]
            static void SaveIt()
            {
                StatsManager sm = StatsManager.Instance;

                if (Times.SceneToTime.ContainsKey(SceneManager.GetActiveScene().name))
                {
                    if (Times.SceneToTime[SceneManager.GetActiveScene().name] > sm.seconds)
                        Times.SceneToTime[SceneManager.GetActiveScene().name] = sm.seconds;

                    if (Times.SceneToKills[SceneManager.GetActiveScene().name] < sm.kills)
                        Times.SceneToKills[SceneManager.GetActiveScene().name] = sm.kills;

                    if (Times.SceneToStyle[SceneManager.GetActiveScene().name] < sm.stylePoints)
                        Times.SceneToStyle[SceneManager.GetActiveScene().name] = sm.stylePoints;
                }
                else
                {
                    Times.SceneToTime.Add(SceneManager.GetActiveScene().name, sm.seconds);
                    Times.SceneToKills.Add(SceneManager.GetActiveScene().name, sm.kills);
                    Times.SceneToStyle.Add(SceneManager.GetActiveScene().name, sm.stylePoints);
                }

                Times.Save();
            }

            [HarmonyPatch(typeof(LevelSelectPanel), nameof(LevelSelectPanel.OnEnable))]
            [HarmonyPrefix]
            static void AddHover(LevelSelectPanel __instance)
            {
                if(__instance.gameObject.ChildByName("Stats").ChildByName("Rank").GetComponent<HoverRank>() == null)
                {
                    __instance.gameObject.ChildByName("Stats").ChildByName("Rank").AddComponent<HoverRank>();
                }
            }

            class HoverRank : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
            {
                GameObject SecretPanel;
                string DefaultValue;
                string SceneName;

                public void Start()
                {
                    SecretPanel = transform.parent.gameObject.ChildByName("Challenge").transform.GetChild(1).gameObject;
                    SceneName = "Level " + GetComponentInParent<LevelSelectPanel>().gameObject.ChildByName("Image").GetComponent<Image>().mainTexture.name.Substring(0, 3);
                    GetComponent<Image>().raycastTarget = true;
                }

                public void OnPointerEnter(PointerEventData eventData)
                {
                    Debug.Log($"{SceneName} has SP null {SecretPanel == null}");

                    if (Times.SceneToKills.ContainsKey(SceneName))
                    {
                        SecretPanel.SetActive(true);
                        DefaultValue = SecretPanel.ChildByName("Text").GetComponent<Text>().text;

                        string joe = TimeSpan.FromSeconds(Times.SceneToTime[SceneName]).ToString();
                        if (joe.StartsWith("00:"))
                            joe = joe.Substring(3, joe.Length - 7);
                        else
                            joe = joe.Substring(0, joe.Length - 4);


                        SecretPanel.ChildByName("Text").GetComponent<Text>().text = 
                            $"<b>TIME:</b> {joe}, <b>KILLS:</b> {Times.SceneToKills[SceneName]}, <b>STYLE:</b> {Times.SceneToStyle[SceneName]} ";
                    }
                }

                public void OnPointerExit(PointerEventData eventData)
                {
                    if (Times.SceneToKills.ContainsKey(SceneName))
                    {
                        SecretPanel.SetActive(false);
                        SecretPanel.ChildByName("Text").GetComponent<Text>().text = DefaultValue;
                    }
                }
            }
        }
    }
}