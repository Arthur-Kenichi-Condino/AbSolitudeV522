using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AKCondinoO.Buildings{public class SimObject:MonoBehaviour{public bool LOG=true;public int LOG_LEVEL=1;public int GIZMOS_ENABLED=1;
protected virtual void Awake(){if(transform.parent!=Buildings.staticScript.transform){transform.parent=Buildings.staticScript.transform;}



}
}
}