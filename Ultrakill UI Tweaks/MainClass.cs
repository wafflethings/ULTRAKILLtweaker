using FallFactory;
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
        public float ToRemove;

        // Some mods need a scene reload to work, so if mods are changed in game this is set to true. If it is true on the option menu being turned off, the scene reloads.
        public bool ModsChanged = false;

        // Have settings initialised? Needed so that idToSetting isn't null. 
        public bool SettingsInit = false;

        Coroutine MCL;

        #endregion

        public void OnGUI()
        {
            if (SettingsInit)
            {
                if (Convert.ToBoolean(SettingRegistry.idToSetting["fpscounter"].value))
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
            harmony.PatchAll(typeof(DoInitOnTime));
            harmony.PatchAll(typeof(ModResultsPatch));
            harmony.PatchAll(typeof(HitstopPatches));
            harmony.PatchAll(typeof(EnemySpawnPatch));
            harmony.PatchAll(typeof(AddArms));

            SceneManager.sceneLoaded += OnSceneWasLoaded;

            // OnSceneWasLoaded is where patching happens, but if it is not called (e.g when the mod is not on at startup) it is not called on time. We have to do it now.
            if (SceneManager.GetActiveScene().name == "Main Menu")
            {
               OnSceneWasLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
            }
        }

        public override void OnModUnload()
        {
            // Unpatch Harmony, destroy GOs from UKt, remove events set, all the stuff that happens on startup.
            harmony.UnpatchSelf();

            List<GameObject> ToDestroy = new List<GameObject>()
            {
                TweakerButton, TweakerMenu, CyberGrind, DiceRoll
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
            }

            // There is probably a better way to do this than GO.Find. This adds the UKt UI elements to the UI.
            if (GameObject.Find("Canvas") != null)
            {
                StartCoroutine(PatchOptionsMenu());
            }

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

            // Load all the UI from AssetBundle, DDOL it if it's needed, and disable it.
            TweakerMenu = Instantiate(UIBundle.LoadAsset<GameObject>("Canvas"));
            if (CyberGrind == null)
                CyberGrind = Instantiate(UIBundle.LoadAsset<GameObject>("GrindCanvas"));
            if (DiceRoll == null)
                DiceRoll = Instantiate(UIBundle.LoadAsset<GameObject>("DicerollCanvas"));
            DontDestroyOnLoad(CyberGrind);
            DontDestroyOnLoad(DiceRoll);
            CyberGrind.SetActive(false);
            DiceRoll.SetActive(false);

            // For some reason, the game crashes when you try set the transform.parent of a GameObject the same frame as it was instantiated.
            // Here, we wait a frame and then do the stuff.

            StartCoroutine(WeirdgeFix());
        }

        public IEnumerator WeirdgeFix()
        {
            yield return null;

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

            #region Register, load, all the settings.
            SettingRegistry.settings.Clear();
            SettingRegistry.idToSetting.Clear();
            SettingRegistry.settings.Add(new SliderSetting("uiscale", TweakerMenu.ChildByName("Tweaks").ChildByName("Page 1").ChildByName("UI Scale"), 0, 110, 100, true, "{0}%"));
            SettingRegistry.settings.Add(new SliderSetting("hitstopmult", TweakerMenu.ChildByName("Tweaks").ChildByName("Page 1").ChildByName("Hitstop Multiplier"), 0, 2, 1, false, "{0}x"));
            SettingRegistry.settings.Add(new ToggleSetting("forcegun", TweakerMenu.ChildByName("Tweaks").ChildByName("Page 1").ChildByName("Force Gun Modal"), false));
            SettingRegistry.settings.Add(new ToggleSetting("cybergrindstats", TweakerMenu.ChildByName("Tweaks").ChildByName("Page 1").ChildByName("Cybergrind Stats"), false));
            SettingRegistry.settings.Add(new ToggleSetting("cybergrindmusic", TweakerMenu.ChildByName("Tweaks").ChildByName("Page 2").ChildByName("CybergrindMusic"), false));
            SettingRegistry.settings.Add(new ToggleSetting("seeviewmodel", TweakerMenu.ChildByName("Tweaks").ChildByName("Page 2").ChildByName("No Viewmodel"), false));
            SettingRegistry.settings.Add(new ToggleSetting("fpscounter", TweakerMenu.ChildByName("Tweaks").ChildByName("Page 2").ChildByName("FPS"), false));
            SettingRegistry.settings.Add(new ArtifactSetting("ARTIFACT_sandify", TweakerMenu.ChildByName("Modifiers").ChildByName("Sandify"), false, true, "Sandify", "Every enemy gets covered in sand. Parrying is the only way to heal."));
            SettingRegistry.settings.Add(new ArtifactSetting("ARTIFACT_noHP", TweakerMenu.ChildByName("Modifiers").ChildByName("Fragility"), false, true, "Fragility", "You only have 1 HP - if you get hit, you die."));
            SettingRegistry.settings.Add(new ArtifactSetting("ARTIFACT_glass", TweakerMenu.ChildByName("Modifiers").ChildByName("Glass"), true, true, "Glass", "Deal two times the damage - at the cost of 70% of your health."));
            SettingRegistry.settings.Add(new ArtifactSetting("ARTIFACT_superhot", TweakerMenu.ChildByName("Modifiers").ChildByName("Superhot"), true, true, "UltraHot", "Time only moves when you move."));
            SettingRegistry.settings.Add(new ArtifactSetting("ARTIFACT_tank", TweakerMenu.ChildByName("Modifiers").ChildByName("Tankify"), false, true, "Tankify", "Every enemy gets two times the health."));
            SettingRegistry.settings.Add(new ArtifactSetting("ARTIFACT_distance", TweakerMenu.ChildByName("Modifiers").ChildByName("Distance"), false, true, "Close Quarters", "Enemies become blessed when too far."));
            SettingRegistry.settings.Add(new ArtifactSetting("ARTIFACT_noweapons", TweakerMenu.ChildByName("Modifiers").ChildByName("No Weapons"), false,true, "Empty Handed", "No weapons, punch your enemies to death. Good luck beating P-1."));
            SettingRegistry.settings.Add(new ArtifactSetting("ARTIFACT_nostamina", TweakerMenu.ChildByName("Modifiers").ChildByName("NoStamina"), false, true, "Lethargy", "V1 is tired, and has no stamina. No sliding, dash-jumps, or power-slams."));
            SettingRegistry.settings.Add(new ArtifactSetting("ARTIFACT_diceroll", TweakerMenu.ChildByName("Modifiers").ChildByName("Random"), true, true, "Dice-Roll", "Every 30 seconds, your weapon loadout is randomised. Includes scrapped and unowned weapons! (if the current update has any)"));
            SettingRegistry.settings.Add(new ArtifactSetting("ARTIFACT_water", TweakerMenu.ChildByName("Modifiers").ChildByName("Water"), true, true, "Submerged", "Every level is flooded with water."));
            SettingRegistry.settings.Add(new ArtifactSetting("ARTIFACT_gofast", TweakerMenu.ChildByName("Modifiers").ChildByName("GoFast"), true, true, "Speed", "You run at 2 times the speed. Enemy speed is multiplied by 7.5 to keep up."));
            SettingRegistry.settings.Add(new ArtifactSetting("ARTIFACT_noarm", TweakerMenu.ChildByName("Modifiers").ChildByName("No Arms"), false, true, "Disarmed", "V1 has no arms. You can't punch, whiplash, or parry."));
            SettingRegistry.settings.Add(new ArtifactSetting("ARTIFACT_fuelleak", TweakerMenu.ChildByName("Modifiers").ChildByName("Fuel Leak"), false, true, "Fuel Leak", "Blood is actually fuel, and gets used over time. Heal before all of your HP runs out."));

            SettingRegistry.settings.Add(new SliderSetting("artiset_fuelleak_multi", TweakerMenu.ChildByName("Modifiers").ChildByName("Fuel Leak").ChildByName("Extra Settings").ChildByName("SETTINGS").ChildByName("Panel").ChildByName("Damage Drain"), 0.1f, 2, 1, false, "{0}x"));
            SettingRegistry.settings.Add(new SliderSetting("artiset_noHP_hpamount", TweakerMenu.ChildByName("Modifiers").ChildByName("Fragility").ChildByName("Extra Settings").ChildByName("SETTINGS").ChildByName("Panel").ChildByName("HP"), 1, 100, 1, true, "{0} HP"));
            SettingRegistry.settings.Add(new SliderSetting("artiset_diceroll_timereset", TweakerMenu.ChildByName("Modifiers").ChildByName("Random").ChildByName("Extra Settings").ChildByName("SETTINGS").ChildByName("Panel").ChildByName("Time"), 5, 300, 30, true, "{0}s"));
            SettingRegistry.settings.Add(new SliderSetting("artiset_distance_distfromplayer", TweakerMenu.ChildByName("Modifiers").ChildByName("Distance").ChildByName("Extra Settings").ChildByName("SETTINGS").ChildByName("Panel").ChildByName("Distance"), 5, 50, 15, true, "{0} u"));
            SettingRegistry.settings.Add(new SliderSetting("artiset_gofast_player", TweakerMenu.ChildByName("Modifiers").ChildByName("GoFast").ChildByName("Extra Settings").ChildByName("SETTINGS").ChildByName("Panel").ChildByName("Player"), 0.5f, 7.5f, 2, false, "{0}x"));
            SettingRegistry.settings.Add(new SliderSetting("artiset_gofast_enemy", TweakerMenu.ChildByName("Modifiers").ChildByName("GoFast").ChildByName("Extra Settings").ChildByName("SETTINGS").ChildByName("Panel").ChildByName("Enemy"), 0.5f, 7.5f, 7.5f, false, "{0}x"));
            SettingRegistry.settings.Add(new SliderSetting("artiset_tankify_mult", TweakerMenu.ChildByName("Modifiers").ChildByName("Tankify").ChildByName("Extra Settings").ChildByName("SETTINGS").ChildByName("Panel").ChildByName("Mult"), 1f, 10f, 2f, false, "{0}x"));
            #endregion

            // This sets the text to show where your custom music directory is.
            TweakerMenu.ChildByName("Tweaks").ChildByName("Page 2").ChildByName("CybergrindMusic").ChildByName("Path").GetComponent<Text>().text = Path.Combine(Utils.GameDirectory(), @"BepInEx\UMM Mods\plugins\ULTRAKILLtweaker\Cybergrind Music");

            foreach (Setting setting in SettingRegistry.settings)
            {
                SettingRegistry.idToSetting.Add(setting.ID, setting);
                Debug.Log($"Setting registered: {setting.ID}.");
            }

            // EnableDisableEvent is added the the optmenu, all it does is call MenuEnable/MenuDisable on OnEnable/OnDisable
            OptionsMenu.AddComponent<EnableDisableEvent>();
            MenuEnable();

            // Define which pages are used
            Pages = new List<GameObject>()
            {
                TweakerMenu.ChildByName("Tweaks").ChildByName("Page 1"),
                TweakerMenu.ChildByName("Tweaks").ChildByName("Page 2")
            };

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
                if (Page != Pages.Count)
                {
                    Page++;
                    UpdatePages();
                }
            });

            TweakerMenu.ChildByName("Tweaks").ChildByName("Page 1").ChildByName("Reset").GetComponent<Button>().onClick.AddListener(() =>
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
                    if (TweakerButton.transform.position != new Vector3(45, 1000, 0) || TweakerButton.transform.localScale != Vector3.one)
                    {
                        TweakerButton.transform.position = new Vector3(45, 1000, 0);
                        TweakerButton.transform.localScale = new Vector3(1, 1, 1);
                    }
                }

                if (TweakerMenu != null)
                {
                    if (TweakerMenu.transform.position != new Vector3(960, 540, 0) || TweakerMenu.transform.localScale != Vector3.one)
                    {
                        TweakerMenu.transform.position = new Vector3(960, 540, 0);
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
                if (Convert.ToBoolean(SettingRegistry.idToSetting["seeviewmodel"].value))
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
                if (SceneManager.GetActiveScene().name == "Endless")
                {
                    if (Convert.ToBoolean(SettingRegistry.idToSetting["cybergrindmusic"].value))
                    {
                        audiosource.volume = volslider.normalizedValue;
                    }
                }

                if (SceneManager.GetActiveScene().name != "Main Menu")
                {
                    UpdateArtifact();
                }
            }
        }

        public void UpdateArtifact()
        {
            if (Convert.ToBoolean(SettingRegistry.idToSetting["cybergrindstats"].value) && SceneManager.GetActiveScene().name == "Endless" && CyberGrind.activeSelf)
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

            if (!Convert.ToBoolean(SettingRegistry.idToSetting["ARTIFACT_noHP"].value))
            {
                if (nm != null)
                {
                    nm.antiHp = 100 - Convert.ToSingle(SettingRegistry.idToSetting["artiset_noHP_hpamount"].value);
                    if (nm.hp > Convert.ToSingle(SettingRegistry.idToSetting["artiset_noHP_hpamount"].value))
                    {
                        nm.ForceAntiHP((int)(100 - Convert.ToSingle(SettingRegistry.idToSetting["artiset_noHP_hpamount"].value)));
                    }
                }
            }

            if (!Convert.ToBoolean(SettingRegistry.idToSetting["ARTIFACT_glass"].value))
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

            if (!Convert.ToBoolean(SettingRegistry.idToSetting["ARTIFACT_superhot"].value))
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

            if (!Convert.ToBoolean(SettingRegistry.idToSetting["ARTIFACT_nostamina"].value))
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

            if (!Convert.ToBoolean(SettingRegistry.idToSetting["ARTIFACT_diceroll"].value))
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

            if (!Convert.ToBoolean(SettingRegistry.idToSetting["ARTIFACT_fuelleak"].value))
            {
                if (nm != null && Instance.statman.timer)
                {
                    ToRemove += Time.deltaTime * 5 * Convert.ToSingle(SettingRegistry.idToSetting["artiset_fuelleak_multi"].value);

                    if (ToRemove > 1)
                    {
                        nm.hp -= 1;
                        ToRemove -= 1;
                    }

                    if (nm.hp <= 0)
                    {
                        nm.GetHurt(200, false, 1, true, true);
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
            yield return new WaitForSeconds(1f);
            Instance.Init();
        }

        public void InitReset()
        {
            GameObject hud = player.ChildByName("Main Camera").ChildByName("HUD Camera").ChildByName("HUD");
            if (Convert.ToBoolean(SettingRegistry.idToSetting["forcegun"].value))
            {
                hud.ChildByName("GunCanvas").ChildByName("GunPanel").SetActive(true);
            }

            if (!Convert.ToBoolean(SettingRegistry.idToSetting["ARTIFACT_noweapons"].value))
            {
                hud.ChildByName("GunCanvas").ChildByName("GunPanel").SetActive(false);
            }
        }

        public void InitSceneLoad()
        {
            // Set instances
            player = GameObject.Find("Player");
            nm = FindObjectOfType<NewMovement>();
            GameObject hud = player.ChildByName("Main Camera").ChildByName("HUD Camera").ChildByName("HUD");

            // Reset the CG panel values back to 0
            CyberGrind.ChildByName("Panel").ChildByName("WaveCount").GetComponent<Text>().text = "0";
            CyberGrind.ChildByName("Panel").ChildByName("EnemyCount").GetComponent<Text>().text = "0";
            CyberGrind.ChildByName("Panel").ChildByName("Time").GetComponent<Text>().text = "00:00:000";
            CyberGrind.ChildByName("Panel").ChildByName("Kills").GetComponent<Text>().text = "0";
            CyberGrind.ChildByName("Panel").ChildByName("Style").GetComponent<Text>().text = "0";

            #region Stuff that happens, on player spawn, for the settings
            if (Convert.ToSingle(SettingRegistry.idToSetting["uiscale"].value) != 100)
            {
                foreach (GameObject go in hud.ChildrenList())
                {
                    go.transform.localScale = go.transform.localScale * Convert.ToSingle(SettingRegistry.idToSetting["uiscale"].value) / 100;
                }
            }

            if (!Convert.ToBoolean(SettingRegistry.idToSetting["ARTIFACT_water"].value))
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

            if (!Convert.ToBoolean(SettingRegistry.idToSetting["ARTIFACT_diceroll"].value))
            {
                CurrentRandom = StartCoroutine(RandomiseEvery30());
            }

            if (Convert.ToBoolean(SettingRegistry.idToSetting["cybergrindmusic"].value) && SceneManager.GetActiveScene().name == "Endless")
            {
                music = GetClipsFromFolder();
                volslider = OptionsMenu.ChildByName("Audio Options").ChildByName("Image").ChildByName("Music Volume").ChildByName("Button").ChildByName("Slider (1)").GetComponent<Slider>();
                UnityEngine.Object.Destroy(GameObject.Find("Everything").transform.GetChild(3).transform.GetChild(0).gameObject);
                StartCoroutine(MusLoopWhenWaveCount());
            }

            if (!Convert.ToBoolean(SettingRegistry.idToSetting["ARTIFACT_gofast"].value))
            {
                if (nm != null)
                {
                    nm.walkSpeed *= Convert.ToSingle(SettingRegistry.idToSetting["artiset_gofast_player"].value);
                }
            }

            if (Convert.ToBoolean(SettingRegistry.idToSetting["cybergrindstats"].value))
            {
                CyberGrind.SetActive(SceneManager.GetActiveScene().name == "Endless");
            }

            if (!Convert.ToBoolean(SettingRegistry.idToSetting["ARTIFACT_diceroll"].value))
            {
                DiceRoll.SetActive(true);
            }

            if (!Convert.ToBoolean(SettingRegistry.idToSetting["ARTIFACT_noarm"].value))
            {
                player.ChildByName("Main Camera").ChildByName("Punch").SetActive(false);
            }
            #endregion
        }
        #endregion

        #region Music stuff, skidded from eps's CGUtils.
        public IEnumerator RandomiseEvery30()
        {
            while (true)
            {
                int Time = (int)Convert.ToSingle(SettingRegistry.idToSetting["artiset_diceroll_timereset"].value);
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
        [HarmonyPatch(typeof(FinalRank), "SetRank")]
        public static class ModResultsPatch
        {
            public static void Postfix(FinalRank __instance)
            {
                GameObject hud = Instance.player.ChildByName("Main Camera").ChildByName("HUD Camera").ChildByName("HUD");
                Text text = hud.ChildByName("FinishCanvas").ChildByName("Panel").ChildByName("Extra Info").ChildByName("Text").GetComponent<Text>();
                string mods = "";

                foreach (Setting mod in SettingRegistry.settings)
                {
                    if (mod.GetType() == typeof(ArtifactSetting))
                    {
                        if (!Convert.ToBoolean(mod.value))
                        {
                            mods += ((ArtifactSetting)mod).Name.ToUpper() + ", ";
                        }
                    }
                }
                text.text = "<size=20>+ " + mods.Substring(0, mods.Length - 2) + "</size>\n<size=10>\n</size>" + text.text;
            }
        }

        [HarmonyPatch(typeof(GunSetter), nameof(GunSetter.ResetWeapons))]
        public static class DoInitOnTime
        {
            public static void Postfix(GunSetter __instance)
            {
                MainClass.Instance.StartCoroutine(WaitAndInit());

                if (!Convert.ToBoolean(SettingRegistry.idToSetting["ARTIFACT_noweapons"].value))
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

                    if (!Convert.ToBoolean(SettingRegistry.idToSetting["ARTIFACT_diceroll"].value))
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
                if (!Convert.ToBoolean(SettingRegistry.idToSetting["ARTIFACT_gofast"].value))
                {
                    __instance.GetComponent<NavMeshAgent>().speed *= Convert.ToSingle(SettingRegistry.idToSetting["artiset_gofast_enemy"].value);
                    __instance.GetComponent<NavMeshAgent>().acceleration *= Convert.ToSingle(SettingRegistry.idToSetting["artiset_gofast_enemy"].value);
            }

                if (!Convert.ToBoolean(SettingRegistry.idToSetting["ARTIFACT_sandify"].value))
                    __instance.sandified = true;

                if (!Convert.ToBoolean(SettingRegistry.idToSetting["ARTIFACT_distance"].value))
                    __instance.gameObject.AddComponent<BlessWhenFar>();

                if (!Convert.ToBoolean(SettingRegistry.idToSetting["ARTIFACT_tank"].value) || !Convert.ToBoolean(SettingRegistry.idToSetting["ARTIFACT_glass"].value))
                {
                    float MULT = Convert.ToSingle(SettingRegistry.idToSetting["artiset_tankify_mult"].value);

                if (!Convert.ToBoolean(SettingRegistry.idToSetting["ARTIFACT_glass"].value))
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
                length = Convert.ToSingle(SettingRegistry.idToSetting["hitstopmult"].value) * length;
            }

            [HarmonyPatch(typeof(TimeController), "TrueStop")]
            [HarmonyPrefix]
            static void TrueStop(ref float length)
            {
                length = Convert.ToSingle(SettingRegistry.idToSetting["hitstopmult"].value) * length;
            }

            [HarmonyPatch(typeof(TimeController), "SlowDown")]
            [HarmonyPrefix]
            static void SlowDown(ref float amount)
            {
                amount = Convert.ToSingle(SettingRegistry.idToSetting["hitstopmult"].value) * amount;
            }
        }
    #endregion
}

