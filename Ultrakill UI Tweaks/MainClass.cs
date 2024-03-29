﻿using FallFactory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.IO;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.AI;
using UMM;

namespace ULTRAKILLtweaker
{
    [UKPlugin("ULTRAKILLtweaker", "1.0.0", "Tweak your Ultrakill, with extra settings and game modifiers.\n\nCreated by Waffle.", true, true)]
    public class MainClass : UKMod
    {
        #region Variables
        public static MainClass Instance;

        // UI elements.
        public GameObject TweakerButton;
        public GameObject OptionsMenu;
        public GameObject TweakerMenu;
        public GameObject CyberGrind;
        public GameObject DiceRoll;
        public GameObject Speedometer;
        public GameObject CustomWeaponPanel;
        public GameObject PlayerInfo;
        public GameObject DPSPanel;
        public List<GameObject> Panels = new List<GameObject>(); // This is the panel objects, e.g speedometer and weapons

        // Stuff that handles the pages for the tweaks.
        public List<GameObject> Pages = new List<GameObject>();
        public GameObject Modifiers;
        public static int Page = 0;

        // The assetbundle which all of the UKt assets are from.
        AssetBundle UIBundle;

        // This will be true until Init happens, then it is set to false. If it is true, InitSceneLoad will be called on ResetWeapons.
        public static bool HasntHappenedThisScene;

        // The current instance of the RandomiseEvery30. The instance needs to be called to stop the coroutine with StopCoroutine.
        Coroutine CurrentRandom;
        // Current musiccheckloop
        Coroutine MCL;
        // Same for SetAfterTime.
        Coroutine SetAfterTime_INST;

        // The current instance of the things.
        public StatsManager statman;
        NewMovement nm;
        GameObject player;
        Harmony harmony;

        // This is set to DateTime.Now + 30s on DiceRoll reset, the countdown uses this to tick down.
        DateTime NextReset;

        // The volume slider, this is needed to set custom Cyber Grind music to the correct volume.
        Slider volslider;

        // NM.hp is an int, and the amount removed per frame is a small float. Therefore, we increment ToRemove by the small amount, until it is 1.
        // 1 can be removed from the int, so then we take 1 from the HP and ToRemove.
        public float ToRemove_FL; // for fuel leak
        public float ToRemove_FIL; // for floorislava
        public float ToRemove_FR; // for fresh

        // Some mods need a scene reload to work, so if mods are changed in game this is set to true. If it is true on the option menu being turned off, the scene reloads.
        public bool ModsChanged = false;

        // Have settings initialised? Needed so that idToSetting isn't null. 
        public bool SettingsInit = false;

        // I don't remember but it doesn't need changing 
        bool HasFirstPatch = false;
        bool NotFirstLoad = false;

        // All hits that have occured in the last Second
        public List<Hit> HitsSecond = new List<Hit>();

        public bool ShouldDamage_FIL = false;

        public StyleFreshnessState Freshness;

        #endregion

        public void OnGUI()
        {
            if (SettingsInit)
            {
                if (Utils.GetSetting<bool>("fpscounter"))
                {
                    float FPS = 1.00f / Time.unscaledDeltaTime;
                    GUI.Label(new Rect(3, 0, 100, 100), "FPS: " + ((int)FPS));
                }
            }
        }

        #region UMM stuff, handles loading and unloading.
        public override void OnModLoaded()
        {
            Instance = this;

            // Patch stuff.
            harmony = new Harmony("waffle.ultrakill.UKtweaker");
            harmony.PatchAll();

            SceneManager.sceneLoaded += OnSceneWasLoaded;

            // OnSceneWasLoaded is where patching happens, but if it is not called (e.g when the mod is not on at startup) it is not called on time. We have to do it now.
            if (SceneManager.GetActiveScene().name == "Main Menu")
            {
               OnSceneWasLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
            }

            ResourcePack.GetAllPacks();
        }

        public override void OnModUnload()
        {
            // Unpatch Harmony, destroy GOs from UKt, remove events set, all the stuff that happens on startup.
            harmony.UnpatchSelf();

            List<GameObject> ToDestroy = new List<GameObject>()
            {
                TweakerButton, TweakerMenu, CyberGrind, DiceRoll, Speedometer, CustomWeaponPanel, PlayerInfo, DPSPanel
            };

            foreach(GameObject go in ToDestroy)
            {
                GameObject.Destroy(go);
            }

            UIBundle.Unload(true);
            UIBundle = null;
            SceneManager.sceneLoaded -= OnSceneWasLoaded;
        }
        #endregion

        #region Menu enable and disable events
        public static void MenuEnable()
        {
            SettingRegistry.Read();
        }

        public static void MenuDisable()
        {
            SettingRegistry.Save();

            if(Instance.ModsChanged && SceneManager.GetActiveScene().name != "Main Menu")
                MonoSingleton<CanvasController>.Instance.gameObject.ChildByName("PauseMenu").ChildByName("Restart Mission").GetComponent<Button>().onClick.Invoke();
        }
        #endregion

        #region Events
        public void OnSceneWasLoaded(Scene scene, LoadSceneMode mode)
        {
            SettingsInit = false;
            ModsChanged = false;
            HasntHappenedThisScene = true;

            // Load the assetbundle if it isn't loaded.
            if (UIBundle == null)
            {
                UIBundle = AssetBundle.LoadFromFile(Path.Combine(Utils.GameDirectory(), @"BepInEx\UMM Mods\ULTRAKILLtweaker\tweakerassets.bundle"));
            }

            // Set the statman - it is used for various things, doesn't exist in Main Menu though
            if (scene.name != "Main Menu")
            {
                statman = GameObject.Find("StatsManager").GetComponent<StatsManager>();
            } else
            {
                Time.timeScale = 1;
            }

            if (scene.name != "Main Menu" && scene.name != "Intro")
            {
                NotFirstLoad = true;
            }

            // There is probably a better way to do this than GO.Find. This adds the UKt UI elements to the UI.
            if (GameObject.Find("Canvas") != null)
            {
                if (!HasFirstPatch || scene.name != "Main Menu" || NotFirstLoad) 
                {
                    HasFirstPatch = true;
                    StartCoroutine(PatchOptionsMenu());
                }
            }

            UKAPI.RemoveDisableCyberGrindReason($"UKTW: CG score tweak disabled");

            // If the player is in CG, create the AudioSource for custom music, and check if a CG-illegal mod is enabled.
            if (SceneManager.GetActiveScene().name == "Endless")
            {
                audiosource = new GameObject("ULTRAKILLtweaker: AUDIO MANAGER").AddComponent<AudioSource>();

                foreach(Setting set in SettingRegistry.settings)
                {
                    if(set.GetType() == typeof(ArtifactSetting))
                    {
                        ArtifactSetting arti = (ArtifactSetting)set;
                        if (arti.DisableCG) 
                        {
                            Debug.Log($"Remove UKTW: Modifier {set.ID} enabled");
                            UKAPI.RemoveDisableCyberGrindReason($"UKTW: Modifier {set.ID} enabled");
                            if (!Convert.ToBoolean(arti.value))
                            {
                                UKAPI.DisableCyberGrindSubmission($"UKTW: Modifier {set.ID} enabled");
                                Debug.Log($"Add UKTW: Modifier {set.ID} enabled");
                            }
                        }
                    }
                }
            }

            StartCoroutine(ResourcePack.PatchTextures());
        }

