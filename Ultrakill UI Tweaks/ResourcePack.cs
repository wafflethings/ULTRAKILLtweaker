using FallFactory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace ULTRAKILLtweaker
{
    public class ResourcePack
    {
        public static List<ResourcePack> Packs = new List<ResourcePack>();

        public static void GetAllPacks()
        {
            foreach(string dir in Directory.GetDirectories(PacksPath))
            {
                Packs.Add(new ResourcePack(dir.PathToName()));
            }
        }

        #region Static stuff
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
                    return www.GetAudioClip();
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
            /*
            yield return new WaitForSeconds(3f);

            foreach (GameObject obj in GameObject.FindObjectsOfType<GameObject>())
            {
                if (obj.scene.name == SceneManager.GetActiveScene().name)
                {
                    obj.AddComponent<TextureOverwatch>();
                }
            }
            */

            #region Old method for patching, slow and bad, I'm just keeping it in case I need it
            /*
            Debug.Log("Patching textures.");
            List<Renderer> rens = new List<Renderer>();
            foreach (GameObject go in Utils.FindSceneObjects(SceneManager.GetActiveScene().name))
            {
                Debug.Log(go.name);
                if (go.GetComponent<Renderer>() != null)
                {
                    if(go.name != "Quad" && go.GetComponent<Renderer>().material.mainTexture != null)
                        rens.Add(go.GetComponent<Renderer>());
                }
            }

            foreach (ResourcePack pack in Packs)
            {
                foreach (Texture2D tex in pack.Textures)
                {
                    foreach (Renderer ren in rens)
                    {
                        if (ren.material != null)
                        {
                            if (ren.material.mainTexture != null)
                            {
                                Debug.Log($"Texture: [{ren.material.mainTexture.name}]+[{tex.name}].");
                                if (ren.material.mainTexture.name == tex.name)
                                {
                                    Debug.Log($"Texture swapping: {ren.material.mainTexture.name} == {tex.name}.");
                                    ren.material.mainTexture = tex;
                                }
                            }
                        }
                    }
                }
            }*/
            #endregion
        }

        #endregion

        #region Stuff that is for the pack objects
        public List<Texture2D> Textures = new List<Texture2D>();
        public List<AudioClip> AudioClips = new List<AudioClip>();
        public List<Sprite> Sprites = new List<Sprite>();
        public string Name;

        public ResourcePack(string name)
        {
            Name = name;

            Debug.Log($"ResourcePack constructor: {name}.");
            Debug.Log($"Texture exists: {Directory.Exists(Path.Combine(PacksPath, name, "Textures"))}.");
            Debug.Log($"Texture folder is {Path.Combine(PacksPath, name, "Textures")}.");

            if (Directory.Exists(Path.Combine(PacksPath, name, "Textures"))) 
            {
                foreach (string dir in Directory.GetDirectories(Path.Combine(PacksPath, name, "Textures")))
                {
                    foreach (string file in Directory.GetFiles(dir))
                    {
                        Debug.Log($"Adding texture {file}.");
                        Textures.Add(GetTexFromPath(file));
                    }
                }
            }

            if (Directory.Exists(Path.Combine(PacksPath, name, "AudioClips")))
            {
                foreach (string dir in Directory.GetDirectories(Path.Combine(PacksPath, name, "AudioClips")))
                {
                    foreach (string file in Directory.GetFiles(dir))
                    {
                        Debug.Log($"Adding clip {file}.");
                        AudioClips.Add(GetClipFromPath(file));
                    }
                }
            }

            if (Directory.Exists(Path.Combine(PacksPath, name, "Sprites")))
            {
                foreach (string dir in Directory.GetDirectories(Path.Combine(PacksPath, name, "Sprites")))
                {
                    foreach (string file in Directory.GetFiles(dir))
                    {
                        Debug.Log($"Adding sprite {file}.");
                        Sprites.Add(GetSprFromPath(file));
                    }
                }
            }
        }
        #endregion
    }
}
