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
    public class NoArms : TweakHandler
    {
        private Harmony harmony = new Harmony("waffle.ultrakill.UKtweaker.noarms");

        public override void OnTweakEnabled()
        {
            base.OnTweakEnabled();
            harmony.PatchAll(typeof(NoArmsPatches));
        }

        public override void OnTweakDisabled()
        {
            base.OnTweakDisabled();
            harmony.UnpatchSelf();
        }

        public static class NoArmsPatches
        {
            [HarmonyPatch(typeof(HudController), nameof(HudController.Start))]
            [HarmonyPostfix]
            static void HideFistPanel(HudController __instance)
            {
                if (__instance.gameObject.name == "HUD")
                {
                    __instance.gameObject.ChildByName("GunCanvas").ChildByName("StatsPanel").ChildByName("Filler").ChildByName("FistPanel").SetActive(false);
                }
            }

            [HarmonyPatch(typeof(NewMovement), nameof(NewMovement.Start))]
            [HarmonyPostfix]
            static void RemoveArms(NewMovement __instance)
            {
                __instance.gameObject.ChildByName("Main Camera").ChildByName("Punch").SetActive(false);
            }
        }
    }
}