        public void OnEnemyDamage(EnemyIdentifier eid, float dmg)
        {
            if (Utils.GetSetting<bool>("dmgsub"))
            {
                string Data = $"Damage done from {eid.hitter} to {eid.name.Replace("(Clone)", "")}: {Math.Round(dmg, 3)}";
                Debug.Log(Data);
                MonoSingleton<SubtitleController>.Instance.DisplaySubtitle($"<size=10>{Data}</size>");
            }

            HitsSecond.Add(new Hit(eid, dmg));
        }

        public void TouchingGroundChanged(GroundCheck check)
        {
            // Debug.Log($"GROUND CHECK CHANGED: {check.touchingGround}.");

            if(check.touchingGround)
            {
                SetAfterTime_INST = StartCoroutine(SetAfterTime(Utils.GetSetting<float>("artiset_floorlava_time")));
            } else
            {
                ShouldDamage_FIL = false;
                StopCoroutine(SetAfterTime_INST);
            }
        }

        public IEnumerator SetAfterTime(float time)
        {
            yield return new WaitForSeconds(time);

            if(player.ChildByName("GroundCheck").GetComponent<GroundCheck>().touchingGround)
            {
                ShouldDamage_FIL = true;
            } else
            {
                ShouldDamage_FIL = false;
            }
        }

        #endregion

        #region Stuff to do with making the extra UKt options menu stuff work
        public IEnumerator PatchOptionsMenu()
        {
            yield return null;

            while(MonoSingleton<CanvasController>.Instance.gameObject.ChildByName("OptionsMenu") == null)
            {
                yield return null;
            }

            // Set the OptionsMenu to the one that was found.
            OptionsMenu = MonoSingleton<CanvasController>.Instance.gameObject.ChildByName("OptionsMenu");
            OptionsMenu.SetActive(true);
            Debug.Log($"Canvas with OptionsMenu found: {OptionsMenu.transform.parent.name}.");

            // Duplicate the save button.
            TweakerButton = Instantiate(OptionsMenu.ChildByName("Saves"));

            // Load all the UI from AssetBundle and disable it.
            TweakerMenu = Instantiate(UIBundle.LoadAsset<GameObject>("Canvas"));
            if (CyberGrind == null)
                CyberGrind = Instantiate(UIBundle.LoadAsset<GameObject>("GrindCanvas"));
            if (DiceRoll == null)
                DiceRoll = Instantiate(UIBundle.LoadAsset<GameObject>("DicerollCanvas"));
            if (Speedometer == null)
                Speedometer = Instantiate(UIBundle.LoadAsset<GameObject>("Speedometer"));
            if (CustomWeaponPanel == null)
                CustomWeaponPanel = Instantiate(UIBundle.LoadAsset<GameObject>("Weapons"));
            if(PlayerInfo == null)
                PlayerInfo = Instantiate(UIBundle.LoadAsset<GameObject>("Info"));
            if(DPSPanel == null)
                DPSPanel = Instantiate(UIBundle.LoadAsset<GameObject>("DPS"));

            CyberGrind.SetActive(false);
            DiceRoll.SetActive(false);
            Speedometer.SetActive(false);
            CustomWeaponPanel.SetActive(false);
            PlayerInfo.SetActive(false);
            DPSPanel.SetActive(false);

            // Set up OnClick for the tweaker button, as it is a clone of the Save Slot button it still enables the save slots, so it needs to be disabled it.
            // I tried clearing the listeners, but it still showed the Save Slots. Bad code, but it works, so don't touch.
            TweakerButton.name = "Tweaker Button";
            TweakerButton.ChildByName("Text").GetComponent<Text>().text = "TWEAKER OPTIONS";
            TweakerButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                TweakerMenu.SetActive(true);
                OptionsMenu.ChildByName("Save Slots").SetActive(false);
            });

            // This makes all of the other buttons in options disable the tweak menu.
            foreach (GameObject go in OptionsMenu.ChildrenList())
            {
                if (go.GetComponent<Button>() != null && go != TweakerButton)
                {
                    go.GetComponent<Button>().onClick.AddListener(() =>
                    {
                        TweakerMenu.SetActive(false);
                    });
                }
            }

            // Not exactly sure why this is needed, but the HudOpenEffect makes the button go all weird and stretch, so it needs to be removed.
            Destroy(TweakerButton.GetComponent<HudOpenEffect>());

            // SetParent is meant to do the same thing as setting trans.parent, but it doesn't work (for me) so I have to do this dumb stuff.
            TweakerButton.transform.parent = OptionsMenu.transform;
            TweakerMenu.transform.parent = OptionsMenu.transform;
            TweakerButton.transform.localScale = Vector3.one;
            TweakerMenu.transform.localScale = Vector3.one;

            // Define which pages are used
            Pages = new List<GameObject>()
            {
                TweakerMenu.ChildByName("Tweaks").ChildByName("Misc"),
                TweakerMenu.ChildByName("Tweaks").ChildByName("Misc HUD"),
                TweakerMenu.ChildByName("Tweaks").ChildByName("UI Panels"),
                TweakerMenu.ChildByName("Tweaks").ChildByName("Cybergrind"),
                TweakerMenu.ChildByName("Tweaks").ChildByName("Fun")
            };

            GameObject mods = TweakerMenu.ChildByName("Modifiers");

            #region Register, load, all the settings. 
            SettingRegistry.settings.Clear();
            SettingRegistry.idToSetting.Clear();

            SettingRegistry.settings.Add(new SliderSetting("hitstopmult", Pages[0].PageContent().ChildByName("Hitstop Multiplier"), 0, 2, 1, false, "{0}x", true));
            SettingRegistry.settings.Add(new ToggleSetting("seeviewmodel", Pages[0].PageContent().ChildByName("No Viewmodel"), false, true));
            SettingRegistry.settings.Add(new ToggleSetting("dmgsub", Pages[0].PageContent().ChildByName("Damage Sign"), false, true));
            SettingRegistry.settings.Add(new ToggleSetting("nobob", Pages[0].PageContent().ChildByName("No Viewmodel Bob"), false, true));
            SettingRegistry.settings.Add(new ToggleSetting("notilt", Pages[0].PageContent().ChildByName("No Assist Tilt"), false, true));

            SettingRegistry.settings.Add(new SliderSetting("uiscalecanv", Pages[1].PageContent().ChildByName("UI Scale (Canvas)"), 0, 100, 100, true, "{0}%", true));
            SettingRegistry.settings.Add(new SliderSetting("uiscale", Pages[1].PageContent().ChildByName("UI Scale (HP)"), 0, 110, 100, true, "{0}%", true));
            SettingRegistry.settings.Add(new SliderSetting("uiscalestyle", Pages[1].PageContent().ChildByName("UI Scale (Style)"), 0, 110, 100, true, "{0}%", true));
            SettingRegistry.settings.Add(new SliderSetting("uiscaleresults", Pages[1].PageContent().ChildByName("UI Scale (Results)"), 0, 100, 100, true, "{0}%", true));
            SettingRegistry.settings.Add(new SliderSetting("uiscaleboss", Pages[1].PageContent().ChildByName("BossbarSc"), 0, 100, 100, true, "{0}%", true));
            SettingRegistry.settings.Add(new ToggleSetting("forcegun", Pages[1].PageContent().ChildByName("Force Gun Modal"), false, true));
            SettingRegistry.settings.Add(new ToggleSetting("fpscounter", Pages[1].PageContent().ChildByName("FPS"), false, true));

