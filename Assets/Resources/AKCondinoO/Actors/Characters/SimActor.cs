using AKCondinoO.Voxels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using static AKCondinoO.Voxels.TerrainChunk;using static AKCondinoO.Voxels.World;
namespace AKCondinoO.Actors{public class SimActor:MonoBehaviour{public bool LOG=true;public int LOG_LEVEL=1;public int GIZMOS_ENABLED=1;
bool Stop{
get{bool tmp;lock(Stop_Syn){tmp=Stop_v;      }return tmp;}
set{         lock(Stop_Syn){    Stop_v=value;}if(value){foregroundData.Set();}}
}[NonSerialized]readonly object Stop_Syn=new object();[NonSerialized]bool Stop_v=false;[NonSerialized]readonly AutoResetEvent foregroundData=new AutoResetEvent(false);[NonSerialized]readonly ManualResetEvent backgroundData=new ManualResetEvent(true);[NonSerialized]Task task;
[NonSerialized]public readonly object saving_Syn=new object();
public Type type{get;protected set;}
protected virtual void Awake(){if(transform.parent!=Actors.staticScript.transform){transform.parent=Actors.staticScript.transform;}



task=Task.Factory.StartNew(BG,new object[]{LOG,LOG_LEVEL,savePath,},TaskCreationOptions.LongRunning);
void BG(object state){Thread.CurrentThread.IsBackground=false;Thread.CurrentThread.Priority=System.Threading.ThreadPriority.BelowNormal;
try{
if(state is object[]parameters&&parameters[0]is bool LOG&&parameters[1]is int LOG_LEVEL&&parameters[2]is string savePath){
if(LOG&&LOG_LEVEL<=1)Debug.Log("inicializar trabalho em plano de fundo para ator");
while(!Stop){foregroundData.WaitOne();if(Stop)goto _Stop;

}_Stop:{
}
if(LOG&&LOG_LEVEL<=1)Debug.Log("finalizar trabalho em plano de fundo para ator graciosamente");
}
}catch(Exception e){Debug.LogError(e?.Message+"\n"+e?.StackTrace+"\n"+e?.Source);}
}
}
protected virtual void OnDestroy(){

//...
Stop=true;try{task.Wait();}catch(Exception e){Debug.LogError(e?.Message+"\n"+e?.StackTrace+"\n"+e?.Source);}foregroundData.Dispose();backgroundData.Dispose();

}






/*public Type type{get;protected set;}
[NonSerialized]public new CharacterControllerPhys collider;
protected virtual void Awake(){if(transform.parent!=Actors.staticScript.transform){transform.parent=Actors.staticScript.transform;}
type=GetType();
collider=GetComponent<CharacterControllerPhys>();
if(LOG&&LOG_LEVEL<=1)Debug.Log("I got instantiated and I am of type.."+type+"..now, add myself to actors pool",this);
Actors.Disable(this);

//...

}
public int id{get;set;}[NonSerialized]public LinkedListNode<SimActor>Disabled=null;*/



//public virtual bool IsOutOfSight{get{return IsOutOfSight_v;}set{if(IsOutOfSight_v!=value){IsOutOfSight_v=value;
//collider.controller.enabled=!value;
//collider           .enabled=!value;
//if(value){
//Actors.Disable(this);
//}else{
//var loadInfo=Actors.GetNextId(this);

////...

//}
//if(LOG&&LOG_LEVEL<=1)Debug.Log("I am now..IsOutOfSight:"+value+"..so my id is.."+id,this);
//}}
//}[NonSerialized]protected bool IsOutOfSight_v;

//...

/*
protected virtual void Awake(){if(transform.parent!=Actors.staticScript.transform){transform.parent=Actors.staticScript.transform;}
type=GetType();
collider=GetComponent<CharacterControllerPhys>();
if(LOG&&LOG_LEVEL<=1)Debug.Log("I got instantiated and I am of type.."+type+"..now, add myself to actors pool",this);

//...
IsOutOfSight=true;pos=pos_Pre=transform.position;cCoord=cCoord_Pre=vecPosTocCoord(pos);cnkIdx=GetcnkIdx(cCoord.x,cCoord.y);
if(LOG&&LOG_LEVEL<=1)Debug.Log("I am currently at.."+pos+"..and my cCoord is.."+cCoord+"..,so my cnkIdx is.."+cnkIdx,this);

}
protected virtual void OnDestroy(){

//...

}
public int id{get;set;}[NonSerialized]public LinkedListNode<SimActor>Disabled=null;
public virtual bool IsOutOfSight{get{return IsOutOfSight_v;}set{if(IsOutOfSight_v!=value){IsOutOfSight_v=value;

//...
collider.controller.enabled=!value;
collider           .enabled=!value;
if(value){
Disabled=Actors.Pool(this);
}else{
//...
}
if(LOG&&LOG_LEVEL<=1)Debug.Log("I am now..IsOutOfSight:"+value+"..so my id is.."+id,this);

}}
}[NonSerialized]protected bool IsOutOfSight_v;
[NonSerialized]bool firstLoop=true;
[NonSerialized]Vector3    actPos;
[NonSerialized]Vector2Int aCoord,aCoord_Pre;
[NonSerialized]protected Vector3 pos;
[NonSerialized]protected Vector3 pos_Pre;
[NonSerialized]protected Vector2Int cCoord;
[NonSerialized]protected Vector2Int cCoord_Pre;
[NonSerialized]protected int cnkIdx;[NonSerialized]protected TerrainChunk cnk=null;
protected virtual void Update(){
if(!IsOutOfSight){
var gotcnk=false;void getcnk(){ActiveTerrain.TryGetValue(cnkIdx,out cnk);gotcnk=true;}     
pos=transform.position;

//...

}
}*/

//...

//public Type type{get;protected set;}public int id{get;protected set;}
//[NonSerialized]protected new CharacterControllerPhys collider;
//public LinkedListNode<SimActor>Disabled=null;
//protected virtual void Awake(){if(transform.parent!=Actors.staticScript.transform){transform.parent=Actors.staticScript.transform;}

////...
//collider=GetComponent<CharacterControllerPhys>();
//IsOutOfSight=true;

//}

////...

//public virtual bool IsOutOfSight{get{return IsOutOfSight_v;}set{if(IsOutOfSight_v!=value){IsOutOfSight_v=value;
//if(LOG&&LOG_LEVEL<=1)Debug.Log("I am now..IsOutOfSight:"+value,this);
//if(value){

////...
//collider           .enabled=false;
//collider.controller.enabled=false;

//}else{

////...
//collider           .enabled=true;
//collider.controller.enabled=true;

//}
////...

//}}
//}[NonSerialized]protected bool IsOutOfSight_v;

/*public Type type{get;protected set;}public int id{get;protected set;}
protected virtual void Awake(){

//...
var added=Actors.Add(this);type=added.type;id=added.id;

}*/
/*public Type type{get;protected set;}public int id{get;protected set;}
[NonSerialized]protected new CharacterControllerPhys collider;
protected virtual void Awake(){if(transform.parent!=Actors.staticScript.transform){transform.parent=Actors.staticScript.transform;}
      
//...
type=GetType();if(!Actors.Count.ContainsKey(type)){Actors.Count.Add(type,1);}else{id=Actors.Count[type]++;}Actors.Get.Add((type,id),this);
if(LOG&&LOG_LEVEL<=1)Debug.Log("I am.."+type+"..and I got instantiated with id.."+id,this);
collider=GetComponent<CharacterControllerPhys>();
//...

IsOutOfSight=true;pos=pos_Pre=transform.position;cCoord=cCoord_Pre=vecPosTocCoord(pos);cnkIdx=GetcnkIdx(cCoord.x,cCoord.y);
if(LOG&&LOG_LEVEL<=1)Debug.Log("I am currently at.."+pos+"..and my cCoord is.."+cCoord+"..,so my cnkIdx is.."+cnkIdx,this);

}
protected virtual void Start(){

//...

}
protected virtual void OnDestroy(){
            
//...

}
[NonSerialized]bool firstLoop=true;
[NonSerialized]Vector3    actPos;
[NonSerialized]Vector2Int aCoord,aCoord_Pre;
public virtual bool IsOutOfSight{get{return IsOutOfSight_v;}set{if(IsOutOfSight_v!=value){IsOutOfSight_v=value;
if(LOG&&LOG_LEVEL<=1)Debug.Log("I am now..IsOutOfSight:"+value,this);
if(value){

//...
Actors.Disabled.Add(this);Actors. Enabled.Remove(this);
collider.enabled=(false);collider.controller.enabled=(false);

}else{

//...
Actors. Enabled.Add(this);Actors.Disabled.Remove(this);
collider.enabled=( true);collider.controller.enabled=( true);

}
//...

}}
}[NonSerialized]protected bool IsOutOfSight_v;
[NonSerialized]protected Vector3 pos;
[NonSerialized]protected Vector3 pos_Pre;
[NonSerialized]protected Vector2Int cCoord;
[NonSerialized]protected Vector2Int cCoord_Pre;
[NonSerialized]protected int cnkIdx;[NonSerialized]protected TerrainChunk cnk=null;
protected virtual void Update(){
var gotcnk=false;void getcnk(){ActiveTerrain.TryGetValue(cnkIdx,out cnk);gotcnk=true;}            
pos=transform.position;
if(pos!=pos_Pre){//  sempre que eu mudar de posi��o...
if(LOG&&LOG_LEVEL<=-110)Debug.Log("I changed from pos_Pre.."+pos_Pre+"..to pos.."+pos,this);
cCoord=vecPosTocCoord(pos);if(cCoord!=cCoord_Pre){cnkIdx=GetcnkIdx(cCoord.x,cCoord.y);//  ...calcule o cnkIdx se necess�rio...
if(LOG&&LOG_LEVEL<=1)Debug.Log("I changed from cCoord_Pre.."+cCoord_Pre+"..to cCoord.."+cCoord+"..so now my cnkIdx is.."+cnkIdx,this);
if(!gotcnk){getcnk();}//  ...e verifique se a coordenada para onde eu me mudei tem ou n�o um chunk.
cCoord_Pre=cCoord;}
pos_Pre=pos;}
if(firstLoop||actPos!=Camera.main.transform.position){if(LOG&&LOG_LEVEL<=-110){Debug.Log("actPos anterior:.."+actPos+"..;actPos novo:.."+Camera.main.transform.position);}
              actPos=(Camera.main.transform.position);//  sempre que a c�mera mudar de posi��o...
if(firstLoop |aCoord!=(aCoord=vecPosTocCoord(actPos))){if(LOG&&LOG_LEVEL<=1){Debug.Log("aCoord novo:.."+aCoord+"..;aCoord_Pre:.."+aCoord_Pre);}
if(!gotcnk){getcnk();}//  ...e a coordenada mudar (e houver recarregamento), cheque se o chunk em que estou existe.
aCoord_Pre=aCoord;}
}
if(pos.y<-128){//  marque como fora do mundo (sem op��o de testar como dentro do mundo em outras condi��es) se estiver abaixo da altura m�nima permitida.
if(LOG&&LOG_LEVEL<=-120)Debug.Log("I am out of the World (pos.y.."+pos.y+"..<-128)",this);
IsOutOfSight=true;
}else{
IsOutOfSight=(cnk==null||!cnk.Built
||!bounds.Contains(transform.position)
);
}

//...

firstLoop=false;}*/
}
}