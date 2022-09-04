using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ULTRAKILLtweaker
{
    public class MitosisController : MonoBehaviour
    {
        public GameObject Base;
        public bool BaseSet = false;

        public void Update()
        {
            if (BaseSet)
            {
                if (Base == null)
                    Destroy(this);

                gameObject.SetActive(Base.activeSelf);
            }
        }

        public void SetBase(GameObject go)
        {
            Base = go;
            BaseSet = true;
        }
    }
}
