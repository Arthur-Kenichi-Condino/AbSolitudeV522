using AKCondinoO.Voxels;
using MLAPI;
using MLAPI.NetworkVariable;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using static AKCondinoO.Voxels.World;using static AKCondinoO.Voxels.TerrainChunk;
namespace AKCondinoO.Networking{public class UNetDefaultPrefab:NetworkBehaviour{public bool LOG=true;public int LOG_LEVEL=1;public int GIZMOS_ENABLED=1;
[NonSerialized]public NetworkObject network;[NonSerialized]public readonly NetworkVariableInt networkSeed=new NetworkVariableInt(new NetworkVariableSettings{WritePermission=NetworkVariablePermission.ServerOnly,ReadPermission=NetworkVariablePermission.Everyone,});
[NonSerialized]public readonly NetworkVariableVector3 networkPosition=new NetworkVariableVector3(new NetworkVariableSettings{WritePermission=NetworkVariablePermission.OwnerOnly,ReadPermission=NetworkVariablePermission.Everyone,});
[NonSerialized]public Bounds bounds;
[NonSerialized]public NavMeshDataInstance navMesh;[NonSerialized]public NavMeshData navMeshData;
void Awake(){
network=GetComponent<NetworkObject>();networkSeed.OnValueChanged+=OnSeedChanged;
}
public override void NetworkStart(){
                base.NetworkStart();
if(NetworkManager.Singleton.IsServer){
if(!IsLocalPlayer){
bounds=new Bounds(Vector3.zero,World.bounds.size);
navMeshData=new NavMeshData(0){//  Humanoid agent
hideFlags=HideFlags.None,
};
navMesh=NavMesh.AddNavMeshData(navMeshData);
}
}
if(NetworkManager.Singleton.IsClient){
bounds=new Bounds(Vector3.zero,World.bounds.size);
}
}
void OnDestroy(){
players.Remove(this);
if(LOG&&LOG_LEVEL<=1){Debug.Log("I'm now unregistered from players;players.Count:.."+players.Count);}
if(navMesh.valid){
NavMesh.RemoveNavMeshData(navMesh);
}
OnPlayerRemoved(this,LOG,LOG_LEVEL);
}
[NonSerialized]bool firstLoop=true;
[NonSerialized]Vector3 pos;
[NonSerialized]Vector3 pos_Pre;
[NonSerialized]public Vector2Int cCoord;
[NonSerialized]public Vector2Int cCoord_Pre;
[NonSerialized]Vector2Int cnkRgn;
void Update(){
if(IsLocalPlayer){
networkPosition.Value=transform.position=Camera.main.transform.position;
}else{
transform.position=networkPosition.Value;
}
if(NetworkManager.Singleton.IsServer){
if(biome.Seed!=networkSeed.Value){networkSeed.Value=biome.Seed;}

//...

if(!players.ContainsKey(this)||players[this]==null){
pos=transform.position;
if(firstLoop||pos!=pos_Pre){//  sempre que eu mudar de posição...
cCoord=vecPosTocCoord(pos);if(firstLoop||cCoord!=cCoord_Pre){
cnkRgn=cCoordTocnkRgn(cCoord);
players[this]=(cCoord,cCoord_Pre);
if(!IsLocalPlayer){Buildings.Buildings.playersChangedCoord[this]=(cCoord,cCoord_Pre);Actors.Actors.playersChangedCoord[this]=(cCoord,cCoord_Pre);
bounds.center=new Vector3(cnkRgn.x,0,cnkRgn.y);

//...

}

//...

cCoord_Pre=cCoord;}
pos_Pre=pos;}

//...

firstLoop=false;
}
}
if(NetworkManager.Singleton.IsClient){
if(!players.ContainsKey(this)||players[this]==null){
pos=transform.position;
if(firstLoop||pos!=pos_Pre){//  sempre que eu mudar de posição...
cCoord=vecPosTocCoord(pos);if(firstLoop||cCoord!=cCoord_Pre){
cnkRgn=cCoordTocnkRgn(cCoord);
players[this]=(cCoord,cCoord_Pre);

//...

bounds.center=new Vector3(cnkRgn.x,0,cnkRgn.y);

//...

cCoord_Pre=cCoord;}
pos_Pre=pos;}
firstLoop=false;
}
}
}
void OnSeedChanged(int old,int value){
if(LOG&&LOG_LEVEL<=1){Debug.Log("Seed changed from .."+old+".. to .."+value);}

//...

}

//...

#if UNITY_EDITOR
void OnDrawGizmos(){
if(GIZMOS_ENABLED<=1){

//...

if(NetworkManager.Singleton.IsServer){
if(!IsLocalPlayer){
DrawBounds(bounds,Color.yellow);
}
}
if(NetworkManager.Singleton.IsClient){
DrawBounds(bounds,Color.yellow);
}
}
}
#endif
}
}