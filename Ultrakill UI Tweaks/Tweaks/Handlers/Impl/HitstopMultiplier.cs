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
    public class HitstopMultiplier : TweakHandler
    {
        private Harmony harmony = new Harmony("waffle.ultrakill.UKtweaker.hitstopmult");

        public override void OnTweakEnabled()
        {
            base.OnTweakEnabled();
            if (Utils.GetSetting<bool>("parryflashoff"))
                CanvasController.Instance.gameObject.ChildByName("ParryFlash").GetComponent<Image>().enabled = false;
            harmony.PatchAll(typeof(HitstopPatches));
        }

        public override void OnTweakDisabled()
        {
            base.OnTweakDisabled();
            CanvasController.Instance.gameObject.ChildByName("ParryFlash").GetComponent<Image>().enabled = true;
            harmony.UnpatchSelf();
        }

        public override void OnSceneLoad(Scene scene, LoadSceneMode mode)
        { 
            if(Utils.GetSetting<bool>("parryflashoff"))
                CanvasController.Instance.gameObject.ChildByName("ParryFlash").GetComponent<Image>().enabled = false;
        }

        public static class HitstopPatches
        {
            [HarmonyPatch(typeof(TimeController), nameof(TimeController.HitStop))]
            [HarmonyPrefix]
            static void PatchHitstop(ref float length)
            {
                length = Utils.GetSetting<float>("hitstopmult") * length;
            }

            [HarmonyPatch(typeof(TimeController), nameof(TimeController.TrueStop))]
            [HarmonyPrefix]
            static void PatchTruestop(ref float length)
            {
                length = Utils.GetSetting<float>("truestopmult") * length;
            }

            [HarmonyPatch(typeof(TimeController), nameof(TimeController.SlowDown))]
            [HarmonyPrefix]
            static void PatchSlowdown(ref float amount)
            {
                amount = Utils.GetSetting<float>("slowdownmult") * amount;
            }
        }
    }
}
