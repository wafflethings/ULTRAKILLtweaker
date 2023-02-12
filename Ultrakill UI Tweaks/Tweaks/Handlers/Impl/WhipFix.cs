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
    public class WhipFix : TweakHandler
    {
        private Harmony harmony = new Harmony("waffle.ultrakill.UKtweaker.whipfix");

        public override void OnTweakEnabled()
        {
            base.OnTweakEnabled();
            harmony.PatchAll(typeof(WhipPatches));
        }

        public override void OnTweakDisabled()
        {
            base.OnTweakDisabled();
            harmony.UnpatchSelf();
        }

        public static class WhipPatches
        {
            [HarmonyPatch(typeof(NewMovement), "ForceAddAntiHP")]
            [HarmonyPrefix]
            public static void HardDamage(ref float amount)
            {
                System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace();
                if (stackTrace.ToString().Contains("Hook"))
                {
                    amount *= Utils.GetSetting<float>("artiset_whip_hard_mult");
                }
            }
        }
    }
}
