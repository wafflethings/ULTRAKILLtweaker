using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ULTRAKILLtweaker.Tweaks.Handlers.Impl
{
    public class Mitosis : TweakHandler
    {
        private Harmony harmony = new Harmony("waffle.ultrakill.UKtweaker.mitosis");

        public override void OnTweakEnabled()
        {
            base.OnTweakEnabled();
            harmony.PatchAll(typeof(MitosisPatches));
        }

        public override void OnTweakDisabled()
        {
            base.OnTweakDisabled();
            harmony.UnpatchSelf();
        }

        public static class MitosisPatches
        {
            [HarmonyPatch(typeof(EnemyIdentifier), "Start")]
            [HarmonyPrefix]
            public static void HmmTodayIWillUndergoMitosis(EnemyIdentifier __instance)
            {
                if (!__instance.gameObject.name.Contains("(MITOSIS)"))
                {
                    for (int i = 0; i < Utils.GetSetting<float>("artiset_mitosis_amount") - 1; i++)
                    {
                        GameObject obj = Instantiate(__instance.gameObject, __instance.transform.parent);

                        if (obj.GetComponentInParent<ActivateNextWave>() != null)
                        {
                            obj.GetComponentInParent<ActivateNextWave>().enemyCount++;
                        }

                        obj.name = __instance.gameObject.name + "(MITOSIS)";
                        obj.transform.position = __instance.transform.position;
                    }
                }
            }
        }
    }
}