            SettingRegistry.settings.Add(new ToggleSetting("hppanel", Pages[2].PageContent().ChildByName("Info Panel"), false, true));
            SettingRegistry.settings.Add(new ToggleSetting("weapanel", Pages[2].PageContent().ChildByName("Weapon Panel"), false, true));
            SettingRegistry.settings.Add(new ToggleSetting("dps", Pages[2].PageContent().ChildByName("DPS panel"), false, true));
            SettingRegistry.settings.Add(new ToggleSetting("speedometer", Pages[2].PageContent().ChildByName("Speedometer"), false, true));

            SettingRegistry.settings.Add(new ToggleSetting("cybergrindstats", Pages[3].PageContent().ChildByName("Cybergrind Stats"), false, true));
            SettingRegistry.settings.Add(new ToggleSetting("cybergrindmusic", Pages[3].PageContent().ChildByName("CybergrindMusic"), false, true));
            SettingRegistry.settings.Add(new ToggleSetting("cybergrindnoscores", Pages[3].PageContent().ChildByName("Disable Scores"), false, true));

            SettingRegistry.settings.Add(new ToggleSetting("explorsion", Pages[4].PageContent().ChildByName("ExplodeDeath"), false, true));
            SettingRegistry.settings.Add(new ToggleSetting("legally_distinct_florp", Pages[4].PageContent().ChildByName("FlorpSkull"), false, true));

            SettingRegistry.settings.Add(new ArtifactSetting("ARTIFACT_sandify", mods.ChildByName("Sandify"), false, true, "Sandify", "Every enemy gets covered in sand. Parrying is the only way to heal."));
            SettingRegistry.settings.Add(new ArtifactSetting("ARTIFACT_noHP", mods.ChildByName("Fragility"), false, true, "Fragility", "You only have 1 HP - if you get hit, you die."));
            SettingRegistry.settings.Add(new ArtifactSetting("ARTIFACT_glass", mods.ChildByName("Glass"), true, true, "Glass", "Deal two times the damage - at the cost of 70% of your health."));
            SettingRegistry.settings.Add(new ArtifactSetting("ARTIFACT_superhot", mods.ChildByName("Superhot"), true, true, "UltraHot", "Time only moves when you move."));
            SettingRegistry.settings.Add(new ArtifactSetting("ARTIFACT_tank", mods.ChildByName("Tankify"), false, true, "Tankify", "Every enemy gets two times the health."));
            SettingRegistry.settings.Add(new ArtifactSetting("ARTIFACT_distance", mods.ChildByName("Distance"), false, true, "Close Quarters", "Enemies become blessed when too far."));
            SettingRegistry.settings.Add(new ArtifactSetting("ARTIFACT_noweapons", mods.ChildByName("No Weapons"), false, true, "Empty Handed", "No weapons, punch your enemies to death. Good luck beating P-1."));
            SettingRegistry.settings.Add(new ArtifactSetting("ARTIFACT_nostamina", mods.ChildByName("NoStamina"), false, true, "Lethargy", "V1 is tired, and has no stamina. No sliding, dash-jumps, or power-slams."));
            SettingRegistry.settings.Add(new ArtifactSetting("ARTIFACT_diceroll", mods.ChildByName("Random"), true, true, "Dice-Roll", "Every 30 seconds, your weapon loadout is randomised. Includes scrapped and unowned weapons! (if the current update has any)"));
            SettingRegistry.settings.Add(new ArtifactSetting("ARTIFACT_water", mods.ChildByName("Water"), true, true, "Submerged", "Every level is flooded with water."));
            SettingRegistry.settings.Add(new ArtifactSetting("ARTIFACT_gofast", mods.ChildByName("GoFast"), true, true, "Speed", "You run at 2 times the speed. Enemy speed is multiplied by 7.5 to keep up."));
            SettingRegistry.settings.Add(new ArtifactSetting("ARTIFACT_noarm", mods.ChildByName("No Arms"), false, true, "Disarmed", "V1 has no arms. You can't punch, whiplash, or parry."));
            SettingRegistry.settings.Add(new ArtifactSetting("ARTIFACT_fuelleak", mods.ChildByName("Fuel Leak"), false, true, "Fuel Leak", "Blood is actually fuel, and gets used over time. Heal before all of your HP runs out."));
            SettingRegistry.settings.Add(new ArtifactSetting("ARTIFACT_whiphard", mods.ChildByName("WhipFix"), true, true, "Whiplash Fix", "Reduce hard damage from whiplash use, or get rid of it entirely."));
            SettingRegistry.settings.Add(new ArtifactSetting("ARTIFACT_floorlava", mods.ChildByName("FloorIsLava"), false, true, "Floor Is Lava", "You are damaged when grounded. Based on a mod by <b>nptnk#0001</b>."));
            SettingRegistry.settings.Add(new ArtifactSetting("ARTIFACT_mitosis", mods.ChildByName("Mitosis"), true, true, "Mitosis", "Enemies are duplicated. You can go above 10x by editing <b>settings.kel</b>. <color=red>THIS WILL SLAUGTHER YOUR FPS.</color> Idea from Vera in the UK Discord."));
            SettingRegistry.settings.Add(new ArtifactSetting("ARTIFACT_fresh", mods.ChildByName("Fresh"), true, true, "Freshness", "You get hurt whenever your style rank is below a certain amount. Very configurable."));

