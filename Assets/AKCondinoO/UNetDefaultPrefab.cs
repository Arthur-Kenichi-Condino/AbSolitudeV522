using MLAPI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AKCondinoO.Networking{public class UNetDefaultPrefab:NetworkBehaviour{
void Update(){
if(IsLocalPlayer){
transform.position=Camera.main.transform.position;
}
if(NetworkManager.Singleton.IsServer){

//...

}
}
}
}