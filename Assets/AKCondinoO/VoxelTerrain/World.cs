using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class World:MonoBehaviour{
public Text UI_FPS;[NonSerialized]float UI_FPS_RefreshTimer;[NonSerialized]float UI_FPS_RefreshTime=1.0f;
public const int Width=6250;
public const int Depth=6250;
public GameObject ChunkPrefab;
[SerializeField]public int targetFrameRate=60;
void Awake(){
QualitySettings.vSyncCount=0;Application.targetFrameRate=targetFrameRate;

//...

TerrainChunk.AtlasHelper.GetAtlasData(ChunkPrefab.GetComponent<MeshRenderer>().sharedMaterial);

var gO=Instantiate(ChunkPrefab);gO.GetComponent<TerrainChunk>().OncCoordChanged(new Vector2Int(0,0));
                                //gO.GetComponent<TerrainChunk>().OncCoordChanged(new Vector2Int(1,0));
    //gO=Instantiate(ChunkPrefab);gO.GetComponent<TerrainChunk>().OncCoordChanged(new Vector2Int(1,1));
//...

}
public static float FPS{
        get{float tmp;lock(FPS_Syn)tmp=FPS_v;return tmp;}
private set{          lock(FPS_Syn)    FPS_v=value;     }
}[NonSerialized]static readonly object FPS_Syn=new object();[NonSerialized]static float FPS_v;
public static float averageFramerate{
        get{float tmp;lock(averageFramerate_Syn){tmp=averageFramerate_v;      }return tmp;}
private set{          lock(averageFramerate_Syn){    averageFramerate_v=value;}           }
}[NonSerialized]static readonly object averageFramerate_Syn=new object();[NonSerialized]static float averageFramerate_v=60;[NonSerialized]int frameCounter;[NonSerialized]float averageFramerateRefreshTimer;[NonSerialized]float averageFramerateRefreshTime=1.0f;
[NonSerialized]float frameTimeVariation;[NonSerialized]float millisecondsPerFrame;
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
}
}