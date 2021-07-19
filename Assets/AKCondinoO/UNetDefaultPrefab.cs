using AKCondinoO.Voxels;
using MLAPI;
using MLAPI.NetworkVariable;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AKCondinoO.Networking{public class UNetDefaultPrefab:NetworkBehaviour{
[NonSerialized]public NetworkObject network;
[NonSerialized]public readonly NetworkVariableVector3 networkPosition=new NetworkVariableVector3(new NetworkVariableSettings{WritePermission=NetworkVariablePermission.OwnerOnly,ReadPermission=NetworkVariablePermission.Everyone,});
void Awake(){
network=GetComponent<NetworkObject>();
}
void OnDestroy(){
World.players.Remove(this);
}
void Update(){
if(IsLocalPlayer){
networkPosition.Value=transform.position=Camera.main.transform.position;
}else{
transform.position=networkPosition.Value;
}
if(NetworkManager.Singleton.IsServer){
World.players[this]=transform.position;

//...

}
}
}
}