            SettingRegistry.settings.Add(new SliderSetting("artiset_fuelleak_multi", mods.ChildByName("Fuel Leak").ChildByName("Extra Settings").ChildByName("SETTINGS").ChildByName("Panel").ChildByName("Damage Drain"), 0.1f, 2, 1, false, "{0}x"));
            SettingRegistry.settings.Add(new SliderSetting("artiset_noHP_hpamount", mods.ChildByName("Fragility").ChildByName("Extra Settings").ChildByName("SETTINGS").ChildByName("Panel").ChildByName("HP"), 1, 100, 1, true, "{0} HP"));
            SettingRegistry.settings.Add(new SliderSetting("artiset_diceroll_timereset", mods.ChildByName("Random").ChildByName("Extra Settings").ChildByName("SETTINGS").ChildByName("Panel").ChildByName("Time"), 5, 300, 30, true, "{0}s"));
            SettingRegistry.settings.Add(new SliderSetting("artiset_distance_distfromplayer", mods.ChildByName("Distance").ChildByName("Extra Settings").ChildByName("SETTINGS").ChildByName("Panel").ChildByName("Distance"), 5, 50, 15, true, "{0} u"));
            SettingRegistry.settings.Add(new SliderSetting("artiset_gofast_player", mods.ChildByName("GoFast").ChildByName("Extra Settings").ChildByName("SETTINGS").ChildByName("Panel").ChildByName("Player"), 0.5f, 7.5f, 2, false, "{0}x"));
            SettingRegistry.settings.Add(new SliderSetting("artiset_gofast_enemy", mods.ChildByName("GoFast").ChildByName("Extra Settings").ChildByName("SETTINGS").ChildByName("Panel").ChildByName("Enemy"), 0.5f, 7.5f, 7.5f, false, "{0}x"));
            SettingRegistry.settings.Add(new SliderSetting("artiset_tankify_mult", mods.ChildByName("Tankify").ChildByName("Extra Settings").ChildByName("SETTINGS").ChildByName("Panel").ChildByName("Mult"), 1f, 10f, 2f, false, "{0}x"));
            SettingRegistry.settings.Add(new SliderSetting("artiset_whip_hard_mult", mods.ChildByName("WhipFix").ChildByName("Extra Settings").ChildByName("SETTINGS").ChildByName("Panel").ChildByName("Mult"), 0f, 2f, 0.5f, false, "{0}x"));
            SettingRegistry.settings.Add(new SliderSetting("artiset_floorlava_mult", mods.ChildByName("FloorIsLava").ChildByName("Extra Settings").ChildByName("SETTINGS").ChildByName("Panel").ChildByName("Mult"), 0.1f, 20, 1, false, "{0}x"));
            SettingRegistry.settings.Add(new SliderSetting("artiset_floorlava_time", mods.ChildByName("FloorIsLava").ChildByName("Extra Settings").ChildByName("SETTINGS").ChildByName("Panel").ChildByName("Wait"), 0f, 5, 0, false, "{0}s"));
            SettingRegistry.settings.Add(new SliderSetting("artiset_mitosis_amount", mods.ChildByName("Mitosis").ChildByName("Extra Settings").ChildByName("SETTINGS").ChildByName("Panel").ChildByName("Mult"), 2, 10, 2, true, "{0}x"));
            SettingRegistry.settings.Add(new SliderSetting("artiset_fresh_fr", mods.ChildByName("Fresh").ChildByName("Extra Settings").ChildByName("SETTINGS").ChildByName("Panel").ChildByName("Fresh"), 0, 100, 0, true, "{0}"));
            SettingRegistry.settings.Add(new SliderSetting("artiset_fresh_us", mods.ChildByName("Fresh").ChildByName("Extra Settings").ChildByName("SETTINGS").ChildByName("Panel").ChildByName("Used"), 0, 100, 1, true, "{0}"));
            SettingRegistry.settings.Add(new SliderSetting("artiset_fresh_st", mods.ChildByName("Fresh").ChildByName("Extra Settings").ChildByName("SETTINGS").ChildByName("Panel").ChildByName("Stale"), 0, 100, 2, true, "{0}"));
            SettingRegistry.settings.Add(new SliderSetting("artiset_fresh_du", mods.ChildByName("Fresh").ChildByName("Extra Settings").ChildByName("SETTINGS").ChildByName("Panel").ChildByName("Dull"), 0, 100, 5, true, "{0}"));
            #endregion

            // This sets the text to show where your custom music directory is.
            Pages[3].PageContent().ChildByName("CybergrindMusic").ChildByName("Toggle").ChildByName("Path").GetComponent<Text>().text = Path.Combine(Utils.GameDirectory(), @"BepInEx\UMM Mods\plugins\ULTRAKILLtweaker\Cybergrind Music");

            foreach (Setting setting in SettingRegistry.settings)
            {
                SettingRegistry.idToSetting.Add(setting.ID, setting);
                Debug.Log($"Setting registered: {setting.ID}.");
            }

            // EnableDisableEvent is added the the optmenu, all it does is call MenuEnable/MenuDisable on OnEnable/OnDisable
            OptionsMenu.AddComponent<EnableDisableEvent>();
            MenuEnable();

            Modifiers = TweakerMenu.ChildByName("Modifiers");

            #region Add button listeners
            TweakerMenu.ChildByName("MODIFIERSbtn").GetComponent<Button>().onClick.AddListener(() =>
            {
                Modifiers.SetActive(!Modifiers.activeSelf);
                TweakerMenu.ChildByName("Tweaks").SetActive(!TweakerMenu.ChildByName("Tweaks").activeSelf);
            });

            TweakerMenu.ChildByName("Left").GetComponent<Button>().onClick.AddListener(() =>
            {
                if (Page - 1 != -1)
                {
                    Page--;
                    UpdatePages();
                }
            });

            TweakerMenu.ChildByName("Right").GetComponent<Button>().onClick.AddListener(() =>
            {
                if (Page != Pages.Count-1)
                {
                    Page++;
                    UpdatePages();
                }
            });

            Pages[0].PageContent().ChildByName("Reset").GetComponent<Button>().onClick.AddListener(() =>
            {
                OptionsMenu.SetActive(false);
                SettingRegistry.Validate(true);
                OptionsMenu.SetActive(true);
            });
            #endregion

            UpdatePages();

            TweakerMenu.SetActive(false);
            Modifiers.SetActive(false);
            OptionsMenu.SetActive(false);

