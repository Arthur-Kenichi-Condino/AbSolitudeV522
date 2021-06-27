using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static AKCondinoO.Voxels.TerrainChunk;using static AKCondinoO.Voxels.World;
namespace AKCondinoO.Actors{public class Actors:MonoBehaviour{public bool LOG=true;public int LOG_LEVEL=1;
[NonSerialized]public static Actors staticScript;[NonSerialized]public static readonly Dictionary<(Type type,int id),SimActor>Get=new Dictionary<(Type,int),SimActor>();[NonSerialized]public static readonly Dictionary<Type,int>Count=new Dictionary<Type,int>();

//...
[NonSerialized]Vector3    actPos;
[NonSerialized]Vector2Int aCoord,aCoord_Pre;
[NonSerialized]Vector2Int actRgn;

void Awake(){staticScript=this;
            
actPos=Camera.main.transform.position;aCoord=aCoord_Pre=vecPosTocCoord(actPos);actRgn=cCoordTocnkRgn(aCoord);
for(Vector2Int iCoord=new Vector2Int(),cCoord1=new Vector2Int();iCoord.y<=instantiationDistance.y;iCoord.y++){for(cCoord1.y=-iCoord.y+aCoord.y;cCoord1.y<=iCoord.y+aCoord.y;cCoord1.y+=iCoord.y*2){
for(           iCoord.x=0                                      ;iCoord.x<=instantiationDistance.x;iCoord.x++){for(cCoord1.x=-iCoord.x+aCoord.x;cCoord1.x<=iCoord.x+aCoord.x;cCoord1.x+=iCoord.x*2){
if(Math.Abs(cCoord1.x)>=MaxcCoordx||
   Math.Abs(cCoord1.y)>=MaxcCoordy){
if(LOG&&LOG_LEVEL<=1)Debug.Log("do not try to load out of world data at coord:.."+cCoord1);
goto _skip;
}
if(LOG&&LOG_LEVEL<=1)Debug.Log("try to load sim actors at:.."+cCoord1);

//...

_skip:{}
if(iCoord.x==0){break;}}}
if(iCoord.y==0){break;}}}
//...to do: instanciar todos os actors

}
void Start(){
            
//...to do: 

}
[NonSerialized]public static readonly List<SimActor>Enabled=new List<SimActor>();[NonSerialized]public static readonly List<SimActor>Disabled=new List<SimActor>();
}
}