using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ULTRAKILLtweaker.Components;
using UnityEngine;
using UnityEngine.UI;

namespace ULTRAKILLtweaker.Tweaks.Handlers.Impl
{
    public class TexturePack : TweakHandler
    {
        private Harmony harmony = new Harmony("waffle.ultrakill.UKtweaker.texpacks");
        public static string PacksPath = Path.Combine(Utils.GameDirectory(), @"BepInEx\UMM Mods\ULTRAKILLtweaker\Resource Packs");

        public override void OnTweakEnabled()
        {
            base.OnTweakEnabled();
            harmony.PatchAll(typeof(PackPatches));
        }

        public override void OnTweakDisabled()
        {
            base.OnTweakDisabled();
            harmony.UnpatchSelf();
        }

        public void Start()
        {
            foreach (string dir in Directory.GetDirectories(PacksPath))
            {
                Pack.Packs.Add(new Pack(dir.PathToName()));
            }
        }

        public static class PackLoading
        {
            public static AudioClip GetClipFromPath(string file)
            {
                string[] AudioExts = new string[]
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

                foreach (string ext in AudioExts)
                {
                    if (file.EndsWith(ext))
                    {
                        WWW www = new WWW("file:///" + file);
                        while (!www.isDone)
                        {
                        }
                        AudioClip clip = www.GetAudioClip();
                        clip.name = file.PathToName().RemoveFileExt();
                        return clip;
                    }
                }
                return default;
            }

            public static Texture2D GetTexFromPath(string file)
            {
                string[] TextureExts = new string[]
                {
                    ".bmp",
                    ".jpg",
                    ".jpeg",
                    ".png"
                };

                foreach (string ext in TextureExts)
                {
                    if (file.EndsWith(ext))
                    {
                        Texture2D tex = null;
                        byte[] fileData;

                        if (File.Exists(file))
                        {
                            fileData = File.ReadAllBytes(file);
                            tex = new Texture2D(2, 2);
                            tex.LoadImage(fileData);
                            tex.filterMode = FilterMode.Point;
                            tex.name = file.PathToName().RemoveFileExt();
                        }
                        return tex;
                    }
                }
                return default;
            }

            public static Sprite GetSprFromPath(string file)
            {
                Texture2D tex = GetTexFromPath(file);
                Sprite spr = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(tex.width / 2, tex.height / 2));
                spr.name = tex.name;
                return spr;
            }
        }

        public class Pack
        {
            public static List<Pack> Packs = new List<Pack>();

            public bool Enabled = true;
            public string FolderName;
            public PackMetadata Meta;

            public Font Font;
            public float FontScale = 1;

            public Pack(string FolderName)
            {
                this.FolderName = FolderName;

                Meta = new PackMetadata(FolderName);

                Debug.Log($"ResourcePack constructor: {FolderName}.");

                if (File.Exists(Path.Combine(PacksPath, FolderName, "Misc", "font.bundle")))
                {
                    AssetBundle FontBundle = AssetBundle.LoadFromFile(Path.Combine(PacksPath, FolderName, "Misc", "font.bundle"));
                    TextAsset data = FontBundle.LoadAllAssets<TextAsset>()[0];

                    string _name = "";
                    float size = 1;

                    try
                    {
                        foreach (string str in data.ToString().Split(','))
                        {
                            if (str.StartsWith("font"))
                                _name = str.Split(':')[1].Replace(",", "");
                            if (str.StartsWith("scale"))
                                size = Convert.ToSingle(str.Split(':')[1].Replace(",", ""));
                        }
                    }
                    catch { }

                    Font = FontBundle.LoadAsset<Font>(_name);
                    FontScale = size;
                }

            }
        }

        public class PackMetadata
        {
            public string name = "A Resource Pack";
            public string author = "Some person";
            public string desc = "This is a texture pack.";
            public string ID = "NO ID, DELETE THIS PACK";
            public Sprite icon = null;

            public PackMetadata(string Name)
            {
                icon = MainClass.UIBundle.LoadAsset<Sprite>("defaultpack");

                if (File.Exists(Path.Combine(PacksPath, Name, "pack.uktmeta")))
                {
                    string[] uktmeta = File.ReadAllLines(Path.Combine(PacksPath, Name, "pack.uktmeta"));

                    foreach (string s in uktmeta)
                    {
                        if (s.StartsWith("name:"))
                            name = s.Split(':')[1];

                        if (s.StartsWith("unique_id:"))
                            ID = s.Split(':')[1];

                        if (s.StartsWith("author:"))
                            author = s.Split(':')[1];

                        if (s.StartsWith("desc:"))
                            desc = s.Split(':')[1];

                        if (s.StartsWith("icon:"))
                        {
                            try // just in case the icon file is called .png but doesnt work
                            {
                                Texture2D tex = PackLoading.GetTexFromPath(Path.Combine(Path.Combine(PacksPath, Name), s.Split(':')[1]));
                                icon = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                            }
                            catch { }
                        }
                    }
                }
            }
        }

        public static class PackPatches
        {
            [HarmonyPatch(typeof(Texture), MethodType.Constructor)]
            [HarmonyPostfix]
            public static void Swap()
            {
                Debug.Log("h");
            }
        }
    }
}
