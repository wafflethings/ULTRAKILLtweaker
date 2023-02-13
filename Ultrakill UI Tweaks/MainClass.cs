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
            { "cybergrindmusic", typeof(CGMusic) },
            { "cybergrindstats", typeof(CGUtils) },
            { "legally_distinct_florp", typeof(Florpification) },
            { "explorsion", typeof(SelfExplo) },

            { "ARTIFACT_noweapons", typeof(NoWeapons) },
            { "ARTIFACT_noarm", typeof(NoArms) },
            { "ARTIFACT_nostamina", typeof(NoStamina) },
            { "ARTIFACT_tank", typeof(Tankify) },
            { "ARTIFACT_sandify", typeof(Sandify) },
            { "ARTIFACT_distance", typeof(Distance) },
            { "ARTIFACT_mitosis", typeof(Mitosis) },
            { "ARTIFACT_noHP", typeof(Fragility) },
            { "ARTIFACT_fuelleak", typeof(FuelLeak) },
            { "ARTIFACT_floorlava", typeof(FloorIsLava) },
            { "ARTIFACT_fresh", typeof(Fresh) },
            { "ARTIFACT_gofast", typeof(Speed) },
            { "ARTIFACT_ice", typeof(Ice) },
            { "ARTIFACT_superhot", typeof(Superhot) },
            { "ARTIFACT_glass", typeof(GlassCannon) },
            { "ARTIFACT_diceroll", typeof(DiceRoll) },
            { "ARTIFACT_water", typeof(Submerged) },
            { "ARTIFACT_whiphard", typeof(WhipFix) }
        };

        public Dictionary<Type, TweakHandler> TypeToHandler = new Dictionary<Type, TweakHandler>()
        {
        };

        // UI elements.
        public GameObject TweakerButton;
        public GameObject OptionsMenu;
        public GameObject TweakerMenu;

        // Stuff that handles the pages for the tweaks.
        public List<Page> Pages = new List<Page>();
        public GameObject Modifiers;
        public static int Page = 0;

        // The assetbundle which all of the UKt assets are from.
        public static AssetBundle UIBundle;

        public Harmony harmony;

        // Some mods need a scene reload to work, so if mods are changed in game this is set to true. If it is true on the option menu being turned off, the scene reloads.
        public bool ModsChanged = false;

        // Have settings initialised? Needed so that idToSetting isn't null. 
        public bool SettingsInit = false;

        // I don't remember but it doesn't need changing 
        bool HasFirstPatch = false;
        bool NotFirstLoad = false;
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

            UIElement.LoadSettingElements();
            Times.Load();
        }

        public override void OnModUnload()
        {
            // Unpatch Harmony, destroy GOs from UKt, remove events set, all the stuff that happens on startup.
            harmony.UnpatchSelf();

            List<GameObject> ToDestroy = new List<GameObject>()
            {
                TweakerButton, TweakerMenu
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

            // Load the assetbundle if it isn't loaded.
            if (UIBundle == null)
            {
                UIBundle = AssetBundle.LoadFromFile(Path.Combine(Utils.GameDirectory(), @"BepInEx\UMM Mods\ULTRAKILLtweaker\tweakerassets.bundle"));
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
                new Page("--TWEAKS: FUN--", TweakerMenu.ChildByName("Tweaks").transform),
                new Page("--TWEAKS: MUTATORS--", TweakerMenu.ChildByName("Tweaks").transform)
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

            var ShowPBs = new TweakSettingElement(Pages[0].Holder, new Tweaks.Metadata("SHOW PBs FOR TIME, KILLS, AND STYLE", "Save and display your PBs on TAB. Hovering on the rank in level select will also show them."));
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
            var GunPanel = new TweakSettingElement(Pages[1].Holder, new Tweaks.Metadata("FORCE GUN PANEL (FOR ALT RAILCANNON PIP)", "Forces the gun panel. Disable WEAPON ICON in HUD, and you will be able to use the alternate railcannon charge indicator.", true));
            #endregion

            #region Page 3 - PANELS
            var GeneralPanels = new TweakSettingElement(Pages[2].Holder, new Tweaks.Metadata("CUSTOM PANELS", "Various small UI elements."));
                var InfoPanel = new ToggleSubsettingElement(GeneralPanels.Subsettings, new Tweaks.Metadata("INFO PANEL", "Shows HP, stamina, rail charge."));
                var WeaponPanel = new ToggleSubsettingElement(GeneralPanels.Subsettings, new Tweaks.Metadata("WEAPON PANEL", "Save and display your PBs on TAB."));
                var DamagePanel = new ToggleSubsettingElement(GeneralPanels.Subsettings, new Tweaks.Metadata("DPS PANEL", "Save and display your PBs on TAB."));
                var SpeedPanel = new ToggleSubsettingElement(GeneralPanels.Subsettings, new Tweaks.Metadata("SPEEDOMETER", "Save and display your PBs on TAB."));
            #endregion

            #region Page 4 - VIEWMODEL
            var ViewmodelTrans = new TweakSettingElement(Pages[3].Holder, new Tweaks.Metadata("VIEWMODEL TWEAKS", "Move, resize, and tweak the viewmodel.", true));
                var ViewmodelScale = new SliderSubsettingElement(ViewmodelTrans.Subsettings, new Tweaks.Metadata("VIEWMODEL SIZE", "How much the VM scale is mult'd by."));
            // var ViewmodelOffset = new SliderSubsettingElement(ViewmodelTrans.Subsettings, new Tweaks.Metadata("VIEWMODEL OFFSET", "How much the VM position is offset by."));
                var ViewmodelFov = new SliderSubsettingElement(ViewmodelTrans.Subsettings, new Tweaks.Metadata("VIEWMODEL FOV", "The VM's field of view."));
                var VMAimRot = new ToggleSubsettingElement(ViewmodelTrans.Subsettings, new Tweaks.Metadata("DISABLE AIM-ASSIST ROTATION", "The viewmodel doesn't rotate with aim-assist."));
                var VMBobbing = new ToggleSubsettingElement(ViewmodelTrans.Subsettings, new Tweaks.Metadata("NO VM BOBBING", "The viewmodel doesn't bob."));
            #endregion

            #region Page 5 - CYBERGRIND
            var CustomCGMus = new TweakSettingElement(Pages[4].Holder, new Tweaks.Metadata("CUSTOM CYBERGRIND MUSIC (OGG/WAV/MP3)", "Customised Cybergrind music, chosen randomly.", true));
                var Comment = new CommentSubsettingElement(CustomCGMus.Subsettings, new Tweaks.Metadata("MUSIC PATH:", Path.Combine(Utils.GameDirectory(), @"BepInEx\UMM Mods\plugins\ULTRAKILLtweaker\Cybergrind Music")));
            var CGUtils = new TweakSettingElement(Pages[4].Holder, new Tweaks.Metadata("CYBERGRIND UTILITIES", "CG stats in the top left. Based on a mod by Epsypolym.", true));
            #endregion

            #region Page 6 - FUN
            var FLORPPP = new TweakSettingElement(Pages[5].Holder, new Tweaks.Metadata("SKULL FLORPIFICATION", "Skulls are replaced by Florp.", true));
            var Explorsion = new TweakSettingElement(Pages[5].Holder, new Tweaks.Metadata("EXPLODE ON DEATH", "When you die, everyone around you does too."));
            #endregion

            #region Page 7 - MUTATORS

            var NoWeapons = new MutatorElement(Pages[6].Holder, new Tweaks.Metadata("EMPTY HANDED", "No weapons, punch your enemies to death. Good luck beating P-1, I've done it."));
          
            var NoArms = new MutatorElement(Pages[6].Holder, new Tweaks.Metadata("DISARMED", "V1 has no arms. You can't punch, whiplash, or parry."));
           
            var NoStamina = new MutatorElement(Pages[6].Holder, new Tweaks.Metadata("LETHARGY", "V1 is tired, and has no stamina. No dash-jumps or power-slams."));
           
            var Tankify = new MutatorElement(Pages[6].Holder, new Tweaks.Metadata("TANKIFY", "Every enemy gets more health."));
                var TankifyMult = new SliderSubsettingElement(Tankify.Subsettings, new Tweaks.Metadata("MULTIPLIER", "Enemy health multiplier."));
          
            var Sandify = new MutatorElement(Pages[6].Holder, new Tweaks.Metadata("SANDIFY", "Every enemy gets covered in sand. Parrying is the only way to heal."));
          
            var Distance = new MutatorElement(Pages[6].Holder, new Tweaks.Metadata("CLOSE QUARTERS", "Enemies become blessed when too far."));
                var DistanceDist = new SliderSubsettingElement(Distance.Subsettings, new Tweaks.Metadata("DISTANCE", "Distance for activation."));
           
            var Mitosis = new MutatorElement(Pages[6].Holder, new Tweaks.Metadata("MITOSIS", "Enemies are duplicated. You can go above 10x, if you enter it several times."));
                var MitosisAmount = new SliderSubsettingElement(Mitosis.Subsettings, new Tweaks.Metadata("MULTIPLIER", "How many enemies per enemy."));
           
            var Fragility = new MutatorElement(Pages[6].Holder, new Tweaks.Metadata("FRAGILITY", "Your health cap is lower."));
                var FragilityMult = new SliderSubsettingElement(Fragility.Subsettings, new Tweaks.Metadata("CAP", "The maximum amount of HP."));
           
            var FuelLeak = new MutatorElement(Pages[6].Holder, new Tweaks.Metadata("FUEL LEAK", "Blood is actually fuel, and gets used over time. Heal before all of your HP runs out."));
                var FLMulti = new SliderSubsettingElement(FuelLeak.Subsettings, new Tweaks.Metadata("MULTIPLIER", "HP lost per second."));
           
            var FloorIsLava = new MutatorElement(Pages[6].Holder, new Tweaks.Metadata("FLOOR IS LAVA", "You are damaged when grounded. Based on a mod by <b>nptnk#0001</b>."));
                var FILDmg = new SliderSubsettingElement(FloorIsLava.Subsettings, new Tweaks.Metadata("DAMAGE", "Damage lost per second."));
                var FILTime = new SliderSubsettingElement(FloorIsLava.Subsettings, new Tweaks.Metadata("TIME", "Time before you start getting hurt."));
          
            var Freshness = new MutatorElement(Pages[6].Holder, new Tweaks.Metadata("FRESHNESS", "You get hurt whenever your freshness rank is below a certain amount."));
                var FrFr = new SliderSubsettingElement(Freshness.Subsettings, new Tweaks.Metadata("FRESH", "HP lost per second on the 'Fresh' rank."));
                var FrUs = new SliderSubsettingElement(Freshness.Subsettings, new Tweaks.Metadata("USED", "HP lost per second on the 'Used' rank."));
                var FrSt = new SliderSubsettingElement(Freshness.Subsettings, new Tweaks.Metadata("STALE", "HP lost per second on the 'Stale' rank."));
                var FrDu = new SliderSubsettingElement(Freshness.Subsettings, new Tweaks.Metadata("DULL", "HP lost per second on the 'Dull' rank."));
          
            var Speed = new MutatorElement(Pages[6].Holder, new Tweaks.Metadata("SPEED", "Change the speed of both yourself, and your enemies."));
                var SpeedPlayer = new SliderSubsettingElement(Speed.Subsettings, new Tweaks.Metadata("PLAYER", "Player speed multiplier."));
                var SpeedEnemy = new SliderSubsettingElement(Speed.Subsettings, new Tweaks.Metadata("ENEMY", "Enemy speed multiplier."));
          
            var Ice = new MutatorElement(Pages[6].Holder, new Tweaks.Metadata("ULTRAKILL ON ICE", "You become slippery."));
                var IceFriction = new SliderSubsettingElement(Ice.Subsettings, new Tweaks.Metadata("FRICTION", "Friction while walking."));
          
            var SuperHot = new MutatorElement(Pages[6].Holder, new Tweaks.Metadata("ULTRAHOT", "Time only moves when you move."));
          
            var Glass = new MutatorElement(Pages[6].Holder, new Tweaks.Metadata("GLASS", "Deal two times the damage - at the cost of 70% of your health."));
          
            var RandomLoadout = new MutatorElement(Pages[6].Holder, new Tweaks.Metadata("DICE ROLL", "Your loadout is randomised at a chosen interval."));
                var RLTime = new SliderSubsettingElement(RandomLoadout.Subsettings, new Tweaks.Metadata("INTERVAL", "Time between resets."));
          
            var Water = new MutatorElement(Pages[6].Holder, new Tweaks.Metadata("SUBMERGED", "Every level is flooded with water."));
           
            var WhipFix = new MutatorElement(Pages[6].Holder, new Tweaks.Metadata("WHIP FIX", "Change the hard damage gained from whiplashing. Made pre-Patch11b, obsolete."));
                var WFMult = new SliderSubsettingElement(WhipFix.Subsettings, new Tweaks.Metadata("MULTIPLIER", "Hard damage multiplier."));

            #endregion

            List<Setting> Settings = new List<Setting>()
            {
                new ToggleSetting("hitstop_enabled", HitstopMult.Self, false),
                    new SliderSubsetting("hitstopmult", HitstopLength.Self, 0, 2, 1, false, "{0}x"),
                    new SliderSubsetting("truestopmult", TruestopLength.Self, 0, 2, 1, false, "{0}x"),
                    new SliderSubsetting("slowdownmult", SlowdownLength.Self, 0, 2, 1, false, "{0}x"),
                    new ToggleSetting("parryflashoff", DisableParry.Self, false),

                new ToggleSetting("savepbs", ShowPBs.Self, false),
                new ToggleSetting("dmgsub", DamageNoti.Self, false),

                new ToggleSetting("uiscale_enabled", UIScale.Self, false),
                    new SliderSubsetting("uiscalecanv", CanvasScale.Self, 0, 100, 100, true, "{0}%"),
                    new SliderSubsetting("uiscale", InfoScale.Self, 0, 110, 100, true, "{0}%"),
                    new SliderSubsetting("uiscalestyle", StyleScale.Self, 0, 110, 100, true, "{0}%"),
                    new SliderSubsetting("uiscaleresults", ResultsScale.Self, 0, 100, 100, true, "{0}%"),
                    new SliderSubsetting("uiscaleboss", BossbarScale.Self, 0, 100, 100, true, "{0}%"),

                new ToggleSetting("forcegun", GunPanel.Self, false),
                new ToggleSetting("fpscounter", FPSCounter.Self, false),

                new ToggleSetting("panels", GeneralPanels.Self, false),
                    new ToggleSetting("hppanel", InfoPanel.Self, false),
                    new ToggleSetting("weapanel", WeaponPanel.Self, false),
                    new ToggleSetting("dps", DamagePanel.Self, false),
                    new ToggleSetting("speedometer", SpeedPanel.Self, false),


                new ToggleSetting("vmtrans", ViewmodelTrans.Self, false),
                    new SliderSubsetting("vmfov", ViewmodelFov.Self, 50, 179, 90, true, "{0}"),
                    new SliderSubsetting("vmmodel", ViewmodelScale.Self, 0, 1.2f, 1, false, "{0}x"),
                    new ToggleSetting("nobob", VMBobbing.Self, false),
                    new ToggleSetting("notilt", VMAimRot.Self, false),

                new ToggleSetting("cybergrindstats", CGUtils.Self, false),
                new ToggleSetting("cybergrindmusic", CustomCGMus.Self, false),

                new ToggleSetting("explorsion", Explorsion.Self, false),
                new ToggleSetting("legally_distinct_florp", FLORPPP.Self, false),

                new ToggleSetting("ARTIFACT_noweapons", NoWeapons.Self, false),
                new ToggleSetting("ARTIFACT_noarm", NoArms.Self, false),
                new ToggleSetting("ARTIFACT_nostamina", NoStamina.Self, false),
                new ToggleSetting("ARTIFACT_tank", Tankify.Self, false),
                    new SliderSubsetting("artiset_tankify_mult", TankifyMult.Self, 1f, 10f, 2f, false, "{0}x"),
                new ToggleSetting("ARTIFACT_sandify", Sandify.Self, false),
                new ToggleSetting("ARTIFACT_distance", Distance.Self, false),
                    new SliderSubsetting("artiset_distance_distfromplayer", DistanceDist.Self, 5, 50, 15, true, "{0} u"),
                new ToggleSetting("ARTIFACT_mitosis", Mitosis.Self, false),
                    new SliderSubsetting("artiset_mitosis_amount", MitosisAmount.Self, 2, 10, 2, true, "{0}x"),
                new ToggleSetting("ARTIFACT_noHP", Fragility.Self, false),
                    new SliderSubsetting("artiset_noHP_hpamount", FragilityMult.Self, 1, 100, 1, true, "{0} HP"),
                new ToggleSetting("ARTIFACT_fuelleak", FuelLeak.Self, false),
                    new SliderSubsetting("artiset_fuelleak_multi", FLMulti.Self, 1, 20, 5, true, "{0}"),
                new ToggleSetting("ARTIFACT_floorlava", FloorIsLava.Self, false),
                    new SliderSubsetting("artiset_floorlava_mult", FILDmg.Self, 1, 100, 100, true, "{0}"),
                    new SliderSubsetting("artiset_floorlava_time", FILTime.Self, 0f, 5, 0.1f, false, "{0}s"),
                new ToggleSetting("ARTIFACT_fresh", Freshness.Self, false),
                    new SliderSubsetting("artiset_fresh_fr", FrFr.Self, 0, 100, 0, true, "{0}"),
                    new SliderSubsetting("artiset_fresh_us", FrUs.Self, 0, 100, 4, true, "{0}"),
                    new SliderSubsetting("artiset_fresh_st", FrSt.Self, 0, 100, 8, true, "{0}"),
                    new SliderSubsetting("artiset_fresh_du", FrDu.Self, 0, 100, 12, true, "{0}"),
                new ToggleSetting("ARTIFACT_gofast", Speed.Self, false),
                    new SliderSubsetting("artiset_gofast_player", SpeedPlayer.Self, 0.5f, 7.5f, 2, false, "{0}x"),
                    new SliderSubsetting("artiset_gofast_enemy", SpeedEnemy.Self, 0.5f, 7.5f, 2, false, "{0}x"),
                new ToggleSetting("ARTIFACT_ice", Ice.Self, false),
                    new SliderSubsetting("artiset_ice_frict", IceFriction.Self, 0, 2, 0.5f, false, "{0}x"),
                new ToggleSetting("ARTIFACT_superhot", SuperHot.Self, false),
                new ToggleSetting("ARTIFACT_glass", Glass.Self, false),
                new ToggleSetting("ARTIFACT_diceroll", RandomLoadout.Self, false),
                    new SliderSubsetting("artiset_diceroll_timereset", RLTime.Self, 5, 300, 30, true, "{0}s"),
                new ToggleSetting("ARTIFACT_water", Water.Self, false),
                new ToggleSetting("ARTIFACT_whiphard", WhipFix.Self, false),
                    new SliderSubsetting("artiset_whip_hard_mult", WFMult.Self, 0f, 2f, 0.5f, false, "{0}x"),
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
                        TweakerButton.transform.position = new Vector3((45f / 1920f) * Screen.width, (1000f / 1080f) * Screen.height, 0);
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
                #endregion

            }
        }

        #endregion

        #region Harmony Patches

        [HarmonyPatch(typeof(FinalRank), "SetRank")]
        public static class ModResultsPatch
        {
            public static void Postfix(FinalRank __instance)
            {
                GameObject hud = NewMovement.Instance.gameObject.ChildByName("Main Camera").ChildByName("HUD Camera").ChildByName("HUD");
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

                HudMessageReceiver.Instance.SendHudMessage(mods);
            }
        }
    }
    #endregion
}

