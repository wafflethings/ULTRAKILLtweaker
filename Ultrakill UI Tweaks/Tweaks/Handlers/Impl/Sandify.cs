using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ULTRAKILLtweaker.Tweaks.Handlers.Impl
{
    public class Sandify : TweakHandler
    {
        private Harmony harmony = new Harmony("waffle.ultrakill.UKtweaker.sandify");

        public override void OnTweakEnabled()
        {
            base.OnTweakEnabled();
            harmony.PatchAll(typeof(SandifyPatches));
        }

        public override void OnTweakDisabled()
        {
            base.OnTweakDisabled();
            harmony.UnpatchSelf();
        }

        public static class SandifyPatches
        {
            [HarmonyPatch(typeof(EnemyIdentifier), nameof(EnemyIdentifier.Awake))]
            [HarmonyPostfix]
            public static void MakeSandy(EnemyIdentifier __instance)
            {
                __instance.sandified = true;
            }
        }
    }
}
