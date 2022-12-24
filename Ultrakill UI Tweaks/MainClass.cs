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
using ULTRAKILLtweaker.Components;
using ULTRAKILLtweaker.Tweaks.UIElements.Impl;
using ULTRAKILLtweaker.Tweaks.UIElements;
using ULTRAKILLtweaker.Tweaks.Handlers;
using ULTRAKILLtweaker.Tweaks.Handlers.Impl;
using ULTRAKILLtweaker.Tweaks;

namespace ULTRAKILLtweaker
{
    [UKPlugin("ULTRAKILLtweaker", "1.0.0", "Tweak your Ultrakill, with extra settings and game modifiers.\n\nCreated by Waffle.", true, true)]
    public class MainClass : UKMod
    {
        #region Variables
        public static MainClass Instance;

        public Dictionary<string, Type> IDToType = new Dictionary<string, Type>()
        {
            { "hitstop_enabled", typeof(HitstopMultiplier) },
            { "savepbs", typeof(SavePBs) },
            { "dmgsub", typeof(DamageNoti) },
            { "uiscale_enabled", typeof(UIScale) },
            { "fpscounter", typeof(FPSCounter) },
            { "forcegun", typeof(ForceGun) },
            { "panels", typeof(CustomPanels) },
            { "vmtrans", typeof(ViewmodelTransform) },
            { "cybergrindmusic", typeof(CGMusic) }

        };
        public Dictionary<Type, TweakHandler> TypeToHandler = new Dictionary<Type, TweakHandler>()
        {
        };

        // UI elements.
        public GameObject TweakerButton;
        public GameObject OptionsMenu;
        public GameObject TweakerMenu;
        public GameObject CyberGrind;
        public GameObject DiceRoll;
  
        public GameObject TexPackMenu;
        public List<GameObject> Panels = new List<GameObject>(); // This is the panel objects, e.g speedometer and weapons

        // Stuff that handles the pages for the tweaks.
        public List<Page> Pages = new List<Page>();
        public GameObject Modifiers;
        public static int Page = 0;

        // The assetbundle which all of the UKt assets are from.
        public static AssetBundle UIBundle;

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
        public Harmony harmony;

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



        #region UMM stuff, handles loading and unloading.
        public override void OnModLoaded()
        {
            Instance = this;

            // Patch stuff.
            harmony = new Harmony("waffle.ultrakill.UKtweaker");
            harmony.PatchAll();

            SceneManager.sceneLoaded += OnSceneWasLoaded;

            GameObject HandlerHolder = new GameObject("Tweak Handler Holder - do NOT destroy");
            DontDestroyOnLoad(HandlerHolder);
            foreach (Type t in IDToType.Values)
            {
                TweakHandler th = (TweakHandler)HandlerHolder.AddComponent(t);
                th.enabled = false;
                TypeToHandler.Add(t, th);
            }

            // OnSceneWasLoaded is where patching happens, but if it is not called (e.g when the mod is not on at startup) it is not called on time. We have to do it now.
            if (SceneManager.GetActiveScene().name == "Main Menu")
            {
                OnSceneWasLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
            }

            UIBundle = AssetBundle.LoadFromFile(Path.Combine(Utils.GameDirectory(), @"BepInEx\UMM Mods\ULTRAKILLtweaker\tweakerassets.bundle"));

            UIPreloader.LoadSettingElements();
            ResourcePack.InitPacks();
            ResourcePack.SetDicts();
            Times.Load();
        }

