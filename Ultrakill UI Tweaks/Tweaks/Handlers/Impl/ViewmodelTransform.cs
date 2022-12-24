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
    public class ViewmodelTransform : TweakHandler
    {
        private Harmony harmony = new Harmony("waffle.ultrakill.UKtweaker.vmtrans");
        private NewMovement nm;

        public override void OnTweakEnabled()
        {
            base.OnTweakEnabled();
            harmony.PatchAll(typeof(ViewmodelPatches));

            GunControl.Instance.GetComponent<WalkingBob>().enabled = Utils.GetSetting<bool>("nobob");
            GunControl.Instance.GetComponent<RotateToFaceFrustumTarget>().enabled = Utils.GetSetting<bool>("notilt");
        }

        public override void OnTweakDisabled()
        {
            base.OnTweakDisabled();
            harmony.UnpatchSelf();
        }

        public override void OnSceneLoad(Scene scene, LoadSceneMode mode)
        {
            nm = NewMovement.Instance;
        }

        public void LateUpdate()
        {
            if (Utils.GetSetting<float>("vmfov") != 90)
            {
                if (nm != null)
                    nm.gameObject.ChildByName("Main Camera").ChildByName("HUD Camera").GetComponent<Camera>().fieldOfView = Utils.GetSetting<float>("vmfov");
            }
        }


        public static class ViewmodelPatches
        {
            [HarmonyPatch(typeof(WeaponPos), nameof(WeaponPos.CheckPosition))]
            [HarmonyPostfix]
            static void PatchWeaponScale_Check(WeaponPos __instance)
            {
                if (__instance.transform.parent.name == "Guns")
                {
                    if (__instance.gameObject.name.Contains("Revolver"))
                    {
                        __instance.gameObject.transform.localScale *= Utils.GetSetting<float>("vmmodel");
                    }
                }
            }

            [HarmonyPatch(typeof(WeaponPos), nameof(WeaponPos.Start))]
            [HarmonyPostfix]
            static void PatchWeaponScale_Start(WeaponPos __instance)
            {
                if (__instance.transform.parent.name == "Guns")
                {
                    if (__instance.gameObject.name.Contains("Revolver"))
                    {
                        // __instance.gameObject.transform.localScale *= Utils.GetSetting<float>("vmmodel");
                    }
                    else
                    {
                        foreach (GameObject child in __instance.gameObject.ChildrenList())
                        {
                            if (child.activeSelf && !child.name.Contains("ShootPoint"))
                                child.transform.localScale *= Utils.GetSetting<float>("vmmodel");
                        }
                    }
                }
            }
        }
    }
}
