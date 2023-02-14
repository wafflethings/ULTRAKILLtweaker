using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;

namespace ULTRAKILLtweaker.Tweaks.UIElements.Impl
{
    public class MutatorElement : TweakSettingElement
    {
        public MutatorElement(LayoutGroup lg, Metadata meta, Setting[] children = null) : base(lg, meta, children)
        {
            if (meta.Image != null) 
            {
                Self.ChildByName("Icon").GetComponent<Image>().sprite = meta.Image;
            }
        }

        public override string AssetName()
        {
            return "Mutator";
        }
    }
}
