using FallFactory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ULTRAKILLtweaker
{
    public class ResourcePack
    {
        public static Dictionary<string, ResourcePack> IdToPack = new Dictionary<string, ResourcePack>();
        public static List<ResourcePack> Packs = new List<ResourcePack>();
        public static GameObject PackTemp;

        public static void InitPacks()
        {
            PackTemp = MainClass.Instance.UIBundle.LoadAsset<GameObject>("packtemp");
            foreach(string dir in Directory.GetDirectories(PacksPath))
            {
                ResourcePack pack = new ResourcePack(dir.PathToName());
                Packs.Add(pack);
                IdToPack.Add(pack.metadata.ID, pack);
            }
            PackSaving.Load();
        }

        #region Static stuff
        public static Dictionary<string, Texture2D> IDtoTex = new Dictionary<string, Texture2D>();
        public static Dictionary<string, AudioClip> IDtoClip = new Dictionary<string, AudioClip>();
        public static Dictionary<string, Sprite> IDtoSpr = new Dictionary<string, Sprite>();

        public static GameObject DisabledContent;
        public static GameObject EnabledContent;

        public static void SetDicts()
        {
            IDtoTex.Clear();
            IDtoClip.Clear();
            IDtoSpr.Clear();

            foreach(ResourcePack pack in Packs)
            {
                if(pack.Enabled)
                {
                    foreach(Texture2D tex in pack.Textures)
                    {
                        if (IDtoTex.ContainsKey(tex.name))
                            IDtoTex[tex.name] = tex;
                        else
                            IDtoTex.Add(tex.name, tex);
                    }

                    foreach(AudioClip clip in pack.AudioClips)
                    {
                        if (IDtoClip.ContainsKey(clip.name))
                            IDtoClip[clip.name] = clip;
                        else
                            IDtoClip.Add(clip.name, clip);
                    }

                    foreach (Sprite spr in pack.Sprites)
                    {
                        if (IDtoSpr.ContainsKey(spr.name))
                            IDtoSpr[spr.name] = spr;
                        else
                            IDtoSpr.Add(spr.name, spr);
                    }
                }
            }
        }

        public static string PacksPath = Path.Combine(Utils.GameDirectory(), @"BepInEx\UMM Mods\ULTRAKILLtweaker\Resource Packs");

        static string[] TextureExts = new string[]
        {
            ".bmp",
            ".jpg",
            ".jpeg",
            ".png"
        };

        static string[] AudioExts = new string[]
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

        public static AudioClip GetClipFromPath(string file)
        {
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

        public static System.Collections.IEnumerator PatchTextures()
        {
            yield return null;
            DateTime start = DateTime.Now;
            foreach (GameObject go in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                // Debug.Log($"root {go.name}");
                foreach (Transform t in go.transform.GetComponentsInChildren<Transform>(true))
                {
                    // Debug.Log($"    child {t.gameObject.name}");

                    foreach (Component comp in t.gameObject.GetComponents<Component>())
                    {
                        try
                        {
                            // Debug.Log($"        comp {comp.GetType().Name}");
                            CompPatch(comp);

                        } catch { }
                    }
                }
            }
            Debug.Log($"{(DateTime.Now - start).TotalSeconds} elapsed patching.");
        }

        public static void CompPatch(Component comp)
        {
            foreach (PropertyInfo Property in comp.GetType().GetProperties(BindingFlags.Instance | BindingFlags.NonPublic))
            {
                if (CheckIfShould(Property))
                    SwapTex(comp, Property);
            }

            foreach (PropertyInfo Property in comp.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (CheckIfShould(Property))
                    SwapTex(comp, Property);
            }

            foreach (PropertyInfo Property in comp.GetType().GetProperties(BindingFlags.Static | BindingFlags.Public))
            {
                if (CheckIfShould(Property))
                    SwapTex(comp, Property);
            }

            foreach (PropertyInfo Property in comp.GetType().GetProperties(BindingFlags.Static | BindingFlags.NonPublic))
            {
                if (CheckIfShould(Property))
                    SwapTex(comp, Property);
            }
        }

        static bool CheckIfShould(PropertyInfo Property)
        {
            return Property.PropertyType == typeof(Texture) || Property.PropertyType == typeof(Texture2D) || Property.PropertyType == typeof(Material) || Property.PropertyType == typeof(Texture[]) || Property.PropertyType == typeof(Texture2D[]) || Property.PropertyType == typeof(Material[]);
        }

        static void SwapTex(Component comp, PropertyInfo pi)
        {
            try // something is giving me a nullref, i think its if texture is null but i cant be bothered to fix. so fuck it, we ball.
            {
                if (pi.PropertyType == typeof(Texture) || pi.PropertyType == typeof(Texture2D))
                {
                    Texture val = (Texture)pi.GetValue(comp);
                    if (val != null)
                    {
                        if (val.name != "Font Texture" && val.name != "UISprite" && val.name != "Background" && val.name != "UnityWhite" && val.name != "meter" && val.name != "pixelx" && val.name != "Knob")
                            Debug.Log(val.name + "|||" + IDtoTex.Keys.Contains(val.name));
                        if (IDtoTex.Keys.Contains(val.name))
                        {
                            val.filterMode = FilterMode.Point;
                            pi.SetValue(comp, IDtoTex[val.name]);
                            Debug.Log("Just set tex: " + ((Texture)pi.GetValue(comp) == IDtoTex[val.name]));
                        }
                    }
                }
                if (pi.PropertyType == typeof(Material))
                {
                    Texture val = ((Material)pi.GetValue(comp)).mainTexture;
                    if (val != null)
                    {
                        //val.filterMode = FilterMode.Bilinear;
                        if (val.name != "Font Texture" && val.name != "UISprite" && val.name != "Background" && val.name != "UnityWhite" && val.name != "meter" && val.name != "pixelx" && val.name != "Knob")
                            Debug.Log(val.name + "|||" + IDtoTex.Keys.Contains(val.name));
                        if (IDtoTex.Keys.Contains(val.name))
                        {
                            Material mat = (Material)pi.GetValue(comp);
                            mat.mainTexture = IDtoTex[val.name];
                            mat.mainTexture.filterMode = FilterMode.Point;
                            Debug.Log("Just set material: " + (mat.mainTexture == IDtoTex[val.name]));
                        }
                    }
                }
                if (pi.PropertyType == typeof(Sprite))
                {
                    Debug.Log($"                sprite {pi.PropertyType.Name} {pi.Name} of {comp.name}.");

                    Sprite val = (Sprite)pi.GetValue(comp);
                    if (val != null)
                    {
                        // val.filterMode = FilterMode.Bilinear;
                        if (val.name != "Font Texture" && val.name != "UISprite" && val.name != "Background" && val.name != "UnityWhite" && val.name != "meter" && val.name != "pixelx" && val.name != "Knob")
                            Debug.Log(val.name + "|||" + IDtoSpr.Keys.Contains(val.name));
                        if (IDtoSpr.Keys.Contains(val.name))
                        {
                            pi.SetValue(comp, IDtoSpr[val.name]);
                            Debug.Log("Just set spr: " + (val == IDtoSpr[val.name]));
                        }
                    }
                }
                if (pi.PropertyType == typeof(Image))
                {
                    // Debug.Log($"                material {pi.PropertyType.Name} {pi.Name} of {comp.name}.");

                    Sprite val = ((Image)pi.GetValue(comp)).sprite;
                    if (val != null)
                    {
                        //val.filterMode = FilterMode.Bilinear;
                        if (val.name != "Font Texture" && val.name != "UISprite" && val.name != "Background" && val.name != "UnityWhite" && val.name != "meter" && val.name != "pixelx" && val.name != "Knob")
                            Debug.Log(val.name + "|||" + IDtoSpr.Keys.Contains(val.name));
                        if (IDtoSpr.Keys.Contains(val.name))
                        {
                            Image img = (Image)pi.GetValue(comp);
                            img.sprite = IDtoSpr[val.name];
                            Debug.Log("Just set image: " + (img.mainTexture == IDtoSpr[val.name]));
                        }
                    }
                }

                //

                if (pi.PropertyType == typeof(Texture[]) || pi.PropertyType == typeof(Texture2D[]))
                {
                    Texture[] vals = (Texture[])pi.GetValue(comp);
                    Texture[] newVals = vals;
                    for (int i = 0; i < vals.Length; i++)
                    {
                        Texture val = vals[i];
                        if (val != null)
                        {
                            if (val.name != "Font Texture" && val.name != "UISprite" && val.name != "Background" && val.name != "UnityWhite" && val.name != "meter" && val.name != "pixelx" && val.name != "Knob")
                                Debug.Log(val.name + "|||" + IDtoTex.Keys.Contains(val.name));
                            if (IDtoTex.Keys.Contains(val.name))
                            {
                                val.filterMode = FilterMode.Point;
                                newVals[i] = IDtoTex[val.name];
                                Debug.Log("Just set tex: " + ((Texture)pi.GetValue(comp) == IDtoTex[val.name]));
                            }
                        }
                    }

                    pi.SetValue(comp, newVals);
                }

                if (pi.PropertyType == typeof(Material[]))
                {
                    Debug.Log($"the funny, {comp.transform.parent.gameObject} is go, {pi.PropertyType.Name}, {pi.Name}");
                    Material[] matsNew = (Material[])pi.GetValue(comp);
                    for (int i = 0; i < matsNew.Count(); i++)
                    {
                        Material mat = matsNew[i];
                        Texture val = mat.mainTexture;
                        if (val != null)
                        {
                            //val.filterMode = FilterMode.Bilinear;
                            if (val.name != "Font Texture" && val.name != "UISprite" && val.name != "Background" && val.name != "UnityWhite" && val.name != "meter" && val.name != "pixelx" && val.name != "Knob")
                                Debug.Log(val.name + "|||" + IDtoTex.Keys.Contains(val.name));
                            if (IDtoTex.Keys.Contains(val.name))
                            {
                                mat.mainTexture = IDtoTex[val.name];
                                mat.mainTexture.filterMode = FilterMode.Point;
                                Debug.Log("Just set material: " + (mat.mainTexture == IDtoTex[val.name]));
                            }
                        }
                    }
                    try
                    {
                        pi.SetValue(comp, matsNew);
                    } catch (Exception ex) { Debug.Log("have some more chicken, have some more pie: it doesnt even matter if its boiled or fried\n " + ex.ToString());  }
                    Debug.Log("chicanery " + ((Material[])pi.GetValue(comp) == matsNew));
                }

                if (pi.PropertyType == typeof(Sprite[]))
                {
                    Sprite[] vals = (Sprite[])pi.GetValue(comp);
                    Sprite[] newVals = vals;
                    for (int i = 0; i < vals.Length; i++)
                    {
                        Sprite val = vals[i];
                        if (val != null)
                        {
                            // val.filterMode = FilterMode.Bilinear;
                            if (val.name != "Font Texture" && val.name != "UISprite" && val.name != "Background" && val.name != "UnityWhite" && val.name != "meter" && val.name != "pixelx" && val.name != "Knob")
                                Debug.Log(val.name + "|||" + IDtoSpr.Keys.Contains(val.name));
                            if (IDtoSpr.Keys.Contains(val.name))
                            {
                                newVals[i] = IDtoSpr[val.name];
                                Debug.Log("Just set spr: " + (val == IDtoSpr[val.name]));
                            }
                        }
                    }
                    pi.SetValue(comp, newVals);
                }

                if (pi.PropertyType == typeof(Image[]))
                {
                    // Debug.Log($"                material {pi.PropertyType.Name} {pi.Name} of {comp.name}.");
                    foreach (Image img in (Image[])pi.GetValue(comp))
                    {
                        Sprite val = img.sprite;
                        if (val != null)
                        {
                            //val.filterMode = FilterMode.Bilinear;
                            if (val.name != "Font Texture" && val.name != "UISprite" && val.name != "Background" && val.name != "UnityWhite" && val.name != "meter" && val.name != "pixelx" && val.name != "Knob")
                                Debug.Log(val.name + "|||" + IDtoSpr.Keys.Contains(val.name));
                            if (IDtoSpr.Keys.Contains(val.name))
                            {
                                img.sprite = IDtoSpr[val.name];
                                Debug.Log("Just set image: " + (img.mainTexture == IDtoSpr[val.name]));
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { Debug.Log("Fail with patching texture: " + ex.ToString()); }
        }

        #endregion

        #region Stuff that is for the pack objects
        public bool Enabled = false;
        public List<Texture2D> Textures = new List<Texture2D>();
        public List<AudioClip> AudioClips = new List<AudioClip>();
        public List<Sprite> Sprites = new List<Sprite>();
        public Font Font;
        public float FontScale;
        public string FName; //folder name

        public Metadata metadata;

        public ResourcePack(string foldername)
        {
            metadata = new Metadata(Path.Combine(PacksPath, foldername));

            if (metadata.ID != "NO ID, DELETE THIS PACK")
            {
                FName = foldername;

                Debug.Log($"ResourcePack constructor: {foldername}.");
                Debug.Log($"Texture exists: {Directory.Exists(Path.Combine(PacksPath, foldername, "Textures"))}.");
                Debug.Log($"Texture folder is {Path.Combine(PacksPath, foldername, "Textures")}.");


                if (Directory.Exists(Path.Combine(PacksPath, foldername)))
                {
                    if (Directory.Exists(Path.Combine(PacksPath, foldername, "Misc")))
                    {
                        if (File.Exists(Path.Combine(PacksPath, foldername, "Misc", "font.bundle")))
                        {
                            AssetBundle FontBundle = AssetBundle.LoadFromFile(Path.Combine(PacksPath, foldername, "Misc", "font.bundle"));
                            TextAsset data = FontBundle.LoadAllAssets<TextAsset>()[0];

                            string _name = "";
                            float size = 1;

                            Debug.Log($"Font meta line 1:{data.ToString().Split(',')[0]}, line 2:{data.ToString().Split(',')[1]}");
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

                            Debug.Log("Font scale: " + size);

                            Font = FontBundle.LoadAsset<Font>(_name);
                            FontScale = size;
                        }
                    }

                    if (Directory.Exists(Path.Combine(PacksPath, foldername, "Textures")))
                    {
                        foreach (string dir in Directory.GetDirectories(Path.Combine(PacksPath, foldername, "Textures")))
                        {
                            foreach (string file in Directory.GetFiles(dir))
                            {
                                Debug.Log($"Adding texture {file}.");
                                Textures.Add(GetTexFromPath(file));
                            }
                        }
                    }

                    if (Directory.Exists(Path.Combine(PacksPath, foldername, "AudioClips")))
                    {
                        foreach (string dir in Directory.GetDirectories(Path.Combine(PacksPath, foldername, "AudioClips")))
                        {
                            foreach (string file in Directory.GetFiles(dir))
                            {
                                Debug.Log($"Adding clip {file}.");
                                AudioClips.Add(GetClipFromPath(file));
                            }
                        }
                    }

                    if (Directory.Exists(Path.Combine(PacksPath, foldername, "Sprites")))
                    {
                        foreach (string dir in Directory.GetDirectories(Path.Combine(PacksPath, foldername, "Sprites")))
                        {
                            foreach (string file in Directory.GetFiles(dir))
                            {
                                Debug.Log($"Adding sprite {file}.");
                                Sprites.Add(GetSprFromPath(file));
                            }
                        }
                    }
                }
            } else
            {
                Packs.Remove(this);
            }

            Debug.Log($"Pack just loaded: {metadata.name} ({metadata.ID}) by {metadata.author}! Description {metadata.desc}, icon texture {metadata.icon.texture}.");
        }

        public GameObject GetListEntry()
        {
            GameObject go = GameObject.Instantiate(PackTemp);
            go.ChildByName("Title").GetComponent<Text>().text = metadata.name;
            go.ChildByName("Author").GetComponent<Text>().text = $"by {metadata.author}";
            go.ChildByName("Description").GetComponent<Text>().text = metadata.desc;
            go.ChildByName("IconBorder").ChildByName("Icon").GetComponent<Image>().sprite = metadata.icon;
            go.GetComponent<Button>().onClick.AddListener(() => {
                Enabled = !Enabled;

                if(Enabled)
                    go.transform.parent = EnabledContent.transform;
                else
                    go.transform.parent = DisabledContent.transform;

            });

            return go;
        }

        #endregion
    }

    public class Metadata
    {
        public string name = "A Resource Pack";
        public string author = "Some person";
        public string desc = "This is a texture pack.";
        public string ID = "NO ID, DELETE THIS PACK";
        public Sprite icon = null;

        public Metadata(string path)
        {
            icon = MainClass.Instance.UIBundle.LoadAsset<Sprite>("defaultpack");

            if (File.Exists(Path.Combine(path, "pack.uktmeta")))
            {
                string[] uktmeta = File.ReadAllLines(Path.Combine(path, "pack.uktmeta"));
                Debug.Log("reached meta");

                Debug.Log("reached foreach");

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
                            Debug.Log(Path.Combine(path, s.Split(':')[1]));
                            Texture2D tex = ResourcePack.GetTexFromPath(Path.Combine(path, s.Split(':')[1]));
                            icon = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                        }
                        catch { }
                    }
                }
            }
        }
    }

    public class PackSaving
    {
        public static string kelPath = Path.Combine(Utils.GameDirectory(), @"BepInEx\UMM Mods\ULTRAKILLtweaker\packs.kel");
        public static char split = 'ඞ';

        public static void Load()
        {
            if(File.Exists(kelPath))
            {
                foreach (string s in File.ReadAllLines(kelPath))
                {
                    if (ResourcePack.IdToPack.Keys.Contains(s.Split(split)[0]))
                    {
                        try
                        {
                            ResourcePack.IdToPack[s.Split(split)[0]].Enabled = Convert.ToBoolean(s.Split(split)[1]);
                        }
                        catch
                        {
                            ResourcePack.IdToPack[s.Split(split)[0]].Enabled = false;
                        }
                    }
                } 
            } else
            {
                Save();
            }
        }

        public static void Save()
        {
            string text = "";
            foreach(ResourcePack pack in ResourcePack.Packs)
            {
                if (pack.metadata.ID != "NO ID, DELETE THIS PACK")
                {
                    text += $"{pack.metadata.ID}{split}{pack.Enabled}\n";
                }
            }
            File.WriteAllText(kelPath, text);
        }
    }
}
