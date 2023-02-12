using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ULTRAKILLtweaker.Tweaks.Handlers.Impl
{
    public class Florpification : TweakHandler
    {
        private Harmony harmony = new Harmony("waffle.ultrakill.UKtweaker.florpy :3");
        private static Dictionary<ItemType, GameObject> Florps;

        public override void OnTweakEnabled()
        {
            base.OnTweakEnabled();

            if(Florps == null)
            {
                Florps = new Dictionary<ItemType, GameObject>()
                {
                    { ItemType.SkullBlue, MainClass.UIBundle.LoadAsset<GameObject>("Blue Florp") },
                    { ItemType.SkullRed, MainClass.UIBundle.LoadAsset<GameObject>("Red Florp") }
                };

                foreach(GameObject go in Florps.Values)
                {
                    go.GetComponentInChildren<Renderer>().material.shader = Shader.Find("psx/unlit/ambient");
                }
            }

            harmony.PatchAll(typeof(FlorpyPatches));
        }

        public override void OnTweakDisabled()
        {
            base.OnTweakDisabled();
            harmony.UnpatchSelf();
        }

        public static class FlorpyPatches
        {
            [HarmonyPatch(typeof(Skull), "Start")]
            [HarmonyPostfix]
            public static void Postfix(Skull __instance)
            {
                Renderer renderer = __instance.gameObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.enabled = false;
                    Instantiate(Florps[__instance.GetComponent<ItemIdentifier>().itemType], renderer.transform);
                }
            }
        }
    }
}
