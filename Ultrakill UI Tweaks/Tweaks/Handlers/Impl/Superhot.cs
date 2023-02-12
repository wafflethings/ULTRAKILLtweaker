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
    public class Superhot : TweakHandler
    {
        private Harmony harmony = new Harmony("waffle.ultrakill.UKtweaker.superhot");

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
            if (IsGameplayScene())
            {
                float VelocityFor1 = 25f; // The amount of velocity needed to make timescale 1
                float Min = 0.01f;
                float Max = 1.5f;
                float LerpSpeed = 15f;

                float thing = PlayerTracker.Instance.GetRigidbody().velocity.magnitude / VelocityFor1;

                if (thing > Max)
                    thing = Max;

                if (thing < Min)
                    thing = Min;

                Time.timeScale = Mathf.Lerp(Time.timeScale, thing, Time.deltaTime * LerpSpeed);
            }
        }
    }
}
