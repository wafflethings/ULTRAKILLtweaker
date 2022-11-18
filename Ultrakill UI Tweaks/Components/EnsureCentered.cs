using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ULTRAKILLtweaker.Components
{
    public class EnsureCentered : MonoBehaviour
    {
        public void Update()
        {
            transform.position = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0);
        }
    }
}
