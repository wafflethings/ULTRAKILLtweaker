using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ULTRAKILLtweaker.Tweaks.Handlers.Impl
{
    public class CGMusic : TweakHandler
    {
        public static CGMusic Instance;

        private Harmony harmony = new Harmony("waffle.ultrakill.UKtweaker.cgmusic");

        public List<AudioClip> Music;
        public List<AudioClip> MusicPool;

        private AudioClip LastClip;
        public AudioSource Source;

        public Coroutine MCL;

        public override void OnTweakEnabled()
        {
            base.OnTweakEnabled();
            Instance = this;
            harmony.PatchAll(typeof(CGMusicPatches));
        }

        public override void OnTweakDisabled()
        {
            base.OnTweakDisabled();
            harmony.UnpatchSelf();
        }

        public override void OnSceneLoad(Scene scene, LoadSceneMode mode)
        {
            if(MCL != null)
            {
                StopCoroutine(MCL);
            }

            if (SceneManager.GetActiveScene().name == "Endless")
            {
                Music = GetClipsFromFolder();
                MusicPool = Music;
                Source = new GameObject("ULTRAKILLtweaker: AUDIO MANAGER").AddComponent<AudioSource>();
                StartCoroutine(MusLoopWhenWaveCount());
            }
        }

        public static List<AudioClip> GetClipsFromFolder()
        {
            string[] allFiles = Directory.GetFiles(Path.Combine(Utils.GameDirectory(), @"BepInEx\UMM Mods\ULTRAKILLtweaker\Cybergrind Music"));

            string[] supportedFileExtensions = new string[]
            {
                ".mp3",
                ".ogg",
                ".wav",
                ".aiff",
                ".mod",
                ".it",
                ".s3m",
                ".xm"
            };

            List<AudioClip> clips = new List<AudioClip>();

            foreach (string file in allFiles)
            {
                foreach (string fileext in supportedFileExtensions)
                {
                    if (file.EndsWith(fileext))
                    {
                        WWW www = new WWW("file:///" + file);
                        while (!www.isDone)
                        {
                        }
                        clips.Add(www.GetAudioClip());
                    }
                }
            }

            Debug.Log($"AudioClips found, {clips.Count()}.");
            return clips;
        }

        public AudioClip RandomClip()
        {
            if (MusicPool.Count == 0)
            {
                MusicPool = Music;
            }
            LastClip = MusicPool[UnityEngine.Random.Range(0, MusicPool.Count)];
            MusicPool.Remove(LastClip);
            return LastClip;
        }

        public IEnumerator MusLoopWhenWaveCount()
        {
            while (GameObject.Find("Wave Number") == null || GameObject.Find("Wave Number").activeSelf == false)
            {
                yield return null;
            }
            MCL = StartCoroutine(MusicCheckLoop());
            Debug.Log("Starting Music Check Loop.");
        }

        private IEnumerator MusicCheckLoop()
        {
            while (true)
            {
                if (!Source.isPlaying)
                {
                    if (GameObject.Find("Wave Number") != null)
                    {
                        Source.PlayOneShot(RandomClip());
                    }
                    else
                    {
                        yield break;
                    }
                    yield return null;
                }
                else
                {
                    yield return null;
                }
            }
        }

        public class CGMusicPatches
        {
            [HarmonyPatch(typeof(ActivateOnSoundEnd), nameof(ActivateOnSoundEnd.Start))]
            [HarmonyPrefix]
            public static void DestroyOGSong(ActivateOnSoundEnd __instance)
            {
                if(SceneManager.GetActiveScene().name == "Endless" && __instance.name == "Intro")
                {
                    Destroy(__instance.gameObject);
                }
            }
        }
    }
}
