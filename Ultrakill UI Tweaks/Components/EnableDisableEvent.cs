using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ULTRAKILLtweaker
{
    public class EnableDisableEvent : MonoBehaviour
    {
        public void OnDisable()
        {
            MainClass.MenuDisable();
        }

        public void OnEnable()
        {
            MainClass.MenuEnable();
        }
    }
}
