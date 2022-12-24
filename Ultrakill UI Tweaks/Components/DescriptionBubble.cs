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
    public class DescriptionBubble : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public GameObject Bubble;

        public void Start()
        {
            if(Bubble == null)
                Bubble = transform.parent.gameObject.ChildByName("Bubble");
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Bubble.SetActive(true);   
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Bubble.SetActive(false);
        }
    }
}
