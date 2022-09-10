using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ULTRAKILLtweaker
{
    internal class TouchingGround : MonoBehaviour
    {
        GroundCheck gc;
        bool LastOne;

        private void Start()
        {
            gc = GetComponent<GroundCheck>();
        }

        private void Update()
        {
            if (gc.touchingGround != LastOne)
            {
                MainClass.Instance.TouchingGroundChanged(gc);
            }
        }

        private void LateUpdate()
        {
            LastOne = gc.touchingGround;
        }
	}
}