        public override void OnModUnload()
        {
            // Unpatch Harmony, destroy GOs from UKt, remove events set, all the stuff that happens on startup.
            harmony.UnpatchSelf();

            List<GameObject> ToDestroy = new List<GameObject>()
            {
                TweakerButton, TweakerMenu, CyberGrind, DiceRoll
            };

            foreach (GameObject go in ToDestroy)
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

            if (Instance.ModsChanged && SceneManager.GetActiveScene().name != "Main Menu")
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
                foreach (Setting set in SettingRegistry.settings)
                {
                    if (set.GetType() == typeof(ArtifactSetting))
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

            if (SceneManager.GetActiveScene().name == scene.name && (ResourcePack.IDtoSpr.Count() != 0 || ResourcePack.IDtoClip.Count() != 0 || ResourcePack.IDtoTex.Count() != 0))
            {
                StartCoroutine(ResourcePack.PatchTextures());
            } 
        }

        public void OnEnemyDamage(EnemyIdentifier eid, float dmg)
        {
            HitsSecond.Add(new Hit(eid, dmg));
        }

        public void TouchingGroundChanged(GroundCheck check)
        {
            // Debug.Log($"GROUND CHECK CHANGED: {check.touchingGround}.");

            if (check.touchingGround)
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

            if (player.ChildByName("GroundCheck").GetComponent<GroundCheck>().touchingGround)
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

            while (MonoSingleton<CanvasController>.Instance.gameObject.ChildByName("OptionsMenu") == null)
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

            if (TexPackMenu == null)
            {
                TexPackMenu = Instantiate(UIBundle.LoadAsset<GameObject>("TexturePackMenu"));
                TexPackMenu.AddComponent<DisableWithEsc>();
            }

            CyberGrind.SetActive(false);
            DiceRoll.SetActive(false);

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
            Pages = new List<Page>()
            {
                new Page("--TWEAKS: MISC--", TweakerMenu.ChildByName("Tweaks").transform),
                // TweakerMenu.ChildByName("Tweaks").ChildByName("Gameplay Tweaks"),
                new Page("--TWEAKS: MISC HUD--", TweakerMenu.ChildByName("Tweaks").transform),
                new Page("--TWEAKS: UI PANELS--", TweakerMenu.ChildByName("Tweaks").transform),
                new Page("--TWEAKS: VIEWMODEL--", TweakerMenu.ChildByName("Tweaks").transform),
                new Page("--TWEAKS: CYBERGRIND--", TweakerMenu.ChildByName("Tweaks").transform),
                new Page("--TWEAKS: FUN--", TweakerMenu.ChildByName("Tweaks").transform)
            };

            GameObject mods = TweakerMenu.ChildByName("Modifiers").ChildByName("Mods");


            #region Register, load, all the settings. 
            SettingRegistry.settings.Clear();
            SettingRegistry.idToSetting.Clear();

            #region Page 1 - MISC
            var HitstopMult = new TweakSettingElement(Pages[0].Holder, new Tweaks.Metadata("HITSTOP MULTIPLIER", "Increase or decrease the length of hitstop."));
                var HitstopLength = new SliderSubsettingElement(HitstopMult.Subsettings, new Tweaks.Metadata("HITSTOP LENGTH", "The HitStop property."));
                var TruestopLength = new SliderSubsettingElement(HitstopMult.Subsettings, new Tweaks.Metadata("TRUESTOP LENGTH", "The TrueStop property."));
                var SlowdownLength = new SliderSubsettingElement(HitstopMult.Subsettings, new Tweaks.Metadata("SLOWDOWN LENGTH", "The SlowDown property."));
                var DisableParry = new ToggleSubsettingElement(HitstopMult.Subsettings, new Tweaks.Metadata("DISABLE PARRY FLASH", "Disable the parry flash."));

            var ShowPBs = new TweakSettingElement(Pages[0].Holder, new Tweaks.Metadata("SHOW PBs FOR TIME, KILLS, AND STYLE", "Save and display your PBs on TAB."));
            var DamageNoti = new TweakSettingElement(Pages[0].Holder, new Tweaks.Metadata("DAMAGE NOTIFICATION (EXPERIMENTAL)", "Show your hits, with info, as subtitles."));
            #endregion

            #region Page 2 - MISC HUD
            var UIScale = new TweakSettingElement(Pages[1].Holder, new Tweaks.Metadata("HUD SCALE", "Change the size of various HUD elements.", true));
                var CanvasScale = new SliderSubsettingElement(UIScale.Subsettings, new Tweaks.Metadata("CANVAS SCALE (EXPERIMENTAL)", "Scale of the UI canvas."));
                var InfoScale = new SliderSubsettingElement(UIScale.Subsettings, new Tweaks.Metadata("INFO PANEL SCALE", "Scale of the HP + more panel."));
                var StyleScale = new SliderSubsettingElement(UIScale.Subsettings, new Tweaks.Metadata("STYLE PANEL SCALE", "Scale of the style panel."));
                var BossbarScale = new SliderSubsettingElement(UIScale.Subsettings, new Tweaks.Metadata("BOSSBAR SCALE", "Scale of the bossbar."));
                var ResultsScale = new SliderSubsettingElement(UIScale.Subsettings, new Tweaks.Metadata("RESULTS PANEL SCALE", "Scale of the results panel."));

            var FPSCounter = new TweakSettingElement(Pages[1].Holder, new Tweaks.Metadata("DISPLAY FPS COUNTER", "Save and display your PBs on TAB."));
            var GunPanel = new TweakSettingElement(Pages[1].Holder, new Tweaks.Metadata("FORCE GUN PANEL (FOR ALT RAILCANNON PIP)", "Forces the gun panel. Disable WEAPON ICON in HUD, and you will be able to use the alternate railcannon charge indicator."));
            #endregion

            #region Page 3 - PANELS
            var GeneralPanels = new TweakSettingElement(Pages[2].Holder, new Tweaks.Metadata("CUSTOM PANELS", "Various small UI elements."));
                var InfoPanel = new ToggleSubsettingElement(GeneralPanels.Subsettings, new Tweaks.Metadata("INFO PANEL", "Shows HP, stamina, rail charge."));
                var WeaponPanel = new ToggleSubsettingElement(GeneralPanels.Subsettings, new Tweaks.Metadata("WEAPON PANEL", "Save and display your PBs on TAB."));
                var DamagePanel = new ToggleSubsettingElement(GeneralPanels.Subsettings, new Tweaks.Metadata("DPS PANEL", "Save and display your PBs on TAB."));
                var SpeedPanel = new ToggleSubsettingElement(GeneralPanels.Subsettings, new Tweaks.Metadata("SPEEDOMETER", "Save and display your PBs on TAB."));
            #endregion

            #region Page 4 - VIEWMODEL
            var ViewmodelTrans = new TweakSettingElement(Pages[3].Holder, new Tweaks.Metadata("VIEWMODEL TWEAKS", "Move, resize, and tweak the viewmodel."));
                var ViewmodelScale = new SliderSubsettingElement(ViewmodelTrans.Subsettings, new Tweaks.Metadata("VIEWMODEL SIZE", "How much the VM scale is mult'd by."));
                // var ViewmodelOffset = new SliderSubsettingElement(ViewmodelTrans.Subsettings, new Tweaks.Metadata("VIEWMODEL OFFSET", "How much the VM position is offset by."));
                var ViewmodelFov = new SliderSubsettingElement(ViewmodelTrans.Subsettings, new Tweaks.Metadata("VIEWMODEL FOV", "The VM's field of view."));
                var VMAimRot = new ToggleSubsettingElement(ViewmodelTrans.Subsettings, new Tweaks.Metadata("DISABLE AIM-ASSIST ROTATION", "The viewmodel doesn't rotate with aim-assist."));
                var VMBobbing = new ToggleSubsettingElement(ViewmodelTrans.Subsettings, new Tweaks.Metadata("NO VM BOBBING", "The viewmodel doesn't bob."));
            #endregion

            #region Page 5 - CYBERGRIND
            var CustomCGMus = new TweakSettingElement(Pages[4].Holder, new Tweaks.Metadata("CUSTOM CYBERGRIND MUSIC (OGG/WAV/MP3)", "Customised Cybergrind music, chosen randomly."));
                var Comment = new CommentSubsettingElement(CustomCGMus.Subsettings, new Tweaks.Metadata("MUSIC PATH:", Path.Combine(Utils.GameDirectory(), @"BepInEx\UMM Mods\plugins\ULTRAKILLtweaker\Cybergrind Music")));
            var CGUtils = new TweakSettingElement(Pages[4].Holder, new Tweaks.Metadata("CYBERGRIND UTILITIES", "CG stats in the top left. Based on a mod by Epsypolym."));
            var NoCGScores = new TweakSettingElement(Pages[4].Holder, new Tweaks.Metadata("DISABLE CYBERGRIND SCORE SUBMISSION", "Disables CG score submission."));
            #endregion

            #region Page 6 - FUN
            var FLORPPP = new TweakSettingElement(Pages[5].Holder, new Tweaks.Metadata("SKULL FLORPIFICATION", "Skulls are replaced by Florp."));
            var Explorsion = new TweakSettingElement(Pages[5].Holder, new Tweaks.Metadata("EXPLODE ON DEATH", "When you die, everyone around you does too."));
            #endregion

            List<Setting> Settings = new List<Setting>()
            {
                new ToggleSetting("hitstop_enabled", HitstopMult.Self, false, true),
                    new SliderSubsetting("hitstopmult", HitstopLength.Self, 0, 2, 1, false, "{0}x", true),
                    new SliderSubsetting("truestopmult", TruestopLength.Self, 0, 2, 1, false, "{0}x", true),
                    new SliderSubsetting("slowdownmult", SlowdownLength.Self, 0, 2, 1, false, "{0}x", true),
                    new ToggleSetting("parryflashoff", DisableParry.Self, false, true),

                new ToggleSetting("savepbs", ShowPBs.Self, false, true),
                new ToggleSetting("dmgsub", DamageNoti.Self, false, true),

                new ToggleSetting("uiscale_enabled", UIScale.Self, false, true),
                    new SliderSubsetting("uiscalecanv", CanvasScale.Self, 0, 100, 100, true, "{0}%", true),
                    new SliderSubsetting("uiscale", InfoScale.Self, 0, 110, 100, true, "{0}%", true),
                    new SliderSubsetting("uiscalestyle", StyleScale.Self, 0, 110, 100, true, "{0}%", true),
                    new SliderSubsetting("uiscaleresults", ResultsScale.Self, 0, 100, 100, true, "{0}%", true),
                    new SliderSubsetting("uiscaleboss", BossbarScale.Self, 0, 100, 100, true, "{0}%", true),

                new ToggleSetting("forcegun", GunPanel.Self, false, true),
                new ToggleSetting("fpscounter", FPSCounter.Self, false, true),

                new ToggleSetting("panels", GeneralPanels.Self, false, true),
                    new ToggleSetting("hppanel", InfoPanel.Self, false, true),
                    new ToggleSetting("weapanel", WeaponPanel.Self, false, true),
                    new ToggleSetting("dps", DamagePanel.Self, false, true),
                    new ToggleSetting("speedometer", SpeedPanel.Self, false, true),


                new ToggleSetting("vmtrans", ViewmodelTrans.Self, false, true),
                    new SliderSubsetting("vmfov", ViewmodelFov.Self, 50, 179, 90, true, "{0}", true),
                    new SliderSubsetting("vmmodel", ViewmodelScale.Self, 0, 1.2f, 1, false, "{0}x", true),
                    new ToggleSetting("nobob", VMBobbing.Self, false, true),
                    new ToggleSetting("notilt", VMAimRot.Self, false, true),

                new ToggleSetting("cybergrindstats", CGUtils.Self, false, true),
                new ToggleSetting("cybergrindmusic", CustomCGMus.Self, false, true),
                new ToggleSetting("cybergrindnoscores", NoCGScores.Self, false, true),

                new ToggleSetting("explorsion", Explorsion.Self, false, true),
                new ToggleSetting("legally_distinct_florp", FLORPPP.Self, false, true),

                new ArtifactSetting("ARTIFACT_sandify", mods.ChildByName("Sandify"), false, true,
                    "Sandify", "Every enemy gets covered in sand. Parrying is the only way to heal."),

                new ArtifactSetting("ARTIFACT_noHP", mods.ChildByName("Fragility"), false, true,
                    "Fragility", "You only have 1 HP - if you get hit, you die.",
                    new List<string>() {"artiset_noHP_hpamount"}),

                new ArtifactSetting("ARTIFACT_glass", mods.ChildByName("Glass"), true, true,
                    "Glass", "Deal two times the damage - at the cost of 70% of your health."),

                new ArtifactSetting("ARTIFACT_superhot", mods.ChildByName("Superhot"), true, true,
                    "UltraHot", "Time only moves when you move."),

                new ArtifactSetting("ARTIFACT_tank", mods.ChildByName("Tankify"), false, true,
                    "Tankify", "Every enemy gets two times the health.",
                    new List<string>() {"artiset_tankify_mult"}),

                new ArtifactSetting("ARTIFACT_distance", mods.ChildByName("Distance"), false, true,
                    "Close Quarters", "Enemies become blessed when too far.",
                    new List<string>() {"artiset_distance_distfromplayer"}),

                new ArtifactSetting("ARTIFACT_noweapons", mods.ChildByName("No Weapons"), false, true,
                    "Empty Handed", "No weapons, punch your enemies to death. Good luck beating P-1."),

                new ArtifactSetting("ARTIFACT_nostamina", mods.ChildByName("NoStamina"), false, true,
                    "Lethargy", "V1 is tired, and has no stamina. No dash-jumps or power-slams."),

                new ArtifactSetting("ARTIFACT_diceroll", mods.ChildByName("Random"), true, true,
                    "Dice-Roll", "Every 30 seconds, your weapon loadout is randomised. Includes scrapped and unowned weapons! (if the current update has any)",
                     new List<string>() {"artiset_diceroll_timereset"}),

                new ArtifactSetting("ARTIFACT_water", mods.ChildByName("Water"), true, true,
                    "Submerged", "Every level is flooded with water."),

                new ArtifactSetting("ARTIFACT_gofast", mods.ChildByName("GoFast"), true, true,
                    "Speed", "You run at 2 times the speed. Enemy speed is multiplied by 7.5 to keep up.",
                    new List<string>() {"artiset_gofast_player", "artiset_gofast_enemy"}),

                new ArtifactSetting("ARTIFACT_noarm", mods.ChildByName("No Arms"), false, true,
                    "Disarmed", "V1 has no arms. You can't punch, whiplash, or parry."),

                new ArtifactSetting("ARTIFACT_fuelleak", mods.ChildByName("Fuel Leak"), false, true,
                    "Fuel Leak", "Blood is actually fuel, and gets used over time. Heal before all of your HP runs out.",
                    new List<string>() {"artiset_fuelleak_multi"}),

                new ArtifactSetting("ARTIFACT_whiphard", mods.ChildByName("WhipFix"), true, true,
                    "Whiplash Fix", "Reduce hard damage from whiplash use, or get rid of it entirely.",
                    new List<string>() {"artiset_whip_hard_mult"}),

                new ArtifactSetting("ARTIFACT_floorlava", mods.ChildByName("FloorIsLava"), false, true,
                    "Floor Is Lava", "You are damaged when grounded. Based on a mod by <b>nptnk#0001</b>.",
                    new List<string>() {"artiset_floorlava_mult", "artiset_floorlava_time"}),

                new ArtifactSetting("ARTIFACT_mitosis", mods.ChildByName("Mitosis"), true, true,
                    "Mitosis", "Enemies are duplicated. You can go above 10x by editing <b>settings.kel</b>. <color=red>THIS WILL SLAUGTHER YOUR FPS.</color> Idea from Vera in the UK Discord.",
                    new List<string>() {"artiset_mitosis_amount"}),

                new ArtifactSetting("ARTIFACT_fresh", mods.ChildByName("Fresh"), false, true,
                    "Freshness", "You get hurt whenever your style rank is below a certain amount. Very configurable.",
                    new List<string>() {"artiset_fresh_fr", "artiset_fresh_us", "artiset_fresh_st", "artiset_fresh_du"}),

                 new ArtifactSetting("ARTIFACT_ice", mods.ChildByName("Ice"), true, true,
                    "ULTRAKILL ON ICE", "You become slippery.",
                    new List<string>() {"artiset_ice_frict"}),

                new SliderSetting("artiset_fuelleak_multi", mods.ChildByName("Fuel Leak").ChildByName("Extra Settings").ChildByName("SETTINGS").ChildByName("Panel").ChildByName("Damage Drain"), 0.1f, 2, 1, false, "{0}x"),
                new SliderSetting("artiset_noHP_hpamount", mods.ChildByName("Fragility").ChildByName("Extra Settings").ChildByName("SETTINGS").ChildByName("Panel").ChildByName("HP"), 1, 100, 1, true, "{0} HP"),
                new SliderSetting("artiset_diceroll_timereset", mods.ChildByName("Random").ChildByName("Extra Settings").ChildByName("SETTINGS").ChildByName("Panel").ChildByName("Time"), 5, 300, 30, true, "{0}s"),
                new SliderSetting("artiset_distance_distfromplayer", mods.ChildByName("Distance").ChildByName("Extra Settings").ChildByName("SETTINGS").ChildByName("Panel").ChildByName("Distance"), 5, 50, 15, true, "{0} u"),
                new SliderSetting("artiset_gofast_player", mods.ChildByName("GoFast").ChildByName("Extra Settings").ChildByName("SETTINGS").ChildByName("Panel").ChildByName("Player"), 0.5f, 7.5f, 2, false, "{0}x"),
                new SliderSetting("artiset_gofast_enemy", mods.ChildByName("GoFast").ChildByName("Extra Settings").ChildByName("SETTINGS").ChildByName("Panel").ChildByName("Enemy"), 0.5f, 7.5f, 7.5f, false, "{0}x"),
                new SliderSetting("artiset_tankify_mult", mods.ChildByName("Tankify").ChildByName("Extra Settings").ChildByName("SETTINGS").ChildByName("Panel").ChildByName("Mult"), 1f, 10f, 2f, false, "{0}x"),
                new SliderSetting("artiset_whip_hard_mult", mods.ChildByName("WhipFix").ChildByName("Extra Settings").ChildByName("SETTINGS").ChildByName("Panel").ChildByName("Mult"), 0f, 2f, 0.5f, false, "{0}x"),
                new SliderSetting("artiset_floorlava_mult", mods.ChildByName("FloorIsLava").ChildByName("Extra Settings").ChildByName("SETTINGS").ChildByName("Panel").ChildByName("Mult"), 0.1f, 20, 1, false, "{0}x"),
                new SliderSetting("artiset_floorlava_time", mods.ChildByName("FloorIsLava").ChildByName("Extra Settings").ChildByName("SETTINGS").ChildByName("Panel").ChildByName("Wait"), 0f, 5, 0, false, "{0}s"),
                new SliderSetting("artiset_mitosis_amount", mods.ChildByName("Mitosis").ChildByName("Extra Settings").ChildByName("SETTINGS").ChildByName("Panel").ChildByName("Mult"), 2, 10, 2, true, "{0}x"),
                new SliderSetting("artiset_fresh_fr", mods.ChildByName("Fresh").ChildByName("Extra Settings").ChildByName("SETTINGS").ChildByName("Panel").ChildByName("Fresh"), 0, 100, 0, true, "{0}"),
                new SliderSetting("artiset_fresh_us", mods.ChildByName("Fresh").ChildByName("Extra Settings").ChildByName("SETTINGS").ChildByName("Panel").ChildByName("Used"), 0, 100, 1, true, "{0}"),
                new SliderSetting("artiset_fresh_st", mods.ChildByName("Fresh").ChildByName("Extra Settings").ChildByName("SETTINGS").ChildByName("Panel").ChildByName("Stale"), 0, 100, 2, true, "{0}"),
                new SliderSetting("artiset_fresh_du", mods.ChildByName("Fresh").ChildByName("Extra Settings").ChildByName("SETTINGS").ChildByName("Panel").ChildByName("Dull"), 0, 100, 5, true, "{0}"),
                new SliderSetting("artiset_ice_frict", mods.ChildByName("Ice").ChildByName("Extra Settings").ChildByName("SETTINGS").ChildByName("Panel").ChildByName("Friction"), 0, 2, 0.5f, false, "{0}x")
            };

            foreach (Setting set in Settings)
                SettingRegistry.settings.Add(set);

            #endregion

            foreach (Setting setting in SettingRegistry.settings)
            {
                SettingRegistry.idToSetting.Add(setting.ID, setting);
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
                if (Page != Pages.Count - 1)
                {
                    Page++;
                    UpdatePages();
                }
            });

            // TODO: Fix
            //Pages[0].PageContent().ChildByName("Reset").GetComponent<Button>().onClick.AddListener(() =>
            //{
            //    OptionsMenu.SetActive(false);
            //    SettingRegistry.Validate(true);
            //    OptionsMenu.SetActive(true);
            //});
            #endregion

            UpdatePages();

            PatchMainMenu(TweakerMenu.transform.parent.transform.parent.GetComponent<OptionsMenuToManager>());

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
                    Pages[i].Self.SetActive(false);
                }
                else
                {
                    Pages[i].Self.SetActive(true);
                }
            }
        }

        public void PatchMainMenu(OptionsMenuToManager __instance)
        {
            if (__instance.pauseMenu.name == "Main Menu (1)") // check to see that we're patching out the main menu's menu, not like an in game menu one
            {
                __instance.pauseMenu.transform.Find("Panel").localPosition = new Vector3(0, 325, 0);

                void Halve(Transform tf, bool left)
                {
                    tf.GetComponent<RectTransform>().sizeDelta = new Vector2(240, 80);
                    if (left)
                        tf.localPosition -= new Vector3(120f, 0, 0);
                    else
                        tf.localPosition += new Vector3(120f, 0, 0);
                }

                Transform options = __instance.pauseMenu.transform.Find("Credits");
                Halve(options, true);

                GameObject TexPack = GameObject.Instantiate(options.gameObject);
                GameObject cont = __instance.pauseMenu.transform.Find("Continue(Clone)").gameObject;

                TexPack.transform.parent = cont.transform.parent;
                TexPack.transform.position = cont.transform.position;
                TexPack.transform.position = new Vector3(TexPack.transform.position.x, options.transform.position.y, TexPack.transform.position.z);

                TexPack.name = "TexPackButton";
                TexPack.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
                TexPack.GetComponent<Button>().onClick.AddListener(() =>
                {
                    TexPackMenu.SetActive(true);
                });

                TexPackMenu.GetComponent<Canvas>().sortingOrder = 6969;
                TexPack.ChildByName("Text").GetComponent<Text>().text = "PACKS";

                Utils.SetPrivate_Field(TexPack.GetComponent<HudOpenEffect>(), "originalHeight", 1f);
                Utils.SetPrivate_Field(TexPack.GetComponent<HudOpenEffect>(), "originalWidth", 1f);
            }

            // Set up resource pack stuff
            ResourcePack.DisabledContent = TexPackMenu.ChildByName("LeftHolder").ChildByName("LeftPack").ChildByName("Viewport").ChildByName("Content");
            ResourcePack.EnabledContent = TexPackMenu.ChildByName("RightHolder").ChildByName("RightPack").ChildByName("Viewport").ChildByName("Content");

            foreach (ResourcePack pack in ResourcePack.Packs)
            {
                if (pack.metadata.ID != "NO ID, DELETE THIS PACK")
                {
                    GameObject go = pack.GetListEntry();

                    if (pack.Enabled)
                        go.transform.parent = ResourcePack.EnabledContent.transform;
                    else
                        go.transform.parent = ResourcePack.DisabledContent.transform;

                    go.transform.localScale = Vector3.one;
                }
            }

            TexPackMenu.SetActive(false);
        }
        #endregion

        #region Stuff that happens on Update
        public void Update()
        {

            if (Input.GetKeyDown(KeyCode.N))
            {
                // StartCoroutine(ResourcePack.PatchTextures());
            }

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

                if (SceneManager.GetActiveScene().name != "Main Menu" && !HasntHappenedThisScene)
                {
                    
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

            if (Utils.GetSetting<bool>("ARTIFACT_noweapons"))
            {
                hud.ChildByName("GunCanvas").ChildByName("GunPanel").SetActive(false);
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

            if (Utils.GetSetting<bool>("ARTIFACT_noarm"))
            {
                player.ChildByName("Main Camera").ChildByName("Punch").SetActive(false);
            }

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
        #endregion

        #region Harmony Patches

        [HarmonyPatch(typeof(FinalRank), nameof(FinalRank.SetTime))]
        class SaveTimes
        { 
            static void Postfix()
            {
                if (Times.SceneToTime.ContainsKey(SceneManager.GetActiveScene().name))
                {
                    if(Times.SceneToTime[SceneManager.GetActiveScene().name] > Instance.statman.seconds)
                        Times.SceneToTime[SceneManager.GetActiveScene().name] = Instance.statman.seconds;

                    if (Times.SceneToKills[SceneManager.GetActiveScene().name] < Instance.statman.kills)
                        Times.SceneToKills[SceneManager.GetActiveScene().name] = Instance.statman.kills;

                    if (Times.SceneToStyle[SceneManager.GetActiveScene().name] < Instance.statman.stylePoints)
                        Times.SceneToStyle[SceneManager.GetActiveScene().name] = Instance.statman.stylePoints;
                } else
                {
                    Times.SceneToTime.Add(SceneManager.GetActiveScene().name, Instance.statman.seconds);
                    Times.SceneToKills.Add(SceneManager.GetActiveScene().name, Instance.statman.kills);
                    Times.SceneToStyle.Add(SceneManager.GetActiveScene().name, Instance.statman.stylePoints);
                }

                Times.Save();
            }
        }



        [HarmonyPatch(typeof(Text), "OnEnable")]
        class PatchFont
        {
            static void Prefix(Text __instance)
            {
                if (__instance.gameObject.GetComponent<ReplacementController>() == null)
                {
                    __instance.gameObject.AddComponent<ReplacementController>();

                    foreach (ResourcePack resourcePack in ResourcePack.Packs)
                    {
                        if (resourcePack.Font != null && resourcePack.Enabled)
                        {
                            __instance.font = resourcePack.Font;
                            __instance.fontSize = (int)(__instance.fontSize * resourcePack.FontScale);
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Material), nameof(Material.mainTexture), MethodType.Getter)]
        class PatchRen
        {
            static void NotActuallyPrefix(Material __instance)
            {
                Debug.Log("In thing");

                if (!__instance.name.Contains("_UKTSWAPPED") && SceneManager.GetActiveScene().name != "Intro")
                {
                    __instance.name += "_UKTSWAPPED";

                    Debug.Log($"Swap attempt mainTexture: mat;{__instance.name}, tex;{__instance.mainTexture.name}");

                    if (ResourcePack.IDtoTex.Keys.Contains(__instance.mainTexture.name))
                    {
                        __instance.mainTexture = ResourcePack.IDtoTex[__instance.mainTexture.name];
                    }
                    __instance.name = __instance.name.Replace("_UKTSWAPPED", "");
                } else
                {
                    Debug.Log($"Embed fail mainTexture: mat;{__instance.name}");
                }
            }
        }

        
        class PatchSound
        {
            [HarmonyPatch(typeof(AudioSource), "PlayHelper")]
            [HarmonyPrefix]
            static void Prefix_NoArgs(AudioSource __instance)
            {
                //Debug.Log("play");
                 
                //if (ResourcePack.IDtoClip.Keys.Contains(__instance.clip.name))
                //    __instance.clip = ResourcePack.IDtoClip[__instance.clip.name];
            }
        }

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

                        GameObject fLORP = Instantiate(UIBundle.LoadAsset<GameObject>(FLORP_NameToGoName[Florp]), renderer.transform);
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
                            mods += ((ArtifactSetting)mod).GetDetails() + ", ";

                        }
                    }
                }

                if(ModAmount != 0)
                    text.text = "<size=14>+ " + mods.Substring(0, mods.Length - 2) + "</size>\n<size=12>\n</size>" + text.text;
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

