using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ULTRAKILLtweaker.Tweaks.Handlers
{
    public class TweakHandler : MonoBehaviour
    {
        public bool WasEnabled { get; private set; }

        public virtual void OnTweakEnabled()
        {
            SceneManager.sceneLoaded += OnSceneLoad;
            WasEnabled = true;
            enabled = true; 
        }

        public virtual void OnTweakDisabled()
        {
            SceneManager.sceneLoaded -= OnSceneLoad;
            WasEnabled = false;
            enabled = false;
        }

        public virtual void OnSceneLoad(Scene scene, LoadSceneMode mode)
        {

        }

        public static bool IsGameplayScene()
        {
            string[] NonGameplay =
            {
                "Main Menu",
                "Level 2-S",
                "Intermission1",
                "Intermission2"
            };

            return !NonGameplay.Contains(SceneManager.GetActiveScene().name);
        }

        // for each setting added, add content height of 80
    }
}
