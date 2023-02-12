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
    public class Speed : TweakHandler
    {
        private Harmony harmony = new Harmony("waffle.ultrakill.UKtweaker.speed");

        public override void OnTweakEnabled()
        {
            base.OnTweakEnabled();
            harmony.PatchAll(typeof(SpeedPatches));
        }

        public override void OnTweakDisabled()
        {
            base.OnTweakDisabled();
            harmony.UnpatchSelf();
        }

        public static class SpeedPatches
        {
            [HarmonyPatch(typeof(NewMovement), nameof(NewMovement.Start))]
            [HarmonyPostfix]
            public static void SpeedPlayer(NewMovement __instance)
            {
                __instance.walkSpeed *= Utils.GetSetting<float>("artiset_gofast_player");
            }

            [HarmonyPatch(typeof(EnemyIdentifier), "Awake")]
            [HarmonyPostfix]
            public static void SpeedEnemy(EnemyIdentifier __instance)
            {
                if (__instance.GetComponent<NavMeshAgent>() != null)
                {
                    __instance.GetComponent<NavMeshAgent>().speed *= Utils.GetSetting<float>("artiset_gofast_enemy");
                    __instance.GetComponent<NavMeshAgent>().acceleration *= Utils.GetSetting<float>("artiset_gofast_enemy");
                }
            }
        }
    }
}
