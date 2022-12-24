using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ULTRAKILLtweaker
{
    public class UnrollSubsettings : MonoBehaviour, IPointerClickHandler
    {
        GameObject Host;

        public void Start()
        {
            Host = transform.parent.gameObject.ChildByName("Subsetting Host");
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Host.SetActive(!Host.activeInHierarchy);
        }
    }
}
