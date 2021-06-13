using LibNoise;
using LibNoise.Generator;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UI;
namespace AKCondinoO.Voxels{public class World:MonoBehaviour{public bool LOG=true;public int LOG_LEVEL=1;public int GIZMOS_ENABLED=1;
public Text UI_FPS;[NonSerialized]float UI_FPS_RefreshTimer;[NonSerialized]float UI_FPS_RefreshTime=1.0f;
public const int Width=6250;
public const int Depth=6250;
public static Vector2Int vecPosTocCoord(Vector3 pos){
                                                pos.x/=(float)TerrainChunk.Width;
                                                pos.z/=(float)TerrainChunk.Depth;
return new Vector2Int((pos.x>0)?(pos.x-(int)pos.x==0.5f?Mathf.FloorToInt(pos.x):Mathf.RoundToInt(pos.x)):(int)Math.Round(pos.x,MidpointRounding.AwayFromZero),
                      (pos.z>0)?(pos.z-(int)pos.z==0.5f?Mathf.FloorToInt(pos.z):Mathf.RoundToInt(pos.z)):(int)Math.Round(pos.z,MidpointRounding.AwayFromZero));
}
public GameObject ChunkPrefab;
Vector2Int expropriationDistance{get;}=new Vector2Int(1,1);[NonSerialized]readonly LinkedList<TerrainChunk>TerrainChunkPool=new LinkedList<TerrainChunk>();
Vector2Int instantiationDistance{get;}=new Vector2Int(1,1);
[NonSerialized]public static readonly BiomeBase biome=new Plains();
[SerializeField]public int targetFrameRate=60;
void Awake(){int maxChunks=(expropriationDistance.x*2+1)*(expropriationDistance.y*2+1);
GarbageCollector.GCMode=GarbageCollector.Mode.Enabled;
            
//...

if(LOG&&LOG_LEVEL<=100)Debug.Log("The number of processors on this computer is:"+Environment.ProcessorCount);
ThreadPool.GetAvailableThreads(out int worker ,out int io         );if(LOG&&LOG_LEVEL<=100){Debug.Log("Thread pool threads available at startup: Worker threads: "+worker+" Asynchronous I/O threads: "+io);}
ThreadPool.GetMaxThreads(out int workerThreads,out int portThreads);if(LOG&&LOG_LEVEL<=100){Debug.Log("Maximum worker threads: "+workerThreads+" Maximum completion port threads: "+portThreads);           }
ThreadPool.GetMinThreads(out int minWorker    ,out int minIOC     );if(LOG&&LOG_LEVEL<=100){Debug.Log("minimum number of worker threads: "+minWorker+" minimum asynchronous I/O: "+minIOC);                 }
var idealMin=(maxChunks+2+Environment.ProcessorCount);if(minWorker!=idealMin){
if(ThreadPool.SetMinThreads(idealMin,minIOC)){if(LOG&&LOG_LEVEL<=100){Debug.Log("changed minimum number of worker threads to:"+(idealMin));}
}else{                                        if(LOG&&LOG_LEVEL<=100){Debug.Log("SetMinThreads failed");                                   }
}
}
QualitySettings.vSyncCount=0;Application.targetFrameRate=targetFrameRate;

//...

TerrainChunk.AtlasHelper.GetAtlasData(ChunkPrefab.GetComponent<MeshRenderer>().sharedMaterial);
            
//...

biome.LOG=LOG;biome.LOG_LEVEL=LOG_LEVEL;biome.Seed=0;       

//...

for(int i=maxChunks-1;i>=0;--i){GameObject obj=Instantiate(ChunkPrefab);TerrainChunk scr=obj.GetComponent<TerrainChunk>();scr.ExpropriationNode=TerrainChunkPool.AddLast(scr);}

//var gO=Instantiate(ChunkPrefab);gO.GetComponent<TerrainChunk>().OncCoordChanged(new Vector2Int(0,0));
//                                //gO.GetComponent<TerrainChunk>().OncCoordChanged(new Vector2Int(1,0));
//    gO=Instantiate(ChunkPrefab);gO.GetComponent<TerrainChunk>().OncCoordChanged(new Vector2Int(0,1));
//...

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
[NonSerialized]Vector3    actPos;
[NonSerialized]Vector2Int aCoord,aCoord_Pre;
void Update(){
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
if(firstLoop||actPos!=Camera.main.transform.position){if(LOG&&LOG_LEVEL<=-110){Debug.Log("actPos anterior:.."+actPos+"..;actPos novo:.."+Camera.main.transform.position);}
              actPos=(Camera.main.transform.position);
if(firstLoop |aCoord!=(aCoord=vecPosTocCoord(actPos))){if(LOG&&LOG_LEVEL<=1){Debug.Log("aCoord novo:.."+aCoord+"..;aCoord_Pre:.."+aCoord_Pre);}

//...

aCoord_Pre=aCoord;}

//...

}

//...

firstLoop=false;}
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
protected virtual double smoothHeight(double sharpValue){double value=sharpValue;

//...

return value;}
protected Vector3 _deround{get;}=new Vector3(.5f,.5f,.5f);
public virtual void result(Vector3Int vCoord2,Vector3 noiseInput,ref double[]noiseCache1,int noiseCache1Index,ref TerrainChunk.Voxel v){if(noiseCache1==null)noiseCache1=new double[TerrainChunk.FlattenOffset];
                                                      noiseInput+=_deround;
double noiseValue1=noiseCache1[noiseCache1Index]!=0?noiseCache1[noiseCache1Index]:(noiseCache1[noiseCache1Index]=Modules[IdxForHgt].GetValue(noiseInput.z,noiseInput.x,0));
if(noiseInput.y<=noiseValue1){

//...

}

//...
                  if(vCoord2.y<=128){v=new TerrainChunk.Voxel(100,Vector3.zero,TerrainChunk.MaterialId.Dirt);return;}
    if(vCoord2.z>=1&&vCoord2.z<=4&&vCoord2.y<=132){v=new TerrainChunk.Voxel(100,Vector3.zero,TerrainChunk.MaterialId.Rock);return;}

v=TerrainChunk.Voxel.Air;}
}
public class Plains:BiomeBase{
}
}
}