using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ULTRAKILLtweaker.Components
{
    public class DisableWithEsc : MonoBehaviour
    {
        public void Update()
        {
            if(Input.GetKeyDown(KeyCode.Escape))
            {
                PackSaving.Save();
                ResourcePack.SetDicts();
                gameObject.SetActive(false);
            }
        }
    }
}
