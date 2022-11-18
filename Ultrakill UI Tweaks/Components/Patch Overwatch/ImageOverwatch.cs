using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace ULTRAKILLtweaker
{
    public class ImageOverwatch : MonoBehaviour
    {
        Sprite Last;
        Image img;

        public void Awake()
        {
            try
            {
                Debug.Log("ImageOverwatch");

                if (GetComponent<Image>() == null)
                    Destroy(this);

                img = GetComponent<Image>();

                if (img.sprite != null)
                {
                    try
                    {
                        if (!ResourcePack.IDtoSpr.Keys.Contains(img.sprite.name))
                        {
                            // Destroy(this);
                        }
                    }
                    catch { }
                }
            } catch { Destroy(this); }
        }

        public void Update()
        {
            if (img.sprite != null)
            {
                img.sprite = ResourcePack.IDtoSpr[img.sprite.name];
            } else
            {
                Destroy(this);
            }
        }
    }
}
