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
    public class NoStamina : TweakHandler
    {
        private Harmony harmony = new Harmony("waffle.ultrakill.UKtweaker.nostamina");

        public override void OnTweakEnabled()
        {
            base.OnTweakEnabled();
            harmony.PatchAll(typeof(NoStaminaPatches));
        }

        public override void OnTweakDisabled()
        {
            base.OnTweakDisabled();
            harmony.UnpatchSelf();
        }

        public static class NoStaminaPatches
        {
            [HarmonyPatch(typeof(NewMovement), nameof(NewMovement.Update))]
            [HarmonyPostfix]
            static void RemoveArms(NewMovement __instance)
            {
                __instance.EmptyStamina();
            }
        }
    }
}
