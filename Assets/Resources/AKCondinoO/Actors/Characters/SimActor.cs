using AKCondinoO.Voxels;
using MLAPI;
using MLAPI.NetworkVariable;
using SebastianLague;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using static AKCondinoO.Util;using static AKCondinoO.Voxels.TerrainChunk;using static AKCondinoO.Voxels.World;using static AKCondinoO.Actors.Actors;
namespace AKCondinoO.Actors{public class SimActor:NetworkBehaviour{public bool LOG=true;public int LOG_LEVEL=1;public int GIZMOS_ENABLED=1;
[NonSerialized]public LinkedListNode<SimActor>DisabledNode=null;
bool Stop{
get{bool tmp;lock(Stop_Syn){tmp=Stop_v;      }return tmp;}
set{         lock(Stop_Syn){    Stop_v=value;}if(value){foregroundData.Set();}}
}[NonSerialized]readonly object Stop_Syn=new object();[NonSerialized]bool Stop_v=false;[NonSerialized]readonly AutoResetEvent foregroundData=new AutoResetEvent(false);[NonSerialized]readonly ManualResetEvent backgroundData=new ManualResetEvent(true);
[NonSerialized]public readonly object load_Syn=new object();
[Serializable]public class SimActorSaveTransform{
public string type{get;set;}public int id{get;set;}
public SerializableQuaternion rotation{get;set;}
public SerializableVector3    position{get;set;}
}[NonSerialized]readonly SimActorSaveTransform saveTransform=new SimActorSaveTransform();[NonSerialized]string transformFolder;[NonSerialized]string transformFile;
[Serializable]public class SimActorSaveStateData{
public string type{get;set;}public int id{get;set;}
}[NonSerialized]readonly SimActorSaveStateData saveStateData=new SimActorSaveStateData();[NonSerialized]string stateDataFolder;[NonSerialized]string stateDataFile;
public Type type{get;protected set;}public int id{get;protected set;}
[NonSerialized]bool disabling;
[NonSerialized]bool releaseId;
[NonSerialized]public(Type type,int id,int?cnkIdx)?loadTuple=null;[NonSerialized]bool loaded;[NonSerialized]bool enable;[NonSerialized]bool enabling;[NonSerialized]bool acting;
[NonSerialized]public NetworkObject network;[NonSerialized]bool networkHidden;[NonSerialized]bool atServer;
[NonSerialized]public readonly NetworkVariableVector3 networkPosition=new NetworkVariableVector3(new NetworkVariableSettings{WritePermission=NetworkVariablePermission.ServerOnly,ReadPermission=NetworkVariablePermission.Everyone,});
[NonSerialized]public new CharacterControllerPhys collider;
[NonSerialized]public NavMeshAgent navMeshAgent;[NonSerialized]public bool useAI=true;
protected virtual void Awake(){if(transform.parent!=Actors.staticScript.transform){transform.parent=Actors.staticScript.transform;}
type=GetType();id=-1;
saveTransform.type=type.FullName;
saveStateData.type=type.FullName;
network=GetComponent<NetworkObject>();
network.CheckObjectVisibility=((clientId)=>{return!networkHidden;});
collider=GetComponent<CharacterControllerPhys>();
navMeshAgent=GetComponent<NavMeshAgent>();
if(LOG&&LOG_LEVEL<=1)Debug.Log("I got instantiated and I am of type.."+type+"..now, add myself to actors pool",this);
acting=false;
collider.controller.enabled=false;
collider           .enabled=false;
navMeshAgent       .enabled=false;
Actors.Disabled.Add(this);Actors.Enabled.Remove(this);IsOutOfSight=true;if(LOG&&LOG_LEVEL<=1){Debug.Log("Actors.Enabled.Count:"+Actors.Enabled.Count+"..Actors.Disabled.Count:"+Actors.Disabled.Count,this);}
DisabledNode=SimActorPool[type].AddLast(this);
pos=pos_Pre=transform.position;cCoord=cCoord_Pre=vecPosTocCoord(pos);cnkIdx=GetcnkIdx(cCoord.x,cCoord.y);
if(LOG&&LOG_LEVEL<=1)Debug.Log("I am awaking at.."+pos+"..and my cCoord is.."+cCoord+"..,so my cnkIdx is.."+cnkIdx,this);

//...

}
public class SimActorTask{
[NonSerialized]static readonly ConcurrentQueue<SimActor>queued=new ConcurrentQueue<SimActor>();[NonSerialized]static readonly AutoResetEvent enqueued=new AutoResetEvent(false);
public static void StartNew(SimActor state){queued.Enqueue(state);enqueued.Set();}

//...

#region current terrain processing data
SimActor current{get;set;}AutoResetEvent foregroundData{get;set;}ManualResetEvent backgroundData{get;set;}
object load_Syn{get;set;}
SimActorSaveTransform saveTransform{get;set;}string transformFile{get{return current.transformFile;}set{current.transformFile=value;}}string transformFolder{get{return current.transformFolder;}set{current.transformFolder=value;}}

//...

Type type{get{return current.type;}set{current.type=value;}}int id{get{return current.id;}set{current.id=value;}}

//...

bool releaseId{get{return current.releaseId;}set{current.releaseId=value;}}
(Type type,int id,int?cnkIdx)?loadTuple{get{return current.loadTuple;}set{current.loadTuple=value;}}bool loaded{get{return current.loaded;}set{current.loaded=value;}}bool enable{get{return current.enable;}set{current.enable=value;}}

//...

void RenewData(SimActor next){
current=next;
foregroundData=next.foregroundData;backgroundData=next.backgroundData;
load_Syn=next.load_Syn;

//...
                
saveTransform=next.saveTransform;
}
void ReleaseData(){
foregroundData=null;backgroundData=null;
load_Syn=null;

//...

saveTransform=null;
current=null;
}
#endregion current terrain processing data

//...

public static bool Stop{
get{bool tmp;lock(Stop_Syn){tmp=Stop_v;      }return tmp;}
set{         lock(Stop_Syn){    Stop_v=value;}if(value){enqueued.Set();}}
}[NonSerialized]static readonly object Stop_Syn=new object();[NonSerialized]static bool Stop_v=false;[NonSerialized]readonly Task task;public void Wait(){try{task.Wait();}catch(Exception e){Debug.LogError(e?.Message+"\n"+e?.StackTrace+"\n"+e?.Source);}}
public SimActorTask(bool LOG,int LOG_LEVEL){

//...

task=Task.Factory.StartNew(BG,new object[]{LOG,LOG_LEVEL,savePath,actorsFolder,},TaskCreationOptions.LongRunning);
void BG(object state){Thread.CurrentThread.IsBackground=false;Thread.CurrentThread.Priority=System.Threading.ThreadPriority.BelowNormal;
try{
if(state is object[]parameters&&parameters[0]is bool LOG&&parameters[1]is int LOG_LEVEL&&parameters[2]is string savePath&&parameters[3]is string actorsFolder){
if(LOG&&LOG_LEVEL<=1)Debug.Log("inicializar trabalho em plano de fundo para ator");
var watch=new System.Diagnostics.Stopwatch();
while(!Stop){enqueued.WaitOne();if(Stop){enqueued.Set();goto _Stop;}if(queued.TryDequeue(out SimActor dequeued)){RenewData(dequeued);}else{continue;};if(queued.Count>0){enqueued.Set();}foregroundData.WaitOne();
if(LOG&&LOG_LEVEL<=1){Debug.Log("começar novo processamento de dados de arquivo para este ator:"+id,current);watch.Restart();}
lock(load_Syn){
#region safe
if(id!=-1){
if(!string.IsNullOrEmpty(transformFile)&&File.Exists(transformFile)){
if(LOG&&LOG_LEVEL<=1){Debug.Log("não gerar duplicata: já há um arquivo carregado registrado para ator:"+id+"..deletar antes de abrir um novo");}
File.Delete(transformFile);
}
Vector2Int cCoord1=vecPosTocCoord(saveTransform.position);int cnkIdx1=GetcnkIdx(cCoord1.x,cCoord1.y);
transformFolder=string.Format("{0}/{1}",actorsFolder,cnkIdx1);transformFile=string.Format("{0}/{1}",transformFolder,string.Format("({0},{1}).StreamWriter",saveTransform.type,saveTransform.id));
Directory.CreateDirectory(string.Format("{0}/",transformFolder));
if(LOG&&LOG_LEVEL<=1){Debug.Log("my id:"+id+"..my transform save file:.."+transformFile,current);}
using(FileStream file=new FileStream(transformFile,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)){
using(StreamWriter writer=new StreamWriter(file)){
writer.WriteLine(saveTransform.id);
writer.WriteLine(saveTransform.position);
writer.WriteLine(saveTransform.rotation);
}
}
}
if(id==-1){
if(loadTuple.HasValue){
if(LOG&&LOG_LEVEL<=1){Debug.Log("I need to be activated with id:"+loadTuple.Value.id,current);}
id=loadTuple.Value.id;
if(loadTuple.Value.cnkIdx.HasValue){
int cnkIdx1=loadTuple.Value.cnkIdx.Value;
transformFolder=string.Format("{0}/{1}",actorsFolder,cnkIdx1);transformFile=string.Format("{0}/{1}",transformFolder,string.Format("({0},{1}).StreamWriter",type,id));
if(LOG&&LOG_LEVEL<=1){Debug.Log("my id:"+id+"..my transform load file:.."+transformFile,current);}
using(FileStream file=new FileStream(transformFile,FileMode.Open,FileAccess.Read,FileShare.None)){
using(StreamReader reader=new StreamReader(file)){
saveTransform.id=id;reader.ReadLine();
string line;
var positionValues=(line=reader.ReadLine()).Substring(1,line.Length-2).Split('_');saveTransform.position=new SerializableVector3(float.Parse(positionValues[0]),float.Parse(positionValues[1]),float.Parse(positionValues[2]));
var rotationValues=(line=reader.ReadLine()).Substring(1,line.Length-2).Split('_');saveTransform.rotation=new SerializableQuaternion(float.Parse(rotationValues[0]),float.Parse(rotationValues[1]),float.Parse(rotationValues[2]),float.Parse(rotationValues[3]));
}
}
loaded=true;
}
enable=true;
}
}
#endregion safe
}
if(releaseId){releaseId=false;
if(LOG&&LOG_LEVEL<=1){Debug.Log("I'm releasing my id:"+id,current);}
id=-1;
transformFolder=null;transformFile=null;
}
if(LOG&&LOG_LEVEL<=1)Debug.Log("terminado processamento de dados de arquivo para este ator:"+id+"..levou:"+watch.ElapsedMilliseconds+"ms",current);
backgroundData.Set();ReleaseData();
}_Stop:{
}
if(LOG&&LOG_LEVEL<=1)Debug.Log("finalizar trabalho em plano de fundo para ator graciosamente");
}
}catch(Exception e){Debug.LogError(e?.Message+"\n"+e?.StackTrace+"\n"+e?.Source);}finally{
if(backgroundData!=null)backgroundData.Set();ReleaseData();
while(!Stop){enqueued.WaitOne(1000);if(Stop){enqueued.Set();goto _Stop;}if(queued.TryDequeue(out SimActor dequeued)){RenewData(dequeued);}else{continue;};if(queued.Count>0){enqueued.Set();}if(!backgroundData.WaitOne(0))backgroundData.Set();ReleaseData();Thread.Sleep(1);}_Stop:{}
}
}
}
}
protected virtual void OnDestroy(){
if(!exitSaved){OnExitSave();OnRemove();}
Stop=true;foregroundData.Dispose();backgroundData.Dispose();
OnActorDestroyed(this);
if(LOG&&LOG_LEVEL<=1)Debug.Log("destruição completa");
}
[NonSerialized]bool exitSaved=false;public void OnExitSave(List<ManualResetEvent>waitAll=null){exitSaved=true;
#region exit save
if(atServer){
backgroundData.WaitOne();
#region save for the last time and release id...
releaseId=true;
backgroundData.Reset();foregroundData.Set();SimActorTask.StartNew(this);
#endregion
if(waitAll==null)backgroundData.WaitOne();else waitAll.Add(backgroundData);
}
if(LOG&&LOG_LEVEL<=1)Debug.Log("I am now deactivated so I can be deleted..my id:"+id,this);
#endregion

//...

}
public void OnRemove(){
#region id released so set as not loaded... but don't add to pool because it's being destroyed!
Actors.Disabled.Remove(this);Actors.Enabled.Remove(this);if(LOG&&LOG_LEVEL<=1){Debug.Log("Actors.Enabled.Count:"+Actors.Enabled.Count+"..Actors.Disabled.Count:"+Actors.Disabled.Count,this);}
loadTuple=null;Loaded[type].Remove(this);
if(DisabledNode!=null)SimActorPool[type].Remove(DisabledNode);DisabledNode=null;
#endregion

//...

}
public virtual bool IsOutOfSight{get{return IsOutOfSight_v;}protected set{if(IsOutOfSight_v!=value){IsOutOfSight_v=value;

//...

if(LOG&&LOG_LEVEL<=1)Debug.Log("I am now..IsOutOfSight:"+value+"..my id is.."+id,this);
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
[SerializeField]protected float savingInterval=120f;[NonSerialized]protected float nextSaveTimer=0f;
protected virtual void Update(){
if(NetworkManager.Singleton.IsServer||atServer){atServer=true;
if(nextSaveTimer>0){nextSaveTimer-=Time.deltaTime;}
var gotcnk=false;void getcnk(){ActiveTerrain.TryGetValue(cnkIdx,out cnk);gotcnk=true;}      
if(!IsOutOfSight_v){//  Previne duplicata em Actors.Disabled
pos=transform.position;
if(pos!=pos_Pre||enabling){//  sempre que eu mudar de posição...
if(LOG&&LOG_LEVEL<=-110)Debug.Log("I changed from pos_Pre.."+pos_Pre+"..to pos.."+pos,this);
if(pos.y<-128&&pos_Pre.y<-128){transform.position=pos=pos_Pre;}
cCoord=vecPosTocCoord(pos);if(cCoord!=cCoord_Pre||enabling){cnkIdx=GetcnkIdx(cCoord.x,cCoord.y);//  ...calcule o cnkIdx se necessário...
if(LOG&&LOG_LEVEL<=1)Debug.Log("I changed from cCoord_Pre.."+cCoord_Pre+"..to cCoord.."+cCoord+"..so now my cnkIdx is.."+cnkIdx,this);
if(!gotcnk){getcnk();}//  ...e verifique se a coordenada para onde eu me mudei tem ou não um chunk.
cCoord_Pre=cCoord;}
pos_Pre=pos;}
if(firstLoop||actPos!=Camera.main.transform.position){if(LOG&&LOG_LEVEL<=-110){Debug.Log("actPos anterior:.."+actPos+"..;actPos novo:.."+Camera.main.transform.position);}
              actPos=(Camera.main.transform.position);//  sempre que a câmera mudar de posição...
if(firstLoop |aCoord!=(aCoord=vecPosTocCoord(actPos))){if(LOG&&LOG_LEVEL<=1){Debug.Log("aCoord novo:.."+aCoord+"..;aCoord_Pre:.."+aCoord_Pre);}
if(!gotcnk){getcnk();}//  ...e a coordenada mudar (e houver recarregamento), cheque se o chunk em que estou existe.
aCoord_Pre=aCoord;}
}
void Disable(){
if(LOG&&LOG_LEVEL<=1)Debug.Log("I am now being deactivated so I can sleep until I'm needed..my id:"+id,this);
acting=false;
collider.controller.enabled=false;
collider           .enabled=false;
navMeshAgent       .enabled=false;
Actors.Disabled.Add(this);Actors.Enabled.Remove(this);IsOutOfSight=true;if(LOG&&LOG_LEVEL<=1){Debug.Log("Actors.Enabled.Count:"+Actors.Enabled.Count+"..Actors.Disabled.Count:"+Actors.Disabled.Count,this);}
if(!networkHidden)foreach(var client in NetworkManager.ConnectedClients){if(client.Key==NetworkManager.Singleton.ServerClientId)continue;
network.NetworkHide(client.Key);}
networkHidden=true;
disabling=true;
}
if(pos.y<-128){//  marque como fora do mundo (sem opção de testar como dentro do mundo em outras condições) se estiver abaixo da altura mínima permitida.
if(LOG&&LOG_LEVEL<=-120)Debug.Log("I am out of the World (pos.y.."+pos.y+"..<-128)",this);
Disable();
}else if(atServer&&!NetworkManager.Singleton.IsServer){
if(LOG&&LOG_LEVEL<=1)Debug.Log("deactivate myself because the server shutdown",this);
Disable();
}else if(cnk==null||!cnk.Built
||(!bounds.Contains(transform.position)&&players.All(p=>p.Key.IsLocalPlayer||!p.Key.bounds.Contains(transform.position)))
){
Disable();
}else if(enabling){
acting=true;
collider.controller.enabled=true;
collider           .enabled=true;
if(networkHidden)foreach(var client in NetworkManager.ConnectedClients){if(client.Key==NetworkManager.Singleton.ServerClientId)continue;
network.NetworkShow(client.Key);}
networkHidden=false;
}
firstLoop=false;enabling=false;}
if(acting){
collider.isUsingAI=useAI;
if(useAI){

//...

if(!navMeshAgent.enabled){
if(navMeshAsyncOperation!=null&&navMeshAsyncOperation.isDone&&(NavMesh.SamplePosition(transform.position,out NavMeshHit hitResult,Mathf.Max(Width,Depth),navMeshAgent.areaMask)
                                                             ||NavMesh.SamplePosition(transform.position,out            hitResult,Height                ,navMeshAgent.areaMask))){
transform.position=hitResult.position;
navMeshAgent       .enabled=true;
}
}
}else{
if(navMeshAgent.enabled){
navMeshAgent       .enabled=false;
}

//...

}

//...

}
if(backgroundData.WaitOne(0)){
if(id!=-1){
#region get data if loaded or set if saving...
if(loaded){loaded=false;
transform.rotation=saveTransform.rotation;
transform.position=saveTransform.position;
}else{
saveTransform.id=id;
saveTransform.rotation=transform.rotation;
saveTransform.position=transform.position;
}
#endregion
if(enable){enable=false;
if(IsOutOfSight_v){//  Previne duplicata em Actors.Enabled
Actors.Enabled.Add(this);Actors.Disabled.Remove(this);IsOutOfSight=false;enabling=true;if(LOG&&LOG_LEVEL<=1){Debug.Log("Actors.Enabled.Count:"+Actors.Enabled.Count+"..Actors.Disabled.Count:"+Actors.Disabled.Count,this);}
}
}
}
if(disabling){
#region when disabling...
if(id!=-1){
#region save for the last time and release id...
if(LOG&&LOG_LEVEL<=1){Debug.Log("mark my id:"+id+" to be released",this);}
releaseId=true;
backgroundData.Reset();foregroundData.Set();SimActorTask.StartNew(this);
#endregion
}else{disabling=false;
atServer=false;
#region id released so add to pool...
loadTuple=null;Loaded[type].Remove(this);DisabledNode=SimActorPool[type].AddLast(this);
if(LOG&&LOG_LEVEL<=1)Debug.Log("I am now deactivated and sleeping until I'm needed..my id:"+id,this);
#endregion
}
#endregion
}else{
if(id==-1){
#region when enabling...
if(loadTuple.HasValue){
if(LOG&&LOG_LEVEL<=1)Debug.Log("I need to wake up..loadTuple:"+loadTuple,this);
nextSaveTimer=savingInterval;
backgroundData.Reset();foregroundData.Set();SimActorTask.StartNew(this);
}
#endregion 
}else if(nextSaveTimer<=0){
#region when saving...

//...

nextSaveTimer=savingInterval;
backgroundData.Reset();foregroundData.Set();SimActorTask.StartNew(this);
#endregion 
}
}
}
}
NetworkUpdate();
}
protected virtual void NetworkUpdate(){
if(NetworkManager.Singleton.IsServer){
if(!networkHidden){
networkPosition.Value=transform.position;
}
}
if(NetworkManager.Singleton.IsClient){
transform.position=networkPosition.Value;
}
}
public class AStarPathfinder{

//...

public class Node:IHeapItem<Node>{
public int HeapIndex{get;set;}
public float F{get;private set;}//  heuristics
public float G{get{return g;}set{g=value;F=g+h;}}float g;//  node dis to start
public float H{get{return h;}set{h=value;F=g+h;}}float h;//  node dis to target
public int CompareTo(Node toCompare){
   int comparison=F.CompareTo(toCompare.F);if(comparison==0){
       comparison=H.CompareTo(toCompare.H);}
return-comparison;}
public Vector3 Position{get;set;}
public readonly ConcurrentDictionary<(int index,int depthReferent),(Node node,int depth)?>Neighbors=new ConcurrentDictionary<(int,int),(Node,int)?>(2,26);

//...

public static Vector3 Size=new Vector3(0.28125f*2f,1.75f,0.28125f*2f);
}
}
}
}