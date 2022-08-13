using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ULTRAKILLtweaker
{
    public class BlessWhenFar : MonoBehaviour
    {
        EnemyIdentifier eid;

        public void Start()
        {
            eid = GetComponent<EnemyIdentifier>();
        }

        public void Update()
        {
            if(Vector3.Distance(transform.position, GameObject.FindGameObjectWithTag("Player").transform.position) > Convert.ToSingle(SettingRegistry.idToSetting["artiset_distance_distfromplayer"].value))
            {
                if(!eid.blessed)
                {
                    eid.Bless();
                }
            } else
            {
                if (eid.blessed)
                {
                    eid.Unbless();
                }
            }
        }
    }
}
