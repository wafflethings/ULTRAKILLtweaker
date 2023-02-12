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
    public class Tankify : TweakHandler
    {
        private Harmony harmony = new Harmony("waffle.ultrakill.UKtweaker.tankify");
        private static List<Component> Done = new List<Component>();

        public override void OnTweakEnabled()
        {
            base.OnTweakEnabled();
            harmony.PatchAll(typeof(TankifyPatches));
        }

        public override void OnTweakDisabled()
        {
            base.OnTweakDisabled();
            harmony.UnpatchSelf();
        }

        public override void OnSceneLoad(Scene scene, LoadSceneMode mode)
        {
            Done.Clear();
        }

        public static class TankifyPatches
        {
            [HarmonyPatch(typeof(BossHealthBar), nameof(BossHealthBar.Awake))]
            [HarmonyPostfix]
            public static void IncreaseBossbar(BossHealthBar __instance)
            {
                GameObject bossbar = __instance.GetFieldValue<GameObject>("bossBar");
                foreach (GameObject slider in bossbar.ChildByName("Panel").ChildByName("Filler").ChildrenList())
                {
                    slider.GetComponent<Slider>().maxValue *= Utils.GetSetting<float>("artiset_tankify_mult");
                }
            }

            [HarmonyPatch(typeof(EnemyIdentifier), nameof(EnemyIdentifier.Awake))]
            [HarmonyPostfix]
            public static void IncreaseHealth(EnemyIdentifier __instance)
            {
                float ToHeal = (__instance.health * Utils.GetSetting<float>("artiset_tankify_mult")) - __instance.health;
                __instance.DeliverDamage(__instance.gameObject, Vector3.zero, Vector3.zero, -1 * ToHeal, false, 0);
            }
        }
    }
}
