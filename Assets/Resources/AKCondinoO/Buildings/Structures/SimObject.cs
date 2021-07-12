using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using static AKCondinoO.Util;using static AKCondinoO.Voxels.TerrainChunk;using static AKCondinoO.Voxels.World;using static AKCondinoO.Buildings.Buildings;
namespace AKCondinoO.Buildings{public class SimObject:MonoBehaviour{public bool LOG=true;public int LOG_LEVEL=1;public int GIZMOS_ENABLED=1;
[NonSerialized]public LinkedListNode<SimObject>DisabledNode=null;
bool Stop{
get{bool tmp;lock(Stop_Syn){tmp=Stop_v;      }return tmp;}
set{         lock(Stop_Syn){    Stop_v=value;}if(value){foregroundData.Set();}}
}[NonSerialized]readonly object Stop_Syn=new object();[NonSerialized]bool Stop_v=false;[NonSerialized]readonly AutoResetEvent foregroundData=new AutoResetEvent(false);[NonSerialized]readonly ManualResetEvent backgroundData=new ManualResetEvent(true);[NonSerialized]Task task;
[NonSerialized]public readonly object load_Syn=new object();
[Serializable]public class SaveTransform{
public string type{get;set;}public int id{get;set;}
public SerializableQuaternion rotation{get;set;}
public SerializableVector3    position{get;set;}
}[NonSerialized]readonly SaveTransform saveTransform=new SaveTransform();[NonSerialized]string transformFolder;[NonSerialized]string transformFile;
[Serializable]public class SaveStateData{
public string type{get;set;}public int id{get;set;}
}[NonSerialized]readonly SaveStateData saveStateData=new SaveStateData();[NonSerialized]string stateDataFolder;[NonSerialized]string stateDataFile;
public Type type{get;protected set;}public int id{get;protected set;}
[NonSerialized]bool disabling;
[NonSerialized]bool releaseId;
[NonSerialized]public(Type type,int id,int?cnkIdx)?loadTuple=null;[NonSerialized]bool loaded;[NonSerialized]bool enable;[NonSerialized]bool enabling;

//...

[NonSerialized]public new Collider[]collider;
protected virtual void Awake(){if(transform.parent!=Buildings.staticScript.transform){transform.parent=Buildings.staticScript.transform;}
type=GetType();id=-1;
saveTransform.type=type.FullName;
saveStateData.type=type.FullName;

//...

collider=GetComponents<Collider>();
if(LOG&&LOG_LEVEL<=1)Debug.Log("I got instantiated and I am of type.."+type+"..now, add myself to sim objects pool",this);
foreach(var col in collider){col.enabled=false;}
Buildings.Disabled.Add(this);Buildings.Enabled.Remove(this);IsOutOfSight=true;
DisabledNode=SimObjectPool[type].AddLast(this);

//...

task=Task.Factory.StartNew(BG,new object[]{LOG,LOG_LEVEL,savePath,buildingsFolder,},TaskCreationOptions.LongRunning);
void BG(object state){Thread.CurrentThread.IsBackground=false;Thread.CurrentThread.Priority=System.Threading.ThreadPriority.BelowNormal;
try{
if(state is object[]parameters&&parameters[0]is bool LOG&&parameters[1]is int LOG_LEVEL&&parameters[2]is string savePath&&parameters[3]is string buildingsFolder){
if(LOG&&LOG_LEVEL<=1)Debug.Log("inicializar trabalho em plano de fundo para objeto sim");
var watch=new System.Diagnostics.Stopwatch();

//...

while(!Stop){foregroundData.WaitOne();if(Stop)goto _Stop;
if(LOG&&LOG_LEVEL<=1){Debug.Log("começar novo processamento de dados de arquivo para este objeto sim:"+id,this);watch.Restart();}
lock(load_Syn){

//...

}

//...

if(LOG&&LOG_LEVEL<=1)Debug.Log("terminado processamento de dados de arquivo para este ator:"+id+"..levou:"+watch.ElapsedMilliseconds+"ms",this);
backgroundData.Set();
}_Stop:{
}
if(LOG&&LOG_LEVEL<=1)Debug.Log("finalizar trabalho em plano de fundo para objeto sim graciosamente");
}
}catch(Exception e){Debug.LogError(e?.Message+"\n"+e?.StackTrace+"\n"+e?.Source);}finally{
while(!Stop){if(!backgroundData.WaitOne(0))backgroundData.Set();Thread.Sleep(1);}
}
}
}
protected virtual void OnDestroy(){
#region exit save
backgroundData.WaitOne();
#region save for the last time and release id...
releaseId=true;
backgroundData.Reset();foregroundData.Set();
#endregion
backgroundData.WaitOne();
#region id released so set as not loaded... but don't add to pool because it's being destroyed!
loadTuple=null;Loaded[type].Remove(this);
#endregion
if(LOG&&LOG_LEVEL<=1)Debug.Log("I am now deactivated so I can be deleted..my id:"+id,this);
#endregion
Stop=true;try{task.Wait();}catch(Exception e){Debug.LogError(e?.Message+"\n"+e?.StackTrace+"\n"+e?.Source);}foregroundData.Dispose();backgroundData.Dispose();
if(DisabledNode!=null)SimObjectPool[type].Remove(DisabledNode);DisabledNode=null;

//...

if(LOG&&LOG_LEVEL<=1)Debug.Log("destruição completa");
}
public virtual bool IsOutOfSight{get{return IsOutOfSight_v;}protected set{if(IsOutOfSight_v!=value){IsOutOfSight_v=value;

//...

if(LOG&&LOG_LEVEL<=1)Debug.Log("I am now..IsOutOfSight:"+value+"..my id is.."+id,this);
}}
}[NonSerialized]protected bool IsOutOfSight_v;

//...

}
}