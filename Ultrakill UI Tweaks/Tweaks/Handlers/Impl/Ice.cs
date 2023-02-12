using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ULTRAKILLtweaker.Tweaks.Handlers.Impl
{
    public class Ice : TweakHandler
    {
        private Harmony harmony = new Harmony("waffle.ultrakill.UKtweaker.ice");

        public override void OnTweakEnabled()
        {
            base.OnTweakEnabled();
            harmony.PatchAll(typeof(IcePatches));
        }

        public override void OnTweakDisabled()
        {
            base.OnTweakDisabled();
            harmony.UnpatchSelf();
        }

        public static class IcePatches
        {
            [HarmonyPatch(typeof(NewMovement), nameof(NewMovement.Start))]
            [HarmonyPostfix]
            public static void IcePlayer(NewMovement __instance)
            {
                __instance.modForcedFrictionMultip = Utils.GetSetting<float>("artiset_ice_frict");
            }
        }
    }
}
