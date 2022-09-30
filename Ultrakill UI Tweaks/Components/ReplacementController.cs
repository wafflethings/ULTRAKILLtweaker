using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ULTRAKILLtweaker.Components
{
    public class ReplacementController : MonoBehaviour
    {
        // since the only patchable method for the replacement objects is OnEnable, we do stuff on enable
        // sometimes gets fucky when you do it more than once so this class's sole purpose is to prevent this
        // if this class exists, nothing happens. this class is literally empty
    }
}
