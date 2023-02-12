using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace ULTRAKILLtweaker.Tweaks.UIElements
{
    public class Page
    {
        public GameObject Self;
        public LayoutGroup Holder;
        public string PageName;

        public Page(string name, Transform parent)
        {
            Self = GameObject.Instantiate(UIElement.Settings["Page"], parent);
            Self.ChildByName("Title").GetComponent<Text>().text = name;
            Holder = Self.GetComponentInChildren<LayoutGroup>();
            PageName = name;
        }
    }
}
