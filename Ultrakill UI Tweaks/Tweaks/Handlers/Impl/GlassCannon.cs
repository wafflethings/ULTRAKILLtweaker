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
    public class GlassCannon : TweakHandler
    {
        private Harmony harmony = new Harmony("waffle.ultrakill.UKtweaker.glass");

        public override void OnTweakEnabled()
        {
            base.OnTweakEnabled();
            harmony.PatchAll(typeof(GlassPatches));
        }

        public override void OnTweakDisabled()
        {
            base.OnTweakDisabled();
            harmony.UnpatchSelf();
        }

        public void Update()
        {
            NewMovement nm = NewMovement.Instance;

            if (Utils.GetSetting<bool>("ARTIFACT_glass"))
            {
                if (nm != null)
                {
                    nm.antiHp = 70;
                    if (nm.hp > 30)
                    {
                        nm.ForceAntiHP(70);
                    }
                }
            }
        }

        public static class GlassPatches
        {
            [HarmonyPatch(typeof(EnemyIdentifier), nameof(EnemyIdentifier.Awake))]
            [HarmonyPostfix]
            public static void HalfHealth(EnemyIdentifier __instance)
            {
                __instance.DeliverDamage(__instance.gameObject, Vector3.zero, Vector3.zero, __instance.health / 2, false, 0);
            }
        }
    }
}
