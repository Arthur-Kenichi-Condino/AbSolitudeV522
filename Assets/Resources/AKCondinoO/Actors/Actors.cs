using MessagePack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using static AKCondinoO.Voxels.TerrainChunk;using static AKCondinoO.Voxels.World;
namespace AKCondinoO.Actors{public class Actors:MonoBehaviour{public bool LOG=true;public int LOG_LEVEL=1;        
[NonSerialized]public static Actors staticScript;
static bool Stop{
get{bool tmp;lock(Stop_Syn){tmp=Stop_v;      }return tmp;}
set{         lock(Stop_Syn){    Stop_v=value;}if(value){foregroundData1.Set();foregroundData2.Set();}}
}[NonSerialized]static readonly object Stop_Syn=new object();[NonSerialized]static bool Stop_v=false;
[NonSerialized]static readonly AutoResetEvent foregroundData1=new AutoResetEvent(false);[NonSerialized]static readonly ManualResetEvent backgroundData1=new ManualResetEvent(true);[NonSerialized]static Task task1=null;
[NonSerialized]static readonly AutoResetEvent foregroundData2=new AutoResetEvent(false);[NonSerialized]static readonly ManualResetEvent backgroundData2=new ManualResetEvent(true);[NonSerialized]static Task task2=null;
[NonSerialized]public static readonly object loading_Syn=new object();
void Awake(){staticScript=this;

//...
actPos=Camera.main.transform.position;aCoord=aCoord_Pre=vecPosTocCoord(actPos);actRgn=cCoordTocnkRgn(aCoord);

task1=Task.Factory.StartNew(BG1,new object[]{LOG,LOG_LEVEL,savePath,},TaskCreationOptions.LongRunning);
task2=Task.Factory.StartNew(BG2,new object[]{LOG,LOG_LEVEL,savePath,},TaskCreationOptions.LongRunning);
static void BG1(object state){Thread.CurrentThread.IsBackground=false;Thread.CurrentThread.Priority=System.Threading.ThreadPriority.BelowNormal;
try{
if(state is object[]parameters&&parameters[0]is bool LOG&&parameters[1]is int LOG_LEVEL&&parameters[2]is string savePath){
if(LOG&&LOG_LEVEL<=1)Debug.Log("inicializar trabalho em plano de fundo 1 para gerenciar atores");
while(!Stop){foregroundData1.WaitOne();if(Stop)goto _Stop;

}_Stop:{
}
if(LOG&&LOG_LEVEL<=1)Debug.Log("finalizar trabalho em plano de fundo 1 para gerenciar atores graciosamente");
}
}catch(Exception e){Debug.LogError(e?.Message+"\n"+e?.StackTrace+"\n"+e?.Source);}
}
static void BG2(object state){Thread.CurrentThread.IsBackground=false;Thread.CurrentThread.Priority=System.Threading.ThreadPriority.BelowNormal;
try{
if(state is object[]parameters&&parameters[0]is bool LOG&&parameters[1]is int LOG_LEVEL&&parameters[2]is string savePath){
if(LOG&&LOG_LEVEL<=1)Debug.Log("inicializar trabalho em plano de fundo 2 para gerenciar atores");
while(!Stop){foregroundData2.WaitOne();if(Stop)goto _Stop;

}_Stop:{
}
if(LOG&&LOG_LEVEL<=1)Debug.Log("finalizar trabalho em plano de fundo 2 para gerenciar atores graciosamente");
}
}catch(Exception e){Debug.LogError(e?.Message+"\n"+e?.StackTrace+"\n"+e?.Source);}
}
}
void OnDestroy(){
Stop=true;try{task1.Wait();}catch(Exception e){Debug.LogError(e?.Message+"\n"+e?.StackTrace+"\n"+e?.Source);}foregroundData1.Dispose();backgroundData1.Dispose();
          try{task2.Wait();}catch(Exception e){Debug.LogError(e?.Message+"\n"+e?.StackTrace+"\n"+e?.Source);}foregroundData2.Dispose();backgroundData2.Dispose();
}
[NonSerialized]bool firstLoop=true;
[NonSerialized]Vector3    actPos;
[NonSerialized]Vector2Int aCoord,aCoord_Pre;
[NonSerialized]Vector2Int actRgn;
void Update(){
if(backgroundData2.WaitOne(0)){
if(backgroundData1.WaitOne(0)){

//...

firstLoop=false;}}}





        



/*[NonSerialized]public static Actors staticScript;
[NonSerialized]static readonly Dictionary<Type,GameObject>Prefabs=new Dictionary<Type,GameObject>();
static bool Stop{
get{bool tmp;lock(Stop_Syn){tmp=Stop_v;      }return tmp;}
set{         lock(Stop_Syn){    Stop_v=value;}if(value){foregroundData1.Set();}}
}[NonSerialized]static readonly object Stop_Syn=new object();[NonSerialized]static bool Stop_v=false;[NonSerialized]static readonly AutoResetEvent foregroundData1=new AutoResetEvent(false);[NonSerialized]static readonly ManualResetEvent backgroundData1=new ManualResetEvent(false);[NonSerialized]static Task task1;        
void Awake(){staticScript=this;
var objects=Resources.LoadAll("AKCondinoO/Actors/Characters",typeof(GameObject));
foreach(var o in objects){var p=o as GameObject;var t=p.GetComponent<SimActor>().GetType();
Prefabs[t]=p;
if(LOG&&LOG_LEVEL<=1)Debug.Log("prefab "+o.name+" (type "+t+") registered");
}
task1=Task.Factory.StartNew(BG1,new object[]{LOG,LOG_LEVEL,savePath,},TaskCreationOptions.LongRunning);
static void BG1(object state){Thread.CurrentThread.IsBackground=false;Thread.CurrentThread.Priority=System.Threading.ThreadPriority.BelowNormal;
try{
if(state is object[]parameters&&parameters[0]is bool LOG&&parameters[1]is int LOG_LEVEL&&parameters[2]is string savePath){
if(LOG&&LOG_LEVEL<=1)Debug.Log("inicializar trabalho em plano de fundo para gerenciar atores");
string idsFile=string.Format("{0}{1}",savePath,"actors.MessagePack");
using(FileStream file=new FileStream(idsFile,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)){
if(file.Length>0){
Count=MessagePackSerializer.Deserialize(typeof(Dictionary<Type,int>),file)as Dictionary<Type,int>;
}else{
Count=new Dictionary<Type,int>();
}
}
if(LOG&&LOG_LEVEL<=1){foreach(var c in Count){Debug.Log("type.."+c.Key+"..has.."+c.Value+"..actor(s) in world");}}
backgroundData1.Set();
while(!Stop){foregroundData1.WaitOne();if(Stop)goto _Stop;
using(FileStream file=new FileStream(idsFile,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)){
MessagePackSerializer.Serialize(typeof(Dictionary<Type,int>),file,Count);
}
backgroundData1.Set();
}_Stop:{
}
if(LOG&&LOG_LEVEL<=1)Debug.Log("finalizar trabalho em plano de fundo para gerenciar atores graciosamente");
}
}catch(Exception e){Debug.LogError(e?.Message+"\n"+e?.StackTrace+"\n"+e?.Source);}finally{
backgroundData1.Set();
}
}
backgroundData1.WaitOne();
}
void OnDestroy(){
#region exit save
backgroundData1.WaitOne();
backgroundData1.Reset();foregroundData1.Set();
backgroundData1.WaitOne();
#endregion
Stop=true;try{task1.Wait();}catch(Exception e){Debug.LogError(e?.Message+"\n"+e?.StackTrace+"\n"+e?.Source);}foregroundData1.Dispose();backgroundData1.Dispose();
}
#region ids
[NonSerialized]static readonly Dictionary<Type,LinkedList<SimActor>>SimActorPool=new Dictionary<Type,LinkedList<SimActor>>();
#region called from SimActor
public static LinkedListNode<SimActor>Disable(SimActor actor){
if(!SimActorPool.ContainsKey(actor.type))SimActorPool.Add(actor.type,new LinkedList<SimActor>());
actor.id=-1;
actor.collider.controller.enabled=false;
actor.collider           .enabled=false;
return actor.Disabled=SimActorPool[actor.type].AddLast(actor);}
#endregion
#region needs thread safety with backgroundData1.WaitOne() to be called
static SimActor Create(Type type){//  called for placing an actor in the scene
_repeat:{}
if(SimActorPool.ContainsKey(type)&&SimActorPool[type].Count>0){//  Get
var actor=SimActorPool[type].First.Value;SimActorPool[type].RemoveFirst();actor.Disabled=null;
           
GetNextId(actor);
                
return actor;
}else{//  Instantiate
Instantiate(Prefabs[type]);
goto _repeat;
}
}
[NonSerialized]static Dictionary<Type,int>Count;
static(int id,string loadFile)GetNextId(SimActor actor){
            
//...to do: load data
int id=0;if(!Count.ContainsKey(actor.type)){Count.Add(actor.type,1);}else{id=Count[actor.type]++;}
actor.id=id;
actor.collider.controller.enabled=true;
actor.collider           .enabled=true;

return(id,"");}
#endregion
#endregion
[SerializeField]protected Vector3 DEBUG_CREATE_SIM_ACTOR_ROTATION;
[SerializeField]protected Vector3 DEBUG_CREATE_SIM_ACTOR_POSITION;
[SerializeField]protected bool    DEBUG_CREATE_SIM_ACTOR=false;
void Update(){
if(backgroundData1.WaitOne(0)){

if(DEBUG_CREATE_SIM_ACTOR){
   DEBUG_CREATE_SIM_ACTOR=false;

//...
Create(typeof(SimActor));

backgroundData1.Reset();foregroundData1.Set();
}

}
}*/


//[NonSerialized]public static Actors staticScript;
//[NonSerialized]public static readonly Dictionary<Type,GameObject>Prefabs=new Dictionary<Type,GameObject>();
//#region ids
//bool Stop{
//get{bool tmp;lock(Stop_Syn){tmp=Stop_v;      }return tmp;}
//set{         lock(Stop_Syn){    Stop_v=value;}if(value){foregroundData.Set();}}
//}[NonSerialized]readonly object Stop_Syn=new object();[NonSerialized]bool Stop_v=false;[NonSerialized]static readonly AutoResetEvent foregroundData=new AutoResetEvent(true);[NonSerialized]static readonly ManualResetEvent backgroundData=new ManualResetEvent(false);[NonSerialized]Task task;
//[NonSerialized]public static Dictionary<Type,int>Count;
//#endregion
//void Awake(){staticScript=this;
//var objects=Resources.LoadAll("AKCondinoO/Actors/Characters",typeof(GameObject));
//foreach(var o in objects){var p=o as GameObject;var t=p.GetComponent<SimActor>().GetType();
//Prefabs[t]=p;
//if(LOG&&LOG_LEVEL<=1)Debug.Log("prefab "+o.name+" (type "+t+") registered");
//}
//task=Task.Factory.StartNew(BG,new object[]{LOG,LOG_LEVEL,savePath,},TaskCreationOptions.LongRunning);
//void BG(object state){Thread.CurrentThread.IsBackground=false;Thread.CurrentThread.Priority=System.Threading.ThreadPriority.BelowNormal;
//try{
//if(state is object[]parameters&&parameters[0]is bool LOG&&parameters[1]is int LOG_LEVEL&&parameters[2]is string savePath){
//if(LOG&&LOG_LEVEL<=1)Debug.Log("inicializar trabalho em plano de fundo para gerenciar atores");
//foregroundData.WaitOne();
//string idsFile=string.Format("{0}{1}",savePath,"actors.MessagePack");
//using(FileStream file=new FileStream(idsFile,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)){
//if(file.Length>0){
//Count=MessagePackSerializer.Deserialize(typeof(Dictionary<Type,int>),file)as Dictionary<Type,int>;
//}else{
//Count=new Dictionary<Type,int>();
//}
//}
//if(LOG&&LOG_LEVEL<=1){foreach(var c in Count){Debug.Log("type.."+c.Key+"..has.."+c.Value+"..actor(s) in world");}}
//backgroundData.Set();
//while(!Stop){foregroundData.WaitOne();if(Stop)goto _Stop;
//using(FileStream file=new FileStream(idsFile,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)){
//MessagePackSerializer.Serialize(typeof(Dictionary<Type,int>),file,Count);
//}
//backgroundData.Set();
//}_Stop:{
//}
//if(LOG&&LOG_LEVEL<=1)Debug.Log("finalizar trabalho em plano de fundo para gerenciar atores graciosamente");
//}
//}catch(Exception e){Debug.LogError(e?.Message+"\n"+e?.StackTrace+"\n"+e?.Source);}finally{
//backgroundData.Set();
//}
//}
//backgroundData.WaitOne();
            
////...

//}
//void OnDestroy(){
//backgroundData.WaitOne();
//backgroundData.Reset();foregroundData.Set();
//backgroundData.WaitOne();
//Stop=true;try{task.Wait();}catch(Exception e){Debug.LogError(e?.Message+"\n"+e?.StackTrace+"\n"+e?.Source);}foregroundData.Dispose();backgroundData.Dispose();
//}
//#region called from SimActor
//[NonSerialized]public static readonly Dictionary<Type,LinkedList<SimActor>>SimActorPool=new Dictionary<Type,LinkedList<SimActor>>();
//public static LinkedListNode<SimActor>Disable(SimActor actor){
//if(!SimActorPool.ContainsKey(actor.type))SimActorPool.Add(actor.type,new LinkedList<SimActor>());actor.id=-1;return actor.Disabled=SimActorPool[actor.type].AddLast(actor);}
//public static(int id,string loadFile)GetNextId(SimActor actor){
            
////...to do: load data
//backgroundData.WaitOne();
//int id=0;if(!Count.ContainsKey(actor.type)){Count.Add(actor.type,1);}else{id=Count[actor.type]++;}actor.id=id;

//return(id,"");}
//#endregion
//public static SimActor Create(Type type){//  called for placing an actor in the scene
//_repeat:{}
//if(SimActorPool.ContainsKey(type)&&SimActorPool[type].Count>0){//  Get
//var actor=SimActorPool[type].First.Value;SimActorPool[type].RemoveFirst();actor.IsOutOfSight=false;return actor;
//}else{//  Instantiate
//Instantiate(Prefabs[type]);
//goto _repeat;
//}
//}
//[SerializeField]protected Vector3 DEBUG_CREATE_SIM_ACTOR_ROTATION;
//[SerializeField]protected Vector3 DEBUG_CREATE_SIM_ACTOR_POSITION;
//[SerializeField]protected bool    DEBUG_CREATE_SIM_ACTOR=false;
//void Update(){
//if(backgroundData.WaitOne(0)){

//if(DEBUG_CREATE_SIM_ACTOR){
//   DEBUG_CREATE_SIM_ACTOR=false;

////...
//Create(typeof(SimActor));

//backgroundData.Reset();foregroundData.Set();
//}

//}
//}

//...

/*
void Update(){

//...
if(DEBUG_CREATE_SIM_ACTOR){
   DEBUG_CREATE_SIM_ACTOR=false;
backgroundData.Reset();foregroundData.Set();

//...
var actor=GetOrInstantiate(typeof(SimActor));int id=0;if(!Count.ContainsKey(actor.type)){Count.Add(actor.type,1);}else{id=Count[actor.type]++;}actor.id=id;actor.IsOutOfSight=false;

}

}*/

//...

//[NonSerialized]public static Actors staticScript;
        
////...
//[NonSerialized]public static readonly Dictionary<Type,LinkedList<SimActor>>SimActorPool=new Dictionary<Type,LinkedList<SimActor>>();
//void Awake(){staticScript=this;

////...

//}
        
//[NonSerialized]public static string idsFile;[NonSerialized]public static readonly string idsFileName="ids.MessagePack";
/*[NonSerialized]public static Actors staticScript;
void Awake(){staticScript=this;
            
//...
idsFile=string.Format("{0}{1}",savePath,idsFileName);
using(FileStream file=new FileStream(idsFile,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)){
if(file.Length>0){
Count=MessagePackSerializer.Deserialize(typeof(Dictionary<Type,int>),file)as Dictionary<Type,int>;
}else{
Count=new Dictionary<Type,int>();
}
//...

}

}
void OnDestroy(){

//...
using(FileStream file=new FileStream(idsFile,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)){
MessagePackSerializer.Serialize(typeof(Dictionary<Type,int>),file,Count);
}

}
[NonSerialized]public static Dictionary<Type,int>Count;[NonSerialized]static Dictionary<Type,int>added=new Dictionary<Type,int>();
public static(int id,Type type)Add(SimActor actor){

//...
int id=0;Type type=actor.GetType();if(!Count.ContainsKey(type)){Count.Add(type,1);}else{

//...
//id=Count[type]++;

}
//ids[id]=actor;

return(id,type);}*/
/*[NonSerialized]public static Actors staticScript;[NonSerialized]public static readonly Dictionary<(Type type,int id),SimActor>Get=new Dictionary<(Type,int),SimActor>();[NonSerialized]public static readonly Dictionary<Type,int>Count=new Dictionary<Type,int>();

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
[NonSerialized]public static readonly List<SimActor>Enabled=new List<SimActor>();[NonSerialized]public static readonly List<SimActor>Disabled=new List<SimActor>();*/
}
}