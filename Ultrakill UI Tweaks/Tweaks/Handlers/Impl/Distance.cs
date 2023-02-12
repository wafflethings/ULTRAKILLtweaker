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
    public class Distance : TweakHandler
    {
        private Harmony harmony = new Harmony("waffle.ultrakill.UKtweaker.closequarters");

        public override void OnTweakEnabled()
        {
            base.OnTweakEnabled();
            harmony.PatchAll(typeof(DistancePatches));
        }

        public override void OnTweakDisabled()
        {
            base.OnTweakDisabled();
            harmony.UnpatchSelf();
        }

        public static class DistancePatches
        {
            [HarmonyPatch(typeof(EnemyIdentifier), nameof(EnemyIdentifier.Awake))]
            [HarmonyPostfix]
            public static void AddComp(EnemyIdentifier __instance)
            {
                __instance.gameObject.AddComponent<BlessWhenFar>();
            }
        }
    }
}
