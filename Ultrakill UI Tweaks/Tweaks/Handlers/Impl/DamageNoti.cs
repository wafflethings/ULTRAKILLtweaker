using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ULTRAKILLtweaker.Tweaks.Handlers.Impl
{
    public class DamageNoti : TweakHandler
    {
        private Harmony harmony = new Harmony("waffle.ultrakill.UKtweaker.dmgnoti");

        public override void OnTweakEnabled()
        {
            base.OnTweakEnabled();
            harmony.PatchAll(typeof(DmgNotiPatches));
        }

        public override void OnTweakDisabled()
        {
            base.OnTweakDisabled();
            harmony.UnpatchSelf();
        }

        public static class DmgNotiPatches
        {
            [HarmonyPatch(typeof(EnemyIdentifier), nameof(EnemyIdentifier.DeliverDamage))]
            [HarmonyPrefix]
            public static void SetState(EnemyIdentifier __instance, out float __state)
            {
                __state = __instance.health;
            }

            [HarmonyPatch(typeof(EnemyIdentifier), nameof(EnemyIdentifier.DeliverDamage))]
            [HarmonyPostfix]
            public static void Postfix(EnemyIdentifier __instance, float __state)
            {
                float damage = __state - __instance.health;

                if (damage > 0f)
                {
                    string Data = $"{__instance.hitter} -> {__instance.name.Replace("(Clone)", "")}: {Math.Round(damage, 3)}";
                    SubtitleController.Instance.DisplaySubtitle($"<size=10>{Data}</size>");
                }
            }
        }
    }
}
