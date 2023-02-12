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
    public class FuelLeak : TweakHandler
    {
        private Harmony harmony = new Harmony("waffle.ultrakill.UKtweaker.fuelleak");
        private float ToRemove = 0;

        public override void OnTweakEnabled()
        {
            base.OnTweakEnabled();
        }

        public override void OnTweakDisabled()
        {
            base.OnTweakDisabled();
            harmony.UnpatchSelf();
        }

        public void Update()
        {
            NewMovement nm = NewMovement.Instance;

            if (nm != null && StatsManager.Instance.timer && GunControl.Instance.activated)
            {
                ToRemove += Time.deltaTime * Utils.GetSetting<float>("artiset_fuelleak_multi");

                if ((int)ToRemove >= 1)
                {
                    nm.hp -= (int)ToRemove;
                    ToRemove -= (int)ToRemove;
                }

                if (nm.hp <= 0)
                {
                    nm.GetHurt(int.MaxValue, false, 1, true, true);
                }
            }
        }
    }
}
