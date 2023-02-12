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
    public class Submerged : TweakHandler
    {
        private Harmony harmony = new Harmony("waffle.ultrakill.UKtweaker.submerged");

        public override void OnTweakEnabled()
        {
            base.OnTweakEnabled();
            harmony.PatchAll(typeof(SubmergedPatches));
        }

        public override void OnTweakDisabled()
        {
            base.OnTweakDisabled();
            harmony.UnpatchSelf();
        }

        public static class SubmergedPatches
        {
            [HarmonyPatch(typeof(NewMovement), nameof(NewMovement.Start))]
            [HarmonyPostfix]
            public static void SpawnWater(NewMovement __instance)
            {
                if (IsGameplayScene())
                {
                    GameObject water = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    water.name = "UKT WATER!!!";
                    water.AddComponent<Rigidbody>();
                    water.GetComponent<Rigidbody>().isKinematic = true;
                    water.GetComponent<Collider>().isTrigger = true;
                    water.AddComponent<Water>();
                    water.GetComponent<Water>().bubblesParticle = new GameObject();
                    water.GetComponent<Water>().clr = new Color(0, 0.5f, 1);
                    water.GetComponent<MeshRenderer>().enabled = false;
                    water.transform.localScale = Vector3.one * 10000000000; // I think this should be big enough
                }
            }

            [HarmonyPatch(typeof(Water), nameof(Water.Start))]
            [HarmonyPrefix]
            public static void DisableOtherWaters(Water __instance)
            {
                if(__instance.gameObject.name != "UKT WATER!!!")
                {
                    __instance.enabled = false;
                }
            }

            [HarmonyPatch(typeof(BloodsplatterManager), nameof(BloodsplatterManager.GetGore))]
            [HarmonyPrefix]
            public static void MakeUnderwater(ref bool isUnderwater)
            {
                isUnderwater = true;
            }
        }
    }
}
