using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ULTRAKILLtweaker.Tweaks.Handlers.Impl
{
    public class FPSCounter : TweakHandler
    {
        public void OnGUI()
        {
            if (Utils.GetSetting<bool>("fpscounter") && MainClass.Instance.SettingsInit)
            {
                float FPS = 1.00f / Time.unscaledDeltaTime;
                GUI.Label(new Rect(3, 0, 100, 100), "FPS: " + ((int)FPS));
            }
        }
    }
}
