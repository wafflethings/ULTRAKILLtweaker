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
    public class CustomPanels : TweakHandler
    {
        private Harmony harmony = new Harmony("waffle.ultrakill.UKtweaker.custompanels");

        public GameObject PlayerInfo;
        public GameObject CustomWeaponPanel;
        public GameObject DPSPanel;
        public GameObject Speedometer;

        // All hits that have occured in the last Second
        public static List<Hit> HitsSecond = new List<Hit>();

        public class Hit
        {
            public DateTime time;
            public EnemyIdentifier eid;
            public float dmg;

            public Hit(EnemyIdentifier eid, float dmg)
            {
                this.eid = eid;
                this.dmg = dmg;
                time = DateTime.Now;
            }
        }

        private bool PanelsExist = false;

        public override void OnTweakEnabled()
        {
            base.OnTweakEnabled();
            harmony.PatchAll(typeof(PanelPatches));

            CreatePanels();

            if (Utils.GetSetting<bool>("weapanel") || Utils.GetSetting<bool>("hppanel"))
            {
                Speedometer.ChildByName("Panel").transform.position += new Vector3(0, (68.5f / 1080f) * Screen.height, 0);
                DPSPanel.ChildByName("Panel").transform.position += new Vector3(0, (68.5f / 1080f) * Screen.height, 0);
            }

            if (Utils.GetSetting<bool>("speedometer"))
            {
                DPSPanel.ChildByName("Panel").transform.position += new Vector3((274f / 1920f) * Screen.width, 0, 0);
            }

            if (Utils.GetSetting<bool>("hppanel"))
            {
                CustomWeaponPanel.ChildByName("Panel").transform.position += new Vector3((274f / 1920f) * Screen.width, 0, 0);
            }

            if (CustomWeaponPanel.activeSelf == true)
                CustomWeaponPanel.ChildByName("Panel").ChildByName("Gun").transform.localScale += new Vector3(0, 0.5f, 0);

            UpdateIfActive();
        }

        public override void OnSceneLoad(Scene scene, LoadSceneMode mode)
        {
            UpdateIfActive();
        }

        public void UpdateIfActive()
        {
            if (!IsGameplayScene())
            {
                PlayerInfo.SetActive(false);
                CustomWeaponPanel.SetActive(false);
                DPSPanel.SetActive(false);
                Speedometer.SetActive(false);
            }
            else
            {
                PlayerInfo.SetActive(true);
                CustomWeaponPanel.SetActive(true);
                DPSPanel.SetActive(true);
                Speedometer.SetActive(true);
            }
        }

        public override void OnTweakDisabled()
        {
            base.OnTweakDisabled();
            harmony.UnpatchSelf();

            PanelsExist = false;
            Destroy(PlayerInfo);
            Destroy(CustomWeaponPanel);
            Destroy(DPSPanel);
            Destroy(Speedometer);
        }

        public void Update()
        {
            NewMovement nm = NewMovement.Instance;

            if (PanelsExist)
            {
                bool Info = Utils.GetSetting<bool>("hppanel");
                bool Weapon = Utils.GetSetting<bool>("weapanel");
                bool DPS = Utils.GetSetting<bool>("dps");
                bool Speed = Utils.GetSetting<bool>("speedometer");

                if (Info && nm != null && !nm.dead)
                {
                    string prefix = "";
                    string suffix = "";
                    if (nm.antiHp != 0)
                    {
                        prefix += "<color=#7C7A7B>";
                        suffix += "</color>";
                    }

                    PlayerInfo.ChildByName("Panel").ChildByName("HP").GetComponent<Text>().text = $"{nm.hp}{prefix} / {100 - Math.Round(nm.antiHp, 0)}{suffix}";
                    PlayerInfo.ChildByName("Panel").ChildByName("Stamina").GetComponent<Text>().text = $"{(nm.boostCharge / 100).ToString("0.00")} / 3.00";
                }

                if (Weapon && nm != null && !nm.dead)
                {
                    if (Utils.GetSetting<bool>("ARTIFACT_noweapons"))
                    {
                        CustomWeaponPanel.ChildByName("Panel").ChildByName("Gun").SetActive(false);
                    }
                    else
                    {
                        try
                        {
                            CustomWeaponPanel.ChildByName("Panel").ChildByName("Gun").GetComponent<Image>().sprite = MonoSingleton<WeaponHUD>.Instance.gameObject.GetComponent<Image>().sprite;
                            CustomWeaponPanel.ChildByName("Panel").ChildByName("Gun").GetComponent<Image>().color = MonoSingleton<WeaponHUD>.Instance.gameObject.GetComponent<Image>().color;
                        }
                        catch { /* fuck it, we ball. */ }
                    }

                    if (Utils.GetSetting<bool>("ARTIFACT_noarm"))
                    {
                        CustomWeaponPanel.ChildByName("Panel").ChildByName("Fist").SetActive(false);
                    }
                    else
                    {
                        try
                        {
                            CustomWeaponPanel.ChildByName("Panel").ChildByName("Fist").GetComponent<Image>().color = GameObject.Find("FistPanel").ChildByName("Panel").GetComponent<Image>().color;
                        }
                        catch { /* fuck it, we ball. */ }
                    }

                    CustomWeaponPanel.ChildByName("Panel").ChildByName("Slider").GetComponent<Slider>().value = MonoSingleton<WeaponCharges>.Instance.raicharge;
                }

                if (DPS && nm != null && !nm.dead)
                {
                    float Damage = 0;
                    // I would use foreach but you can't edit the list in foreaches
                    for (int i = 0; i < HitsSecond.Count; i++)
                    {
                        Hit hit = HitsSecond[i];
                        if ((DateTime.Now - hit.time).TotalSeconds > 1)
                        {
                            HitsSecond.RemoveAt(i);
                            i--;
                        }
                        else
                        {
                            float ActualDmg = hit.dmg;

                            if (ActualDmg < 1000000 && ActualDmg > 0)
                                Damage += ActualDmg;
                        }
                    }

                    DPSPanel.ChildByName("Panel").ChildByName("DPS").GetComponent<Text>().text = Math.Round(Damage, 2).ToString();
                }

                if (Speed && nm != null && !nm.dead)
                {
                    Speedometer.ChildByName("Panel").ChildByName("SPEED").GetComponent<Text>().text = Math.Round(nm.rb.velocity.magnitude, 1).ToString();
                }
            }
        }

        public void CreatePanels()
        {
            PlayerInfo = Instantiate(UIElement.Settings["Info"]);
            CustomWeaponPanel = Instantiate(UIElement.Settings["Weapons"]);
            DPSPanel = Instantiate(UIElement.Settings["DPS"]);
            Speedometer = Instantiate(UIElement.Settings["Speedometer"]);
            PanelsExist = true;

            DontDestroyOnLoad(PlayerInfo);
            DontDestroyOnLoad(CustomWeaponPanel);
            DontDestroyOnLoad(DPSPanel);
            DontDestroyOnLoad(Speedometer);
        }

        public class PanelPatches
        {
            [HarmonyPatch(typeof(EnemyIdentifier), nameof(EnemyIdentifier.DeliverDamage))]
            [HarmonyPrefix]
            public static void SetHealthBefore(EnemyIdentifier __instance, out float __state)
            {
                __state = __instance.health;
            }

            [HarmonyPatch(typeof(EnemyIdentifier), nameof(EnemyIdentifier.DeliverDamage))]
            [HarmonyPostfix]
            public static void DoHealthAfter(EnemyIdentifier __instance, float __state)
            {
                float damage = __state - __instance.health;
                if (damage != 0f)
                {
                    HitsSecond.Add(new Hit(__instance, damage));
                }
            }
        }
    }
}
