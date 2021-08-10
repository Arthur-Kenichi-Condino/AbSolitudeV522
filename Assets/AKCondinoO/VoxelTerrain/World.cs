using AKCondinoO.Networking;
using LibNoise;
using LibNoise.Generator;
using LibNoise.Operator;
using MLAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Threading;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Profiling;
using UnityEngine.Scripting;
using UnityEngine.UI;
using static AKCondinoO.Voxels.TerrainChunk;
using static AKCondinoO.Voxels.TerrainChunk.Editor;
namespace AKCondinoO.Voxels{public class World:MonoBehaviour{public bool LOG=true;public int LOG_LEVEL=1;public int GIZMOS_ENABLED=1;public bool DEBUG_MODE=true;
[NonSerialized]public static string savePath;[NonSerialized]public static string saveName="world";[NonSerialized]public static readonly string saveFolder=Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData).Replace("\\","/").ToString()+"/AbSolitudeV522";
public Text UI_FPS;[NonSerialized]float UI_FPS_RefreshTimer;[NonSerialized]float UI_FPS_RefreshTime=1.0f;
public const int MaxcCoordx=6250;
public const int MaxcCoordy=6250;
public static Vector2Int vecPosTocCoord(Vector3 pos){
                                                pos.x/=(float)Width;
                                                pos.z/=(float)Depth;
return new Vector2Int((pos.x>0)?(pos.x-(int)pos.x==0.5f?Mathf.FloorToInt(pos.x):Mathf.RoundToInt(pos.x)):(int)Math.Round(pos.x,MidpointRounding.AwayFromZero),
                      (pos.z>0)?(pos.z-(int)pos.z==0.5f?Mathf.FloorToInt(pos.z):Mathf.RoundToInt(pos.z)):(int)Math.Round(pos.z,MidpointRounding.AwayFromZero));
}
public static Vector2Int vecPosTocnkRgn(Vector3 pos){Vector2Int coord=vecPosTocCoord(pos);
return new Vector2Int(coord.x*Width,coord.y*Depth);
}
public GameObject ChunkPrefab;
public static Vector2Int expropriationDistance{get;}=new Vector2Int(5,5);[NonSerialized]public static readonly LinkedList<TerrainChunk>TerrainChunkPool=new LinkedList<TerrainChunk>();[NonSerialized]public static readonly Dictionary<int,TerrainChunk>ActiveTerrain=new Dictionary<int,TerrainChunk>();[NonSerialized]static readonly TerrainChunkTask[]tasks=new TerrainChunkTask[tasksCount];const int tasksCount=121;
public static Vector2Int instantiationDistance{get;}=new Vector2Int(4,4);
[NonSerialized]public static Bounds bounds;
[NonSerialized]public static NavMeshDataInstance navMesh;[NonSerialized]public static NavMeshData navMeshData;[NonSerialized]public static NavMeshBuildSettings navMeshBuildSettings;
[NonSerialized]public static readonly Dictionary<GameObject,NavMeshBuildSource>navMeshSources=new Dictionary<GameObject,NavMeshBuildSource>();[NonSerialized]public static readonly List<NavMeshBuildSource>sources=new List<NavMeshBuildSource>();
[NonSerialized]public static readonly Dictionary<GameObject,NavMeshBuildMarkup>navMeshMarkups=new Dictionary<GameObject,NavMeshBuildMarkup>();[NonSerialized]public static readonly List<NavMeshBuildMarkup>markups=new List<NavMeshBuildMarkup>();
[NonSerialized]public static AsyncOperation navMeshAsyncOperation;[NonSerialized]public static bool navMeshDirty;
[NonSerialized]public static readonly BiomeBase biome=new Plains();
[SerializeField]public int targetFrameRate=60;
[NonSerialized]public const int maxPlayers=6;[NonSerialized]public static readonly Dictionary<UNetDefaultPrefab,(Vector2Int cCoord,Vector2Int cCoord_Pre)?>players=new Dictionary<UNetDefaultPrefab,(Vector2Int,Vector2Int)?>(maxPlayers);
void Awake(){
GCSettings.LatencyMode=GCLatencyMode.SustainedLowLatency;  
#if !UNITY_EDITOR
GarbageCollector.GCMode=GarbageCollector.Mode.Manual;
#endif
MemoryManagement.Run(LOG,LOG_LEVEL);//  Start
            
//...

Directory.CreateDirectory(savePath=string.Format("{0}/{1}/",saveFolder,saveName));

if(LOG&&LOG_LEVEL<=100)Debug.Log("The number of processors on this computer is:"+Environment.ProcessorCount);
ThreadPool.GetAvailableThreads(out int worker ,out int io         );if(LOG&&LOG_LEVEL<=100){Debug.Log("Thread pool threads available at startup: Worker threads: "+worker+" Asynchronous I/O threads: "+io);}
ThreadPool.GetMaxThreads(out int workerThreads,out int portThreads);if(LOG&&LOG_LEVEL<=100){Debug.Log("Maximum worker threads: "+workerThreads+" Maximum completion port threads: "+portThreads);           }
ThreadPool.GetMinThreads(out int minWorker    ,out int minIOC     );if(LOG&&LOG_LEVEL<=100){Debug.Log("minimum number of worker threads: "+minWorker+" minimum asynchronous I/O: "+minIOC);                 }
var idealMin=(tasksCount+Buildings.Buildings.tasksCount+2);if(minWorker!=idealMin){
if(ThreadPool.SetMinThreads(idealMin,minIOC)){if(LOG&&LOG_LEVEL<=100){Debug.Log("changed minimum number of worker threads to:"+(idealMin));}
}else{                                        if(LOG&&LOG_LEVEL<=100){Debug.Log("SetMinThreads failed");                                   }
}
}
QualitySettings.vSyncCount=0;Application.targetFrameRate=targetFrameRate;

//...

AtlasHelper.GetAtlasData(ChunkPrefab.GetComponent<MeshRenderer>().sharedMaterial);
Vector3 fadeEnd,fadeStart;
AtlasHelper.Material.SetVector(AtlasHelper._Shader_Input[1],fadeEnd=new Vector3((instantiationDistance.x+.5f)*Width,Height/2f,(instantiationDistance.y+.5f)*Depth));
AtlasHelper.Material.SetVector(AtlasHelper._Shader_Input[2],fadeStart=fadeEnd-new Vector3(8,8,8));
            
//...

biome.LOG=LOG;biome.LOG_LEVEL=LOG_LEVEL;biome.Seed=0;       

//...

bounds=new Bounds(Vector3.zero,new Vector3((instantiationDistance.x*2+1)*Width,Height,
                                           (instantiationDistance.y*2+1)*Depth));
navMeshBuildSettings=new NavMeshBuildSettings{
agentTypeID=0,//  Humanoid agent
agentHeight=1.75f,
agentRadius=0.28125f,
agentClimb=0.75f,
agentSlope=60f,
overrideTileSize=true,
        tileSize=Width*Depth,
overrideVoxelSize=true,
        voxelSize=0.1406f,
minRegionArea=0.31640625f,
debug=new NavMeshBuildDebugSettings{
    flags=NavMeshBuildDebugFlags.None,
},
};
var navMeshValidation=navMeshBuildSettings.ValidationReport(bounds);if(navMeshValidation.Length==0){
if(LOG&&LOG_LEVEL<=1){Debug.Log("navMeshBuildSettings validated with no errors");}
navMeshData=new NavMeshData(0){//  Humanoid agent
hideFlags=HideFlags.None,
};
navMesh=NavMesh.AddNavMeshData(navMeshData);
}else{
foreach(var s in navMeshValidation){Debug.LogError(s);}
}

//...

Editor.Awake(LOG,LOG_LEVEL);
for(int i=0;i<tasks.Length;++i){tasks[i]=new TerrainChunkTask(LOG,LOG_LEVEL);}

//...

MemoryManagement.Run(LOG,LOG_LEVEL);//  After init cleaning
}
void Start(){
MemoryManagement.Run(LOG,LOG_LEVEL);//  After other objects init cleaning
}
public static class MemoryManagement{
public const long MaxMemoryUsage=32*1024L*1024L*1024L;
public const long ForcedGCThreshold=16L*1024L*1024L*1024L;const float forcedGCDelay=30f;[NonSerialized]static float forcedGCTimer=0f;
public const long collectAfterAllocating=160L*1024L*1024L;[NonSerialized]const float collectDelay=1f;[NonSerialized]static float collectTimer=0f;[NonSerialized]static long nextCollectAt;
public static long currentFrameMemory{get;private set;}public static long lastFrameMemory{get;private set;}
public static void Run(bool LOG,int LOG_LEVEL){
lastFrameMemory=currentFrameMemory;currentFrameMemory=Profiler.GetMonoUsedSizeLong();

//...

if(forcedGCTimer>0f){forcedGCTimer-=Time.deltaTime;}
static void fullBlockingGC(){
GCSettings.LargeObjectHeapCompactionMode=GCLargeObjectHeapCompactionMode.CompactOnce;
GC.Collect(GC.MaxGeneration,GCCollectionMode.Forced,true,true);
GC.WaitForPendingFinalizers();
}
if(collectTimer>0){collectTimer-=Time.deltaTime;}
static void nonBlockingGC(){
GC.Collect(0,GCCollectionMode.Optimized,false,false);
}
if(currentFrameMemory<lastFrameMemory){//  GC happened.
nextCollectAt=currentFrameMemory+collectAfterAllocating;
if(LOG&&LOG_LEVEL<=100)Debug.Log("GC happened: currentFrameMemory.."+currentFrameMemory+"..<..lastFrameMemory.."+lastFrameMemory+"..;non blocking GC nextCollectAt.."+nextCollectAt);
}
if(currentFrameMemory>MaxMemoryUsage){
if(LOG&&LOG_LEVEL<=100)Debug.Log("Trigger immediate GC: currentFrameMemory.."+currentFrameMemory+"..>..MaxMemoryUsage.."+MaxMemoryUsage);
fullBlockingGC();
nextCollectAt=(currentFrameMemory=Profiler.GetMonoUsedSizeLong())+collectAfterAllocating;
if(LOG&&LOG_LEVEL<=100)Debug.Log("immediate GC done: currentFrameMemory.."+currentFrameMemory+"..;non blocking GC nextCollectAt.."+nextCollectAt);
}else
if(currentFrameMemory>ForcedGCThreshold&&forcedGCTimer<=0f){//  Trigger immediate GC
if(LOG&&LOG_LEVEL<=100)Debug.Log("Trigger immediate GC: currentFrameMemory.."+currentFrameMemory+"..>..ForcedGCThreshold.."+ForcedGCThreshold);
fullBlockingGC();
forcedGCTimer=forcedGCDelay;
nextCollectAt=(currentFrameMemory=Profiler.GetMonoUsedSizeLong())+collectAfterAllocating;
if(LOG&&LOG_LEVEL<=100)Debug.Log("immediate GC done: currentFrameMemory.."+currentFrameMemory+"..;non blocking GC nextCollectAt.."+nextCollectAt);
}else 
if(currentFrameMemory>=nextCollectAt&&collectTimer<=0){//  Trigger non blocking GC because incremental GC didn't collect enough
if(LOG&&LOG_LEVEL<=100)Debug.Log("Trigger non blocking GC: currentFrameMemory.."+currentFrameMemory+"..>=..nextCollectAt.."+nextCollectAt);
nonBlockingGC();
collectTimer=collectDelay;
nextCollectAt=(currentFrameMemory=Profiler.GetMonoUsedSizeLong())+collectAfterAllocating;
if(LOG&&LOG_LEVEL<=100)Debug.Log("non blocking GC done: currentFrameMemory.."+currentFrameMemory+"..;non blocking GC nextCollectAt.."+nextCollectAt);
}
}
}
void OnDestroy(){
            
//...

Editor.OnDestroy(LOG,LOG_LEVEL);
TerrainChunkTask.Stop=true;for(int i=0;i<tasks.Length;++i){tasks[i].Wait();}

//...to do: biome dispose

}
[NonSerialized]bool firstLoop=true;
public static float FPS{
        get{float tmp;lock(FPS_Syn)tmp=FPS_v;return tmp;}
private set{          lock(FPS_Syn)    FPS_v=value;     }
}[NonSerialized]static readonly object FPS_Syn=new object();[NonSerialized]static float FPS_v;
public static float averageFramerate{
        get{float tmp;lock(averageFramerate_Syn){tmp=averageFramerate_v;      }return tmp;}
private set{          lock(averageFramerate_Syn){    averageFramerate_v=value;}           }
}[NonSerialized]static readonly object averageFramerate_Syn=new object();[NonSerialized]static float averageFramerate_v=60;[NonSerialized]int frameCounter;[NonSerialized]float averageFramerateRefreshTimer;[NonSerialized]float averageFramerateRefreshTime=1.0f;
[NonSerialized]float frameTimeVariation;[NonSerialized]float millisecondsPerFrame;
[NonSerialized]static Vector3    actPos;
[NonSerialized]static Vector2Int aCoord,aCoord_Pre;
[NonSerialized]static Vector2Int actRgn;
[SerializeField]protected bool       DEBUG_EDIT=false;
[SerializeField]protected Vector3    DEBUG_EDIT_AT=Vector3.zero;
[SerializeField]protected EditMode   DEBUG_EDIT_MODE=EditMode.cube;
[SerializeField]protected Vector3Int DEBUG_EDIT_SIZE=new Vector3Int(3,3,3);
[SerializeField]protected double     DEBUG_EDIT_DENSITY=100.0;
[SerializeField]protected MaterialId DEBUG_EDIT_MATERIAL_ID=MaterialId.Dirt;
[SerializeField]protected int        DEBUG_EDIT_SMOOTHNESS=5;
[SerializeField]protected bool DEBUG_BAKE_NAV_MESH=false;
void Update(){
MemoryManagement.Run(LOG,LOG_LEVEL);
if(Application.targetFrameRate!=targetFrameRate)Application.targetFrameRate=targetFrameRate;
frameTimeVariation+=(Time.deltaTime-frameTimeVariation);millisecondsPerFrame=frameTimeVariation*1000.0f;FPS=1.0f/frameTimeVariation;
frameCounter++;averageFramerateRefreshTimer+=Time.deltaTime;
if(averageFramerateRefreshTimer>=averageFramerateRefreshTime){
averageFramerate=frameCounter/averageFramerateRefreshTimer;
frameCounter=0;averageFramerateRefreshTimer=0.0f;
}
UI_FPS_RefreshTimer+=Time.deltaTime;
if(UI_FPS_RefreshTimer>=UI_FPS_RefreshTime){
UI_FPS.text="FPS:"+FPS;
UI_FPS_RefreshTimer=0;
}
if(NetworkManager.Singleton.IsServer){
if(TerrainChunkPool.Count==0){
if(LOG&&LOG_LEVEL<=1)Debug.Log("init TerrainChunkPool");
int maxChunks=(expropriationDistance.x*2+1)*(expropriationDistance.y*2+1)+(maxPlayers-1)*(expropriationDistance.x*2+1)*(expropriationDistance.y*2+1);
for(int i=maxChunks-1;i>=0;--i){
#if UNITY_EDITOR
long chunkMemoryUsage=-1;if(LOG&&LOG_LEVEL<=-1000){chunkMemoryUsage=System.GC.GetTotalMemory(true);}
#endif
GameObject obj=Instantiate(ChunkPrefab,transform);TerrainChunk scr=obj.GetComponent<TerrainChunk>();scr.ExpropriationNode=TerrainChunkPool.AddLast(scr);scr.network.Spawn();
#if UNITY_EDITOR
if(LOG&&LOG_LEVEL<=-1000){if(chunkMemoryUsage>=0){chunkMemoryUsage=System.GC.GetTotalMemory(true)-chunkMemoryUsage;GC.KeepAlive(obj);Debug.Log("instantiating chunk took "+chunkMemoryUsage+" bytes");}}
#endif
}
}

//...

if(firstLoop||actPos!=Camera.main.transform.position){if(LOG&&LOG_LEVEL<=-110){Debug.Log("actPos anterior:.."+actPos+"..;actPos novo:.."+Camera.main.transform.position);}
              actPos=(Camera.main.transform.position);
if(firstLoop |aCoord!=(aCoord=vecPosTocCoord(actPos))){if(LOG&&LOG_LEVEL<=1){Debug.Log("aCoord novo:.."+aCoord+"..;aCoord_Pre:.."+aCoord_Pre);}
              actRgn=(cCoordTocnkRgn(aCoord));

//...

bounds.center=new Vector3(actRgn.x,0,actRgn.y);
for(Vector2Int eCoord=new Vector2Int(),cCoord1=new Vector2Int();eCoord.y<=expropriationDistance.y;eCoord.y++){for(cCoord1.y=-eCoord.y+aCoord_Pre.y;cCoord1.y<=eCoord.y+aCoord_Pre.y;cCoord1.y+=eCoord.y*2){
for(           eCoord.x=0                                      ;eCoord.x<=expropriationDistance.x;eCoord.x++){for(cCoord1.x=-eCoord.x+aCoord_Pre.x;cCoord1.x<=eCoord.x+aCoord_Pre.x;cCoord1.x+=eCoord.x*2){
if(Math.Abs(cCoord1.x)>=MaxcCoordx||
   Math.Abs(cCoord1.y)>=MaxcCoordy){
if(LOG&&LOG_LEVEL<=1)Debug.Log("do not try to expropriate out of world chunk at coord:.."+cCoord1);
goto _skip;
}
if(LOG&&LOG_LEVEL<=1)Debug.Log("try to expropriate chunk:.."+cCoord1);
if((Mathf.Abs(cCoord1.x-aCoord.x)>instantiationDistance.x||
    Mathf.Abs(cCoord1.y-aCoord.y)>instantiationDistance.y)&&players.All(p=>{return(p.Key.IsLocalPlayer||(Mathf.Abs(cCoord1.x-p.Key.cCoord.x)>instantiationDistance.x||
                                                                                                         Mathf.Abs(cCoord1.y-p.Key.cCoord.y)>instantiationDistance.y));})){
int cnkIdx1=GetcnkIdx(cCoord1.x,cCoord1.y);if(ActiveTerrain.ContainsKey(cnkIdx1)){
if(LOG&&LOG_LEVEL<=1)Debug.Log("do expropriate chunk for:.."+cnkIdx1);
TerrainChunk scr=ActiveTerrain[cnkIdx1];if(scr.ExpropriationNode==null){scr.ExpropriationNode=TerrainChunkPool.AddLast(scr);
}else{
if(LOG&&LOG_LEVEL<=1)Debug.Log("but chunk is already expropriated:.."+cnkIdx1);
}
}else{
if(LOG&&LOG_LEVEL<=1)Debug.Log("no chunk to expropriate for index:.."+cnkIdx1);
}
}else{
if(LOG&&LOG_LEVEL<=1)Debug.Log("no need to expropriate chunk at:.."+cCoord1);
}
_skip:{}
if(eCoord.x==0){break;}}}
if(eCoord.y==0){break;}}}
for(Vector2Int iCoord=new Vector2Int(),cCoord1=new Vector2Int();iCoord.y<=instantiationDistance.y;iCoord.y++){for(cCoord1.y=-iCoord.y+aCoord.y;cCoord1.y<=iCoord.y+aCoord.y;cCoord1.y+=iCoord.y*2){
for(           iCoord.x=0                                      ;iCoord.x<=instantiationDistance.x;iCoord.x++){for(cCoord1.x=-iCoord.x+aCoord.x;cCoord1.x<=iCoord.x+aCoord.x;cCoord1.x+=iCoord.x*2){
if(Math.Abs(cCoord1.x)>=MaxcCoordx||
   Math.Abs(cCoord1.y)>=MaxcCoordy){
if(LOG&&LOG_LEVEL<=1)Debug.Log("do not try to activate out of world chunk at coord:.."+cCoord1);
goto _skip;
}
if(LOG&&LOG_LEVEL<=1)Debug.Log("try to activate chunk:.."+cCoord1);
int cnkIdx1=GetcnkIdx(cCoord1.x,cCoord1.y);if(!ActiveTerrain.ContainsKey(cnkIdx1)){
if(LOG&&LOG_LEVEL<=1)Debug.Log("do activate chunk for:.."+cnkIdx1+";[current TerrainChunkPool.Count:.."+TerrainChunkPool.Count);

//...

TerrainChunk scr=TerrainChunkPool.First.Value;TerrainChunkPool.RemoveFirst();scr.ExpropriationNode=(null);if(scr.Initialized&&ActiveTerrain.ContainsKey(scr.cnkIdx))ActiveTerrain.Remove(scr.cnkIdx);ActiveTerrain.Add(cnkIdx1,scr);scr.OncCoordChanged(cCoord1,cnkIdx1);
}else{
if(LOG&&LOG_LEVEL<=1)Debug.Log("but chunk is already active:.."+cnkIdx1);
TerrainChunk scr=ActiveTerrain[cnkIdx1];if(scr.ExpropriationNode!=null){TerrainChunkPool.Remove(scr.ExpropriationNode);scr.ExpropriationNode=(null);}
}
_skip:{}
if(iCoord.x==0){break;}}}
if(iCoord.y==0){break;}}}

//...

navMeshDirty=true;
aCoord_Pre=aCoord;}
AtlasHelper.Material.SetVector(AtlasHelper._Shader_Input[0],actPos);
}

//...

foreach(var player in players){if(!player.Value.HasValue||player.Key.IsLocalPlayer){continue;}if(LOG&&LOG_LEVEL<=-100)Debug.Log("net player .."+player.Key.network.OwnerClientId+".. changed coord: .."+player.Value);
var pCoord_Pre=player.Value.Value.cCoord_Pre;var pCoord=player.Value.Value.cCoord;

//...

for(Vector2Int eCoord=new Vector2Int(),cCoord1=new Vector2Int();eCoord.y<=expropriationDistance.y;eCoord.y++){for(cCoord1.y=-eCoord.y+pCoord_Pre.y;cCoord1.y<=eCoord.y+pCoord_Pre.y;cCoord1.y+=eCoord.y*2){
for(           eCoord.x=0                                      ;eCoord.x<=expropriationDistance.x;eCoord.x++){for(cCoord1.x=-eCoord.x+pCoord_Pre.x;cCoord1.x<=eCoord.x+pCoord_Pre.x;cCoord1.x+=eCoord.x*2){
if(Math.Abs(cCoord1.x)>=MaxcCoordx||
   Math.Abs(cCoord1.y)>=MaxcCoordy){
if(LOG&&LOG_LEVEL<=1)Debug.Log("do not try to expropriate out of world chunk at coord:.."+cCoord1);
goto _skip;
}
if(LOG&&LOG_LEVEL<=1)Debug.Log("try to expropriate chunk:.."+cCoord1);
if((Mathf.Abs(cCoord1.x-aCoord.x)>instantiationDistance.x||
    Mathf.Abs(cCoord1.y-aCoord.y)>instantiationDistance.y)&&players.All(p=>{return(Mathf.Abs(cCoord1.x-p.Key.cCoord.x)>instantiationDistance.x||
                                                                                   Mathf.Abs(cCoord1.y-p.Key.cCoord.y)>instantiationDistance.y);})){
int cnkIdx1=GetcnkIdx(cCoord1.x,cCoord1.y);if(ActiveTerrain.ContainsKey(cnkIdx1)){
if(LOG&&LOG_LEVEL<=1)Debug.Log("do expropriate chunk for:.."+cnkIdx1);
TerrainChunk scr=ActiveTerrain[cnkIdx1];if(scr.ExpropriationNode==null){scr.ExpropriationNode=TerrainChunkPool.AddLast(scr);
}else{
if(LOG&&LOG_LEVEL<=1)Debug.Log("but chunk is already expropriated:.."+cnkIdx1);
}
}else{
if(LOG&&LOG_LEVEL<=1)Debug.Log("no chunk to expropriate for index:.."+cnkIdx1);
}
}else{
if(LOG&&LOG_LEVEL<=1)Debug.Log("no need to expropriate chunk at:.."+cCoord1);
}
_skip:{}
if(eCoord.x==0){break;}}}
if(eCoord.y==0){break;}}}
for(Vector2Int iCoord=new Vector2Int(),cCoord1=new Vector2Int();iCoord.y<=instantiationDistance.y;iCoord.y++){for(cCoord1.y=-iCoord.y+pCoord.y;cCoord1.y<=iCoord.y+pCoord.y;cCoord1.y+=iCoord.y*2){
for(           iCoord.x=0                                      ;iCoord.x<=instantiationDistance.x;iCoord.x++){for(cCoord1.x=-iCoord.x+pCoord.x;cCoord1.x<=iCoord.x+pCoord.x;cCoord1.x+=iCoord.x*2){
if(Math.Abs(cCoord1.x)>=MaxcCoordx||
   Math.Abs(cCoord1.y)>=MaxcCoordy){
if(LOG&&LOG_LEVEL<=1)Debug.Log("do not try to activate out of world chunk at coord:.."+cCoord1);
goto _skip;
}
int cnkIdx1=GetcnkIdx(cCoord1.x,cCoord1.y);if(!ActiveTerrain.ContainsKey(cnkIdx1)){
if(LOG&&LOG_LEVEL<=1)Debug.Log("do activate chunk for:.."+cnkIdx1+";[current TerrainChunkPool.Count:.."+TerrainChunkPool.Count);

//...

TerrainChunk scr=TerrainChunkPool.First.Value;TerrainChunkPool.RemoveFirst();scr.ExpropriationNode=(null);if(scr.Initialized&&ActiveTerrain.ContainsKey(scr.cnkIdx))ActiveTerrain.Remove(scr.cnkIdx);ActiveTerrain.Add(cnkIdx1,scr);scr.OncCoordChanged(cCoord1,cnkIdx1);
}else{
if(LOG&&LOG_LEVEL<=1)Debug.Log("but chunk is already active:.."+cnkIdx1);
TerrainChunk scr=ActiveTerrain[cnkIdx1];if(scr.ExpropriationNode!=null){TerrainChunkPool.Remove(scr.ExpropriationNode);scr.ExpropriationNode=(null);}
}
_skip:{}
if(iCoord.x==0){break;}}}
if(iCoord.y==0){break;}}}
}

//...

if(DEBUG_EDIT){
   DEBUG_EDIT=false;

//...

Editor.Edit(DEBUG_EDIT_AT,DEBUG_EDIT_MODE,DEBUG_EDIT_SIZE,DEBUG_EDIT_DENSITY,DEBUG_EDIT_MATERIAL_ID,DEBUG_EDIT_SMOOTHNESS,LOG,LOG_LEVEL);

}

//...

Editor.Update(LOG,LOG_LEVEL,DEBUG_MODE);

//...

if(navMeshDirty||DEBUG_BAKE_NAV_MESH){

//...

if(navMeshAsyncOperation==null||navMeshAsyncOperation.isDone){
navMeshDirty=false;DEBUG_BAKE_NAV_MESH=false;
sources.Clear();sources.AddRange(navMeshSources.Values);
markups.Clear();markups.AddRange(navMeshMarkups.Values);
NavMeshBuilder.CollectSources(transform,LayerMask.GetMask("Default"),NavMeshCollectGeometry.RenderMeshes,0,markups,sources);
navMeshAsyncOperation=NavMeshBuilder.UpdateNavMeshDataAsync(navMeshData,navMeshBuildSettings,sources,bounds);
}
}

//...

var keys=players.Keys.ToList();for(int i=0;i<keys.Count;++i){players[keys[i]]=null;}
firstLoop=false;
}
if(NetworkManager.Singleton.IsClient){

//...

var keys=players.Keys.ToList();for(int i=0;i<keys.Count;++i){players[keys[i]]=null;}
firstLoop=false;
}
}
public static void OnPlayerRemoved(UNetDefaultPrefab player,bool LOG,int LOG_LEVEL){
var pCoord=vecPosTocCoord(player.transform.position);
for(Vector2Int iCoord=new Vector2Int(),cCoord1=new Vector2Int();iCoord.y<=instantiationDistance.y;iCoord.y++){for(cCoord1.y=-iCoord.y+pCoord.y;cCoord1.y<=iCoord.y+pCoord.y;cCoord1.y+=iCoord.y*2){
for(           iCoord.x=0                                      ;iCoord.x<=instantiationDistance.x;iCoord.x++){for(cCoord1.x=-iCoord.x+pCoord.x;cCoord1.x<=iCoord.x+pCoord.x;cCoord1.x+=iCoord.x*2){
if(Math.Abs(cCoord1.x)>=MaxcCoordx||
   Math.Abs(cCoord1.y)>=MaxcCoordy){
if(LOG&&LOG_LEVEL<=1)Debug.Log("do not try to expropriate out of world chunk at coord:.."+cCoord1);
goto _skip;
}
if(LOG&&LOG_LEVEL<=1)Debug.Log("try to expropriate chunk:.."+cCoord1);
if((Mathf.Abs(cCoord1.x-aCoord.x)>instantiationDistance.x||
    Mathf.Abs(cCoord1.y-aCoord.y)>instantiationDistance.y)&&players.All(p=>{return(Mathf.Abs(cCoord1.x-p.Key.cCoord.x)>instantiationDistance.x||
                                                                                   Mathf.Abs(cCoord1.y-p.Key.cCoord.y)>instantiationDistance.y);})){
int cnkIdx1=GetcnkIdx(cCoord1.x,cCoord1.y);if(ActiveTerrain.ContainsKey(cnkIdx1)){
if(LOG&&LOG_LEVEL<=1)Debug.Log("do expropriate chunk for:.."+cnkIdx1);
TerrainChunk scr=ActiveTerrain[cnkIdx1];if(scr.ExpropriationNode==null){scr.ExpropriationNode=TerrainChunkPool.AddLast(scr);
}else{
if(LOG&&LOG_LEVEL<=1)Debug.Log("but chunk is already expropriated:.."+cnkIdx1);
}
}else{
if(LOG&&LOG_LEVEL<=1)Debug.Log("no chunk to expropriate for index:.."+cnkIdx1);
}
}else{
if(LOG&&LOG_LEVEL<=1)Debug.Log("no need to expropriate chunk at:.."+cCoord1);
}

//...

_skip:{}
if(iCoord.x==0){break;}}}
if(iCoord.y==0){break;}}}
}

//...

public class BiomeBase{public bool LOG=true;public int LOG_LEVEL=1;
#region Initialize
protected readonly System.Random[]Random=new System.Random[2];
public virtual int IdxForRnd{get{return 0;}}
public virtual int IdxForHgt{get{return 4;}}//  Base Height Result Module
public int Seed{
get{return Seed_v;}
set{       Seed_v=value;
if(LOG&&LOG_LEVEL<=1)Debug.Log("Seed value.."+value);
Random[0]=new System.Random(Seed_v);
Random[1]=new System.Random(Random[0].Next());

//...

SetModules();
}
}int Seed_v;
public readonly List<ModuleBase>Modules=new List<ModuleBase>();
public ModuleBase _0{get{return Modules[0];}}
public ModuleBase _1{get{return Modules[1];}}
public ModuleBase _Neg1{get{return Modules[2];}}
public ModuleBase _Half{get{return Modules[3];}}
public ModuleBase _128{get{return Modules[4];}}
protected virtual void SetModules(){
Modules.Add(new Const( 0));
Modules.Add(new Const( 1));
Modules.Add(new Const(-1));
Modules.Add(new Const(.5));
Modules.Add(new Const(128));
if(LOG&&LOG_LEVEL<=1)Debug.Log("SetModules() at "+GetType()+" resulted in Count:"+Modules.Count);
}
#endregion 
protected virtual double density(double density,Vector3 input,double noise,float smoothing=3f){double value=density;
double delta=(noise-input.y);//  input.y sempre ser� menor ou igual a noise
if(delta<=smoothing){
double smoothingValue=(smoothing-delta)/smoothing;
value*=1d-smoothingValue;
if(value<0)
   value=0;
else if(value>100)
        value=100;
}
return value;}
protected Select[]MaterialIdSelectors=new Select[1];
protected(MaterialId,MaterialId)[]MaterialIdPicking=new(MaterialId,MaterialId)[1]{
(MaterialId.Rock,MaterialId.Dirt),
};
protected virtual MaterialId material(double density,Vector3 input,MaterialId[][][]mCache,int nbrIdx,int inputIndex){if(-density>=IsoLevel){return MaterialId.Air;}MaterialId m;
m=MaterialIdPicking[0].Item1;
return m;}
protected Vector3 _deround{get;}=new Vector3(.5f,.5f,.5f);
public virtual int cacheCount{get{return 1;}}
public virtual void result(Vector3Int vCoord,Vector3 input,double[][][]nCache,MaterialId[][][]mCache,int nbrIdx,int inputIndex,ref Voxel v){if(nCache[0][nbrIdx]==null)nCache[0][nbrIdx]=new double[FlattenOffset];if(mCache[0][nbrIdx]==null)mCache[0][nbrIdx]=new MaterialId[FlattenOffset];
                                                     input+=_deround;
double noiseValue1=nCache[0][nbrIdx][inputIndex]!=0?nCache[0][nbrIdx][inputIndex]:(nCache[0][nbrIdx][inputIndex]=Modules[IdxForHgt].GetValue(input.z,input.x,0));
if(input.y<=noiseValue1){double d;
v=new Voxel(d=density(100,input,noiseValue1),Vector3.zero,material(d,input,mCache,nbrIdx,inputIndex));return;
}
v=Voxel.Air;}
}
public class Plains:BiomeBase{
public override int IdxForRnd{get{return 1;}}
public override int IdxForHgt{get{return 5;}}//  Base Height Result Module
protected override void SetModules(){
                   base.SetModules();
#region 1
ModuleBase module1=new Const(5);
#endregion
#region 2
ModuleBase module2a=new RidgedMultifractal(frequency:Mathf.Pow(2,-8),lacunarity:2.0,octaves:6,seed:Random[IdxForRnd].Next(),quality:QualityMode.Low);
ModuleBase module2b=new Turbulence(input:module2a); 
((Turbulence)module2b).Seed=Random[IdxForRnd].Next();
((Turbulence)module2b).Frequency=Mathf.Pow(2,-2);
((Turbulence)module2b).Power=1;
ModuleBase module2c=new ScaleBias(scale:1.0,bias:30.0,input:module2b);  
#endregion
#region 3
ModuleBase module3a=new Billow(frequency:Mathf.Pow(2,-7)*1.6,lacunarity:2.0,persistence:0.5,octaves:8,seed:Random[IdxForRnd].Next(),quality:QualityMode.Low);
ModuleBase module3b=new Turbulence(input:module3a);
((Turbulence)module3b).Seed=Random[IdxForRnd].Next();
((Turbulence)module3b).Frequency=Mathf.Pow(2,-2);  
((Turbulence)module3b).Power=1.8;
ModuleBase module3c=new ScaleBias(scale:1.0,bias:31.0,input:module3b);
#endregion
#region 4
ModuleBase module4a=new Perlin(frequency:Mathf.Pow(2,-6),lacunarity:2.0,persistence:0.5,octaves:6,seed:Random[IdxForRnd].Next(),quality:QualityMode.Low);
ModuleBase module4b=new Select(inputA:module2c,inputB:module3c,controller:module4a);
((Select)module4b).SetBounds(min:-.2,max:.2);
((Select)module4b).FallOff=.25;
ModuleBase module4c=new Multiply(lhs:module4b,rhs:module1);
#endregion
Modules.Add(module4c);
MaterialIdSelectors[0]=(Select)module4b;
}
protected override MaterialId material(double density,Vector3 input,MaterialId[][][]mCache,int nbrIdx,int inputIndex){if(-density>=IsoLevel){return MaterialId.Air;}MaterialId m;
if(mCache[0][nbrIdx][inputIndex]!=0){return mCache[0][nbrIdx][inputIndex];}
double min=MaterialIdSelectors[0].Minimum;
double max=MaterialIdSelectors[0].Maximum;
double fallOff=MaterialIdSelectors[0].FallOff*.5;
var selectValue=MaterialIdSelectors[0].Controller.GetValue(input.z,input.x,0);
if(selectValue<=min-fallOff||selectValue>=max+fallOff){
m=MaterialIdPicking[0].Item2;
}else{
m=MaterialIdPicking[0].Item1;
}
return mCache[0][nbrIdx][inputIndex]=m;}
}
#if UNITY_EDITOR
public static void DrawBounds(Bounds b,Color color,float duration=0){//[https://gist.github.com/unitycoder/58f4b5d80f423d29e35c814a9556f9d9]
var p1=new Vector3(b.min.x,b.min.y,b.min.z);// bottom
var p2=new Vector3(b.max.x,b.min.y,b.min.z);
var p3=new Vector3(b.max.x,b.min.y,b.max.z);
var p4=new Vector3(b.min.x,b.min.y,b.max.z);
var p5=new Vector3(b.min.x,b.max.y,b.min.z);// top
var p6=new Vector3(b.max.x,b.max.y,b.min.z);
var p7=new Vector3(b.max.x,b.max.y,b.max.z);
var p8=new Vector3(b.min.x,b.max.y,b.max.z);
Debug.DrawLine(p1,p2,color,duration);
Debug.DrawLine(p2,p3,color,duration);
Debug.DrawLine(p3,p4,color,duration);
Debug.DrawLine(p4,p1,color,duration);
Debug.DrawLine(p5,p6,color,duration);
Debug.DrawLine(p6,p7,color,duration);
Debug.DrawLine(p7,p8,color,duration);
Debug.DrawLine(p8,p5,color,duration);
Debug.DrawLine(p1,p5,color,duration);// sides
Debug.DrawLine(p2,p6,color,duration);
Debug.DrawLine(p3,p7,color,duration);
Debug.DrawLine(p4,p8,color,duration);
}
void OnDrawGizmos(){
if(GIZMOS_ENABLED<=1){

//...

DrawBounds(bounds,Color.yellow);
}
}
#endif
}
}