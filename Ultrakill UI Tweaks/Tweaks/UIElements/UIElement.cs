using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace ULTRAKILLtweaker.Tweaks.UIElements
{
    public class UIElement
    {
        public Metadata meta;
        public GameObject Self;

        public UIElement(LayoutGroup lg, Metadata meta)
        {
            Self = GameObject.Instantiate(UIPreloader.Settings[AssetName()]);
            Self.GetComponent<RectTransform>().SetParent(lg.transform);

            RectTransform rt = lg.GetComponent<RectTransform>();
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rt.rect.height + Offset());
            Self.transform.localScale = Vector3.one;

            // Set name, desc, stuff
        }

        public virtual string AssetName()
        {
            return "";
        }

        public virtual int Offset()
        {
            return 80;
        }
    }
}
