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
    public class Fresh : TweakHandler
    {
        private Harmony harmony = new Harmony("waffle.ultrakill.UKtweaker.fresh");
        private float ToRemove = 0;

        public override void OnTweakEnabled()
        {
            base.OnTweakEnabled();
        }

        public override void OnTweakDisabled()
        {
            base.OnTweakDisabled();
        }

        public void Update()
        {
            Dictionary<StyleFreshnessState, float> dict = new Dictionary<StyleFreshnessState, float>()
            {
                { StyleFreshnessState.Fresh, Utils.GetSetting<float>("artiset_fresh_fr") },
                { StyleFreshnessState.Used, Utils.GetSetting<float>("artiset_fresh_us") },
                { StyleFreshnessState.Stale, Utils.GetSetting<float>("artiset_fresh_st") },
                { StyleFreshnessState.Dull, Utils.GetSetting<float>("artiset_fresh_du") }
            };

            NewMovement nm = NewMovement.Instance;

            if (nm != null && StatsManager.Instance.timer && GunControl.Instance.activated)
            {
                ToRemove += dict[StyleHUD.Instance.GetFreshnessState(GunControl.Instance.currentWeapon)] * Time.deltaTime;

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