            SettingsInit = true;
        }

        public void UpdatePages()
        {
            for (int i = 0; i < Pages.Count; i++)
            {
                if (i != Page)
                {
                    Pages[i].SetActive(false);
                }
                else
                {
                    Pages[i].SetActive(true);
                }
            }
        }
        #endregion

        #region Stuff that happens on Update
        public void Update()
        {
            if (OptionsMenu != null)
            {
                #region This code is bad, have to do it because the scale/pos breaks when you set parent. Not too bad, as when it is in the correct pos/scale it doesn't happen
                if (TweakerButton != null)
                {
                    if (TweakerButton.transform.position != new Vector3((45f / 1920f) * Screen.width, (1000f / 1080f) * Screen.height, 0) || TweakerButton.transform.localScale != Vector3.one)
                    {
                        TweakerButton.transform.position = new Vector3((45f/1920f) * Screen.width, (1000f / 1080f) * Screen.height, 0);
                        TweakerButton.transform.localScale = new Vector3(1, 1, 1);
                    }
                }

                if (TweakerMenu != null)
                {
                    if (TweakerMenu.transform.position != new Vector3(Screen.width / 2, Screen.height / 2, 0) || TweakerMenu.transform.localScale != Vector3.one)
                    {
                        TweakerMenu.transform.position = new Vector3(Screen.width / 2, Screen.height / 2, 0);
                        TweakerMenu.transform.localScale = new Vector3(1, 1, 1);
                    }
                }

                // This is mainly just for the sliders, every frame the text is set from here
                if (OptionsMenu.activeSelf)
                {
                    foreach (Setting setting in SettingRegistry.settings)
                    {
                        if (setting.GetType() == typeof(SliderSetting))
                        {
                            setting.SetDisplay();
                        }
                    }
                }
                #endregion
           
            }

            if (SettingsInit)
            {
                // The scale changes sometimes, so I have to do this every frame. Bad code, it works.
                if (Utils.GetSetting<bool>("seeviewmodel"))
                {
                    GameObject[] children = player.ChildByName("Main Camera").ChildByName("Guns").ChildrenList().ToArray();
                    GameObject[] children2 = player.ChildByName("Main Camera").ChildByName("Punch").ChildrenList().ToArray();
                    foreach (GameObject go in children)
                    {
                        if (go.name != "KickBackPos" || go.name != "PickUpPos")
                        {
                            go.transform.localScale = Vector3.zero;
                        }
                    }
                    foreach (GameObject go in children2)
                    {
                        if (go.name != "Projectile Parry Zone")
                        {
                            go.transform.localScale = Vector3.zero;
                        }
                    }
                }

                // Updates the volume of the custom CG song.
                if (SceneManager.GetActiveScene().name == "Endless" && !HasntHappenedThisScene)
                {
                    if (Utils.GetSetting<bool>("cybergrindmusic"))
                    {
                        audiosource.volume = volslider.normalizedValue;
                    }
                }

                if (SceneManager.GetActiveScene().name != "Main Menu" && !HasntHappenedThisScene)
                {
                    if (Utils.GetSetting<bool>("speedometer") && nm.gameObject != null && !nm.dead)
                    {
                        Speedometer.ChildByName("Panel").ChildByName("SPEED").GetComponent<Text>().text = Math.Round(nm.rb.velocity.magnitude, 1).ToString();
                    }

                    if (Utils.GetSetting<bool>("weapanel") && nm.gameObject != null && !HasntHappenedThisScene && !nm.dead)
                    {
                        if (Utils.GetSetting<bool>("ARTIFACT_noweapons"))
                        {
                            CustomWeaponPanel.ChildByName("Panel").ChildByName("Gun").SetActive(false);
                        } else
                        {
                            try
                            {
                                CustomWeaponPanel.ChildByName("Panel").ChildByName("Gun").GetComponent<Image>().sprite = MonoSingleton<WeaponHUD>.Instance.gameObject.GetComponent<Image>().sprite;
                                CustomWeaponPanel.ChildByName("Panel").ChildByName("Gun").GetComponent<Image>().color = MonoSingleton<WeaponHUD>.Instance.gameObject.GetComponent<Image>().color;
                            }
                            catch { /* fuck it, we ball. */ }
                        }

                        if (Utils.GetSetting<bool>("ARTIFACT_noarm"))
                        {
                            CustomWeaponPanel.ChildByName("Panel").ChildByName("Fist").SetActive(false);
                        }
                        else
                        {
                            try
                            {
                                CustomWeaponPanel.ChildByName("Panel").ChildByName("Fist").GetComponent<Image>().color = GameObject.Find("FistPanel").ChildByName("Panel").GetComponent<Image>().color;
                            }
                            catch { /* fuck it, we ball. */ }
                        }

                        CustomWeaponPanel.ChildByName("Panel").ChildByName("Slider").GetComponent<Slider>().value = MonoSingleton<WeaponCharges>.Instance.raicharge;
                    }

                    if (Utils.GetSetting<bool>("hppanel") && nm.gameObject != null && !HasntHappenedThisScene && !nm.dead)
                    {
                        string prefix = "";
                        string suffix = "";
                        if(nm.antiHp != 0)
                        {
                            prefix += "<color=#7C7A7B>";
                            suffix += "</color>";
                        }

                        PlayerInfo.ChildByName("Panel").ChildByName("HP").GetComponent<Text>().text = $"{nm.hp}{prefix} / {100 - Math.Round(nm.antiHp, 0)}{suffix}";
                        PlayerInfo.ChildByName("Panel").ChildByName("Stamina").GetComponent<Text>().text = $"{(nm.boostCharge / 100).ToString("0.00")} / 3.00";
                    }

                    if (Utils.GetSetting<bool>("dps") && nm.gameObject != null && !nm.dead)
                    {
                        float DPS = 0;
                        // I would use foreach but you can't edit the list in foreaches
                        for (int i = 0; i < HitsSecond.Count; i++)
                        {
                            Hit hit = HitsSecond[i];
                            if ((DateTime.Now - hit.time).TotalSeconds > 1)
                            {
                                HitsSecond.RemoveAt(i);
                                i--;
                            }
                            else 
                            {
                                float ActualDmg = hit.dmg;

                                if(ActualDmg < 1000000 && ActualDmg > 0)
                                    DPS += ActualDmg;
                            }
                        }

                        DPSPanel.ChildByName("Panel").ChildByName("DPS").GetComponent<Text>().text = Math.Round(DPS, 2).ToString();
                    }

                    if (Utils.GetSetting<bool>("cybergrindstats") && SceneManager.GetActiveScene().name == "Endless" && CyberGrind.activeSelf)
                    {
                        GameObject WaveNum = GameObject.Find("Wave Number");
                        if (WaveNum != null)
                        {
                            GameObject panel = WaveNum.transform.parent.gameObject;
                            CyberGrind.ChildByName("Panel").ChildByName("WaveCount").GetComponent<Text>().text = WaveNum.GetComponent<Text>().text;
                            CyberGrind.ChildByName("Panel").ChildByName("EnemyCount").GetComponent<Text>().text = panel.ChildByName("Enemies Left Number").GetComponent<Text>().text;
                            CyberGrind.ChildByName("Panel").ChildByName("Kills").GetComponent<Text>().text = statman.kills + "";
                            CyberGrind.ChildByName("Panel").ChildByName("Style").GetComponent<Text>().text = statman.stylePoints + "";
                            string joe = TimeSpan.FromSeconds(statman.seconds).ToString();
                            if (joe.StartsWith("00:"))
                            {
                                CyberGrind.ChildByName("Panel").ChildByName("Time").GetComponent<Text>().text = joe.Substring(3, joe.Length - 7);
                            }
                            else
                            {
                                CyberGrind.ChildByName("Panel").ChildByName("Time").GetComponent<Text>().text = joe.Substring(0, joe.Length - 4);
                            }
                        }
                    }

                    UpdateArtifact();
                }
            }

            
        }

        public void UpdateArtifact()
        {
            if (Utils.GetSetting<bool>("ARTIFACT_noHP"))
            {
                if (nm != null)
                {
                    nm.antiHp = 100 - Utils.GetSetting<float>("artiset_noHP_hpamount");
                    if (nm.hp > Utils.GetSetting<float>("artiset_noHP_hpamount"))
                    {
                        nm.ForceAntiHP((int)(100 - Utils.GetSetting<float>("artiset_noHP_hpamount")));
                    }
                }
            }

            if (Utils.GetSetting<bool>("ARTIFACT_glass"))
            {
                if (nm != null)
                {
                    nm.antiHp = 70;
                    if (nm.hp > 30)
                    {
                        nm.ForceAntiHP(70);
                    }
                }
            }

            if (Utils.GetSetting<bool>("ARTIFACT_superhot"))
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

            if (Utils.GetSetting<bool>("ARTIFACT_nostamina"))
            {
                if (nm != null)
                {
                    try
                    {
                        nm.EmptyStamina();
                    }
                    catch { }
                }
            }

            if (Utils.GetSetting<bool>("ARTIFACT_diceroll"))
            {
                string joe = TimeSpan.FromSeconds((NextReset - DateTime.Now).TotalSeconds).ToString();

                if (joe.StartsWith("00:"))
                {
                    DiceRoll.ChildByName("Panel").ChildByName("Time").GetComponent<Text>().text = joe.Substring(3, joe.Length - 7);
                }
                else
                {
                    DiceRoll.ChildByName("Panel").ChildByName("Time").GetComponent<Text>().text = joe.Substring(0, joe.Length - 4);
                }
            }

            if (Utils.GetSetting<bool>("ARTIFACT_fresh"))
            {
                Dictionary<StyleFreshnessState, float> dict = new Dictionary<StyleFreshnessState, float>()
                {
                    { StyleFreshnessState.Fresh, Utils.GetSetting<float>("artiset_fresh_fr") },
                    { StyleFreshnessState.Used, Utils.GetSetting<float>("artiset_fresh_us") },
                    { StyleFreshnessState.Stale, Utils.GetSetting<float>("artiset_fresh_st") },
                    { StyleFreshnessState.Dull, Utils.GetSetting<float>("artiset_fresh_du") }
                };

                if (nm != null)
                {
                    ToRemove_FR += Time.deltaTime * dict[Freshness];

                    if (ToRemove_FR > 1)
                    {
                        nm.hp -= 1;
                        ToRemove_FR -= 1;
                    }

                    if (nm.hp <= 0)
                    {
                        nm.GetHurt(int.MaxValue, false);
                    }
                }
            }

            if (Utils.GetSetting<bool>("ARTIFACT_fuelleak"))
            {
                if (nm != null && Instance.statman.timer)
                {
                    ToRemove_FL += Time.deltaTime * 5 * Utils.GetSetting<float>("artiset_fuelleak_multi");

                    if (ToRemove_FL > 1)
                    {
                        nm.hp -= 1;
                        ToRemove_FL -= 1;
                    }

                    if (nm.hp <= 0)
                    {
                        nm.GetHurt(int.MaxValue, false, 1, true, true);
                    }
                }
            }

            if (Utils.GetSetting<bool>("ARTIFACT_floorlava") && !nm.dead)
            {
                if (nm != null && Instance.statman.timer && ShouldDamage_FIL)
                {
                    //im not sure why it doesnt scale lmao, still damages at 0
                    if(nm.hp >= 0 && !nm.dead)
                        ToRemove_FIL += Time.deltaTime * 10 * Utils.GetSetting<float>("artiset_floorlava_mult");

                    if (ToRemove_FIL > 1)
                    {
                        nm.hp -= 1;
                        ToRemove_FIL -= 1;
                    }

                    if (nm.hp <= 0)
                    {
                        GameObject DeathExplosion = MonoSingleton<GunSetter>.Instance.shotgunPump[0].GetComponent<Shotgun>().explosion;
                        GameObject InstExpl = Instantiate(DeathExplosion, Instance.player.transform);

                        foreach (Explosion explosion in InstExpl.GetComponentsInChildren<Explosion>())
                        {
                            explosion.enemyDamageMultiplier = 15f;
                            explosion.maxSize *= 15;
                            explosion.damage = 0;
                        }

                        nm.GetHurt(int.MaxValue, false, 1, true);
                    }
                }
            }
        }
        #endregion

        #region Stuff that happens on Init
        public void Init()
        {
            if (HasntHappenedThisScene && SceneManager.GetActiveScene().name != "Intro")
                InitSceneLoad_InclMM();

            if (SceneManager.GetActiveScene().name != "Main Menu" && SceneManager.GetActiveScene().name != "Intro")
            {
                if(HasntHappenedThisScene)
                    InitSceneLoad();

                InitReset();
            }

            HasntHappenedThisScene = false;
        }

        public static IEnumerator WaitAndInit()
        {
            yield return null;
            Instance.Init();
        }

        public void InitReset()
        {
            GameObject hud = player.ChildByName("Main Camera").ChildByName("HUD Camera").ChildByName("HUD");
            if (Utils.GetSetting<bool>("forcegun") || Utils.GetSetting<bool>("weapanel")) // weapanel needs the thing to exist
            {
                hud.ChildByName("GunCanvas").ChildByName("GunPanel").SetActive(true);
            }

            if (Utils.GetSetting<bool>("ARTIFACT_noweapons"))
            {
                hud.ChildByName("GunCanvas").ChildByName("GunPanel").SetActive(false);
            }
        }

        public void InitSceneLoad_InclMM()
        {
            if (Utils.GetSetting<float>("uiscalecanv") != 100)
            {
                GameObject go = MonoSingleton<CanvasController>.Instance.gameObject;
                go.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                go.GetComponent<CanvasScaler>().scaleFactor = 1920 / Screen.width * 1.5f;
                go.GetComponent<CanvasScaler>().scaleFactor *= Utils.GetSetting<float>("uiscalecanv") / 100;
            }
        }

        public void InitSceneLoad()
        {
            // Set instances
            player = GameObject.Find("Player");
            player.ChildByName("GroundCheck").AddComponent<TouchingGround>();
            nm = FindObjectOfType<NewMovement>(); 

            GameObject hud = player.ChildByName("Main Camera").ChildByName("HUD Camera").ChildByName("HUD");

            // Reset the CG panel values back to 0
            CyberGrind.ChildByName("Panel").ChildByName("WaveCount").GetComponent<Text>().text = "0";
            CyberGrind.ChildByName("Panel").ChildByName("EnemyCount").GetComponent<Text>().text = "0";
            CyberGrind.ChildByName("Panel").ChildByName("Time").GetComponent<Text>().text = "00:00.000";
            CyberGrind.ChildByName("Panel").ChildByName("Kills").GetComponent<Text>().text = "0";
            CyberGrind.ChildByName("Panel").ChildByName("Style").GetComponent<Text>().text = "0";

            #region Stuff that happens on player spawn, for the settings
            if (Utils.GetSetting<float>("uiscale") != 100)
            {
                GameObject go = hud.ChildByName("GunCanvas");
                go.transform.localScale *= Utils.GetSetting<float>("uiscale") / 100;
            }

            if (Utils.GetSetting<float>("uiscalestyle") != 100)
            {
                GameObject go = hud.ChildByName("StyleCanvas");
                go.transform.localScale *= Utils.GetSetting<float>("uiscalestyle") / 100;
            }
            
            if (Utils.GetSetting<float>("uiscaleresults") != 100)
            {
                GameObject go = hud.ChildByName("FinishCanvas");
                go.transform.localScale *= Utils.GetSetting<float>("uiscaleresults") / 100;
            }

            if (Utils.GetSetting<bool>("nobob"))
                player.ChildByName("Main Camera").ChildByName("Guns").GetComponent<WalkingBob>().enabled = false;

            if (Utils.GetSetting<bool>("notilt"))
                player.ChildByName("Main Camera").ChildByName("Guns").GetComponent<RotateToFaceFrustumTarget>().enabled = false;

            if (Utils.GetSetting<bool>("ARTIFACT_water"))
            {
                GameObject water = GameObject.CreatePrimitive(PrimitiveType.Cube);
                water.AddComponent<Rigidbody>();
                water.GetComponent<Rigidbody>().isKinematic = true;
                water.GetComponent<Collider>().isTrigger = true;
                water.AddComponent<Water>();
                water.GetComponent<Water>().bubblesParticle = new GameObject();
                water.GetComponent<MeshRenderer>().enabled = false;
                water.transform.localScale = Vector3.one * 10000000000; // I think this should be big enough
            }

            if (CurrentRandom != null) 
            {
                StopCoroutine(CurrentRandom);
            }

            if (MCL != null)
            {
                StopCoroutine(MCL);
            }

            if (Utils.GetSetting<bool>("ARTIFACT_diceroll"))
            {
                CurrentRandom = StartCoroutine(RandomiseEvery30());
            }

            if (Utils.GetSetting<bool>("cybergrindmusic") && SceneManager.GetActiveScene().name == "Endless")
            {
                music = GetClipsFromFolder();
                volslider = OptionsMenu.ChildByName("Audio Options").ChildByName("Image").ChildByName("Music Volume").ChildByName("Button").ChildByName("Slider (1)").GetComponent<Slider>();
                UnityEngine.Object.Destroy(GameObject.Find("Everything").transform.GetChild(3).transform.GetChild(0).gameObject);
                StartCoroutine(MusLoopWhenWaveCount());
            }

            if (Utils.GetSetting<bool>("ARTIFACT_gofast"))
            {
                if (nm != null)
                {
                    nm.walkSpeed *= Utils.GetSetting<float>("artiset_gofast_player");
                }
            }

            if (Utils.GetSetting<bool>("cybergrindstats"))
            {
                CyberGrind.SetActive(SceneManager.GetActiveScene().name == "Endless");
            }

            if (Utils.GetSetting<bool>("ARTIFACT_diceroll"))
            {
                DiceRoll.SetActive(true);
            }

            DPSPanel.SetActive(Utils.GetSetting<bool>("dps"));
            Speedometer.SetActive(Utils.GetSetting<bool>("speedometer"));
            CustomWeaponPanel.SetActive(Utils.GetSetting<bool>("weapanel"));
            PlayerInfo.SetActive(Utils.GetSetting<bool>("hppanel"));

            if (Utils.GetSetting<bool>("ARTIFACT_noarm"))
            {
                player.ChildByName("Main Camera").ChildByName("Punch").SetActive(false);
            }

            if (Utils.GetSetting<bool>("weapanel") || Utils.GetSetting<bool>("hppanel"))
            {
                Speedometer.ChildByName("Panel").transform.position += new Vector3(0, (68.5f / 1080f) * Screen.height, 0);
                DPSPanel.ChildByName("Panel").transform.position += new Vector3(0, (68.5f / 1080f) * Screen.height, 0);
            }

            if(Utils.GetSetting<bool>("speedometer"))
            {
                DPSPanel.ChildByName("Panel").transform.position += new Vector3((274f / 1920f) * Screen.width, 0, 0);
            }

            if (Utils.GetSetting<bool>("hppanel"))
            {
                CustomWeaponPanel.ChildByName("Panel").transform.position += new Vector3((274f / 1920f) * Screen.width, 0, 0);
            }

            if (CustomWeaponPanel.activeSelf == true)
                CustomWeaponPanel.ChildByName("Panel").ChildByName("Gun").transform.localScale += new Vector3(0, 0.5f, 0);

            if (Utils.GetSetting<bool>("cybergrindnoscores"))
                UKAPI.DisableCyberGrindSubmission($"UKTW: CG score tweak disabled");

            #endregion
        }
        #endregion

        #region Music stuff, skidded from eps's CGUtils.
        public IEnumerator RandomiseEvery30()
        {
            while (true)
            {
                int Time = (int)Utils.GetSetting<float>("artiset_diceroll_timereset");
                NextReset = DateTime.Now.AddSeconds(Time);

                yield return new WaitForSecondsRealtime(Time);
                Debug.Log("Reseting DiceRoll weapons!");
                GameObject.FindObjectOfType<GunSetter>().ResetWeapons();
            }
        }

        public List<AudioClip> GetClipsFromFolder()
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
                foreach(string fileext in supportedFileExtensions)
                {
                    if(file.EndsWith(fileext))
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

        public List<AudioClip> music;
        private AudioClip lastClip;
        public AudioSource audiosource;

        public IEnumerator MusLoopWhenWaveCount()
        {
            while(GameObject.Find("Wave Number") == null || GameObject.Find("Wave Number").activeSelf == false)
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
                if (!audiosource.isPlaying)
                {
                    if (GameObject.Find("Wave Number") != null)
                    {
                        audiosource.PlayOneShot(RandomClip());
                    } else
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

        private AudioClip RandomClip()
        {
            int num = 3;
            AudioClip audioClip = music[UnityEngine.Random.Range(0, music.Count)];
            while (audioClip == lastClip && num > 0)
            {
                audioClip = music[UnityEngine.Random.Range(0, music.Count)];
                num--;
            }
            lastClip = audioClip;
            return audioClip;
        }
        #endregion

        #region Harmony Patches

        [HarmonyPatch(typeof(StyleHUD), nameof(StyleHUD.UpdateHUD))]
        public static class UpdateHUD
        {
            public static void Postfix(StyleHUD __instance)
            {
                if (!HasntHappenedThisScene && MonoSingleton<GunControl>.Instance != null && SceneManager.GetActiveScene().name != "Main Menu" && SceneManager.GetActiveScene().name != "Intro")
                {
                    // Debug.Log($"StyleHUD UpdateHUD: freshness state {__instance.GetFreshnessState(MonoSingleton<GunControl>.Instance.currentWeapon)}.");
                    Instance.Freshness = __instance.GetFreshnessState(MonoSingleton<GunControl>.Instance.currentWeapon);
                }
            }
        }

        [HarmonyPatch(typeof(EnemyIdentifier), "Start")]
        public static class HmmTodayIWillUndergoMitosis
        {
            public static void Prefix(EnemyIdentifier __instance)
            {
                if (!__instance.gameObject.name.Contains("(MITOSIS)"))
                {
                    Console.WriteLine($"Prefix {__instance.gameObject.name}");
                    if (Utils.GetSetting<bool>("ARTIFACT_mitosis"))
                    {
                        Console.WriteLine($"Mitosis {__instance.gameObject.name}");
                        for (int i = 0; i < Utils.GetSetting<float>("artiset_mitosis_amount")-1; i++)
                        {
                            Console.WriteLine($"Iterate {__instance.gameObject.name}, {i}");
                            GameObject obj = Instantiate(__instance.gameObject, __instance.transform.parent);
                            obj.name = __instance.gameObject.name + "(MITOSIS)";
                            obj.transform.position = __instance.transform.position;
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Skull), "Start")]
        public static class Florpify
        {
            public static void Postfix(Skull __instance)
            {
                if (Utils.GetSetting<bool>("legally_distinct_florp")) 
                {
                    Renderer renderer = __instance.gameObject.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        string Florp;
                        switch (__instance.GetComponent<ItemIdentifier>().itemType)
                        {
                            case ItemType.SkullBlue:
                                Florp = "Blue";
                                break;

                            case ItemType.SkullRed:
                                Florp = "Red";
                                break;

                            default:
                                return;
                        }

                        renderer.enabled = false;

                        Dictionary<string, string> FLORP_NameToGoName = new Dictionary<string, string>()
                        {
                            { "Blue", "Blue Florp" },
                            { "Red", "Red Florp" }
                        };

                        GameObject fLORP = Instantiate(Instance.UIBundle.LoadAsset<GameObject>(FLORP_NameToGoName[Florp]), renderer.transform);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(NewMovement), nameof(NewMovement.GetHurt))]
        public static class OnPlayerHit
        {
            public static void Postfix()
            {
                if (Utils.GetSetting<bool>("explorsion") && Instance.nm.hp <= 0)
                {
                    GameObject DeathExplosion = MonoSingleton<GunSetter>.Instance.shotgunPump[0].GetComponent<Shotgun>().explosion;
                    GameObject InstExpl = Instantiate(DeathExplosion, Instance.player.transform);

                    foreach (Explosion explosion in InstExpl.GetComponentsInChildren<Explosion>())
                    {
                        explosion.enemyDamageMultiplier = 15f;
                        explosion.maxSize *= 15;
                        explosion.damage = 0;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(BossHealthBar), "Awake")]
        public static class TankAwake
        {
            public static void Postfix(BossHealthBar __instance)
            {
                if (Utils.GetSetting<bool>("ARTIFACT_tank")) 
                {
                    GameObject bossbar = __instance.GetFieldValue<GameObject>("bossBar");
                    Debug.Log($"Bossbar found: {bossbar}.");
                    foreach (GameObject slider in bossbar.ChildByName("Panel").ChildByName("Filler").ChildrenList())
                    {
                        slider.GetComponent<Slider>().maxValue *= Utils.GetSetting<float>("artiset_tankify_mult");
                    }
                }

                if(Utils.GetSetting<float>("uiscaleboss") != 100)
                {
                    GameObject bossbar = __instance.GetFieldValue<GameObject>("bossBar");
                    bossbar.transform.localScale *= Utils.GetSetting<float>("uiscaleboss") / 100;
                }
            }
        }

        [HarmonyPatch(typeof(EnemyIdentifier), "DeliverDamage")]
        public static class OnDamagePatch
        {
            public static void Prefix(EnemyIdentifier __instance, out float __state)
            {
                __state = __instance.health;
            }

            public static void Postfix(EnemyIdentifier __instance, float __state)
            {
                float damage = __state - __instance.health;
                if (damage != 0f)
                {
                    Instance.OnEnemyDamage(__instance, damage);
                }
            }
        }

        [HarmonyPatch(typeof(FinalRank), "SetRank")]
        public static class ModResultsPatch
        {
            public static void Postfix(FinalRank __instance)
            {
                GameObject hud = Instance.player.ChildByName("Main Camera").ChildByName("HUD Camera").ChildByName("HUD");
                Text text = hud.ChildByName("FinishCanvas").ChildByName("Panel").ChildByName("Extra Info").ChildByName("Text").GetComponent<Text>();
                string mods = "";
                int ModAmount = 0;

                foreach (Setting mod in SettingRegistry.settings)
                {
                    if (mod.GetType() == typeof(ArtifactSetting))
                    {
                        if (!Convert.ToBoolean(mod.value))
                        {
                            ModAmount++;
                            mods += ((ArtifactSetting)mod).Name.ToUpper() + ", ";
                        }
                    }
                }

                if(ModAmount != 0)
                    text.text = "<size=20>+ " + mods.Substring(0, mods.Length - 2) + "</size>\n<size=10>\n</size>" + text.text;
            }
        }

        [HarmonyPatch(typeof(NewMovement), "ForceAddAntiHP")]
        public static class WhipFix
        {
            [HarmonyPrefix]
            public static void HardDamage(NewMovement __instance, ref float amount)
            {
                if (Utils.GetSetting<bool>("ARTIFACT_whiphard"))
                {
                    System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace();
                    if (stackTrace.ToString().Contains("Hook"))
                    {
                        amount *= Utils.GetSetting<float>("artiset_whip_hard_mult");
                    }
                }
            }
        }

        [HarmonyPatch(typeof(NewMovement), nameof(NewMovement.Start))]
        public static class DoInitOnTime
        {
            public static void Postfix()
            {
                Instance.StartCoroutine(WaitAndInit());
            }
        }

        [HarmonyPatch(typeof(GunSetter), nameof(GunSetter.ResetWeapons))]
        public static class PatchReset
        {
            public static void Postfix(GunSetter __instance)
            {
                if (Utils.GetSetting<bool>("ARTIFACT_noweapons"))
                    {
                        List<GameObject>[] slots = new List<GameObject>[6]
                        {
                        __instance.gunc.slot1,
                        __instance.gunc.slot2,
                        __instance.gunc.slot3,
                        __instance.gunc.slot4,
                        __instance.gunc.slot5,
                        __instance.gunc.slot6
                        };

                        foreach (var slot in slots)
                        {
                            slot.Clear();
                        }
                    }

                    if (Utils.GetSetting<bool>("ARTIFACT_diceroll"))
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

    [HarmonyPatch(typeof(FistControl), nameof(FistControl.ResetFists))]
    public static class AddArms
    {
        public static void Postfix(FistControl __instance)
        {
            //
        }
    }

    [HarmonyPatch(typeof(EnemyIdentifier), "Awake")]
        public static class EnemySpawnPatch
        {
            public static void Postfix(EnemyIdentifier __instance)
            {
                if (Utils.GetSetting<bool>("ARTIFACT_gofast"))
                {
                    __instance.GetComponent<NavMeshAgent>().speed *= Utils.GetSetting<float>("artiset_gofast_enemy");
                    __instance.GetComponent<NavMeshAgent>().acceleration *= Utils.GetSetting<float>("artiset_gofast_enemy");
            }

                if (Utils.GetSetting<bool>("ARTIFACT_sandify"))
                    __instance.sandified = true;

                if (Utils.GetSetting<bool>("ARTIFACT_distance"))
                    __instance.gameObject.AddComponent<BlessWhenFar>();

                if (Utils.GetSetting<bool>("ARTIFACT_tank") || Utils.GetSetting<bool>("ARTIFACT_glass"))
                {
                    float MULT = Utils.GetSetting<float>("artiset_tankify_mult");

                    if (Utils.GetSetting<bool>("ARTIFACT_glass"))
                    {
                        MULT = 0.5f;
                    }

                    if (__instance.enemyType == EnemyType.Drone || __instance.enemyType == EnemyType.Virtue)
                    {
                        if (!__instance.drone)
                        {
                            __instance.drone = __instance.GetComponent<Drone>();
                        }
                        if (__instance.drone)
                        {
                            __instance.drone.health *= MULT;
                            __instance.health = __instance.drone.health;
                            return;
                        }
                    }
                    else if (__instance.enemyType == EnemyType.MaliciousFace)
                    {
                        if (!__instance.spider)
                        {
                            __instance.spider = __instance.GetComponent<SpiderBody>();
                        }
                        if (__instance.spider)
                        {
                            __instance.spider.health *= MULT;
                            __instance.health = __instance.spider.health;
                            return;
                        }
                    }
                    else
                    {
                        switch (__instance.enemyClass)
                        {
                            case EnemyClass.Husk:
                                if (!__instance.zombie)
                                {
                                    __instance.zombie = __instance.GetComponent<Zombie>();
                                }
                                if (__instance.zombie)
                                {
                                    __instance.zombie.health *= MULT;
                                    __instance.health = __instance.zombie.health;
                                    return;
                                }
                                break;
                            case EnemyClass.Machine:
                                if (!__instance.machine)
                                {
                                    __instance.machine = __instance.GetComponent<Machine>();
                                }
                                if (__instance.machine)
                                {
                                    __instance.machine.health *= MULT;
                                    __instance.health = __instance.machine.health;
                                }
                                break;
                            case EnemyClass.Demon:
                                if (!__instance.statue)
                                {
                                    __instance.statue = __instance.GetComponent<Statue>();
                                }
                                if (__instance.statue)
                                {
                                    __instance.statue.health *= MULT;
                                    __instance.health = __instance.statue.health;
                                    return;
                                }
                                break;
                            default:
                                return;
                        }
                    }
                }

            }
        }

        public static class HitstopPatches
        {
            [HarmonyPatch(typeof(TimeController), "HitStop")]
            [HarmonyPrefix]
            static void HitStop(ref float length)
            {
                length = Utils.GetSetting<float>("hitstopmult") * length;
            }

            [HarmonyPatch(typeof(TimeController), "TrueStop")]
            [HarmonyPrefix]
            static void TrueStop(ref float length)
            {
                length = Utils.GetSetting<float>("hitstopmult") * length;
            }

            [HarmonyPatch(typeof(TimeController), "SlowDown")]
            [HarmonyPrefix]
            static void SlowDown(ref float amount)
            {
                amount = Utils.GetSetting<float>("hitstopmult") * amount;
            }
        }
    #endregion

    public class Hit
    {
        public DateTime time;
        public EnemyIdentifier eid;
        public float dmg;

        public Hit(EnemyIdentifier eid, float dmg)
        {
            this.eid = eid;
            this.dmg = dmg;
            time = DateTime.Now;
        }
    }
}

