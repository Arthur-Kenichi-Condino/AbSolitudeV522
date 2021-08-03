using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AKCondinoO.Sky{public class SkyCamera:MonoBehaviour{
void LateUpdate(){
transform.rotation=Camera.main.transform.rotation;
}
}
}