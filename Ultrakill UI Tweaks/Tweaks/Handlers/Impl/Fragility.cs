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
    public class Fragility : TweakHandler
    {
        private Harmony harmony = new Harmony("waffle.ultrakill.UKtweaker.fragility");

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

            if (nm != null)
            {
                nm.antiHp = 100 - Utils.GetSetting<float>("artiset_noHP_hpamount");
                if (nm.hp > Utils.GetSetting<float>("artiset_noHP_hpamount"))
                {
                    nm.ForceAntiHP((int)(100 - Utils.GetSetting<float>("artiset_noHP_hpamount")));
                }
            }
        }
    }
}
