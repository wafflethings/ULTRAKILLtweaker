using FallFactory;
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
    public class ArtifactHover : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        public ArtifactSetting me;

        public void OnPointerEnter(PointerEventData eventData)
        {
            // HudMessageReceiver.Instance.SendHudMessage(me.Description);
            MainClass.Instance.TweakerMenu.ChildByName("Modifiers").ChildByName("Description").GetComponent<Text>().text = $"<b>{me.Name.ToUpper()}</b>: " + me.Description;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log("Pointer click detected on artifact toggle.");
            if(GetComponent<Toggle>().enabled && eventData.button == PointerEventData.InputButton.Left)
            {
                MainClass.Instance.ModsChanged = true;
            }

            if (GetComponent<Toggle>().enabled && eventData.button == PointerEventData.InputButton.Right)
            {
                MainClass.Instance.ModsChanged = true;

                GameObject ex = gameObject.ChildByName("Extra Settings");
                if (ex != null)
                {
                    gameObject.GetComponent<Toggle>().enabled = false;
                    ex.SetActive(true);
                    foreach(Setting set in SettingRegistry.settings)
                    {
                        if(set.GetType() == typeof(ArtifactSetting))
                        {
                            if(((ArtifactSetting)set).toggle.gameObject != gameObject)
                                ((ArtifactSetting)set).toggle.gameObject.SetActive(false);
                        }
                    }
                }
            }
        }

        public void OnEnable()
        {

        }
    }
}
