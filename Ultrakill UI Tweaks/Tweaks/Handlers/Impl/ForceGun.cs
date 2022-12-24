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
    public class ForceGun : TweakHandler
    {
        private Harmony harmony = new Harmony("waffle.ultrakill.UKtweaker.forcegun");

        public override void OnTweakEnabled()
        {
            base.OnTweakEnabled();
            harmony.PatchAll(typeof(ForceGunPatches));
            if (IsGameplayScene())
                GunControl.Instance.gunPanel[0].SetActive(true);
        }

        public override void OnTweakDisabled()
        {
            base.OnTweakDisabled();
            harmony.UnpatchSelf();
            GunControl.Instance.gunPanel[0].SetActive(false);
        }

        public static class ForceGunPatches
        {
            [HarmonyPatch(typeof(GunControl), nameof(GunControl.Start))]
            [HarmonyPostfix]
            static void PatchGunPanel(GunControl __instance)
            {
                if(IsGameplayScene())
                    __instance.gunPanel[0].SetActive(true);
            }
        }
    }
}
