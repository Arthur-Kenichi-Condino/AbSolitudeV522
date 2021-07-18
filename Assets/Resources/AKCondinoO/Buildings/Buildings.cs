using MessagePack;
using MLAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using static AKCondinoO.Voxels.TerrainChunk;using static AKCondinoO.Voxels.World;
namespace AKCondinoO.Buildings{public class Buildings:MonoBehaviour{public bool LOG=true;public int LOG_LEVEL=1;
static bool Stop{
get{bool tmp;lock(Stop_Syn){tmp=Stop_v;      }return tmp;}
set{         lock(Stop_Syn){    Stop_v=value;}if(value){foregroundData1.Set();foregroundData2.Set();}}
}[NonSerialized]static readonly object Stop_Syn=new object();[NonSerialized]static bool Stop_v=false;
[NonSerialized]static readonly AutoResetEvent foregroundData1=new AutoResetEvent(false);[NonSerialized]static readonly ManualResetEvent backgroundData1=new ManualResetEvent(true);[NonSerialized]static Task task1=null;
[NonSerialized]static readonly AutoResetEvent foregroundData2=new AutoResetEvent(false);[NonSerialized]static readonly ManualResetEvent backgroundData2=new ManualResetEvent(true);[NonSerialized]static Task task2=null;
[NonSerialized]public static string buildingsPath;[NonSerialized]public static string buildingsFolder;
[NonSerialized]public static string unplacedsPath;[NonSerialized]public static string unplacedsFolder;
[NonSerialized]public static readonly List<object>load_Syn_All=new List<object>();
[NonSerialized]static readonly Dictionary<Type,GameObject>Prefabs=new Dictionary<Type,GameObject>();[NonSerialized]public static readonly Dictionary<Type,LinkedList<SimObject>>SimObjectPool=new Dictionary<Type,LinkedList<SimObject>>();[NonSerialized]public static readonly Dictionary<Type,List<SimObject>>Loaded=new Dictionary<Type,List<SimObject>>();[NonSerialized]static readonly Dictionary<Type,List<(Type type,int id,int cnkIdx)>>Loading=new Dictionary<Type,List<(Type type,int id,int cnkIdx)>>();
[NonSerialized]static Dictionary<Type,int>Count;[NonSerialized]public static Dictionary<Type,List<int>>Unplaced;
[NonSerialized]public static readonly List<SimObject>Enabled=new List<SimObject>();[NonSerialized]public static readonly List<SimObject>Disabled=new List<SimObject>();
[NonSerialized]public static Buildings staticScript;
void Awake(){staticScript=this;
buildingsFolder=string.Format("{0}{1}",savePath,"buildings");
Directory.CreateDirectory(buildingsPath=string.Format("{0}/",buildingsFolder));
unplacedsFolder=string.Format("{0}{1}",buildingsPath,"unplaced");
Directory.CreateDirectory(unplacedsPath=string.Format("{0}/",unplacedsFolder));
var objects=Resources.LoadAll("AKCondinoO/Buildings/Structures",typeof(GameObject));
foreach(var o in objects){var p=o as GameObject;var sO=p.GetComponent<SimObject>();if(sO==null)continue;var t=sO.GetType();
Prefabs[t]=p;SimObjectPool[t]=new LinkedList<SimObject>();Loaded[t]=new List<SimObject>();Loading[t]=new List<(Type type,int id,int cnkIdx)>();
if(LOG&&LOG_LEVEL<=1)Debug.Log("prefab "+o.name+" (type "+t+") registered");
}

//...            

backgroundData1.Reset();foregroundData1.Set();
task1=Task.Factory.StartNew(BG1,new object[]{LOG,LOG_LEVEL,buildingsFolder,},TaskCreationOptions.LongRunning);
task2=Task.Factory.StartNew(BG2,new object[]{LOG,LOG_LEVEL,buildingsFolder,},TaskCreationOptions.LongRunning);
static void BG1(object state){Thread.CurrentThread.IsBackground=false;Thread.CurrentThread.Priority=System.Threading.ThreadPriority.BelowNormal;
try{
if(state is object[]parameters&&parameters[0]is bool LOG&&parameters[1]is int LOG_LEVEL&&parameters[2]is string buildingsFolder){
if(LOG&&LOG_LEVEL<=1)Debug.Log("inicializar trabalho em plano de fundo 1 para gerenciar construções");
var watch=new System.Diagnostics.Stopwatch();
foregroundData1.WaitOne();
if(LOG&&LOG_LEVEL<=1){Debug.Log("começar carregamento de dados de ids");watch.Restart();}
string idsFile=string.Format("{0}/{1}",buildingsFolder,"buildings.MessagePack");
using(FileStream file=new FileStream(idsFile,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)){
if(file.Length>0){
Count=MessagePackSerializer.Deserialize(typeof(Dictionary<Type,int>),file)as Dictionary<Type,int>;
}else{
Count=new Dictionary<Type,int>();
}
if(LOG&&LOG_LEVEL<=1){foreach(var c in Count){Debug.Log("type.."+c.Key+"..has.."+c.Value+"..sim object(s) in world");}}
}
string unplacedIdsFile=string.Format("{0}/{1}",buildingsFolder,"unplaced.MessagePack");
using(FileStream file=new FileStream(unplacedIdsFile,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)){
if(file.Length>0){
Unplaced=MessagePackSerializer.Deserialize(typeof(Dictionary<Type,List<int>>),file)as Dictionary<Type,List<int>>;
}else{
Unplaced=new Dictionary<Type,List<int>>();
}
if(LOG&&LOG_LEVEL<=1){foreach(var d in Unplaced){Debug.Log("type.."+d.Key+"..has.."+d.Value.Count+"..unplaced sim object(s) in world");}}
}
if(LOG&&LOG_LEVEL<=1)Debug.Log("terminado carregamento de dados de ids..levou:"+watch.ElapsedMilliseconds+"ms");
backgroundData1.Set();
while(!Stop){foregroundData1.WaitOne();if(Stop)goto _Stop;
if(LOG&&LOG_LEVEL<=1){Debug.Log("começar salvamento de dados de ids");watch.Restart();}
using(FileStream file=new FileStream(idsFile,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)){
MessagePackSerializer.Serialize(file,Count);
}
using(FileStream file=new FileStream(unplacedIdsFile,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)){
MessagePackSerializer.Serialize(file,Unplaced);
}
if(LOG&&LOG_LEVEL<=1)Debug.Log("terminado salvamento de dados de ids..levou:"+watch.ElapsedMilliseconds+"ms");
backgroundData1.Set();
}_Stop:{
}
if(LOG&&LOG_LEVEL<=1)Debug.Log("finalizar trabalho em plano de fundo 1 para gerenciar construções graciosamente");
}
}catch(Exception e){Debug.LogError(e?.Message+"\n"+e?.StackTrace+"\n"+e?.Source);}finally{
while(!Stop){if(!backgroundData1.WaitOne(0))backgroundData1.Set();Thread.Sleep(1);}
}
}
static void BG2(object state){Thread.CurrentThread.IsBackground=false;Thread.CurrentThread.Priority=System.Threading.ThreadPriority.BelowNormal;
try{
if(state is object[]parameters&&parameters[0]is bool LOG&&parameters[1]is int LOG_LEVEL&&parameters[2]is string buildingsFolder){
if(LOG&&LOG_LEVEL<=1)Debug.Log("inicializar trabalho em plano de fundo 2 para gerenciar construções");
var watch=new System.Diagnostics.Stopwatch();
while(!Stop){foregroundData2.WaitOne();if(Stop)goto _Stop;
if(LOG&&LOG_LEVEL<=1){Debug.Log("começar carregamento de dados de construções");watch.Restart();}
foreach(var syn in load_Syn_All)Monitor.Enter(syn);try{
#region safe
for(Vector2Int iCoord=new Vector2Int(),cCoord1=new Vector2Int();iCoord.y<=instantiationDistance.y;iCoord.y++){for(cCoord1.y=-iCoord.y+aCoord.y;cCoord1.y<=iCoord.y+aCoord.y;cCoord1.y+=iCoord.y*2){
for(           iCoord.x=0                                      ;iCoord.x<=instantiationDistance.x;iCoord.x++){for(cCoord1.x=-iCoord.x+aCoord.x;cCoord1.x<=iCoord.x+aCoord.x;cCoord1.x+=iCoord.x*2){
if(Math.Abs(cCoord1.x)>=MaxcCoordx||
   Math.Abs(cCoord1.y)>=MaxcCoordy){
if(LOG&&LOG_LEVEL<=1)Debug.Log("do not try to load out of world data at coord:.."+cCoord1);
goto _skip;
}
if(LOG&&LOG_LEVEL<=1)Debug.Log("try to load sim objects at:.."+cCoord1);
int cnkIdx1=GetcnkIdx(cCoord1.x,cCoord1.y);
var transformFolder=string.Format("{0}/{1}",buildingsFolder,cnkIdx1);var transformPath=string.Format("{0}/",transformFolder);
if(Directory.Exists(transformPath))foreach(var transformFile in Directory.GetFiles(transformPath)){
if(LOG&&LOG_LEVEL<=1)Debug.Log("loading sim object at:.."+cCoord1+"..transformFile:.."+transformFile);
var transformFileName=Path.GetFileName(transformFile);
if(LOG&&LOG_LEVEL<=1)Debug.Log("sim object at:.."+cCoord1+"..transformFileName:"+transformFileName);
string typeAndId=transformFileName.Split('(',')')[1];string[]typeAndIdSplit=typeAndId.Split(',');string typeString=typeAndIdSplit[0];string idString=typeAndIdSplit[1];
if(LOG&&LOG_LEVEL<=1)Debug.Log("sim object at:.."+cCoord1+"..type:"+typeString+"..id:"+idString);
Type type=Type.GetType(typeString);int id=int.Parse(idString);
Loading[type].Add((type,id,cnkIdx1));
}
_skip:{}
if(iCoord.x==0){break;}}}
if(iCoord.y==0){break;}}}
#endregion safe
}catch{throw;}finally{foreach(var syn in load_Syn_All)Monitor.Exit(syn);}
aCoord_Pre=aCoord;
firstLoop=false;
if(LOG&&LOG_LEVEL<=1)Debug.Log("terminado carregamento de dados de construções..levou:"+watch.ElapsedMilliseconds+"ms");
backgroundData2.Set();
}_Stop:{
}
if(LOG&&LOG_LEVEL<=1)Debug.Log("finalizar trabalho em plano de fundo 2 para gerenciar construções graciosamente");
}
}catch(Exception e){Debug.LogError(e?.Message+"\n"+e?.StackTrace+"\n"+e?.Source);}finally{
while(!Stop){if(!backgroundData2.WaitOne(0))backgroundData2.Set();Thread.Sleep(1);}
}
}
}
[NonSerialized]static bool disposed;
void OnDestroy(){
#region exit save
backgroundData1.WaitOne();
backgroundData1.Reset();foregroundData1.Set();
backgroundData1.WaitOne();
#endregion
Stop=true;try{task1.Wait();}catch(Exception e){Debug.LogError(e?.Message+"\n"+e?.StackTrace+"\n"+e?.Source);}foregroundData1.Dispose();backgroundData1.Dispose();
          try{task2.Wait();}catch(Exception e){Debug.LogError(e?.Message+"\n"+e?.StackTrace+"\n"+e?.Source);}foregroundData2.Dispose();backgroundData2.Dispose();
disposed=true;
}
[NonSerialized]static bool firstLoop=true;
[NonSerialized]static Vector3    actPos;
[NonSerialized]static Vector2Int aCoord,aCoord_Pre;
[NonSerialized]static Vector2Int actRgn;
[SerializeField]protected float reloadInterval=1f;[NonSerialized]protected float reloadTimer=0f;
[SerializeField]protected Vector3   DEBUG_CREATE_SIM_OBJECT_ROTATION;
[SerializeField]protected Vector3   DEBUG_CREATE_SIM_OBJECT_POSITION;
[SerializeField]protected SimObject DEBUG_CREATE_SIM_OBJECT=null;
void Update(){
if(NetworkManager.Singleton.IsServer){
if(reloadTimer>0){reloadTimer-=Time.deltaTime;}
if(backgroundData2.WaitOne(0)){
if(backgroundData1.WaitOne(0)){
SimObject Create(Type type,Vector3 position,Vector3 rotation){
_getSimObject:{}
if(SimObjectPool[type].Count>0){//  get from pool
SimObject simObject=SimObjectPool[type].First.Value;SimObjectPool[type].RemoveFirst();simObject.DisabledNode=null;simObject.transform.rotation=Quaternion.Euler(rotation);simObject.transform.position=position;load_Syn_All.Add(simObject.load_Syn);simObject.network.Spawn();return simObject;
}else{
Instantiate(Prefabs[type]);
goto _getSimObject;
}
}
#region process loading values
foreach(var loading in Loading){var type=loading.Key;foreach(var loadTuple in loading.Value){
if(LOG&&LOG_LEVEL<=1)Debug.Log("loading:..type:"+type+"..id:"+loadTuple.id);
foreach(var simObjectLoaded in Loaded[type]){
if(simObjectLoaded.loadTuple.HasValue&&simObjectLoaded.loadTuple.Value.id==loadTuple.id){
if(LOG&&LOG_LEVEL<=1)Debug.Log("already loaded:..type:"+type+"..id:"+loadTuple.id,simObjectLoaded);
goto _next;
}
}
SimObject simObjectToLoad=Create(type,Vector3.zero,Vector3.zero);
simObjectToLoad.loadTuple=loadTuple;Loaded[type].Add(simObjectToLoad);
if(LOG&&LOG_LEVEL<=1)Debug.Log("simObject set to be loaded:..type:"+type+"..id:"+loadTuple.id,simObjectToLoad);
_next:{}
}loading.Value.Clear();
}
#endregion   
if(DEBUG_CREATE_SIM_OBJECT){
if(LOG&&LOG_LEVEL<=1)Debug.Log("DEBUG_CREATE_SIM_OBJECT of prefab:.."+DEBUG_CREATE_SIM_OBJECT);

//...

Type type=DEBUG_CREATE_SIM_OBJECT.GetComponent<SimObject>().GetType();
if(LOG&&LOG_LEVEL<=1)Debug.Log("DEBUG_CREATE_SIM_OBJECT of type:.."+type);
int id=0;if(!Count.ContainsKey(type)){Count.Add(type,1);}else{
                    
//...                            

if(Unplaced.ContainsKey(type)&&Unplaced[type].Count>0){var unplacedIds=Unplaced[type];
id=unplacedIds[unplacedIds.Count-1];unplacedIds.RemoveAt(unplacedIds.Count-1);
}else{
id=Count[type]++;
}                            
}
SimObject simObjectToLoad=Create(type,DEBUG_CREATE_SIM_OBJECT_POSITION,DEBUG_CREATE_SIM_OBJECT_ROTATION);
simObjectToLoad.loadTuple=(type,id,null);Loaded[type].Add(simObjectToLoad);
DEBUG_CREATE_SIM_OBJECT=null;
backgroundData1.Reset();foregroundData1.Set();
}
if(firstLoop||reloadTimer<=0||actPos!=Camera.main.transform.position){if(LOG&&LOG_LEVEL<=-110){Debug.Log("actPos anterior:.."+actPos+"..;actPos novo:.."+Camera.main.transform.position);}
                              actPos=(Camera.main.transform.position);
if(firstLoop |reloadTimer<=0 |aCoord!=(aCoord=vecPosTocCoord(actPos))){if(LOG&&LOG_LEVEL<=1){Debug.Log("aCoord novo:.."+aCoord+"..;aCoord_Pre:.."+aCoord_Pre);}
                              actRgn=(cCoordTocnkRgn(aCoord));
reloadTimer=reloadInterval;
foreach(var l in SimObjectPool){var list=l.Value;for(var node=list.First;node!=null;node=node.Next){load_Syn_All.Remove(node.Value.load_Syn);}}
backgroundData2.Reset();foregroundData2.Set();
}
}
}
}
}
}
public static void OnSimObjectDestroyed(SimObject simObject){//  sim objects should not be destroyed if the game is running, but added to the pool, by design
if(!disposed){Debug.LogWarning("sim objects should not be destroyed if the game is running, but added to the pool, by design");
backgroundData2.WaitOne();
backgroundData1.WaitOne();}
load_Syn_All.Remove(simObject.load_Syn);
}
}
}