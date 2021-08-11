using AKCondinoO.Voxels;
using MLAPI;
using MLAPI.NetworkVariable;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using static AKCondinoO.Util;using static AKCondinoO.Voxels.TerrainChunk;using static AKCondinoO.Voxels.World;using static AKCondinoO.Buildings.Buildings;
namespace AKCondinoO.Buildings{public class SimObject:NetworkBehaviour{public bool LOG=true;public int LOG_LEVEL=1;public int GIZMOS_ENABLED=1;
[NonSerialized]public LinkedListNode<SimObject>DisabledNode=null;
bool Stop{
get{bool tmp;lock(Stop_Syn){tmp=Stop_v;      }return tmp;}
set{         lock(Stop_Syn){    Stop_v=value;}if(value){foregroundData.Set();}}
}[NonSerialized]readonly object Stop_Syn=new object();[NonSerialized]bool Stop_v=false;[NonSerialized]readonly AutoResetEvent foregroundData=new AutoResetEvent(false);[NonSerialized]readonly ManualResetEvent backgroundData=new ManualResetEvent(true);
[NonSerialized]public readonly object load_Syn=new object();
[Serializable]public class SimObjectSaveTransform{
public string type{get;set;}public int id{get;set;}
public SerializableQuaternion rotation{get;set;}
public SerializableVector3    position{get;set;}
}[NonSerialized]readonly SimObjectSaveTransform saveTransform=new SimObjectSaveTransform();[NonSerialized]string transformFolder;[NonSerialized]string transformFile;
[Serializable]public class SimObjectSaveStateData{
public string type{get;set;}public int id{get;set;}
}[NonSerialized]readonly SimObjectSaveStateData saveStateData=new SimObjectSaveStateData();[NonSerialized]string stateDataFolder;[NonSerialized]string stateDataFile;
public Type type{get;protected set;}public int id{get;protected set;}
[NonSerialized]bool disabling;[NonSerialized]bool unplace;[NonSerialized]bool unplacing;[NonSerialized]int unplacedId;
[NonSerialized]bool releaseId;
[NonSerialized]public(Type type,int id,int?cnkIdx)?loadTuple=null;[NonSerialized]bool loaded;[NonSerialized]bool enable;[NonSerialized]bool enabling;
[NonSerialized]public NetworkObject network;[NonSerialized]protected bool atServer;
[NonSerialized]public readonly NetworkVariableVector3 networkPosition=new NetworkVariableVector3(new NetworkVariableSettings{WritePermission=NetworkVariablePermission.ServerOnly,ReadPermission=NetworkVariablePermission.Everyone,});
[NonSerialized]public new Collider[]collider;[NonSerialized]public new Rigidbody rigidbody;
protected virtual void Awake(){if(transform.parent!=Buildings.staticScript.transform){transform.parent=Buildings.staticScript.transform;}
type=GetType();id=-1;
saveTransform.type=type.FullName;
saveStateData.type=type.FullName;
network=GetComponent<NetworkObject>();
collider=GetComponents<Collider>();rigidbody=GetComponent<Rigidbody>();
if(LOG&&LOG_LEVEL<=1)Debug.Log("I got instantiated and I am of type.."+type+"..now, add myself to sim objects pool",this);
foreach(var col in collider){col.enabled=false;}if(rigidbody){rigidbody.velocity=Vector3.zero;rigidbody.angularVelocity=Vector3.zero;rigidbody.constraints=RigidbodyConstraints.FreezeAll;}
Buildings.Disabled.Add(this);Buildings.Enabled.Remove(this);IsOutOfSight=true;if(LOG&&LOG_LEVEL<=1){Debug.Log("Buildings.Enabled.Count:"+Buildings.Enabled.Count+"..Buildings.Disabled.Count:"+Buildings.Disabled.Count,this);}
DisabledNode=SimObjectPool[type].AddLast(this);
pos=pos_Pre=transform.position;cCoord=cCoord_Pre=vecPosTocCoord(pos);cnkIdx=GetcnkIdx(cCoord.x,cCoord.y);
if(LOG&&LOG_LEVEL<=1)Debug.Log("I am awaking at.."+pos+"..and my cCoord is.."+cCoord+"..,so my cnkIdx is.."+cnkIdx,this);

//...

}
public class SimObjectTask{
[NonSerialized]static readonly ConcurrentQueue<SimObject>queued=new ConcurrentQueue<SimObject>();[NonSerialized]static readonly AutoResetEvent enqueued=new AutoResetEvent(false);
public static void StartNew(SimObject state){queued.Enqueue(state);enqueued.Set();}

//...

#region current terrain processing data
SimObject current{get;set;}AutoResetEvent foregroundData{get;set;}ManualResetEvent backgroundData{get;set;}
object load_Syn{get;set;}
SimObjectSaveTransform saveTransform{get;set;}string transformFile{get{return current.transformFile;}set{current.transformFile=value;}}string transformFolder{get{return current.transformFolder;}set{current.transformFolder=value;}}

//...

Type type{get{return current.type;}set{current.type=value;}}int id{get{return current.id;}set{current.id=value;}}
bool unplacing{get{return current.unplacing;}set{current.unplacing=value;}}int unplacedId{get{return current.unplacedId;}set{current.unplacedId=value;}}

//...

bool releaseId{get{return current.releaseId;}set{current.releaseId=value;}}
(Type type,int id,int?cnkIdx)?loadTuple{get{return current.loadTuple;}set{current.loadTuple=value;}}bool loaded{get{return current.loaded;}set{current.loaded=value;}}bool enable{get{return current.enable;}set{current.enable=value;}}

//...

void RenewData(SimObject next){
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
public SimObjectTask(bool LOG,int LOG_LEVEL){
task=Task.Factory.StartNew(BG,new object[]{LOG,LOG_LEVEL,savePath,buildingsFolder,},TaskCreationOptions.LongRunning);
void BG(object state){Thread.CurrentThread.IsBackground=false;Thread.CurrentThread.Priority=System.Threading.ThreadPriority.BelowNormal;
try{
if(state is object[]parameters&&parameters[0]is bool LOG&&parameters[1]is int LOG_LEVEL&&parameters[2]is string savePath&&parameters[3]is string buildingsFolder){
if(LOG&&LOG_LEVEL<=1)Debug.Log("inicializar trabalho em plano de fundo para objeto sim");
var watch=new System.Diagnostics.Stopwatch();
while(!Stop){enqueued.WaitOne();if(Stop){enqueued.Set();goto _Stop;}if(queued.TryDequeue(out SimObject dequeued)){RenewData(dequeued);}else{continue;};if(queued.Count>0){enqueued.Set();}foregroundData.WaitOne();
if(LOG&&LOG_LEVEL<=1){Debug.Log("come�ar novo processamento de dados de arquivo para este objeto sim:"+id,current);watch.Restart();}
lock(load_Syn){
#region safe
if(id!=-1){
if(!string.IsNullOrEmpty(transformFile)&&File.Exists(transformFile)){
if(LOG&&LOG_LEVEL<=1){Debug.Log("n�o gerar duplicata: j� h� um arquivo carregado registrado para objeto:"+id+"..deletar antes de abrir um novo");}
File.Delete(transformFile);
}
if(!unplacing){
Vector2Int cCoord1=vecPosTocCoord(saveTransform.position);int cnkIdx1=GetcnkIdx(cCoord1.x,cCoord1.y);
transformFolder=string.Format("{0}/{1}",buildingsFolder,cnkIdx1);transformFile=string.Format("{0}/{1}",transformFolder,string.Format("({0},{1}).StreamWriter",saveTransform.type,saveTransform.id));
Directory.CreateDirectory(string.Format("{0}/",transformFolder));
if(LOG&&LOG_LEVEL<=1){Debug.Log("my id:"+id+"..my transform save file:.."+transformFile,current);}
using(FileStream file=new FileStream(transformFile,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)){
using(StreamWriter writer=new StreamWriter(file)){
writer.WriteLine(saveTransform.id);
writer.WriteLine(saveTransform.position);
writer.WriteLine(saveTransform.rotation);
}
}
}else{
if(LOG&&LOG_LEVEL<=1){Debug.Log("I am now unplaced; my id:"+id,current);}
unplacedId=id;
}
}
if(id==-1){
if(loadTuple.HasValue){
if(LOG&&LOG_LEVEL<=1){Debug.Log("I need to be activated with id:"+loadTuple.Value.id,current);}
id=loadTuple.Value.id;
if(loadTuple.Value.cnkIdx.HasValue){
int cnkIdx1=loadTuple.Value.cnkIdx.Value;
transformFolder=string.Format("{0}/{1}",buildingsFolder,cnkIdx1);transformFile=string.Format("{0}/{1}",transformFolder,string.Format("({0},{1}).StreamWriter",type,id));
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
if(LOG&&LOG_LEVEL<=1)Debug.Log("finalizar trabalho em plano de fundo para objeto sim graciosamente");
}
}catch(Exception e){Debug.LogError(e?.Message+"\n"+e?.StackTrace+"\n"+e?.Source);}finally{
if(backgroundData!=null)backgroundData.Set();ReleaseData();
while(!Stop){enqueued.WaitOne(1000);if(Stop){enqueued.Set();goto _Stop;}if(queued.TryDequeue(out SimObject dequeued)){RenewData(dequeued);}else{continue;};if(queued.Count>0){enqueued.Set();}if(!backgroundData.WaitOne(0))backgroundData.Set();ReleaseData();Thread.Sleep(1);}_Stop:{}
}
}
}
}
protected virtual void OnDestroy(){
if(!exitSaved){OnExitSave();OnRemove();}
Stop=true;foregroundData.Dispose();backgroundData.Dispose();
OnSimObjectDestroyed(this);
if(LOG&&LOG_LEVEL<=1)Debug.Log("destrui��o completa");
}
[NonSerialized]bool exitSaved=false;public void OnExitSave(List<ManualResetEvent>waitAll=null){exitSaved=true;
#region exit save
if(atServer){
backgroundData.WaitOne();
#region save for the last time and release id...
releaseId=true;
backgroundData.Reset();foregroundData.Set();SimObjectTask.StartNew(this);
#endregion
if(waitAll==null)backgroundData.WaitOne();else waitAll.Add(backgroundData);
}
if(LOG&&LOG_LEVEL<=1)Debug.Log("I am now deactivated so I can be deleted..my id:"+id,this);
#endregion
}
public void OnRemove(){
#region id released so set as not loaded... but don't add to pool because it's being destroyed!
Buildings.Disabled.Remove(this);Buildings.Enabled.Remove(this);if(LOG&&LOG_LEVEL<=1){Debug.Log("Buildings.Enabled.Count:"+Buildings.Enabled.Count+"..Buildings.Disabled.Count:"+Buildings.Disabled.Count,this);}
loadTuple=null;Loaded[type].Remove(this);
if(DisabledNode!=null)SimObjectPool[type].Remove(DisabledNode);DisabledNode=null;
#endregion
RemoveFromNavMesh();
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
[SerializeField]protected bool DEBUG_UNPLACE=false;
protected virtual void Update(){
if(NetworkManager.Singleton.IsServer||atServer){atServer=true;
if(nextSaveTimer>0){nextSaveTimer-=Time.deltaTime;}
var gotcnk=false;void getcnk(){ActiveTerrain.TryGetValue(cnkIdx,out cnk);gotcnk=true;}      
if(!IsOutOfSight_v){//  Previne duplicata em Buildings.Disabled
pos=transform.position;
if(pos!=pos_Pre||enabling){//  sempre que eu mudar de posi��o...
if(LOG&&LOG_LEVEL<=-110)Debug.Log("I changed from pos_Pre.."+pos_Pre+"..to pos.."+pos,this);
if(pos.y<-128&&pos_Pre.y<-128){transform.position=pos=pos_Pre;}
cCoord=vecPosTocCoord(pos);if(cCoord!=cCoord_Pre||enabling){cnkIdx=GetcnkIdx(cCoord.x,cCoord.y);//  ...calcule o cnkIdx se necess�rio...
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
void Disable(){
if(LOG&&LOG_LEVEL<=1)Debug.Log("I am now being deactivated so I can sleep until I'm needed..my id:"+id,this);
foreach(var col in collider){col.enabled=false;}if(rigidbody){rigidbody.velocity=Vector3.zero;rigidbody.angularVelocity=Vector3.zero;rigidbody.constraints=RigidbodyConstraints.FreezeAll;}
Buildings.Disabled.Add(this);Buildings.Enabled.Remove(this);IsOutOfSight=true;if(LOG&&LOG_LEVEL<=1){Debug.Log("Buildings.Enabled.Count:"+Buildings.Enabled.Count+"..Buildings.Disabled.Count:"+Buildings.Disabled.Count,this);}
RemoveFromNavMesh();
network.Despawn();
disabling=true;
}
if(pos.y<-128){//  marque como fora do mundo (sem op��o de testar como dentro do mundo em outras condi��es) se estiver abaixo da altura m�nima permitida.
if(LOG&&LOG_LEVEL<=-120)Debug.Log("I am out of the World (pos.y.."+pos.y+"..<-128)",this);
Disable();
}else if(atServer&&!NetworkManager.Singleton.IsServer){
if(LOG&&LOG_LEVEL<=1)Debug.Log("deactivate myself because the server shutdown",this);
Disable();
}else if(cnk==null||!cnk.Built
||(!bounds.Contains(transform.position)&&players.All(p=>p.Key.IsLocalPlayer||!p.Key.bounds.Contains(transform.position)))
){
Disable();
}else if(DEBUG_UNPLACE){
if(LOG&&LOG_LEVEL<=1)Debug.Log("DEBUG_UNPLACE");
Disable();
unplace=true;

//...

DEBUG_UNPLACE=false;
}else if(enabling){
foreach(var col in collider){col.enabled=true;}if(rigidbody){rigidbody.velocity=Vector3.zero;rigidbody.angularVelocity=Vector3.zero;rigidbody.constraints=RigidbodyConstraints.None;}
AddToNavMesh();
}
firstLoop=false;enabling=false;}
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
if(IsOutOfSight_v){//  Previne duplicata em Buildings.Enabled
Buildings.Enabled.Add(this);Buildings.Disabled.Remove(this);IsOutOfSight=false;enabling=true;if(LOG&&LOG_LEVEL<=1){Debug.Log("Buildings.Enabled.Count:"+Buildings.Enabled.Count+"..Buildings.Disabled.Count:"+Buildings.Disabled.Count,this);}
}
}
}
if(disabling){
if(unplace){unplace=false;
if(LOG&&LOG_LEVEL<=1)Debug.Log("unplace:I need to remove my saved transform data");
unplacing=true;
}
#region when disabling...
if(id!=-1){
#region save for the last time and release id...
if(LOG&&LOG_LEVEL<=1){Debug.Log("mark my id:"+id+" to be released",this);}
releaseId=true;
backgroundData.Reset();foregroundData.Set();SimObjectTask.StartNew(this);
#endregion
}else{disabling=false;
atServer=false;
#region id released so add to pool...
loadTuple=null;Loaded[type].Remove(this);DisabledNode=SimObjectPool[type].AddLast(this);
if(unplacing){unplacing=false;
if(!Unplaced.ContainsKey(type)){Unplaced.Add(type,new List<int>());}Unplaced[type].Add(unplacedId);unplacedId=-1;
                            
//...

}
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
backgroundData.Reset();foregroundData.Set();SimObjectTask.StartNew(this);
}
#endregion 
}else if(nextSaveTimer<=0){
#region when saving...

//...

nextSaveTimer=savingInterval;
backgroundData.Reset();foregroundData.Set();SimObjectTask.StartNew(this);
#endregion 
}
}
}
}
NetworkUpdate();
}
protected virtual void NetworkUpdate(){
if(NetworkManager.Singleton.IsServer){
networkPosition.Value=transform.position;
}
if(NetworkManager.Singleton.IsClient){
transform.position=networkPosition.Value;
}
}
protected virtual void AddToNavMesh(){
if(LOG&&LOG_LEVEL<=1)Debug.Log("I am now added to the NavMesh system..my id:"+id,this);
}
protected virtual void RemoveFromNavMesh(){
if(LOG&&LOG_LEVEL<=1)Debug.Log("I am now removed from the NavMesh system..my id:"+id,this);
}
}
}