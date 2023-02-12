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
    public class FloorIsLava : TweakHandler
    {
        private Harmony harmony = new Harmony("waffle.ultrakill.UKtweaker.floorlava");
        private float ToRemove = 0;
        private float OnFloorFor;

        private void Update()
        {
            NewMovement nm = NewMovement.Instance;

            if (nm != null) 
            {
                if (nm.gc.touchingGround)
                {
                    OnFloorFor += Time.deltaTime;
                }
                else
                {
                    OnFloorFor = 0;
                }

                if (StatsManager.Instance.timer && GunControl.Instance.activated && OnFloorFor > Utils.GetSetting<float>("artiset_floorlava_time"))
                {
                    ToRemove += Time.deltaTime * Utils.GetSetting<float>("artiset_floorlava_mult");

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

        public override void OnTweakEnabled()
        {
            base.OnTweakEnabled();
        }

        public override void OnTweakDisabled()
        {
            base.OnTweakDisabled();
            harmony.UnpatchSelf();
        }
    }
}
