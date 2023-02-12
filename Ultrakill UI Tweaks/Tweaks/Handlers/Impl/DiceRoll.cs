using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ULTRAKILLtweaker.Tweaks.UIElements;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ULTRAKILLtweaker.Tweaks.Handlers.Impl
{
    public class DiceRoll : TweakHandler
    {
        private Harmony harmony = new Harmony("waffle.ultrakill.UKtweaker.diceroll");
        private float Timer = 0;
        private GameObject Panel;

        public override void OnTweakEnabled()
        {
            base.OnTweakEnabled();
            harmony.PatchAll(typeof(DicePatches));
        }

        public override void OnTweakDisabled()
        {
            base.OnTweakDisabled();
            harmony.UnpatchSelf();
        }

        public void Update()
        {
            if (IsGameplayScene())
            {
                Timer += Time.deltaTime;

                if (Timer > Utils.GetSetting<float>("artiset_diceroll_timereset"))
                {
                    Timer = 0;
                    GunSetter.Instance.ResetWeapons();
                }

                if (Panel == null)
                {
                    Panel = Instantiate(UIElement.Settings["DicerollCanvas"]);
                }
                else
                {
                    string joe = TimeSpan.FromSeconds(Utils.GetSetting<float>("artiset_diceroll_timereset") - Timer).ToString();

                    joe = joe.Substring(3);
                    joe = joe.Replace("0000", "");

                    Panel.ChildByName("Panel").ChildByName("Time").GetComponent<Text>().text = joe;
                }
            }
        }

        public static class DicePatches
        {
            [HarmonyPatch(typeof(GunSetter), nameof(GunSetter.ResetWeapons))]
            [HarmonyPostfix]
            public static void Postfix(GunSetter __instance)
            {
                List<GameObject>[] slots = new List<GameObject>[5]
                    {
                        __instance.gunc.slot1,
                        __instance.gunc.slot2,
                        __instance.gunc.slot3,
                        __instance.gunc.slot4,
                        __instance.gunc.slot5
                    };

                List<GameObject> AllWeapons = new List<GameObject>()
                    {
                        __instance.rocketBlue[0],
                        __instance.nailMagnet[0],
                        __instance.nailOverheat[0],
                        __instance.nailMagnet[1],
                        __instance.nailOverheat[1],
                        __instance.railCannon[0],
                        __instance.railHarpoon[0],
                        __instance.railMalicious[0],
                        __instance.revolverPierce[0],
                        __instance.revolverRicochet[0],
                        __instance.revolverPierce[1],
                        __instance.revolverRicochet[1],
                        __instance.shotgunGrenade[0],
                        __instance.shotgunPump[0],
                    };

                int Slot = 0;

                foreach (List<GameObject> list in slots)
                {
                    list.Clear();
                }

                while (AllWeapons.Count != 0)
                {
                    int r = UnityEngine.Random.Range(0, AllWeapons.Count);
                    slots[Slot].Add(Instantiate<GameObject>(AllWeapons[r], __instance.transform));
                    AllWeapons.Remove(AllWeapons[r]);

                    Slot++;

                    if (Slot == slots.Count())
                        Slot = 0;
                }
            }
        }
    }
}
