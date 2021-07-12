using MessagePack;
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
[NonSerialized]public static readonly List<object>load_Syn_All=new List<object>();
[NonSerialized]static readonly Dictionary<Type,GameObject>Prefabs=new Dictionary<Type,GameObject>();[NonSerialized]public static readonly Dictionary<Type,LinkedList<SimObject>>SimObjectPool=new Dictionary<Type,LinkedList<SimObject>>();[NonSerialized]public static readonly Dictionary<Type,List<SimObject>>Loaded=new Dictionary<Type,List<SimObject>>();[NonSerialized]static readonly Dictionary<Type,List<(Type type,int id,int cnkIdx)>>Loading=new Dictionary<Type,List<(Type type,int id,int cnkIdx)>>();
[NonSerialized]static Dictionary<Type,int>Count;[NonSerialized]static Dictionary<Type,List<int>>Destroyed;
[NonSerialized]public static readonly List<SimObject>Enabled=new List<SimObject>();[NonSerialized]public static readonly List<SimObject>Disabled=new List<SimObject>();
[NonSerialized]public static Buildings staticScript;
void Awake(){staticScript=this;
buildingsFolder=string.Format("{0}{1}",savePath,"buildings");
Directory.CreateDirectory(buildingsPath=string.Format("{0}/",buildingsFolder));
var objects=Resources.LoadAll("AKCondinoO/Buildings/Structures",typeof(GameObject));
foreach(var o in objects){var p=o as GameObject;var t=p.GetComponent<SimObject>().GetType();
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
if(LOG&&LOG_LEVEL<=1)Debug.Log("inicializar trabalho em plano de fundo 1 para gerenciar constru��es");
var watch=new System.Diagnostics.Stopwatch();
foregroundData1.WaitOne();
if(LOG&&LOG_LEVEL<=1){Debug.Log("come�ar carregamento de dados de ids");watch.Restart();}
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
Destroyed=MessagePackSerializer.Deserialize(typeof(Dictionary<Type,List<int>>),file)as Dictionary<Type,List<int>>;
}else{
Destroyed=new Dictionary<Type,List<int>>();
}
if(LOG&&LOG_LEVEL<=1){foreach(var d in Destroyed){Debug.Log("type.."+d.Key+"..has.."+d.Value.Count+"..unplaced sim object(s) in world");}}
}
if(LOG&&LOG_LEVEL<=1)Debug.Log("terminado carregamento de dados de ids..levou:"+watch.ElapsedMilliseconds+"ms");
backgroundData1.Set();
while(!Stop){foregroundData1.WaitOne();if(Stop)goto _Stop;
if(LOG&&LOG_LEVEL<=1){Debug.Log("come�ar salvamento de dados de ids");watch.Restart();}
using(FileStream file=new FileStream(idsFile,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)){
MessagePackSerializer.Serialize(file,Count);
}
using(FileStream file=new FileStream(unplacedIdsFile,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)){
MessagePackSerializer.Serialize(file,Destroyed);
}
if(LOG&&LOG_LEVEL<=1)Debug.Log("terminado salvamento de dados de ids..levou:"+watch.ElapsedMilliseconds+"ms");
backgroundData1.Set();
}_Stop:{
}
if(LOG&&LOG_LEVEL<=1)Debug.Log("finalizar trabalho em plano de fundo 1 para gerenciar constru��es graciosamente");
}
}catch(Exception e){Debug.LogError(e?.Message+"\n"+e?.StackTrace+"\n"+e?.Source);}finally{
while(!Stop){if(!backgroundData1.WaitOne(0))backgroundData1.Set();Thread.Sleep(1);}
}
}
static void BG2(object state){Thread.CurrentThread.IsBackground=false;Thread.CurrentThread.Priority=System.Threading.ThreadPriority.BelowNormal;
try{
if(state is object[]parameters&&parameters[0]is bool LOG&&parameters[1]is int LOG_LEVEL&&parameters[2]is string buildingsFolder){
if(LOG&&LOG_LEVEL<=1)Debug.Log("inicializar trabalho em plano de fundo 2 para gerenciar constru��es");
var watch=new System.Diagnostics.Stopwatch();
while(!Stop){foregroundData2.WaitOne();if(Stop)goto _Stop;
if(LOG&&LOG_LEVEL<=1){Debug.Log("come�ar carregamento de dados de constru��es");watch.Restart();}
foreach(var syn in load_Syn_All)Monitor.Enter(syn);try{
#region safe

//...

#endregion safe
}catch{throw;}finally{foreach(var syn in load_Syn_All)Monitor.Exit(syn);}
aCoord_Pre=aCoord;
firstLoop=false;
if(LOG&&LOG_LEVEL<=1)Debug.Log("terminado carregamento de dados de constru��es..levou:"+watch.ElapsedMilliseconds+"ms");
backgroundData2.Set();
}_Stop:{
}
if(LOG&&LOG_LEVEL<=1)Debug.Log("finalizar trabalho em plano de fundo 2 para gerenciar constru��es graciosamente");
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

//...

void Update(){
if(reloadTimer>0){reloadTimer-=Time.deltaTime;}
if(backgroundData2.WaitOne(0)){
if(backgroundData1.WaitOne(0)){

//...

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

//...

}
}