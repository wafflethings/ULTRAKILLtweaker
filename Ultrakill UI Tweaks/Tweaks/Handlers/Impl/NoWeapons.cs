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
    public class NoWeapons : TweakHandler
    {
        private Harmony harmony = new Harmony("waffle.ultrakill.UKtweaker.noweapons");

        public override void OnTweakEnabled()
        {
            base.OnTweakEnabled();
            harmony.PatchAll(typeof(NoWeaponsPatches));
        }

        public override void OnTweakDisabled()
        {
            base.OnTweakDisabled();
            harmony.UnpatchSelf();
        }

        public static class NoWeaponsPatches
        {
            [HarmonyPatch(typeof(HudController), nameof(HudController.Start))]
            [HarmonyPostfix]
            static void HideGunPanel(HudController __instance)
            {
                if (__instance.gameObject.name == "HUD")
                {
                    __instance.gameObject.ChildByName("GunCanvas").ChildByName("GunPanel").SetActive(false);
                }
            }

            [HarmonyPatch(typeof(GunSetter), nameof(GunSetter.ResetWeapons))]
            [HarmonyPostfix]
            static void RemoveWeapons(GunSetter __instance)
            {
                List<GameObject>[] slots = new List<GameObject>[6]
                {
                    __instance.gunc.slot1,
                    __instance.gunc.slot2,
                    __instance.gunc.slot3,
                    __instance.gunc.slot4,
                    __instance.gunc.slot5,
                    __instance.gunc.slot6
                };

                foreach (var slot in slots)
                {
                    slot.Clear();
                }
            }
        }
    }
}
