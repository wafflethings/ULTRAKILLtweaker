using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace ULTRAKILLtweaker
{
    public class MaterialOverwatch : MonoBehaviour
    {
        Texture Last;
        Renderer ren;
        List<Material> MyMats;

        public void Awwaake()
        {
            try
            {
                if (GetComponent<Renderer>() == null)
                    Destroy(this);

                ren = GetComponent<Renderer>();

                if (ren.material != null && ren.material.mainTexture != null)
                {
                    try
                    {
                        if (!ResourcePack.IDtoTex.Keys.Contains(ren.material.mainTexture.name))
                        {
                           //  Destroy(this);
                        }
                    }
                    catch { }
                }

                bool contains = false;
                if(ren.materials != null)
                {
                    foreach(Material mat in ren.materials)
                    {
                        if (mat.mainTexture != null)
                        {
                            if (ResourcePack.IDtoTex.Keys.Contains(mat.mainTexture.name))
                            {
                                MyMats.Add(mat);
                                contains = true;
                                break;
                            }
                        }
                    }
                }

                if(!contains)
                {
                    Destroy(this);
                }

            } catch { Destroy(this); }
        }

        // SetTextureImpl(int name, Texture value)

        public void Update()
        {
            Debug.Log("Update" + ren.material == null + "asd" + ren.material.mainTexture == null);
            if (ren.material != null && ren.material.mainTexture != null)
            {
                Debug.Log("aghhhh" + MyMats.Contains(ren.material));
                if (MyMats.Contains(ren.material) && Last != ren.material.mainTexture)
                {
                    Debug.Log("jimmy");
                    ren.material.mainTexture = ResourcePack.IDtoTex[ren.material.mainTexture.name];
                    Last = ren.material.mainTexture;
                }
            } else
            {
                Destroy(this);
            }
        }
    }
}
