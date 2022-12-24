using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace ULTRAKILLtweaker.Tweaks.Handlers.Impl
{
    public class UIScale : TweakHandler
    {
        private Harmony harmony = new Harmony("waffle.ultrakill.UKtweaker.uiscale");

        public override void OnTweakEnabled()
        {
            base.OnTweakEnabled();
            harmony.PatchAll(typeof(UIScalePatches));
        }

        public override void OnTweakDisabled()
        {
            base.OnTweakDisabled();
            harmony.UnpatchSelf();
        }

        public static class UIScalePatches
        {
            [HarmonyPatch(typeof(CanvasController), nameof(CanvasController.Awake))]
            [HarmonyPostfix]
            static void PatchCanvasScale(CanvasController __instance)
            {
                float CanvasScale = Utils.GetSetting<float>("uiscalecanv");

                if (CanvasScale != 100)
                {
                    GameObject canvas = __instance.gameObject;
                    canvas.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                    canvas.GetComponent<CanvasScaler>().scaleFactor = 1920 / Screen.width * 1.5f;
                    canvas.GetComponent<CanvasScaler>().scaleFactor *= CanvasScale / 100;
                }
            }

            [HarmonyPatch(typeof(HudController), nameof(HudController.Start))]
            [HarmonyPostfix]
            static void PatchHUDScale(HudController __instance)
            {
                if (__instance.gameObject.name == "HUD")
                {
                    float InfoScale = Utils.GetSetting<float>("uiscale");
                    float StyleScale = Utils.GetSetting<float>("uiscalestyle");
                    float ResultsScale = Utils.GetSetting<float>("uiscaleresults");

                    GameObject Info = __instance.gameObject.ChildByName("GunCanvas");
                    GameObject Style = __instance.gameObject.ChildByName("StyleCanvas");
                    GameObject Results = __instance.gameObject.ChildByName("FinishCanvas");

                    if (InfoScale != 100)
                        Info.transform.localScale *= InfoScale / 100;

                    if (StyleScale != 100)
                        Style.transform.localScale *= StyleScale / 100;

                    if (ResultsScale != 100)
                        Results.transform.localScale *= ResultsScale / 100;
                }
            }

            [HarmonyPatch(typeof(BossHealthBar), nameof(BossHealthBar.Awake))]
            [HarmonyPostfix]
            static void PatchBossbarScale(BossHealthBar __instance)
            {
                float BarScale = Utils.GetSetting<float>("uiscaleboss");
                GameObject Bar = __instance.GetFieldValue<GameObject>("bossBar");

                if (BarScale != 100)
                    Bar.transform.localScale *= BarScale / 100;
            }
        }
    }
}
