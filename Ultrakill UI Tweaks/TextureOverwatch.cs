using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace ULTRAKILLtweaker
{
    public class TextureOverwatch : MonoBehaviour
    {
        public void Start()
        {
            /*Renderer ren = null;

            if (GetComponent<Renderer>() != null)
                ren = GetComponent<Renderer>();

            if (ren == null)
            {
                Destroy(this);
            } 

            if (ren.material.mainTexture == null)
            {
                Destroy(this);
            }
            else
            {

                foreach (ResourcePack pack in ResourcePack.Packs)
                {
                    foreach (Texture2D tex in pack.Textures)
                    {
                        if (ren != null)
                        {
                            if (ren.material.name.StartsWith("UKT_"))
                            {
                                break;
                            }

                            if (ren.material.mainTexture.name == tex.name)
                            {
                                Debug.Log($"Texture swapping: {ren.material.mainTexture.name} == {tex.name}.");
                                ren.material.name = $"UKT_{ren.material.name}";
                                ren.material.mainTexture = tex;
                                break;
                            }
                        }
                    }
                }
            }*/
        }
    }
}
