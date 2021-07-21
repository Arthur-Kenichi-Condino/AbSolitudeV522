using AKCondinoO.Networking;
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
namespace AKCondinoO.Actors{public class Actors:MonoBehaviour{public bool LOG=true;public int LOG_LEVEL=1;   
static bool Stop{
get{bool tmp;lock(Stop_Syn){tmp=Stop_v;      }return tmp;}
set{         lock(Stop_Syn){    Stop_v=value;}if(value){foregroundData1.Set();foregroundData2.Set();}}
}[NonSerialized]static readonly object Stop_Syn=new object();[NonSerialized]static bool Stop_v=false;
[NonSerialized]static readonly AutoResetEvent foregroundData1=new AutoResetEvent(false);[NonSerialized]static readonly ManualResetEvent backgroundData1=new ManualResetEvent(true);[NonSerialized]static Task task1=null;
[NonSerialized]static readonly AutoResetEvent foregroundData2=new AutoResetEvent(false);[NonSerialized]static readonly ManualResetEvent backgroundData2=new ManualResetEvent(true);[NonSerialized]static Task task2=null;
[NonSerialized]public static string actorsPath;[NonSerialized]public static string actorsFolder;
[NonSerialized]public static readonly List<object>load_Syn_All=new List<object>();
[NonSerialized]static readonly Dictionary<Type,GameObject>Prefabs=new Dictionary<Type,GameObject>();[NonSerialized]public static readonly Dictionary<Type,LinkedList<SimActor>>SimActorPool=new Dictionary<Type,LinkedList<SimActor>>();[NonSerialized]public static readonly Dictionary<Type,List<SimActor>>Loaded=new Dictionary<Type,List<SimActor>>();[NonSerialized]static readonly Dictionary<Type,List<(Type type,int id,int cnkIdx)>>Loading=new Dictionary<Type,List<(Type type,int id,int cnkIdx)>>();
[NonSerialized]static Dictionary<Type,int>Count;
[NonSerialized]public static readonly List<SimActor>Enabled=new List<SimActor>();[NonSerialized]public static readonly List<SimActor>Disabled=new List<SimActor>();public static List<SimActor>GetActors{get{return Enabled;}}[NonSerialized]public static readonly Dictionary<Type,List<SimActor>>Get=new Dictionary<Type,List<SimActor>>();
[NonSerialized]public static Actors staticScript;     
void Awake(){staticScript=this;
actorsFolder=string.Format("{0}{1}",savePath,"actors");
Directory.CreateDirectory(actorsPath=string.Format("{0}/",actorsFolder));
var objects=Resources.LoadAll("AKCondinoO/Actors/Characters",typeof(GameObject));
foreach(var o in objects){var p=o as GameObject;var sA=p.GetComponent<SimActor>();if(sA==null)continue;var t=sA.GetType();
Prefabs[t]=p;SimActorPool[t]=new LinkedList<SimActor>();Loaded[t]=new List<SimActor>();Loading[t]=new List<(Type type,int id,int cnkIdx)>();
if(LOG&&LOG_LEVEL<=1)Debug.Log("prefab "+o.name+" (type "+t+") registered");
}

//...

backgroundData1.Reset();foregroundData1.Set();
task1=Task.Factory.StartNew(BG1,new object[]{LOG,LOG_LEVEL,actorsFolder,},TaskCreationOptions.LongRunning);
task2=Task.Factory.StartNew(BG2,new object[]{LOG,LOG_LEVEL,actorsFolder,},TaskCreationOptions.LongRunning);
static void BG1(object state){Thread.CurrentThread.IsBackground=false;Thread.CurrentThread.Priority=System.Threading.ThreadPriority.BelowNormal;
try{
if(state is object[]parameters&&parameters[0]is bool LOG&&parameters[1]is int LOG_LEVEL&&parameters[2]is string actorsFolder){
if(LOG&&LOG_LEVEL<=1)Debug.Log("inicializar trabalho em plano de fundo 1 para gerenciar atores");
var watch=new System.Diagnostics.Stopwatch();
foregroundData1.WaitOne();
if(LOG&&LOG_LEVEL<=1){Debug.Log("começar carregamento de dados de ids");watch.Restart();}
string idsFile=string.Format("{0}/{1}",actorsFolder,"actors.MessagePack");
using(FileStream file=new FileStream(idsFile,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)){
if(file.Length>0){
Count=MessagePackSerializer.Deserialize(typeof(Dictionary<Type,int>),file)as Dictionary<Type,int>;
}else{
Count=new Dictionary<Type,int>();
}
if(LOG&&LOG_LEVEL<=1){foreach(var c in Count){Debug.Log("type.."+c.Key+"..has.."+c.Value+"..actor(s) in world");}}
}
if(LOG&&LOG_LEVEL<=1)Debug.Log("terminado carregamento de dados de ids..levou:"+watch.ElapsedMilliseconds+"ms");
backgroundData1.Set();
while(!Stop){foregroundData1.WaitOne();if(Stop)goto _Stop;
if(LOG&&LOG_LEVEL<=1){Debug.Log("começar salvamento de dados de ids");watch.Restart();}
using(FileStream file=new FileStream(idsFile,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)){
MessagePackSerializer.Serialize(file,Count);
}
if(LOG&&LOG_LEVEL<=1)Debug.Log("terminado salvamento de dados de ids..levou:"+watch.ElapsedMilliseconds+"ms");
backgroundData1.Set();
}_Stop:{
}
if(LOG&&LOG_LEVEL<=1)Debug.Log("finalizar trabalho em plano de fundo 1 para gerenciar atores graciosamente");
}
}catch(Exception e){Debug.LogError(e?.Message+"\n"+e?.StackTrace+"\n"+e?.Source);}finally{
while(!Stop){if(!backgroundData1.WaitOne(0))backgroundData1.Set();Thread.Sleep(1);}
}
}
static void BG2(object state){Thread.CurrentThread.IsBackground=false;Thread.CurrentThread.Priority=System.Threading.ThreadPriority.BelowNormal;
try{
if(state is object[]parameters&&parameters[0]is bool LOG&&parameters[1]is int LOG_LEVEL&&parameters[2]is string actorsFolder){
if(LOG&&LOG_LEVEL<=1)Debug.Log("inicializar trabalho em plano de fundo 2 para gerenciar atores");
var watch=new System.Diagnostics.Stopwatch();
while(!Stop){foregroundData2.WaitOne();if(Stop)goto _Stop;
if(LOG&&LOG_LEVEL<=1){Debug.Log("começar carregamento de dados de atores");watch.Restart();}
foreach(var syn in load_Syn_All)Monitor.Enter(syn);try{
#region safe
for(Vector2Int iCoord=new Vector2Int(),cCoord1=new Vector2Int();iCoord.y<=instantiationDistance.y;iCoord.y++){for(cCoord1.y=-iCoord.y+aCoord.y;cCoord1.y<=iCoord.y+aCoord.y;cCoord1.y+=iCoord.y*2){
for(           iCoord.x=0                                      ;iCoord.x<=instantiationDistance.x;iCoord.x++){for(cCoord1.x=-iCoord.x+aCoord.x;cCoord1.x<=iCoord.x+aCoord.x;cCoord1.x+=iCoord.x*2){
if(Math.Abs(cCoord1.x)>=MaxcCoordx||
   Math.Abs(cCoord1.y)>=MaxcCoordy){
if(LOG&&LOG_LEVEL<=1)Debug.Log("do not try to load out of world data at coord:.."+cCoord1);
goto _skip;
}
if(LOG&&LOG_LEVEL<=1)Debug.Log("try to load sim actors at:.."+cCoord1);
int cnkIdx1=GetcnkIdx(cCoord1.x,cCoord1.y);
var transformFolder=string.Format("{0}/{1}",actorsFolder,cnkIdx1);var transformPath=string.Format("{0}/",transformFolder);
if(Directory.Exists(transformPath))foreach(var transformFile in Directory.GetFiles(transformPath)){
if(LOG&&LOG_LEVEL<=1)Debug.Log("loading sim actor at:.."+cCoord1+"..transformFile:.."+transformFile);
var transformFileName=Path.GetFileName(transformFile);
if(LOG&&LOG_LEVEL<=1)Debug.Log("actor at:.."+cCoord1+"..transformFileName:"+transformFileName);
string typeAndId=transformFileName.Split('(',')')[1];string[]typeAndIdSplit=typeAndId.Split(',');string typeString=typeAndIdSplit[0];string idString=typeAndIdSplit[1];
if(LOG&&LOG_LEVEL<=1)Debug.Log("actor at:.."+cCoord1+"..type:"+typeString+"..id:"+idString);
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
if(LOG&&LOG_LEVEL<=1)Debug.Log("terminado carregamento de dados de atores..levou:"+watch.ElapsedMilliseconds+"ms");
backgroundData2.Set();
}_Stop:{
}
if(LOG&&LOG_LEVEL<=1)Debug.Log("finalizar trabalho em plano de fundo 2 para gerenciar atores graciosamente");
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
[NonSerialized]public static readonly Dictionary<UNetDefaultPrefab,(Vector2Int cCoord,Vector2Int cCoord_Pre)?>playersChangedCoord=new Dictionary<UNetDefaultPrefab,(Vector2Int,Vector2Int)?>(maxPlayers);
[SerializeField]protected float reloadInterval=1f;[NonSerialized]protected float reloadTimer=0f;
[SerializeField]protected Vector3  DEBUG_CREATE_SIM_ACTOR_ROTATION;
[SerializeField]protected Vector3  DEBUG_CREATE_SIM_ACTOR_POSITION;
[SerializeField]protected SimActor DEBUG_CREATE_SIM_ACTOR=null;
void Update(){
if(NetworkManager.Singleton.IsServer){
if(reloadTimer>0){reloadTimer-=Time.deltaTime;}
if(backgroundData2.WaitOne(0)){
if(backgroundData1.WaitOne(0)){
SimActor Create(Type type,Vector3 position,Vector3 rotation){
_getActor:{}
if(SimActorPool[type].Count>0){//  get from pool
SimActor actor=SimActorPool[type].First.Value;SimActorPool[type].RemoveFirst();actor.DisabledNode=null;actor.transform.rotation=Quaternion.Euler(rotation);actor.transform.position=position;load_Syn_All.Add(actor.load_Syn);actor.network.Spawn();return actor;
}else{
Instantiate(Prefabs[type]);
goto _getActor;
}
}
#region process loading values
foreach(var loading in Loading){var type=loading.Key;foreach(var loadTuple in loading.Value){
if(LOG&&LOG_LEVEL<=1)Debug.Log("loading:..type:"+type+"..id:"+loadTuple.id);
foreach(var actorLoaded in Loaded[type]){
if(actorLoaded.loadTuple.HasValue&&actorLoaded.loadTuple.Value.id==loadTuple.id){
if(LOG&&LOG_LEVEL<=1)Debug.Log("already loaded:..type:"+type+"..id:"+loadTuple.id,actorLoaded);
goto _next;
}
}
SimActor actorToLoad=Create(type,Vector3.zero,Vector3.zero);
actorToLoad.loadTuple=loadTuple;Loaded[type].Add(actorToLoad);
if(LOG&&LOG_LEVEL<=1)Debug.Log("actor set to be loaded:..type:"+type+"..id:"+loadTuple.id,actorToLoad);
_next:{}
}loading.Value.Clear();
}
#endregion   
if(DEBUG_CREATE_SIM_ACTOR){
if(LOG&&LOG_LEVEL<=1)Debug.Log("DEBUG_CREATE_SIM_ACTOR of prefab:.."+DEBUG_CREATE_SIM_ACTOR);

//...

Type type=DEBUG_CREATE_SIM_ACTOR.GetComponent<SimActor>().GetType();
if(LOG&&LOG_LEVEL<=1)Debug.Log("DEBUG_CREATE_SIM_ACTOR of type:.."+type);
int id=0;if(!Count.ContainsKey(type)){Count.Add(type,1);}else{id=Count[type]++;}
SimActor actorToLoad=Create(type,DEBUG_CREATE_SIM_ACTOR_POSITION,DEBUG_CREATE_SIM_ACTOR_ROTATION);
actorToLoad.loadTuple=(type,id,null);Loaded[type].Add(actorToLoad);
DEBUG_CREATE_SIM_ACTOR=null;
backgroundData1.Reset();foregroundData1.Set();
}
if(firstLoop||reloadTimer<=0||actPos!=Camera.main.transform.position){if(LOG&&LOG_LEVEL<=-110){Debug.Log("actPos anterior:.."+actPos+"..;actPos novo:.."+Camera.main.transform.position);}
                              actPos=(Camera.main.transform.position);
if(firstLoop |reloadTimer<=0 |aCoord!=(aCoord=vecPosTocCoord(actPos))){if(LOG&&LOG_LEVEL<=1){Debug.Log("aCoord novo:.."+aCoord+"..;aCoord_Pre:.."+aCoord_Pre);}
                              actRgn=(cCoordTocnkRgn(aCoord));
reloadTimer=reloadInterval;
foreach(var l in SimActorPool){var list=l.Value;for(var node=list.First;node!=null;node=node.Next){load_Syn_All.Remove(node.Value.load_Syn);}}
backgroundData2.Reset();foregroundData2.Set();
}
}
}
}
}
}
public static void OnActorDestroyed(SimActor actor){//  actors should not be destroyed if the game is running, but added to the pool, by design
if(!disposed){Debug.LogWarning("actors should not be destroyed if the game is running, but added to the pool, by design");
backgroundData2.WaitOne();
backgroundData1.WaitOne();}
load_Syn_All.Remove(actor.load_Syn);
}
}
}