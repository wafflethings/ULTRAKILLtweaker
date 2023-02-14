using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace ULTRAKILLtweaker.Tweaks.UIElements.Impl
{
    public class TweakSettingElement : UIElement
    {
        public LayoutGroup Subsettings { get; private set; }

        public TweakSettingElement(LayoutGroup lg, Metadata meta, Setting[] children = null) : base(lg, meta)
        {
            Self.ChildByName("Option Name").GetComponent<Text>().text = meta.Name;
            Self.ChildByName("Bubble").ChildByName("Text").GetComponent<Text>().text = meta.Description;
            Self.ChildByName("Description").AddComponent<DescriptionBubble>();
            Self.ChildByName("Arrow").AddComponent<UnrollSubsettings>();
            Subsettings = Self.ChildByName("Subsetting Host").GetComponent<VerticalLayoutGroup>();

            DescriptionBubble db = Self.ChildByName("Warning").AddComponent<DescriptionBubble>();
            db.Bubble = Self.ChildByName("WarningBubble");
            db.Bubble.SetActive(false);

            db.gameObject.SetActive(meta.RequiresRestart);

            if (!meta.DescriptionEnabled)
                Self.ChildByName("Arrow").SetActive(false);

            Self.ChildByName("Bubble").SetActive(false);
            Self.ChildByName("Subsetting Host").SetActive(false);

            Self.AddComponent<TweakSettingBehaviour>();

            if (children != null)
            {
                foreach (Setting s in children)
                {
                    s.self.GetComponent<RectTransform>().SetParent(Subsettings.transform);

                    RectTransform rt = Subsettings.GetComponent<RectTransform>();
                    rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rt.rect.height + Offset());
                    s.self.transform.localScale = Vector3.one;
                }
            }
        }

        public class TweakSettingBehaviour : MonoBehaviour
        {
            public void OnEnable()
            {
                if (gameObject.ChildByName("Subsetting Host").transform.childCount == 0)
                    gameObject.ChildByName("Arrow").SetActive(false);
                else
                    gameObject.ChildByName("Arrow").SetActive(true);
            }
        }

        public override string AssetName()
        {
            return "Setting";
        }
    }
}
