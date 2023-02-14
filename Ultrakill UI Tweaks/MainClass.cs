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

        public static Dictionary<string, Type> IDToType = new Dictionary<string, Type>()
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

        public static Dictionary<Type, TweakHandler> TypeToHandler = new Dictionary<Type, TweakHandler>()
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
                    //TODO disable for bad
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

            List<Setting> Settings = new List<Setting>()
            {
                new ToggleSetting("hitstop_enabled", new TweakSettingElement(Pages[0].Holder, new Metadata("HITSTOP MULTIPLIER", "Increase or decrease the length of hitstop."), new Setting[]
                {
                    new SliderSubsetting("hitstopmult", new SliderSubsettingElement(new Metadata("HITSTOP LENGTH", "The HitStop property.")).Self, 0, 2, 1, false, "{0}x"),
                    new SliderSubsetting("truestopmult", new SliderSubsettingElement(new Metadata("TRUESTOP LENGTH", "The TrueStop property.")).Self, 0, 2, 1, false, "{0}x"),
                    new SliderSubsetting("slowdownmult", new SliderSubsettingElement(new Metadata("SLOWDOWN LENGTH", "The SlowDown property.")).Self, 0, 2, 1, false, "{0}x"),
                    new ToggleSetting("parryflashoff", new ToggleSubsettingElement(new Metadata("DISABLE PARRY FLASH", "Disable the parry flash.")), false),
                }), false),

                new ToggleSetting("savepbs", new TweakSettingElement(Pages[0].Holder, new Metadata("SHOW PBs FOR TIME, KILLS, AND STYLE", "Save and display your PBs on TAB. Hovering on the rank in level select will also show them.")), false),
                new ToggleSetting("dmgsub", new TweakSettingElement(Pages[0].Holder, new Metadata("DAMAGE NOTIFICATION (EXPERIMENTAL)", "Show your hits, with info, as subtitles.")), false),

                new ToggleSetting("uiscale_enabled", new TweakSettingElement(Pages[1].Holder, new Metadata("HUD SCALE", "Change the size of various HUD elements.", true), new Setting[]
                {
                    new SliderSubsetting("uiscalecanv", new SliderSubsettingElement(new Metadata("CANVAS SCALE (EXPERIMENTAL)", "Scale of the UI canvas.")).Self, 0, 100, 100, true, "{0}%"),
                    new SliderSubsetting("uiscale", new SliderSubsettingElement(new Metadata("INFO PANEL SCALE", "Scale of the HP + more panel.")).Self, 0, 110, 100, true, "{0}%"),
                    new SliderSubsetting("uiscalestyle", new SliderSubsettingElement(new Metadata("STYLE PANEL SCALE", "Scale of the style panel.")).Self, 0, 110, 100, true, "{0}%"),
                    new SliderSubsetting("uiscaleresults", new SliderSubsettingElement(new Metadata("BOSSBAR SCALE", "Scale of the bossbar.")).Self, 0, 100, 100, true, "{0}%"),
                    new SliderSubsetting("uiscaleboss", new SliderSubsettingElement(new Metadata("RESULTS PANEL SCALE", "Scale of the results panel.")).Self, 0, 100, 100, true, "{0}%"),
                }), false),

                new ToggleSetting("forcegun", new TweakSettingElement(Pages[1].Holder, new Metadata("FORCE GUN PANEL (FOR ALT RAILCANNON PIP)", "Forces the gun panel. Disable WEAPON ICON in HUD, and you will be able to use the alternate railcannon charge indicator.", true)), false),
                new ToggleSetting("fpscounter", new TweakSettingElement(Pages[1].Holder, new Metadata("DISPLAY FPS COUNTER", "Save and display your PBs on TAB.")), false),

                new ToggleSetting("panels", new TweakSettingElement(Pages[2].Holder, new Metadata("CUSTOM PANELS", "Various small UI elements."), new Setting[]
                {
                    new ToggleSetting("hppanel", new ToggleSubsettingElement(new Metadata("INFO PANEL", "Shows HP, stamina, rail charge.")), false),
                    new ToggleSetting("weapanel", new ToggleSubsettingElement(new Metadata("WEAPON PANEL", "Save and display your PBs on TAB.")), false),
                    new ToggleSetting("dps", new ToggleSubsettingElement(new Metadata("DPS PANEL", "Save and display your PBs on TAB.")), false),
                    new ToggleSetting("speedometer", new ToggleSubsettingElement(new Metadata("SPEEDOMETER", "Save and display your PBs on TAB.")), false),
                }), false),

                new ToggleSetting("vmtrans", new TweakSettingElement(Pages[3].Holder, new Metadata("VIEWMODEL TWEAKS", "Move, resize, and tweak the viewmodel.", true), new Setting[]
                {
                    new SliderSubsetting("vmfov", new SliderSubsettingElement(new Metadata("VIEWMODEL FOV", "The VM's field of view.")).Self, 50, 179, 90, true, "{0}"),
                    new SliderSubsetting("vmmodel", new SliderSubsettingElement(new Metadata("VIEWMODEL SIZE", "How much the VM scale is mult'd by.")).Self, 0, 1.2f, 1, false, "{0}x"),
                    new ToggleSetting("nobob", new ToggleSubsettingElement(new Metadata("DISABLE AIM-ASSIST ROTATION", "The viewmodel doesn't rotate with aim-assist.")), false),
                    new ToggleSetting("notilt", new ToggleSubsettingElement(new Metadata("NO VM BOBBING", "The viewmodel doesn't bob.")), false),
                }), false),

                new ToggleSetting("cybergrindstats", new TweakSettingElement(Pages[4].Holder, new Metadata("CYBERGRIND UTILITIES", "CG stats in the top left. Based on a mod by Epsypolym.", true)), false),
                new ToggleSetting("cybergrindmusic", new TweakSettingElement(Pages[4].Holder, new Metadata("CUSTOM CYBERGRIND MUSIC (OGG/WAV/MP3)", Path.Combine(Utils.GameDirectory(), @"BepInEx\UMM Mods\plugins\ULTRAKILLtweaker\Cybergrind Music"), true)), false),

                new ToggleSetting("explorsion", new TweakSettingElement(Pages[5].Holder, new Metadata("SKULL FLORPIFICATION", "Skulls are replaced by Florp.", true)), false),
                new ToggleSetting("legally_distinct_florp", new TweakSettingElement(Pages[5].Holder, new Metadata("EXPLODE ON DEATH", "When you die, everyone around you does too.")), false),

                new ToggleSetting("ARTIFACT_noweapons", new MutatorElement(Pages[6].Holder, new Metadata("EMPTY HANDED", "No weapons, punch your enemies to death. Good luck beating P-1, I've done it.")), false),
                new ToggleSetting("ARTIFACT_noarm", new MutatorElement(Pages[6].Holder, new Metadata("DISARMED", "V1 has no arms. You can't punch, whiplash, or parry.")), false),
                new ToggleSetting("ARTIFACT_nostamina", new MutatorElement(Pages[6].Holder, new Metadata("LETHARGY", "V1 is tired, and has no stamina. No dash-jumps or power-slams.")), false),
                new ToggleSetting("ARTIFACT_tank", new MutatorElement(Pages[6].Holder, new Metadata("TANKIFY", "Every enemy gets more health."), new Setting[]
                {
                    new SliderSubsetting("artiset_tankify_mult", new SliderSubsettingElement(new Metadata("MULTIPLIER", "Enemy health multiplier.")).Self, 1f, 10f, 2f, false, "{0}x"),
                }), false),
                    
                new ToggleSetting("ARTIFACT_sandify", new MutatorElement(Pages[6].Holder, new Metadata("SANDIFY", "Every enemy gets covered in sand. Parrying is the only way to heal.")), false),

                new ToggleSetting("ARTIFACT_distance",new MutatorElement(Pages[6].Holder, new Metadata("CLOSE QUARTERS", "Enemies become blessed when too far."), new Setting[]
                {
                    new SliderSubsetting("artiset_distance_distfromplayer", new SliderSubsettingElement(new Metadata("DISTANCE", "Distance for activation.")).Self, 5, 50, 15, true, "{0} u")
                }), false),

                new ToggleSetting("ARTIFACT_mitosis", new MutatorElement(Pages[6].Holder, new Metadata("MITOSIS", "Enemies are duplicated. You can go above 10x, if you enter it several times."), new Setting[]
                {
                    new SliderSubsetting("artiset_mitosis_amount", new SliderSubsettingElement(new Metadata("MULTIPLIER", "How many enemies per enemy.")).Self, 2, 10, 2, true, "{0}x"),
                }), false),
                    
                new ToggleSetting("ARTIFACT_noHP", new MutatorElement(Pages[6].Holder, new Tweaks.Metadata("FRAGILITY", "Your health cap is lower."), new Setting[]
                {
                    new SliderSubsetting("artiset_noHP_hpamount", new SliderSubsettingElement(new Metadata("CAP", "The maximum amount of HP.")).Self, 1, 100, 1, true, "{0} HP"),
                }), false),

                    
                new ToggleSetting("ARTIFACT_fuelleak", new MutatorElement(Pages[6].Holder, new Tweaks.Metadata("FUEL LEAK", "Blood is actually fuel, and gets used over time. Heal before all of your HP runs out."), new Setting[]
                {
                    new SliderSubsetting("artiset_fuelleak_multi", new SliderSubsettingElement(new Metadata("MULTIPLIER", "HP lost per second.")).Self, 1, 20, 5, true, "{0}"),
                }), false),
                    
                new ToggleSetting("ARTIFACT_floorlava", new MutatorElement(Pages[6].Holder, new Metadata("FLOOR IS LAVA", "You are damaged when grounded. Based on a mod by <b>nptnk#0001</b>."), new Setting[]
                {
                    new SliderSubsetting("artiset_floorlava_mult", new SliderSubsettingElement(new Metadata("DAMAGE", "Damage lost per second.")).Self, 1, 100, 100, true, "{0}"),
                    new SliderSubsetting("artiset_floorlava_time", new SliderSubsettingElement(new Metadata("TIME", "Time before you start getting hurt.")).Self, 0f, 5, 0.1f, false, "{0}s"),
                }), false),
                    
                new ToggleSetting("ARTIFACT_fresh", new MutatorElement(Pages[6].Holder, new Metadata("FRESHNESS", "You get hurt whenever your freshness rank is below a certain amount."), new Setting[]
                {
                    new SliderSubsetting("artiset_fresh_fr", new SliderSubsettingElement(new Metadata("FRESH", "HP lost per second on the 'Fresh' rank.")).Self, 0, 100, 0, true, "{0}"),
                    new SliderSubsetting("artiset_fresh_us", new SliderSubsettingElement(new Metadata("USED", "HP lost per second on the 'Used' rank.")).Self, 0, 100, 4, true, "{0}"),
                    new SliderSubsetting("artiset_fresh_st", new SliderSubsettingElement(new Metadata("STALE", "HP lost per second on the 'Stale' rank.")).Self, 0, 100, 8, true, "{0}"),
                    new SliderSubsetting("artiset_fresh_du", new SliderSubsettingElement(new Metadata("DULL", "HP lost per second on the 'Dull' rank.")).Self, 0, 100, 12, true, "{0}"),
                }), false),
                    
                new ToggleSetting("ARTIFACT_gofast", new MutatorElement(Pages[6].Holder, new Metadata("SPEED", "Change the speed of both yourself, and your enemies."), new Setting[]
                {
                    new SliderSubsetting("artiset_gofast_player", new SliderSubsettingElement(new Metadata("PLAYER", "Player speed multiplier.")).Self, 0.5f, 7.5f, 2, false, "{0}x"),
                    new SliderSubsetting("artiset_gofast_enemy", new SliderSubsettingElement(new Metadata("ENEMY", "Enemy speed multiplier.")).Self, 0.5f, 7.5f, 2, false, "{0}x"),
                }), false),
                    
                new ToggleSetting("ARTIFACT_ice", new MutatorElement(Pages[6].Holder, new Metadata("ULTRAKILL ON ICE", "You become slippery."), new Setting[]
                {
                    new SliderSubsetting("artiset_ice_frict", new SliderSubsettingElement(new Metadata("FRICTION", "Friction while walking.")).Self, 0, 2, 0.5f, false, "{0}x"),
                }), false),
                    
                new ToggleSetting("ARTIFACT_superhot", new MutatorElement(Pages[6].Holder, new Metadata("ULTRAHOT", "Time only moves when you move.")), false),
                new ToggleSetting("ARTIFACT_glass", new MutatorElement(Pages[6].Holder, new Metadata("GLASS", "Deal two times the damage - at the cost of 70% of your health.")), false),
                new ToggleSetting("ARTIFACT_diceroll", new MutatorElement(Pages[6].Holder, new Metadata("DICE ROLL", "Your loadout is randomised at a chosen interval."), new Setting[]
                {
                    new SliderSubsetting("artiset_diceroll_timereset", new SliderSubsettingElement(new Metadata("INTERVAL", "Time between resets.")).Self, 5, 300, 30, true, "{0}s"),
                }), false),
                    
                new ToggleSetting("ARTIFACT_water", new MutatorElement(Pages[6].Holder, new Metadata("SUBMERGED", "Every level is flooded with water.")), false),
                new ToggleSetting("ARTIFACT_whiphard", new MutatorElement(Pages[6].Holder, new Metadata("WHIP FIX", "Change the hard damage gained from whiplashing. Made pre-Patch11b, obsolete."), new Setting[]
                {
                    new SliderSubsetting("artiset_whip_hard_mult", new SliderSubsettingElement(new Metadata("MULTIPLIER", "Hard damage multiplier.")).Self, 0f, 2f, 0.5f, false, "{0}x"),
                }), false),
            };


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
                string mods = "";

                foreach (TweakHandler th in TypeToHandler.Values)
                {
                    if (th.enabled)
                    {
                        mods += th.GetType().Name;
                    }
                }

                HudMessageReceiver.Instance.SendHudMessage(mods);
            }
        }
    }
    #endregion
}

