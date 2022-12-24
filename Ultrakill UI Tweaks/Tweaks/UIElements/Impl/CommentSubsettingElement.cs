using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;

namespace ULTRAKILLtweaker.Tweaks.UIElements.Impl
{
    public class CommentSubsettingElement : UIElement
    {
        public CommentSubsettingElement(LayoutGroup lg, Metadata meta) : base(lg, meta)
        {
            Self.ChildByName("Option Name").GetComponent<Text>().text = meta.Name;
            Self.ChildByName("Option Description").GetComponent<Text>().text = meta.Description;
        }

        public override string AssetName()
        {
            return "Subsetting - Comment";
        }

        public override int Offset()
        {
            return 20;
        }
    }
}
