﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;

namespace ULTRAKILLtweaker.Tweaks.UIElements.Impl
{
    public class ToggleSubsettingElement : UIElement
    {
        public ToggleSubsettingElement(Metadata meta) : base(null, meta)
        {
            Self.ChildByName("Option Name").GetComponent<Text>().text = meta.Name;
            Self.ChildByName("Option Description").GetComponent<Text>().text = meta.Description;
        }

        public override string AssetName()
        {
            return "Subsetting - Toggle";
        }

        public override int Offset()
        {
            return 20;
        }
    }
}