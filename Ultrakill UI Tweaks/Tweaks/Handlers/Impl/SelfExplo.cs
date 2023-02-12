using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ULTRAKILLtweaker.Tweaks.Handlers.Impl
{
    public class SelfExplo : TweakHandler
    {
        private Harmony harmony = new Harmony("waffle.ultrakill.UKtweaker.selfexplo");

        public override void OnTweakEnabled()
        {
            base.OnTweakEnabled();
            harmony.PatchAll(typeof(ExploPatches));
        }

        public override void OnTweakDisabled()
        {
            base.OnTweakDisabled();
            harmony.UnpatchSelf();
        }

        public static class ExploPatches
        {
            [HarmonyPatch(typeof(NewMovement), nameof(NewMovement.GetHurt))]
            [HarmonyPostfix]
            public static void ExploOnDie()
            {
                if (Utils.GetSetting<bool>("explorsion") && NewMovement.Instance.hp <= 0)
                {
                    GameObject DeathExplosion = MonoSingleton<GunSetter>.Instance.shotgunPump[0].GetComponent<Shotgun>().explosion;
                    GameObject InstExpl = Instantiate(DeathExplosion, NewMovement.Instance.transform.position, NewMovement.Instance.transform.rotation);

                    foreach (Explosion explosion in InstExpl.GetComponentsInChildren<Explosion>())
                    {
                        explosion.enemyDamageMultiplier = 15f;
                        explosion.maxSize *= 15;
                        explosion.damage = 0;
                    }
                }
            }
        }
    }
}
