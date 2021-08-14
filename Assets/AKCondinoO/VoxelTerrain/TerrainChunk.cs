using MessagePack;
using MLAPI;
using MLAPI.NetworkVariable;
using paulbourke.MarchingCubes;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using static AKCondinoO.Voxels.World;
namespace AKCondinoO.Voxels{public class TerrainChunk:NetworkBehaviour{public bool LOG=true;public int LOG_LEVEL=1;public int GIZMOS_ENABLED=1;public bool DEBUG_MODE=true;
public const ushort Height=(256);
public const ushort Width=(16);
public const ushort Depth=(16);
public const ushort FlattenOffset=(Width*Depth);
public const int VoxelsPerChunk=(FlattenOffset*Height);
public static int GetvxlIdx(int vcx,int vcy,int vcz){return vcy*FlattenOffset+vcx*Depth+vcz;}
public static Vector3Int vecPosTovCoord(Vector3 pos){
Vector2Int rgn=vecPosTocnkRgn(pos);
pos.x=(pos.x>0)?(pos.x-(int)pos.x==0.5f?Mathf.FloorToInt(pos.x):Mathf.RoundToInt(pos.x)):(int)Math.Round(pos.x,MidpointRounding.AwayFromZero);
pos.y=(pos.y>0)?(pos.y-(int)pos.y==0.5f?Mathf.FloorToInt(pos.y):Mathf.RoundToInt(pos.y)):(int)Math.Round(pos.y,MidpointRounding.AwayFromZero);
pos.z=(pos.z>0)?(pos.z-(int)pos.z==0.5f?Mathf.FloorToInt(pos.z):Mathf.RoundToInt(pos.z)):(int)Math.Round(pos.z,MidpointRounding.AwayFromZero);
Vector3Int coord=new Vector3Int((int)pos.x-rgn.x,(int)pos.y,(int)pos.z-rgn.y);
coord.x+=Mathf.FloorToInt(Width /2.0f);coord.x=Mathf.Clamp(coord.x,0,Width -1);
coord.y+=Mathf.FloorToInt(Height/2.0f);coord.y=Mathf.Clamp(coord.y,0,Height-1);
coord.z+=Mathf.FloorToInt(Depth /2.0f);coord.z=Mathf.Clamp(coord.z,0,Depth -1);
return coord;}
public static Vector2Int cCoordTocnkRgn(Vector2Int cCoord){return new Vector2Int(cCoord.x*Width,cCoord.y*Depth);}
public static Vector2Int cnkRgnTocCoord(Vector2Int cnkRgn){return new Vector2Int(cnkRgn.x/Width,cnkRgn.y/Depth);}
public static int GetcnkIdx(int cx,int cy){return cy+cx*(MaxcCoordy+1);}
#region ValidateCoord
public static void ValidateCoord(ref Vector2Int region,ref Vector3Int vxlCoord){int a,c;
a=region.x;c=vxlCoord.x;ValidateCoordAxis(ref a,ref c,Width);region.x=a;vxlCoord.x=c;
a=region.y;c=vxlCoord.z;ValidateCoordAxis(ref a,ref c,Depth);region.y=a;vxlCoord.z=c;
}
public static void ValidateCoordAxis(ref int axis,ref int coord,int axisLength){
      if(coord<0){          axis-=axisLength*Mathf.CeilToInt (Math.Abs(coord)/(float)axisLength);coord=(coord%axisLength)+axisLength;
}else if(coord>=axisLength){axis+=axisLength*Mathf.FloorToInt(Math.Abs(coord)/(float)axisLength);coord=(coord%axisLength);}
}
#endregion
/// <summary>
///  Lista de tipos de material de terreno.
/// </summary>
[Serializable]public enum MaterialId:short{
Unknown=-1,
Air=0,//  Default value
Bedrock=1,//  Indestrutível
Dirt=2,
Rock=3,
Sand=4,
}
public static class AtlasHelper{
public static readonly string[]_Shader_Input=new string[]{
"_CameraPosition",
"_FadeQuadrangularEnd",
"_FadeQuadrangularStart",
};
public static Material Material{get;private set;}
public static void GetAtlasData(Material material){Material=material;
float _U,_V;var texture=material.GetTexture("_MainTex");material.SetTexture("_MainTex1",texture);var w=texture.width;var h=texture.height;var tilesResolution=material.GetFloat("_TilesResolution"); 
var TileWidth=(w/tilesResolution);
var TileHeight=(h/tilesResolution);
_U=(TileWidth/w); //  X
_V=(TileHeight/h);//  Y
_UVs[(int)MaterialId.Sand   ]=new Vector2(2*_U,0*_V);
_UVs[(int)MaterialId.Rock   ]=new Vector2(1*_U,0*_V);
_UVs[(int)MaterialId.Dirt   ]=new Vector2(0*_U,1*_V);
_UVs[(int)MaterialId.Bedrock]=new Vector2(1*_U,1*_V);
_UVs[(int)MaterialId.Air    ]=new Vector2(0*_U,0*_V);
}
public static readonly Vector2[]_UVs=new Vector2[Enum.GetNames(typeof(MaterialId)).Length-1];
public static Vector2 GetUV(MaterialId type){switch(type){
case(MaterialId.Sand      ):{return _UVs[(int)MaterialId.Sand   ];}
case(MaterialId.Rock      ):{return _UVs[(int)MaterialId.Rock   ];}
case(MaterialId.Dirt      ):{return _UVs[(int)MaterialId.Dirt   ];}
case(MaterialId.Bedrock   ):{return _UVs[(int)MaterialId.Bedrock];}
default                    :{return _UVs[(int)MaterialId.Air    ];}
}}
public static MaterialId GetMaterialId(Vector2 uv){return(MaterialId)Array.IndexOf(_UVs,uv);}
}
public struct Voxel{
       public Voxel(double d,Vector3 n,MaterialId m){
Density=d;Normal=n;Material=m;IsCreated=true;
       }
public double Density;public Vector3 Normal;public MaterialId Material;public bool IsCreated;
public static Voxel Air    {get;}=new Voxel(  0.0,Vector3.zero,MaterialId.Air    );
public static Voxel Bedrock{get;}=new Voxel(101.0,Vector3.zero,MaterialId.Bedrock);
}
[NonSerialized]public const double IsoLevel=-50.0d;public static Vector3 TrianglePosAdj{get;}=new Vector3((Width/2.0f)-0.5f,(Height/2.0f)-0.5f,(Depth/2.0f)-0.5f);/*  Ajuste para que o mesh do chunk fique centralizado, com pivot em 0,0,0  */static Vector2 EmptyUV{get;}=new Vector2(-1,-1);
public static readonly ReadOnlyCollection<Vector3>Corners=new ReadOnlyCollection<Vector3>(new Vector3[8]{
new Vector3(-.5f,-.5f,-.5f),
new Vector3( .5f,-.5f,-.5f),
new Vector3( .5f, .5f,-.5f),
new Vector3(-.5f, .5f,-.5f),
new Vector3(-.5f,-.5f, .5f),
new Vector3( .5f,-.5f, .5f),
new Vector3( .5f, .5f, .5f),
new Vector3(-.5f, .5f, .5f),
});
[NonSerialized]NativeList<Vertex>TempVer;[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]public struct Vertex{
public Vector3 pos;
public Vector3 normal;
public Color color;
public Vector2 texCoord0;
public Vector2 texCoord1;
public Vector2 texCoord2;
public Vector2 texCoord3;
                        public Vertex(Vector3 p,Vector3 n,Vector2 uv0){
pos=p;
normal=n;
color=new Color(1f,0f,0f,0f);
texCoord0=uv0;
texCoord1=new Vector2(-1f,-1f);
texCoord2=new Vector2(-1f,-1f);
texCoord3=new Vector2(-1f,-1f);
                        }
}
[NonSerialized]NativeList<UInt32>TempTri;
[NonSerialized]public LinkedListNode<TerrainChunk>ExpropriationNode=null;
bool Stop{
get{bool tmp;lock(Stop_Syn){tmp=Stop_v;      }return tmp;}
set{         lock(Stop_Syn){    Stop_v=value;}if(value){foregroundData.Set();}}
}[NonSerialized]readonly object Stop_Syn=new object();[NonSerialized]bool Stop_v=false;[NonSerialized]readonly AutoResetEvent foregroundData=new AutoResetEvent(false);[NonSerialized]readonly ManualResetEvent backgroundData=new ManualResetEvent(true);
[NonSerialized]public static readonly object tasksBusyCount_Syn=new object();[NonSerialized]public static int tasksBusyCount=0;[NonSerialized]public static readonly AutoResetEvent queue=new AutoResetEvent(true);
[NonSerialized]public readonly object load_Syn=new object();[NonSerialized]static readonly List<object>load_Syn_All=new List<object>();//  para dar Monitor.Enter em todos os chunks envolvidos ao editar terreno
[NonSerialized]Vector2Int cCoord1;
[NonSerialized]Vector2Int cnkRgn1;
[NonSerialized]int        cnkIdx1;
[NonSerialized]readonly Voxel[]voxels=new Voxel[VoxelsPerChunk];
[NonSerialized]public NetworkObject network;
[NonSerialized]public readonly NetworkVariableVector3 networkPosition=new NetworkVariableVector3(new NetworkVariableSettings{WritePermission=NetworkVariablePermission.ServerOnly,ReadPermission=NetworkVariablePermission.Everyone,});
[NonSerialized]public Mesh mesh=null;[NonSerialized]MeshUpdateFlags meshFlags=MeshUpdateFlags.DontValidateIndices|MeshUpdateFlags.DontNotifyMeshUsers|MeshUpdateFlags.DontRecalculateBounds;[NonSerialized]public new MeshRenderer renderer=null;[NonSerialized]public new MeshCollider collider=null;[NonSerialized]public Bounds localBounds;
void Awake(){
load_Syn_All.Add(load_Syn);
network=GetComponent<NetworkObject>();
mesh=new Mesh(){bounds=localBounds=new Bounds(Vector3.zero,new Vector3(Width,Height,Depth))};gameObject.GetComponent<MeshFilter>().mesh=mesh;renderer=gameObject.GetComponent<MeshRenderer>();collider=gameObject.GetComponent<MeshCollider>();
navMeshSources[gameObject]=new NavMeshBuildSource{
transform=transform.localToWorldMatrix,
shape=NavMeshBuildSourceShape.Mesh,
sourceObject=mesh,
component=GetComponent<MeshFilter>(),
area=0,//  walkable
};
navMeshMarkups[gameObject]=new NavMeshBuildMarkup{
root=transform,
area=0,//  walkable
overrideArea=false,
ignoreFromBuild=false,
};
bakeJob=new BakerJob(){meshId=mesh.GetInstanceID(),};
TempVer=new NativeList<Vertex>(Allocator.Persistent);
TempTri=new NativeList<UInt32>(Allocator.Persistent);
}
public class TerrainChunkTask{
[NonSerialized]static readonly ConcurrentQueue<TerrainChunk>queued=new ConcurrentQueue<TerrainChunk>();[NonSerialized]static readonly AutoResetEvent enqueued=new AutoResetEvent(false);
public static void StartNew(TerrainChunk state){queued.Enqueue(state);enqueued.Set();}

//...

#region current terrain processing data
[NonSerialized]NativeList<Vertex>TempVer;
[NonSerialized]NativeList<UInt32>TempTri;
TerrainChunk current{get;set;}AutoResetEvent foregroundData{get;set;}ManualResetEvent backgroundData{get;set;}
object load_Syn{get;set;}

//...

Vector2Int cCoord1{get;set;}
Vector2Int cnkRgn1{get;set;}
int        cnkIdx1{get;set;}
Voxel[]voxels{get;set;}
bool bake{get{return current.bake;}set{current.bake=value;}}
void RenewData(TerrainChunk next){
current=next;
TempVer=current.TempVer;
TempTri=current.TempTri;
foregroundData=next.foregroundData;backgroundData=next.backgroundData;
load_Syn=next.load_Syn;

//...

cCoord1=next.cCoord1;
cnkRgn1=next.cnkRgn1;
cnkIdx1=next.cnkIdx1;
voxels=next.voxels;
}
void ReleaseData(){
foregroundData=null;backgroundData=null;
load_Syn=null;

//...

voxels=null;
current=null;
}
#endregion current terrain processing data

//...

public static bool Stop{
get{bool tmp;lock(Stop_Syn){tmp=Stop_v;      }return tmp;}
set{         lock(Stop_Syn){    Stop_v=value;}if(value){enqueued.Set();}}
}[NonSerialized]static readonly object Stop_Syn=new object();[NonSerialized]static bool Stop_v=false;[NonSerialized]readonly Task task;public void Wait(){try{task.Wait();}catch(Exception e){Debug.LogError(e?.Message+"\n"+e?.StackTrace+"\n"+e?.Source);}}
public TerrainChunkTask(bool LOG,int LOG_LEVEL){
task=Task.Factory.StartNew(BG,new object[]{LOG,LOG_LEVEL,savePath,},TaskCreationOptions.LongRunning);
void BG(object state){Thread.CurrentThread.IsBackground=false;Thread.CurrentThread.Priority=System.Threading.ThreadPriority.BelowNormal;
try{
if(state is object[]parameters&&parameters[0]is bool LOG&&parameters[1]is int LOG_LEVEL&&parameters[2]is string savePath){
if(LOG&&LOG_LEVEL<=1)Debug.Log("inicializar trabalho em plano de fundo para pedaço de terreno");
var watch=new System.Diagnostics.Stopwatch();
Voxel[]polygonCell=new Voxel[8];Voxel[]tmpVxl=new Voxel[6];Vector3 polygonCellNormal;

//...

double[][][]nCache=new double[biome.cacheCount][][];MaterialId[][][]mCache=new MaterialId[biome.cacheCount][][];for(int i=0;i<biome.cacheCount;++i){nCache[i]=new double[9][];mCache[i]=new MaterialId[9][];}
Voxel[][][]voxelsBuffer1=new Voxel[3][][]{new Voxel[1][]{new Voxel[4],},new Voxel[Depth][],new Voxel[FlattenOffset][],};for(int i=0;i<voxelsBuffer1[2].Length;++i){voxelsBuffer1[2][i]=new Voxel[4];if(i<voxelsBuffer1[1].Length){voxelsBuffer1[1][i]=new Voxel[4];}}Voxel[][]voxelsBuffer2=new Voxel[3][]{new Voxel[1],new Voxel[Depth],new Voxel[FlattenOffset],};
Vector3[][][]verticesBuffer=new Vector3[3][][]{new Vector3[1][]{new Vector3[4],},new Vector3[Depth][],new Vector3[FlattenOffset][],};for(int i=0;i<verticesBuffer[2].Length;++i){verticesBuffer[2][i]=new Vector3[4];if(i<verticesBuffer[1].Length){verticesBuffer[1][i]=new Vector3[4];}}
MaterialId[]materials=new MaterialId[12];
   Vector3[] vertices=new Vector3[12];
   Vector3[]  normals=new Vector3[12];
double[]density=new double[2];Vector3[]vertex=new Vector3[2];MaterialId[]material=new MaterialId[2];float[]distance=new float[2];
int[]idx=new int[3];Vector3[]verPos=new Vector3[3];Dictionary<Vector3,List<Vector2>>UVByVertex=new Dictionary<Vector3,List<Vector2>>();Dictionary<int,int>weights=new Dictionary<int,int>(4);
int GetoftIdx(Vector2Int offset){
if(offset.x== 0&&offset.y== 0)return 0;
if(offset.x==-1&&offset.y== 0)return 1;
if(offset.x== 1&&offset.y== 0)return 2;
if(offset.x== 0&&offset.y==-1)return 3;
if(offset.x==-1&&offset.y==-1)return 4;
if(offset.x== 1&&offset.y==-1)return 5;
if(offset.x== 0&&offset.y== 1)return 6;
if(offset.x==-1&&offset.y== 1)return 7;
if(offset.x== 1&&offset.y== 1)return 8;
return -1;}
var neighbors=new Dictionary<int,Voxel>[8];for(int i=0;i<neighbors.Length;i++){neighbors[i]=new Dictionary<int,Voxel>();}
while(!Stop){enqueued.WaitOne();if(Stop){enqueued.Set();goto _Stop;}if(queued.TryDequeue(out TerrainChunk dequeued)){RenewData(dequeued);}else{continue;};if(queued.Count>0){enqueued.Set();}foregroundData.WaitOne();lock(tasksBusyCount_Syn){tasksBusyCount++;}queue.WaitOne(tasksBusyCount*5000);
if(LOG&&LOG_LEVEL<=1){Debug.Log("começar nova atualização deste pedaço do terreno:"+cCoord1);watch.Restart();}
Array.Clear(voxels,0,voxels.Length);
TempVer.Clear();
TempTri.Clear();

//...

lock(load_Syn){

//...

string editsFolder=string.Format("{0}{1}",savePath,cnkIdx1);string editsFile=string.Format("{0}/{1}",editsFolder,"terrainEdits.MessagePack");
if(LOG&&LOG_LEVEL<=1)Debug.Log("editsFolder.."+editsFolder+"..e editsFile.."+editsFile+"..para:.."+cCoord1);
if(File.Exists(editsFile)){
using(FileStream file=new FileStream(editsFile,FileMode.Open,FileAccess.Read,FileShare.Read)){
var edits=MessagePackSerializer.Deserialize(typeof(Dictionary<Vector3Int,(double density,MaterialId materialId)>),file)as Dictionary<Vector3Int,(double density,MaterialId materialId)>;
foreach(var edit in edits){
voxels[GetvxlIdx(edit.Key.x,edit.Key.y,edit.Key.z)]=new Voxel(edit.Value.density,Vector3.zero,edit.Value.materialId);

//... Debug.LogWarning(edit);

}
}
}

//...

for(int x=-1;x<=1;x++){
for(int z=-1;z<=1;z++){
if(x==0&&z==0)continue;
Vector2Int nCoord1=cCoord1;nCoord1.x+=x;nCoord1.y+=z;
if(Math.Abs(nCoord1.x)>=MaxcCoordx||
   Math.Abs(nCoord1.y)>=MaxcCoordy){continue;}
int ngbIdx1=GetcnkIdx(nCoord1.x,nCoord1.y);int oftIdx1=GetoftIdx(nCoord1-cCoord1)-1;
string nEditsFolder=string.Format("{0}{1}",savePath,ngbIdx1);string nEditsFile=string.Format("{0}/{1}",nEditsFolder,"terrainEdits.MessagePack");
if(File.Exists(nEditsFile)){
using(FileStream file=new FileStream(nEditsFile,FileMode.Open,FileAccess.Read,FileShare.Read)){
var edits=MessagePackSerializer.Deserialize(typeof(Dictionary<Vector3Int,(double density,MaterialId materialId)>),file)as Dictionary<Vector3Int,(double density,MaterialId materialId)>;
foreach(var edit in edits){
neighbors[oftIdx1][GetvxlIdx(edit.Key.x,edit.Key.y,edit.Key.z)]=new Voxel(edit.Value.density,Vector3.zero,edit.Value.materialId);

//...

}
}
}
}
}
}
ushort vertexCount=0;
Vector2Int posOffset=Vector2Int.zero;
Vector2Int crdOffset=Vector2Int.zero;
Vector3Int vCoord1;
for(vCoord1=new Vector3Int();vCoord1.y<Height;vCoord1.y++){
for(vCoord1.x=0             ;vCoord1.x<Width ;vCoord1.x++){
for(vCoord1.z=0             ;vCoord1.z<Depth ;vCoord1.z++){
int corner=0;Vector3Int vCoord2=vCoord1;                        if(vCoord1.z>0)polygonCell[corner]=voxelsBuffer1[0][0][0];else if(vCoord1.x>0)polygonCell[corner]=voxelsBuffer1[1][vCoord1.z][0];else if(vCoord1.y>0)polygonCell[corner]=voxelsBuffer1[2][vCoord1.z+vCoord1.x*Depth][0];else SetpolygonCellVoxel();
corner++;vCoord2=vCoord1;vCoord2.x+=1;                          if(vCoord1.z>0)polygonCell[corner]=voxelsBuffer1[0][0][1];                                                                       else if(vCoord1.y>0)polygonCell[corner]=voxelsBuffer1[2][vCoord1.z+vCoord1.x*Depth][1];else SetpolygonCellVoxel();
corner++;vCoord2=vCoord1;vCoord2.x+=1;vCoord2.y+=1;             if(vCoord1.z>0)polygonCell[corner]=voxelsBuffer1[0][0][2];                                                                                                                                                              else SetpolygonCellVoxel();
corner++;vCoord2=vCoord1;             vCoord2.y+=1;             if(vCoord1.z>0)polygonCell[corner]=voxelsBuffer1[0][0][3];else if(vCoord1.x>0)polygonCell[corner]=voxelsBuffer1[1][vCoord1.z][1];                                                                                       else SetpolygonCellVoxel();
corner++;vCoord2=vCoord1;                          vCoord2.z+=1;                                                               if(vCoord1.x>0)polygonCell[corner]=voxelsBuffer1[1][vCoord1.z][2];else if(vCoord1.y>0)polygonCell[corner]=voxelsBuffer1[2][vCoord1.z+vCoord1.x*Depth][2];else SetpolygonCellVoxel();
corner++;vCoord2=vCoord1;vCoord2.x+=1;             vCoord2.z+=1;                                                                                                                                      if(vCoord1.y>0)polygonCell[corner]=voxelsBuffer1[2][vCoord1.z+vCoord1.x*Depth][3];else SetpolygonCellVoxel();
corner++;vCoord2=vCoord1;vCoord2.x+=1;vCoord2.y+=1;vCoord2.z+=1;                                                                                                                                                                                                                             SetpolygonCellVoxel();
corner++;vCoord2=vCoord1;             vCoord2.y+=1;vCoord2.z+=1;                                                               if(vCoord1.x>0)polygonCell[corner]=voxelsBuffer1[1][vCoord1.z][3];                                                                                       else SetpolygonCellVoxel();
voxelsBuffer1[0][0][0]=polygonCell[4];
voxelsBuffer1[0][0][1]=polygonCell[5];
voxelsBuffer1[0][0][2]=polygonCell[6];
voxelsBuffer1[0][0][3]=polygonCell[7];
voxelsBuffer1[1][vCoord1.z][0]=polygonCell[1];
voxelsBuffer1[1][vCoord1.z][1]=polygonCell[2];
voxelsBuffer1[1][vCoord1.z][2]=polygonCell[5];
voxelsBuffer1[1][vCoord1.z][3]=polygonCell[6];
voxelsBuffer1[2][vCoord1.z+vCoord1.x*Depth][0]=polygonCell[3];
voxelsBuffer1[2][vCoord1.z+vCoord1.x*Depth][1]=polygonCell[2];
voxelsBuffer1[2][vCoord1.z+vCoord1.x*Depth][2]=polygonCell[7];
voxelsBuffer1[2][vCoord1.z+vCoord1.x*Depth][3]=polygonCell[6];
void SetpolygonCellVoxel(){
if(vCoord2.y<=0){polygonCell[corner]=Voxel.Bedrock;//  fora do mundo, baixo
}else if(vCoord2.y>=Height){polygonCell[corner]=Voxel.Air;//  fora do mundo, cima
}else{
Vector2Int cnkRgn2=cnkRgn1;
Vector2Int cCoord2=cCoord1;
if(vCoord2.x<0||vCoord2.x>=Width||
   vCoord2.z<0||vCoord2.z>=Depth){ValidateCoord(ref cnkRgn2,ref vCoord2);cCoord2=cnkRgnTocCoord(cnkRgn2);}
       int vxlIdx2=GetvxlIdx(vCoord2.x,vCoord2.y,vCoord2.z);
              int oftIdx2=GetoftIdx(cCoord2-cCoord1);
if(oftIdx2==0&&voxels[vxlIdx2].IsCreated){polygonCell[corner]=voxels[vxlIdx2];}else if(oftIdx2>0&&neighbors[oftIdx2-1].ContainsKey(vxlIdx2)){polygonCell[corner]=neighbors[oftIdx2-1][vxlIdx2];//  já construído
}else{//  pegar valor do bioma
Vector3 noiseInput=vCoord2;noiseInput.x+=cnkRgn2.x;
                           noiseInput.z+=cnkRgn2.y;
biome.result(vCoord2,noiseInput,nCache,mCache,oftIdx2,vCoord2.z+vCoord2.x*Depth,ref polygonCell[corner]);
}
if(polygonCell[corner].Material!=MaterialId.Air&&polygonCell[corner].Normal==Vector3.zero){//  calcular normal
int tmpIdx=0;Vector3Int vCoord3=vCoord2;vCoord3.x++;                                                                                                                                                                SetpolygonCellNormalSettmpVxl();
tmpIdx++;vCoord3=vCoord2;               vCoord3.x--;if(vCoord2.z>1&&vCoord2.x>1&&vCoord2.y>1&&voxelsBuffer2[1][vCoord2.z].IsCreated)                tmpVxl[tmpIdx]=voxelsBuffer2[1][vCoord2.z];                else SetpolygonCellNormalSettmpVxl();
tmpIdx++;vCoord3=vCoord2;               vCoord3.y++;                                                                                                                                                                SetpolygonCellNormalSettmpVxl();
tmpIdx++;vCoord3=vCoord2;               vCoord3.y--;if(vCoord2.z>1&&vCoord2.x>1&&vCoord2.y>1&&voxelsBuffer2[2][vCoord2.z+vCoord2.x*Depth].IsCreated)tmpVxl[tmpIdx]=voxelsBuffer2[2][vCoord2.z+vCoord2.x*Depth];else SetpolygonCellNormalSettmpVxl();
tmpIdx++;vCoord3=vCoord2;               vCoord3.z++;                                                                                                                                                                SetpolygonCellNormalSettmpVxl();
tmpIdx++;vCoord3=vCoord2;               vCoord3.z--;if(vCoord2.z>1&&vCoord2.x>1&&vCoord2.y>1&&voxelsBuffer2[0][0].IsCreated)                        tmpVxl[tmpIdx]=voxelsBuffer2[0][0];                        else SetpolygonCellNormalSettmpVxl();
void SetpolygonCellNormalSettmpVxl(){
    if(vCoord3.y<=0){tmpVxl[tmpIdx]=Voxel.Bedrock;
    }else if(vCoord3.y>=Height){tmpVxl[tmpIdx]=Voxel.Air;
    }else{
    Vector2Int cnkRgn3=cnkRgn2;
    Vector2Int cCoord3=cCoord2;
    if(vCoord3.x<0||vCoord3.x>=Width||
       vCoord3.z<0||vCoord3.z>=Depth){ValidateCoord(ref cnkRgn3,ref vCoord3);cCoord3=cnkRgnTocCoord(cnkRgn3);}
           int vxlIdx3=GetvxlIdx(vCoord3.x,vCoord3.y,vCoord3.z);
                  int oftIdx3=GetoftIdx(cCoord3-cCoord1);
    if(oftIdx3==0&&voxels[vxlIdx3].IsCreated){tmpVxl[tmpIdx]=voxels[vxlIdx3];}else if(oftIdx3>0&&neighbors[oftIdx3-1].ContainsKey(vxlIdx3)){tmpVxl[tmpIdx]=neighbors[oftIdx3-1][vxlIdx3];
    }else{
    Vector3 noiseInput=vCoord3;noiseInput.x+=cnkRgn3.x;
                               noiseInput.z+=cnkRgn3.y;
    biome.result(vCoord3,noiseInput,nCache,mCache,oftIdx3,vCoord3.z+vCoord3.x*Depth,ref tmpVxl[tmpIdx]);
    }
    if(oftIdx3==0){voxels[vxlIdx3]=tmpVxl[tmpIdx];}else if(oftIdx3>0){neighbors[oftIdx3-1][vxlIdx3]=tmpVxl[tmpIdx];}
    }
}
polygonCellNormal=new Vector3{
x=(float)(tmpVxl[1].Density-tmpVxl[0].Density),
y=(float)(tmpVxl[3].Density-tmpVxl[2].Density),
z=(float)(tmpVxl[5].Density-tmpVxl[4].Density)};
polygonCell[corner].Normal=polygonCellNormal;
if(polygonCell[corner].Normal!=Vector3.zero){
polygonCell[corner].Normal.Normalize();
}
if(oftIdx2==0){voxels[vxlIdx2]=polygonCell[corner];}else if(oftIdx2>0){neighbors[oftIdx2-1][vxlIdx2]=polygonCell[corner];}//  salvar valor construído no chunk
}
voxelsBuffer2[0][0]=polygonCell[corner];
voxelsBuffer2[1][vCoord2.z]=polygonCell[corner];
voxelsBuffer2[2][vCoord2.z+vCoord2.x*Depth]=polygonCell[corner];
}
}
#region MarchingCubes
int edgeIndex;
/*
    Determine the index into the edge table which
    tells us which vertices are inside of the surface
*/
                                    edgeIndex =  0;
if(-polygonCell[0].Density<IsoLevel)edgeIndex|=  1;
if(-polygonCell[1].Density<IsoLevel)edgeIndex|=  2;
if(-polygonCell[2].Density<IsoLevel)edgeIndex|=  4;
if(-polygonCell[3].Density<IsoLevel)edgeIndex|=  8;
if(-polygonCell[4].Density<IsoLevel)edgeIndex|= 16;
if(-polygonCell[5].Density<IsoLevel)edgeIndex|= 32;
if(-polygonCell[6].Density<IsoLevel)edgeIndex|= 64;
if(-polygonCell[7].Density<IsoLevel)edgeIndex|=128;
    if(Tables.EdgeTable[edgeIndex]!=0){/*  Cube is not entirely in/out of the surface  */
//  Use buffered data if available
vertices[ 0]=(vCoord1.z>0?verticesBuffer[0][0][0]:(vCoord1.y>0?verticesBuffer[2][vCoord1.z+vCoord1.x*Depth][0]:Vector3.zero));
vertices[ 1]=(vCoord1.z>0?verticesBuffer[0][0][1]:Vector3.zero);
vertices[ 2]=(vCoord1.z>0?verticesBuffer[0][0][2]:Vector3.zero);
vertices[ 3]=(vCoord1.z>0?verticesBuffer[0][0][3]:(vCoord1.x>0?verticesBuffer[1][vCoord1.z][0]:Vector3.zero));
vertices[ 4]=(vCoord1.y>0?verticesBuffer[2][vCoord1.z+vCoord1.x*Depth][1]:Vector3.zero);
vertices[ 7]=(vCoord1.x>0?verticesBuffer[1][vCoord1.z][1]:Vector3.zero);
vertices[ 8]=(vCoord1.x>0?verticesBuffer[1][vCoord1.z][2]:(vCoord1.y>0?verticesBuffer[2][vCoord1.z+vCoord1.x*Depth][3]:Vector3.zero));
vertices[ 9]=(vCoord1.y>0?verticesBuffer[2][vCoord1.z+vCoord1.x*Depth][2]:Vector3.zero);
vertices[11]=(vCoord1.x>0?verticesBuffer[1][vCoord1.z][3]:Vector3.zero);
if(0!=(Tables.EdgeTable[edgeIndex]&   1)){vertexInterp(0,1,ref vertices[ 0],ref normals[ 0],ref materials[ 0]);}
if(0!=(Tables.EdgeTable[edgeIndex]&   2)){vertexInterp(1,2,ref vertices[ 1],ref normals[ 1],ref materials[ 1]);}
if(0!=(Tables.EdgeTable[edgeIndex]&   4)){vertexInterp(2,3,ref vertices[ 2],ref normals[ 2],ref materials[ 2]);}
if(0!=(Tables.EdgeTable[edgeIndex]&   8)){vertexInterp(3,0,ref vertices[ 3],ref normals[ 3],ref materials[ 3]);}
if(0!=(Tables.EdgeTable[edgeIndex]&  16)){vertexInterp(4,5,ref vertices[ 4],ref normals[ 4],ref materials[ 4]);}
if(0!=(Tables.EdgeTable[edgeIndex]&  32)){vertexInterp(5,6,ref vertices[ 5],ref normals[ 5],ref materials[ 5]);}
if(0!=(Tables.EdgeTable[edgeIndex]&  64)){vertexInterp(6,7,ref vertices[ 6],ref normals[ 6],ref materials[ 6]);}
if(0!=(Tables.EdgeTable[edgeIndex]& 128)){vertexInterp(7,4,ref vertices[ 7],ref normals[ 7],ref materials[ 7]);}
if(0!=(Tables.EdgeTable[edgeIndex]& 256)){vertexInterp(0,4,ref vertices[ 8],ref normals[ 8],ref materials[ 8]);}
if(0!=(Tables.EdgeTable[edgeIndex]& 512)){vertexInterp(1,5,ref vertices[ 9],ref normals[ 9],ref materials[ 9]);}
if(0!=(Tables.EdgeTable[edgeIndex]&1024)){vertexInterp(2,6,ref vertices[10],ref normals[10],ref materials[10]);}
if(0!=(Tables.EdgeTable[edgeIndex]&2048)){vertexInterp(3,7,ref vertices[11],ref normals[11],ref materials[11]);}
void vertexInterp(int c0,int c1,ref Vector3 p,ref Vector3 n,ref MaterialId m){
density[0]=-polygonCell[c0].Density;vertex[0]=Corners[c0];material[0]=polygonCell[c0].Material;
density[1]=-polygonCell[c1].Density;vertex[1]=Corners[c1];material[1]=polygonCell[c1].Material;
if(Math.Abs(IsoLevel-density[0])<double.Epsilon){p=vertex[0];goto _Normal;}
if(Math.Abs(IsoLevel-density[1])<double.Epsilon){p=vertex[1];goto _Normal;}
if(Math.Abs(density[0]-density[1])<double.Epsilon){p=vertex[0];goto _Normal;}
double marchingUnit=(IsoLevel-density[0])/(density[1]-density[0]);
p.x=(float)(vertex[0].x+marchingUnit*(vertex[1].x-vertex[0].x));
p.y=(float)(vertex[0].y+marchingUnit*(vertex[1].y-vertex[0].y));
p.z=(float)(vertex[0].z+marchingUnit*(vertex[1].z-vertex[0].z));
_Normal:{
distance[0]=Vector3.Distance(vertex[0],vertex[1]);
distance[1]=Vector3.Distance(vertex[1],p);
n=Vector3.Lerp(
polygonCell[c1].Normal,
polygonCell[c0].Normal,distance[1]/distance[0]);
n=n!=Vector3.zero?n.normalized:Vector3.down;
}
m=material[0];if(density[1]<density[0]){m=material[1];}else if(density[1]==density[0]&&(int)material[1]>(int)material[0]){m=material[1];}
}
/*  Create the triangle  */
for(int i=0;Tables.TriangleTable[edgeIndex][i]!=-1;i+=3){idx[0]=Tables.TriangleTable[edgeIndex][i  ];
                                                         idx[1]=Tables.TriangleTable[edgeIndex][i+1];
                                                         idx[2]=Tables.TriangleTable[edgeIndex][i+2];
                                                         Vector3 pos=vCoord1-TrianglePosAdj;pos.x+=posOffset.x;
                                                                                            pos.z+=posOffset.y;
                                                              Vector2 materialUV=AtlasHelper.GetUV((MaterialId)Mathf.Max((int)materials[idx[0]],
                                                                                                                         (int)materials[idx[1]],
                                                                                                                         (int)materials[idx[2]]));
TempVer.Add(new Vertex(verPos[0]=pos+vertices[idx[0]],normals[idx[0]],materialUV));if(!UVByVertex.ContainsKey(verPos[0])){UVByVertex.Add(verPos[0],new List<Vector2>());}UVByVertex[verPos[0]].Add(materialUV);
TempVer.Add(new Vertex(verPos[1]=pos+vertices[idx[1]],normals[idx[1]],materialUV));if(!UVByVertex.ContainsKey(verPos[1])){UVByVertex.Add(verPos[1],new List<Vector2>());}UVByVertex[verPos[1]].Add(materialUV);
TempVer.Add(new Vertex(verPos[2]=pos+vertices[idx[2]],normals[idx[2]],materialUV));if(!UVByVertex.ContainsKey(verPos[2])){UVByVertex.Add(verPos[2],new List<Vector2>());}UVByVertex[verPos[2]].Add(materialUV);
TempTri.Add((ushort)(vertexCount+2));
TempTri.Add((ushort)(vertexCount+1));
TempTri.Add(         vertexCount   );
vertexCount+=3;}
//  Buffer the data
verticesBuffer[0][0][0]=vertices[ 4]+Vector3.back;//  Adiciona um valor "negativo" porque o voxelCoord próximo vai usar esse valor mas precisa obter "uma posição anterior"
verticesBuffer[0][0][1]=vertices[ 5]+Vector3.back;
verticesBuffer[0][0][2]=vertices[ 6]+Vector3.back;
verticesBuffer[0][0][3]=vertices[ 7]+Vector3.back;
verticesBuffer[1][vCoord1.z][0]=vertices[ 1]+Vector3.left;
verticesBuffer[1][vCoord1.z][1]=vertices[ 5]+Vector3.left;
verticesBuffer[1][vCoord1.z][2]=vertices[ 9]+Vector3.left;
verticesBuffer[1][vCoord1.z][3]=vertices[10]+Vector3.left;
verticesBuffer[2][vCoord1.z+vCoord1.x*Depth][0]=vertices[ 2]+Vector3.down;
verticesBuffer[2][vCoord1.z+vCoord1.x*Depth][1]=vertices[ 6]+Vector3.down;
verticesBuffer[2][vCoord1.z+vCoord1.x*Depth][2]=vertices[10]+Vector3.down;
verticesBuffer[2][vCoord1.z+vCoord1.x*Depth][3]=vertices[11]+Vector3.down;
    }
#endregion
}}}
for(crdOffset.y=0,
    posOffset.y=0,
    vCoord1.y=0;vCoord1.y<Height;vCoord1.y++){
for(vCoord1.z=0;vCoord1.z<Depth ;vCoord1.z++){
    vCoord1.x=0;
    crdOffset.x=1;
    posOffset.x=Width;
GetEdgeUVs();
    vCoord1.x=Width-1;
    crdOffset.x=-1;
    posOffset.x=-Width;
GetEdgeUVs();
}}
for(crdOffset.x=0,
    posOffset.x=0,
    vCoord1.y=0;vCoord1.y<Height;vCoord1.y++){
for(vCoord1.x=0;vCoord1.x<Width ;vCoord1.x++){
    vCoord1.z=0;
    crdOffset.y=1;
    posOffset.y=Depth;
GetEdgeUVs();
    vCoord1.z=Depth-1;
    crdOffset.y=-1;
    posOffset.y=-Depth;
GetEdgeUVs();
}}
void GetEdgeUVs(){
    int corner=0;Vector3Int vCoord2=vCoord1;                        SetpolygonCellVoxel();
    corner++;vCoord2=vCoord1;vCoord2.x+=1;                          SetpolygonCellVoxel();
    corner++;vCoord2=vCoord1;vCoord2.x+=1;vCoord2.y+=1;             SetpolygonCellVoxel();
    corner++;vCoord2=vCoord1;             vCoord2.y+=1;             SetpolygonCellVoxel();
    corner++;vCoord2=vCoord1;                          vCoord2.z+=1;SetpolygonCellVoxel();
    corner++;vCoord2=vCoord1;vCoord2.x+=1;             vCoord2.z+=1;SetpolygonCellVoxel();
    corner++;vCoord2=vCoord1;vCoord2.x+=1;vCoord2.y+=1;vCoord2.z+=1;SetpolygonCellVoxel();
    corner++;vCoord2=vCoord1;             vCoord2.y+=1;vCoord2.z+=1;SetpolygonCellVoxel();
void SetpolygonCellVoxel(){
    if(vCoord2.y<=0){polygonCell[corner]=Voxel.Bedrock;//  fora do mundo, baixo
    }else if(vCoord2.y>=Height){polygonCell[corner]=Voxel.Air;//  fora do mundo, cima
    }else{
    Vector2Int cnkRgn2=cnkRgn1+posOffset;
    Vector2Int cCoord2=cCoord1+crdOffset;
    if(vCoord2.x<0||vCoord2.x>=Width||
       vCoord2.z<0||vCoord2.z>=Depth){ValidateCoord(ref cnkRgn2,ref vCoord2);cCoord2=cnkRgnTocCoord(cnkRgn2);}
           int vxlIdx2=GetvxlIdx(vCoord2.x,vCoord2.y,vCoord2.z);
                  int oftIdx2=GetoftIdx(cCoord2-cCoord1);
    if(oftIdx2==0&&voxels[vxlIdx2].IsCreated){polygonCell[corner]=voxels[vxlIdx2];}else if(oftIdx2>0&&neighbors[oftIdx2-1].ContainsKey(vxlIdx2)){polygonCell[corner]=neighbors[oftIdx2-1][vxlIdx2];
    }else{
    Vector3 noiseInput=vCoord2;noiseInput.x+=cnkRgn2.x;
                               noiseInput.z+=cnkRgn2.y;
    biome.result(vCoord2,noiseInput,nCache,mCache,oftIdx2,vCoord2.z+vCoord2.x*Depth,ref polygonCell[corner]);
    }
    }
}
#region MarchingCubes[UVs Only]
    int edgeIndex;
    /*
        Determine the index into the edge table which
        tells us which vertices are inside of the surface
    */
                                        edgeIndex =  0;
    if(-polygonCell[0].Density<IsoLevel)edgeIndex|=  1;
    if(-polygonCell[1].Density<IsoLevel)edgeIndex|=  2;
    if(-polygonCell[2].Density<IsoLevel)edgeIndex|=  4;
    if(-polygonCell[3].Density<IsoLevel)edgeIndex|=  8;
    if(-polygonCell[4].Density<IsoLevel)edgeIndex|= 16;
    if(-polygonCell[5].Density<IsoLevel)edgeIndex|= 32;
    if(-polygonCell[6].Density<IsoLevel)edgeIndex|= 64;
    if(-polygonCell[7].Density<IsoLevel)edgeIndex|=128;
        if(Tables.EdgeTable[edgeIndex]!=0){
    if(0!=(Tables.EdgeTable[edgeIndex]&   1)){vertexInterp(0,1,ref vertices[ 0],ref materials[ 0]);}
    if(0!=(Tables.EdgeTable[edgeIndex]&   2)){vertexInterp(1,2,ref vertices[ 1],ref materials[ 1]);}
    if(0!=(Tables.EdgeTable[edgeIndex]&   4)){vertexInterp(2,3,ref vertices[ 2],ref materials[ 2]);}
    if(0!=(Tables.EdgeTable[edgeIndex]&   8)){vertexInterp(3,0,ref vertices[ 3],ref materials[ 3]);}
    if(0!=(Tables.EdgeTable[edgeIndex]&  16)){vertexInterp(4,5,ref vertices[ 4],ref materials[ 4]);}
    if(0!=(Tables.EdgeTable[edgeIndex]&  32)){vertexInterp(5,6,ref vertices[ 5],ref materials[ 5]);}
    if(0!=(Tables.EdgeTable[edgeIndex]&  64)){vertexInterp(6,7,ref vertices[ 6],ref materials[ 6]);}
    if(0!=(Tables.EdgeTable[edgeIndex]& 128)){vertexInterp(7,4,ref vertices[ 7],ref materials[ 7]);}
    if(0!=(Tables.EdgeTable[edgeIndex]& 256)){vertexInterp(0,4,ref vertices[ 8],ref materials[ 8]);}
    if(0!=(Tables.EdgeTable[edgeIndex]& 512)){vertexInterp(1,5,ref vertices[ 9],ref materials[ 9]);}
    if(0!=(Tables.EdgeTable[edgeIndex]&1024)){vertexInterp(2,6,ref vertices[10],ref materials[10]);}
    if(0!=(Tables.EdgeTable[edgeIndex]&2048)){vertexInterp(3,7,ref vertices[11],ref materials[11]);}
void vertexInterp(int c0,int c1,ref Vector3 p,ref MaterialId m){
    density[0]=-polygonCell[c0].Density;vertex[0]=Corners[c0];material[0]=polygonCell[c0].Material;
    density[1]=-polygonCell[c1].Density;vertex[1]=Corners[c1];material[1]=polygonCell[c1].Material;
    if(Math.Abs(IsoLevel-density[0])<double.Epsilon){p=vertex[0];goto _Material;}
    if(Math.Abs(IsoLevel-density[1])<double.Epsilon){p=vertex[1];goto _Material;}
    if(Math.Abs(density[0]-density[1])<double.Epsilon){p=vertex[0];goto _Material;}
    double marchingUnit=(IsoLevel-density[0])/(density[1]-density[0]);
    p.x=(float)(vertex[0].x+marchingUnit*(vertex[1].x-vertex[0].x));
    p.y=(float)(vertex[0].y+marchingUnit*(vertex[1].y-vertex[0].y));
    p.z=(float)(vertex[0].z+marchingUnit*(vertex[1].z-vertex[0].z));
_Material:{
    m=material[0];if(density[1]<density[0]){m=material[1];}else if(density[1]==density[0]&&(int)material[1]>(int)material[0]){m=material[1];}
}
}
/*  Create the triangle  */
    for(int i=0;Tables.TriangleTable[edgeIndex][i]!=-1;i+=3){idx[0]=Tables.TriangleTable[edgeIndex][i  ];
                                                             idx[1]=Tables.TriangleTable[edgeIndex][i+1];
                                                             idx[2]=Tables.TriangleTable[edgeIndex][i+2];
                                                             Vector3 pos=vCoord1-TrianglePosAdj;pos.x+=posOffset.x;
                                                                                                pos.z+=posOffset.y;
                                                             Vector2 materialUV=AtlasHelper.GetUV((MaterialId)Mathf.Max((int)materials[idx[0]],
                                                                                                                        (int)materials[idx[1]],
                                                                                                                        (int)materials[idx[2]]));
                           verPos[0]=pos+vertices[idx[0]]                             ;if(!UVByVertex.ContainsKey(verPos[0])){UVByVertex.Add(verPos[0],new List<Vector2>());}UVByVertex[verPos[0]].Add(materialUV);
                           verPos[1]=pos+vertices[idx[1]]                             ;if(!UVByVertex.ContainsKey(verPos[1])){UVByVertex.Add(verPos[1],new List<Vector2>());}UVByVertex[verPos[1]].Add(materialUV);
                           verPos[2]=pos+vertices[idx[2]]                             ;if(!UVByVertex.ContainsKey(verPos[2])){UVByVertex.Add(verPos[2],new List<Vector2>());}UVByVertex[verPos[2]].Add(materialUV);
    }
        }
#endregion
}
for(int i=0;i<TempVer.Length/3;i++){idx[0]=i*3;idx[1]=i*3+1;idx[2]=i*3+2;for(int j=0;j<3;j++){
var MaterialIdGroupingOrdered=UVByVertex[verPos[j]=TempVer[idx[j]].pos].ToArray().Select(uv=>{return AtlasHelper.GetMaterialId(uv);}).GroupBy(value=>value).OrderByDescending(group=>group.Key).ThenByDescending(group=>group.Count());weights.Clear();int total=0;
Vector2 uv0=TempVer[idx[j]].texCoord0;foreach(var MaterialIdGroup in MaterialIdGroupingOrdered){bool add;                           
Vector2 uv=AtlasHelper.GetUV(MaterialIdGroup.First());
if(uv0==uv){
total+=weights[0]=MaterialIdGroup.Count();
}else if(((add=TempVer[idx[j]].texCoord1==EmptyUV)&&TempVer[idx[j]].texCoord2!=uv&&TempVer[idx[j]].texCoord3!=uv)||TempVer[idx[j]].texCoord1==uv){
if(add){var v1=TempVer[idx[0]];v1.texCoord1=uv;TempVer[idx[0]]=v1;
            v1=TempVer[idx[1]];v1.texCoord1=uv;TempVer[idx[1]]=v1;
            v1=TempVer[idx[2]];v1.texCoord1=uv;TempVer[idx[2]]=v1;
}
total+=weights[1]=MaterialIdGroup.Count();
}else if(((add=TempVer[idx[j]].texCoord2==EmptyUV)&&TempVer[idx[j]].texCoord3!=uv                               )||TempVer[idx[j]].texCoord2==uv){
if(add){var v1=TempVer[idx[0]];v1.texCoord2=uv;TempVer[idx[0]]=v1;
            v1=TempVer[idx[1]];v1.texCoord2=uv;TempVer[idx[1]]=v1;
            v1=TempVer[idx[2]];v1.texCoord2=uv;TempVer[idx[2]]=v1;
}
total+=weights[2]=MaterialIdGroup.Count();
}else if(((add=TempVer[idx[j]].texCoord3==EmptyUV)                                                              )||TempVer[idx[j]].texCoord3==uv){
if(add){var v1=TempVer[idx[0]];v1.texCoord3=uv;TempVer[idx[0]]=v1;
            v1=TempVer[idx[1]];v1.texCoord3=uv;TempVer[idx[1]]=v1;
            v1=TempVer[idx[2]];v1.texCoord3=uv;TempVer[idx[2]]=v1;
}
total+=weights[3]=MaterialIdGroup.Count();
}
}
if(weights.Count>1){var v2=TempVer[idx[j]];
        Color col=v2.color;col.r=(weights[0]/(float)total);
if(weights.ContainsKey(1)){col.g=(weights[1]/(float)total);}
if(weights.ContainsKey(2)){col.b=(weights[2]/(float)total);}
if(weights.ContainsKey(3)){col.a=(weights[3]/(float)total);}
                  v2.color=col;TempVer[idx[j]]=v2;
}
}}
bake=true;
if(LOG&&LOG_LEVEL<=1)Debug.Log("terminada atualização deste pedaço do terreno:"+cCoord1+"..levou:"+watch.ElapsedMilliseconds+"ms");
lock(tasksBusyCount_Syn){tasksBusyCount--;}queue.Set();backgroundData.Set();ReleaseData();

//...

for(int i=0;i<neighbors.Length;i++){neighbors[i].Clear();}
for(int i=0;i<biome.cacheCount;++i){
for(int j=0;j<nCache[i].Length;++j){if(nCache[i][j]!=null)Array.Clear(nCache[i][j],0,nCache[i][j].Length);}
for(int j=0;j<mCache[i].Length;++j){if(mCache[i][j]!=null)Array.Clear(mCache[i][j],0,mCache[i][j].Length);}
}
for(int i=0;i<voxelsBuffer1[0].Length;++i){Array.Clear(voxelsBuffer1[0][i],0,voxelsBuffer1[0][i].Length);}
for(int i=0;i<voxelsBuffer1[1].Length;++i){Array.Clear(voxelsBuffer1[1][i],0,voxelsBuffer1[1][i].Length);}
for(int i=0;i<voxelsBuffer1[2].Length;++i){Array.Clear(voxelsBuffer1[2][i],0,voxelsBuffer1[2][i].Length);}
for(int i=0;i<voxelsBuffer2.Length;++i){if(voxelsBuffer2[i]!=null)Array.Clear(voxelsBuffer2[i],0,voxelsBuffer2[i].Length);}
for(int i=0;i<verticesBuffer[0].Length;++i){Array.Clear(verticesBuffer[0][i],0,verticesBuffer[0][i].Length);}
for(int i=0;i<verticesBuffer[1].Length;++i){Array.Clear(verticesBuffer[1][i],0,verticesBuffer[1][i].Length);}
for(int i=0;i<verticesBuffer[2].Length;++i){Array.Clear(verticesBuffer[2][i],0,verticesBuffer[2][i].Length);}
UVByVertex.Clear();
}_Stop:{
}
if(LOG&&LOG_LEVEL<=1)Debug.Log("finalizar trabalho em plano de fundo para pedaço de terreno graciosamente");
}
}catch(Exception e){Debug.LogError(e?.Message+"\n"+e?.StackTrace+"\n"+e?.Source);}
}
}

//...

}
void OnDestroy(){
Stop=true;foregroundData.Dispose();backgroundData.Dispose();
TempVer.Dispose();
TempTri.Dispose();
if(LOG&&LOG_LEVEL<=1)Debug.Log("destruição completa");
}
public bool Built{
          get{return Built_v;}
protected set{       Built_v=value;

//...

if(value){
renderer.enabled=true;
collider.enabled=true;
collider.sharedMesh=null;
collider.sharedMesh=mesh;
if(DEBUG_MODE)Debug.Assert(collider.sharedMesh!=null);
navMeshDirty=true;
}else{
renderer.enabled=false;
collider.enabled=false;
}
}
}[NonSerialized]protected bool Built_v;
[NonSerialized]bool rebuild=false;[NonSerialized]bool bake=false;[NonSerialized]BakerJob bakeJob;[NonSerialized]bool baking=false;[NonSerialized]JobHandle bakingHandle;struct BakerJob:IJob{public int meshId;public void Execute(){Physics.BakeMesh(meshId,false);}}
void Update(){
if(NetworkManager.Singleton.IsServer){
if(backgroundData.WaitOne(0)){_repeat:{}
if(baking){
if(bakingHandle.IsCompleted){bakingHandle.Complete();baking=false;
if(LOG&&LOG_LEVEL<=1)Debug.Log("mesh baked",this);
if(!rebuild){Built=true;}
goto _repeat;
}
}else if(bake){bake=false;
if(LOG&&LOG_LEVEL<=1)Debug.Log("hora de construir:TempVer.Length.."+TempVer.Length+"..TempTri.Length.."+TempTri.Length,this);
baking=true;
#region VertexBuffer
bool resize;
if(resize=TempVer.Length>mesh.vertexCount){
    mesh.SetVertexBufferParams(TempVer.Length,layout);}
mesh.SetVertexBufferData(TempVer.AsArray(),0,0,TempVer.Length,0,meshFlags);
#endregion 
#region IndexBuffer
if(resize){
    mesh.SetIndexBufferParams(TempTri.Length,IndexFormat.UInt32);}
mesh.SetIndexBufferData(TempTri.AsArray(),0,0,TempTri.Length,meshFlags);
#endregion 
#region SubMesh
    mesh.subMeshCount=1;
mesh.SetSubMesh(0,new SubMeshDescriptor(0,TempTri.Length){firstVertex=0,vertexCount=TempVer.Length},meshFlags);
#endregion 
bakingHandle=bakeJob.Schedule();
goto _repeat;
}else if(rebuild){rebuild=false;
if(LOG&&LOG_LEVEL<=1)Debug.Log("hora de calcular reconstrução",this);
cCoord1=cCoord;
cnkRgn1=cnkRgn;
cnkIdx1=cnkIdx;
backgroundData.Reset();foregroundData.Set();TerrainChunkTask.StartNew(this);
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
[NonSerialized]bool init=true;public bool Initialized{get{return!init;}}public Vector2Int cCoord{private set;get;}public Vector2Int cnkRgn{private set;get;}public int cnkIdx{private set;get;}public void OncCoordChanged(Vector2Int cCoord,int cnkIdx){
if(!init&&this.cCoord==cCoord)return;init=false;this.cCoord=cCoord;cnkRgn=cCoordTocnkRgn(cCoord);Built=false;localBounds.center=transform.position=new Vector3(cnkRgn.x,0,cnkRgn.y);var navMeshSource=navMeshSources[gameObject];navMeshSource.transform=transform.localToWorldMatrix;navMeshSources[gameObject]=navMeshSource;this.cnkIdx=cnkIdx;
rebuild=true;
if(LOG&&LOG_LEVEL<=1)Debug.Log("OncCoordChanged(Vector2Int cCoord.."+cCoord+"..);cnkRgn.."+cnkRgn+"..;cnkIdx.."+cnkIdx);
}
public void OnEdited(){
rebuild=true;
if(LOG&&LOG_LEVEL<=1)Debug.Log("OnEdited();cnkRgn.."+cnkRgn+"..;cnkIdx.."+cnkIdx);
}
[NonSerialized]static readonly VertexAttributeDescriptor[]layout=new[]{
new VertexAttributeDescriptor(VertexAttribute.Position ,VertexAttributeFormat.Float32,3),
new VertexAttributeDescriptor(VertexAttribute.Normal   ,VertexAttributeFormat.Float32,3),
new VertexAttributeDescriptor(VertexAttribute.Color    ,VertexAttributeFormat.Float32,4),
new VertexAttributeDescriptor(VertexAttribute.TexCoord0,VertexAttributeFormat.Float32,2),
new VertexAttributeDescriptor(VertexAttribute.TexCoord1,VertexAttributeFormat.Float32,2),
new VertexAttributeDescriptor(VertexAttribute.TexCoord2,VertexAttributeFormat.Float32,2),
new VertexAttributeDescriptor(VertexAttribute.TexCoord3,VertexAttributeFormat.Float32,2),
};
public static class Editor{
static bool Stop{
get{bool tmp;lock(Stop_Syn){tmp=Stop_v;      }return tmp;}
set{         lock(Stop_Syn){    Stop_v=value;}if(value){foregroundData1.Set();}}
}[NonSerialized]static readonly object Stop_Syn=new object();[NonSerialized]static bool Stop_v=false;[NonSerialized]static readonly AutoResetEvent foregroundData1=new AutoResetEvent(false);[NonSerialized]static readonly ManualResetEvent backgroundData1=new ManualResetEvent(true);[NonSerialized]static Task task1=null;
public static void Awake(bool LOG,int LOG_LEVEL){

//...

if(task1!=null){return;}task1=Task.Factory.StartNew(BG1,new object[]{LOG,LOG_LEVEL,savePath,},TaskCreationOptions.LongRunning);
static void BG1(object state){Thread.CurrentThread.IsBackground=false;Thread.CurrentThread.Priority=System.Threading.ThreadPriority.BelowNormal;
try{
if(state is object[]parameters&&parameters[0]is bool LOG&&parameters[1]is int LOG_LEVEL&&parameters[2]is string savePath){
if(LOG&&LOG_LEVEL<=1)Debug.Log("inicializar sistema para edições no terreno");
var watch=new System.Diagnostics.Stopwatch();

//...

Dictionary<int,Dictionary<Vector3Int,(double density,MaterialId materialId)>>fileData=new Dictionary<int,Dictionary<Vector3Int,(double,MaterialId)>>();
Dictionary<int,Dictionary<Vector3Int,(double density,MaterialId materialId)>>saveData=new Dictionary<int,Dictionary<Vector3Int,(double,MaterialId)>>();
while(!Stop){foregroundData1.WaitOne();if(Stop)goto _Stop;
if(LOG&&LOG_LEVEL<=1){Debug.Log("começar nova edição no terreno");watch.Restart();}

//... 

for(int i=0;i<BG_editData.Count;++i){var edit=BG_editData[i];Vector3 position=edit.position;EditMode mode=edit.mode;Vector3Int size=edit.size;double density=edit.density;MaterialId materialId=edit.materialId;int smoothness=edit.smoothness;
if(LOG&&LOG_LEVEL<=1)Debug.Log("edit at.."+edit);
switch(mode){
default:{
float sqrt_yx_1=Mathf.Sqrt(Mathf.Pow(size.y,2)+Mathf.Pow(size.x,2)),sqrt_yx_2;
float sqrt_xz_1=Mathf.Sqrt(Mathf.Pow(size.x,2)+Mathf.Pow(size.z,2)),sqrt_xz_2;
float sqrt_zy_1=Mathf.Sqrt(Mathf.Pow(size.z,2)+Mathf.Pow(size.y,2)),sqrt_zy_2;
float sqrt_yx_xz_1=Mathf.Sqrt(Mathf.Pow(sqrt_yx_1,2)+Mathf.Pow(sqrt_xz_1,2));float sqrt_yx_xz_zy_1=Mathf.Sqrt(Mathf.Pow(sqrt_yx_xz_1,2)+Mathf.Pow(sqrt_zy_1,2));
Vector2Int cCoord1=vecPosTocCoord(position),        cCoord3;
Vector3Int vCoord1=vecPosTovCoord(position),vCoord2,vCoord3;
Vector2Int cnkRgn1=cCoordTocnkRgn(cCoord1 ),        cnkRgn3;
for(int y=0;y<size.y+smoothness;++y){for(vCoord2=new Vector3Int(vCoord1.x,vCoord1.y-y,vCoord1.z);vCoord2.y<=vCoord1.y+y;vCoord2.y+=y*2){if(vCoord2.y>=0&&vCoord2.y<Height){
for(int x=0;x<size.x+smoothness;++x){for(vCoord2.x=vCoord1.x-x                                  ;vCoord2.x<=vCoord1.x+x;vCoord2.x+=x*2){
sqrt_yx_2=Mathf.Sqrt(Mathf.Pow(y,2)+Mathf.Pow(x,2));
for(int z=0;z<size.z+smoothness;++z){for(vCoord2.z=vCoord1.z-z                                  ;vCoord2.z<=vCoord1.z+z;vCoord2.z+=z*2){
cCoord3=cCoord1;
cnkRgn3=cnkRgn1;
vCoord3=vCoord2;
if(vCoord2.x<0||vCoord2.x>=Width||
   vCoord2.z<0||vCoord2.z>=Depth){ValidateCoord(ref cnkRgn3,ref vCoord3);cCoord3=cnkRgnTocCoord(cnkRgn3);}
int cnkIdx3=GetcnkIdx(cCoord3.x,cCoord3.y);
sqrt_xz_2=Mathf.Sqrt(Mathf.Pow(x,2)+Mathf.Pow(z,2));
sqrt_zy_2=Mathf.Sqrt(Mathf.Pow(z,2)+Mathf.Pow(y,2));
double resultDensity;
if(y>=size.y||x>=size.x||z>=size.z){
if(y>=size.y&&x>=size.x&&z>=size.z){
float sqrt_yx_xz_2=Mathf.Sqrt(Mathf.Pow(sqrt_yx_2,2)+Mathf.Pow(sqrt_xz_2,2));float sqrt_yx_xz_zy_2=Mathf.Sqrt(Mathf.Pow(sqrt_yx_xz_2,2)+Mathf.Pow(sqrt_zy_2,2));
resultDensity=density*(1f-(sqrt_yx_xz_zy_2-sqrt_yx_xz_1)/(sqrt_yx_xz_zy_2));
}else 
if(y>=size.y&&x>=size.x){
resultDensity=density*(1f-(sqrt_yx_2-sqrt_yx_1)/(sqrt_yx_2));
}else 
if(x>=size.x&&z>=size.z){
resultDensity=density*(1f-(sqrt_xz_2-sqrt_xz_1)/(sqrt_xz_2));
}else 
if(z>=size.z&&y>=size.y){
resultDensity=density*(1f-(sqrt_zy_2-sqrt_zy_1)/(sqrt_zy_2));
}else 
if(y>=size.y){
resultDensity=density*(1f-(y-size.y)/(float)y)*1.414f;
}else 
if(x>=size.x){
resultDensity=density*(1f-(x-size.x)/(float)x)*1.414f;
}else 
if(z>=size.z){
resultDensity=density*(1f-(z-size.z)/(float)z)*1.414f;
}else{
resultDensity=0d;
}
}else{
resultDensity=density;
}

//... to do "mix/blend" to biome or "mix/blend" to loaded data flag

if(!fileData.ContainsKey(cnkIdx3)){
string editsFolder=string.Format("{0}{1}",savePath,cnkIdx3);string editsFile=string.Format("{0}/{1}",editsFolder,"terrainEdits.MessagePack");
if(File.Exists(editsFile)){
using(FileStream file=new FileStream(editsFile,FileMode.Open,FileAccess.Read,FileShare.Read)){
var fileEdits=MessagePackSerializer.Deserialize(typeof(Dictionary<Vector3Int,(double density,MaterialId materialId)>),file)as Dictionary<Vector3Int,(double density,MaterialId materialId)>;
fileData.Add(cnkIdx3,fileEdits);
}
}
}

//...

Voxel currentVoxel;
if(fileData.ContainsKey(cnkIdx3)&&fileData[cnkIdx3].ContainsKey(vCoord3)){var voxelData=fileData[cnkIdx3][vCoord3];
currentVoxel=new Voxel(voxelData.density,Vector3.zero,voxelData.materialId);

//...

}else{
currentVoxel=new Voxel();
Vector3 noiseInput=vCoord3;noiseInput.x+=cnkRgn3.x;
                           noiseInput.z+=cnkRgn3.y;
biome.result(vCoord3,noiseInput,null,null,0,vCoord3.z+vCoord3.x*Depth,ref currentVoxel);
}
resultDensity=Math.Max(resultDensity,currentVoxel.Density);
if(!saveData.ContainsKey(cnkIdx3))saveData.Add(cnkIdx3,new Dictionary<Vector3Int,(double density,MaterialId materialId)>());
saveData[cnkIdx3][vCoord3]=(resultDensity,-resultDensity>=50d?MaterialId.Air:materialId);

//...

BG_dirty.Add(cnkIdx3);
for(int cx=-1;cx<=1;cx++){
for(int cz=-1;cz<=1;cz++){
if(cx==0&&cz==0)continue;
Vector2Int cCoord4=cCoord3+new Vector2Int(cx,cz);
if(Math.Abs(cCoord4.x)>=MaxcCoordx||
   Math.Abs(cCoord4.y)>=MaxcCoordy){continue;}
int cnkIdx4=GetcnkIdx(cCoord4.x,cCoord4.y);
BG_dirty.Add(cnkIdx4);
}}

//...

 if(z==0){break;}}}
 if(x==0){break;}}}
}if(y==0){break;}}}

//...

break;}
}
//foreach(var edits in saveData){int cnkIdx1=edits.Key;

//... to do: load here data from files, or use biome value, to mix/blend density value

//}
}
BG_editData.Clear();

foreach(var syn in load_Syn_All)Monitor.Enter(syn);try{
/*  Save edits for each chunk..  */foreach(var edits in saveData){int cnkIdx1=edits.Key;
string editsFolder=string.Format("{0}{1}",savePath,cnkIdx1);string editsFile=string.Format("{0}/{1}",editsFolder,"terrainEdits.MessagePack");
if(LOG&&LOG_LEVEL<=1)Debug.Log("editsFolder.."+editsFolder+"..e editsFile.."+editsFile+"..para:.."+cnkIdx1+"..(cnkIdx1)");
Directory.CreateDirectory(string.Format("{0}/",editsFolder));

//...

using(FileStream file=new FileStream(editsFile,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)){
if(file.Length>0){
var fileEdits=MessagePackSerializer.Deserialize(typeof(Dictionary<Vector3Int,(double density,MaterialId materialId)>),file)as Dictionary<Vector3Int,(double density,MaterialId materialId)>;
foreach(var fileEdit in fileEdits){

//...

if(!edits.Value.ContainsKey(fileEdit.Key)){edits.Value.Add(fileEdit.Key,fileEdit.Value);}//  Carregue edições já feitas sem substituir as novas, pois senão o que foi editado anteriormente é perdido

//...

}
}
file.SetLength(0);
file.Flush(true);

//...

MessagePackSerializer.Serialize(file,edits.Value);
}

//... 

}
}catch{throw;}finally{foreach(var syn in load_Syn_All)Monitor.Exit(syn);}
if(LOG&&LOG_LEVEL<=1)Debug.Log("terminada edição no terreno (dados salvos nos arquivos)..levou:"+watch.ElapsedMilliseconds+"ms");
backgroundData1.Set();
fileData.Clear();
saveData.Clear();

//...

}_Stop:{
}
if(LOG&&LOG_LEVEL<=1)Debug.Log("finalizar sistema para edições no terreno graciosamente");
}
}catch(Exception e){Debug.LogError(e?.Message+"\n"+e?.StackTrace+"\n"+e?.Source);}
}
}
public static void OnDestroy(bool LOG,int LOG_LEVEL){
if(Stop==true){return;}Stop=true;try{task1.Wait();}catch(Exception e){Debug.LogError(e?.Message+"\n"+e?.StackTrace+"\n"+e?.Source);}foregroundData1.Dispose();backgroundData1.Dispose();
if(LOG&&LOG_LEVEL<=1)Debug.Log("destruição completa do sistema para edições no terreno");
}
[NonSerialized]static readonly HashSet<int>BG_dirty=new HashSet<int>();
public static void Update(bool LOG,int LOG_LEVEL,bool DEBUG_MODE){
if(backgroundData1.WaitOne(0)){

//...

foreach(int dirty in BG_dirty){
if(ActiveTerrain.TryGetValue(dirty,out TerrainChunk chunk)){
chunk.OnEdited();
}

//...

}
BG_dirty.Clear();

//...

if(FG_editData.Count>0){
if(LOG&&LOG_LEVEL<=1)Debug.Log("editData.Count>0[.."+FG_editData.Count+"..];comece a registrar edições");
BG_editData.AddRange(FG_editData);
FG_editData.Clear();
backgroundData1.Reset();foregroundData1.Set();}
}

//...

}
public enum EditMode{cube,}
[NonSerialized]static readonly List<(Vector3 position,EditMode mode,Vector3Int size,double density,MaterialId materialId,int smoothness)>FG_editData=new List<(Vector3,EditMode,Vector3Int,double,MaterialId,int)>();
[NonSerialized]static readonly List<(Vector3 position,EditMode mode,Vector3Int size,double density,MaterialId materialId,int smoothness)>BG_editData=new List<(Vector3,EditMode,Vector3Int,double,MaterialId,int)>();
public static void Edit(Vector3 at,EditMode mode,Vector3Int size,double density,MaterialId materialId,int smoothness,bool LOG,int LOG_LEVEL){

//...

FG_editData.Add((at,mode,size,density,materialId,smoothness));

//...

}
}
#if UNITY_EDITOR
void OnDrawGizmos(){
if(backgroundData.WaitOne(0)){
if(GIZMOS_ENABLED<=-100){for(int i=0;i<TempVer.Length;i++){Debug.DrawRay(transform.position+TempVer[i].pos,TempVer[i].normal,Color.white);}}
if(GIZMOS_ENABLED<=1){

//...

DrawBounds(localBounds,Color.white);

}
}
}
#endif
}
}
namespace paulbourke.MarchingCubes{
    public static class Tables{
        public static readonly ReadOnlyCollection<int>EdgeTable=new ReadOnlyCollection<int>(new int[256]{
            0x0  ,0x109,0x203,0x30a,0x406,0x50f,0x605,0x70c,
            0x80c,0x905,0xa0f,0xb06,0xc0a,0xd03,0xe09,0xf00,
            0x190,0x99 ,0x393,0x29a,0x596,0x49f,0x795,0x69c,
            0x99c,0x895,0xb9f,0xa96,0xd9a,0xc93,0xf99,0xe90,
            0x230,0x339,0x33 ,0x13a,0x636,0x73f,0x435,0x53c,
            0xa3c,0xb35,0x83f,0x936,0xe3a,0xf33,0xc39,0xd30,
            0x3a0,0x2a9,0x1a3,0xaa ,0x7a6,0x6af,0x5a5,0x4ac,
            0xbac,0xaa5,0x9af,0x8a6,0xfaa,0xea3,0xda9,0xca0,
            0x460,0x569,0x663,0x76a,0x66 ,0x16f,0x265,0x36c,
            0xc6c,0xd65,0xe6f,0xf66,0x86a,0x963,0xa69,0xb60,
            0x5f0,0x4f9,0x7f3,0x6fa,0x1f6,0xff ,0x3f5,0x2fc,
            0xdfc,0xcf5,0xfff,0xef6,0x9fa,0x8f3,0xbf9,0xaf0,
            0x650,0x759,0x453,0x55a,0x256,0x35f,0x55 ,0x15c,
            0xe5c,0xf55,0xc5f,0xd56,0xa5a,0xb53,0x859,0x950,
            0x7c0,0x6c9,0x5c3,0x4ca,0x3c6,0x2cf,0x1c5,0xcc ,
            0xfcc,0xec5,0xdcf,0xcc6,0xbca,0xac3,0x9c9,0x8c0,
            0x8c0,0x9c9,0xac3,0xbca,0xcc6,0xdcf,0xec5,0xfcc,
            0xcc ,0x1c5,0x2cf,0x3c6,0x4ca,0x5c3,0x6c9,0x7c0,
            0x950,0x859,0xb53,0xa5a,0xd56,0xc5f,0xf55,0xe5c,
            0x15c,0x55 ,0x35f,0x256,0x55a,0x453,0x759,0x650,
            0xaf0,0xbf9,0x8f3,0x9fa,0xef6,0xfff,0xcf5,0xdfc,
            0x2fc,0x3f5,0xff ,0x1f6,0x6fa,0x7f3,0x4f9,0x5f0,
            0xb60,0xa69,0x963,0x86a,0xf66,0xe6f,0xd65,0xc6c,
            0x36c,0x265,0x16f,0x66 ,0x76a,0x663,0x569,0x460,
            0xca0,0xda9,0xea3,0xfaa,0x8a6,0x9af,0xaa5,0xbac,
            0x4ac,0x5a5,0x6af,0x7a6,0xaa ,0x1a3,0x2a9,0x3a0,
            0xd30,0xc39,0xf33,0xe3a,0x936,0x83f,0xb35,0xa3c,
            0x53c,0x435,0x73f,0x636,0x13a,0x33 ,0x339,0x230,
            0xe90,0xf99,0xc93,0xd9a,0xa96,0xb9f,0x895,0x99c,
            0x69c,0x795,0x49f,0x596,0x29a,0x393,0x99 ,0x190,
            0xf00,0xe09,0xd03,0xc0a,0xb06,0xa0f,0x905,0x80c,
            0x70c,0x605,0x50f,0x406,0x30a,0x203,0x109,0x0
        });
        #region TriangleTable
        public static readonly ReadOnlyCollection<int[]>TriangleTable=new ReadOnlyCollection<int[]>(new int[256][]{
        new int[16]{-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 0, 8, 3,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 0, 1, 9,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 1, 8, 3, 9, 8, 1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 1, 2,10,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 0, 8, 3, 1, 2,10,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 9, 2,10, 0, 2, 9,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 2, 8, 3, 2,10, 8,10, 9, 8,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 3,11, 2,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 0,11, 2, 8,11, 0,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 1, 9, 0, 2, 3,11,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 1,11, 2, 1, 9,11, 9, 8,11,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 3,10, 1,11,10, 3,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 0,10, 1, 0, 8,10, 8,11,10,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 3, 9, 0, 3,11, 9,11,10, 9,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 9, 8,10,10, 8,11,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 4, 7, 8,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 4, 3, 0, 7, 3, 4,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 0, 1, 9, 8, 4, 7,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 4, 1, 9, 4, 7, 1, 7, 3, 1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 1, 2,10, 8, 4, 7,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 3, 4, 7, 3, 0, 4, 1, 2,10,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 9, 2,10, 9, 0, 2, 8, 4, 7,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 2,10, 9, 2, 9, 7, 2, 7, 3, 7, 9, 4,-1,-1,-1,-1},
        new int[16]{ 8, 4, 7, 3,11, 2,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{11, 4, 7,11, 2, 4, 2, 0, 4,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 9, 0, 1, 8, 4, 7, 2, 3,11,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 4, 7,11, 9, 4,11, 9,11, 2, 9, 2, 1,-1,-1,-1,-1},
        new int[16]{ 3,10, 1, 3,11,10, 7, 8, 4,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 1,11,10, 1, 4,11, 1, 0, 4, 7,11, 4,-1,-1,-1,-1},
        new int[16]{ 4, 7, 8, 9, 0,11, 9,11,10,11, 0, 3,-1,-1,-1,-1},
        new int[16]{ 4, 7,11, 4,11, 9, 9,11,10,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 9, 5, 4,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 9, 5, 4, 0, 8, 3,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 0, 5, 4, 1, 5, 0,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 8, 5, 4, 8, 3, 5, 3, 1, 5,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 1, 2,10, 9, 5, 4,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 3, 0, 8, 1, 2,10, 4, 9, 5,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 5, 2,10, 5, 4, 2, 4, 0, 2,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 2,10, 5, 3, 2, 5, 3, 5, 4, 3, 4, 8,-1,-1,-1,-1},
        new int[16]{ 9, 5, 4, 2, 3,11,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 0,11, 2, 0, 8,11, 4, 9, 5,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 0, 5, 4, 0, 1, 5, 2, 3,11,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 2, 1, 5, 2, 5, 8, 2, 8,11, 4, 8, 5,-1,-1,-1,-1},
        new int[16]{10, 3,11,10, 1, 3, 9, 5, 4,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 4, 9, 5, 0, 8, 1, 8,10, 1, 8,11,10,-1,-1,-1,-1},
        new int[16]{ 5, 4, 0, 5, 0,11, 5,11,10,11, 0, 3,-1,-1,-1,-1},
        new int[16]{ 5, 4, 8, 5, 8,10,10, 8,11,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 9, 7, 8, 5, 7, 9,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 9, 3, 0, 9, 5, 3, 5, 7, 3,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 0, 7, 8, 0, 1, 7, 1, 5, 7,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 1, 5, 3, 3, 5, 7,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 9, 7, 8, 9, 5, 7,10, 1, 2,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{10, 1, 2, 9, 5, 0, 5, 3, 0, 5, 7, 3,-1,-1,-1,-1},
        new int[16]{ 8, 0, 2, 8, 2, 5, 8, 5, 7,10, 5, 2,-1,-1,-1,-1},
        new int[16]{ 2,10, 5, 2, 5, 3, 3, 5, 7,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 7, 9, 5, 7, 8, 9, 3,11, 2,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 9, 5, 7, 9, 7, 2, 9, 2, 0, 2, 7,11,-1,-1,-1,-1},
        new int[16]{ 2, 3,11, 0, 1, 8, 1, 7, 8, 1, 5, 7,-1,-1,-1,-1},
        new int[16]{11, 2, 1,11, 1, 7, 7, 1, 5,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 9, 5, 8, 8, 5, 7,10, 1, 3,10, 3,11,-1,-1,-1,-1},
        new int[16]{ 5, 7, 0, 5, 0, 9, 7,11, 0, 1, 0,10,11,10, 0,-1},
        new int[16]{11,10, 0,11, 0, 3,10, 5, 0, 8, 0, 7, 5, 7, 0,-1},
        new int[16]{11,10, 5, 7,11, 5,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{10, 6, 5,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 0, 8, 3, 5,10, 6,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 9, 0, 1, 5,10, 6,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 1, 8, 3, 1, 9, 8, 5,10, 6,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 1, 6, 5, 2, 6, 1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 1, 6, 5, 1, 2, 6, 3, 0, 8,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 9, 6, 5, 9, 0, 6, 0, 2, 6,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 5, 9, 8, 5, 8, 2, 5, 2, 6, 3, 2, 8,-1,-1,-1,-1},
        new int[16]{ 2, 3,11,10, 6, 5,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{11, 0, 8,11, 2, 0,10, 6, 5,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 0, 1, 9, 2, 3,11, 5,10, 6,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 5,10, 6, 1, 9, 2, 9,11, 2, 9, 8,11,-1,-1,-1,-1},
        new int[16]{ 6, 3,11, 6, 5, 3, 5, 1, 3,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 0, 8,11, 0,11, 5, 0, 5, 1, 5,11, 6,-1,-1,-1,-1},
        new int[16]{ 3,11, 6, 0, 3, 6, 0, 6, 5, 0, 5, 9,-1,-1,-1,-1},
        new int[16]{ 6, 5, 9, 6, 9,11,11, 9, 8,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 5,10, 6, 4, 7, 8,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 4, 3, 0, 4, 7, 3, 6, 5,10,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 1, 9, 0, 5,10, 6, 8, 4, 7,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{10, 6, 5, 1, 9, 7, 1, 7, 3, 7, 9, 4,-1,-1,-1,-1},
        new int[16]{ 6, 1, 2, 6, 5, 1, 4, 7, 8,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 1, 2, 5, 5, 2, 6, 3, 0, 4, 3, 4, 7,-1,-1,-1,-1},
        new int[16]{ 8, 4, 7, 9, 0, 5, 0, 6, 5, 0, 2, 6,-1,-1,-1,-1},
        new int[16]{ 7, 3, 9, 7, 9, 4, 3, 2, 9, 5, 9, 6, 2, 6, 9,-1},
        new int[16]{ 3,11, 2, 7, 8, 4,10, 6, 5,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 5,10, 6, 4, 7, 2, 4, 2, 0, 2, 7,11,-1,-1,-1,-1},
        new int[16]{ 0, 1, 9, 4, 7, 8, 2, 3,11, 5,10, 6,-1,-1,-1,-1},
        new int[16]{ 9, 2, 1, 9,11, 2, 9, 4,11, 7,11, 4, 5,10, 6,-1},
        new int[16]{ 8, 4, 7, 3,11, 5, 3, 5, 1, 5,11, 6,-1,-1,-1,-1},
        new int[16]{ 5, 1,11, 5,11, 6, 1, 0,11, 7,11, 4, 0, 4,11,-1},
        new int[16]{ 0, 5, 9, 0, 6, 5, 0, 3, 6,11, 6, 3, 8, 4, 7,-1},
        new int[16]{ 6, 5, 9, 6, 9,11, 4, 7, 9, 7,11, 9,-1,-1,-1,-1},
        new int[16]{10, 4, 9, 6, 4,10,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 4,10, 6, 4, 9,10, 0, 8, 3,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{10, 0, 1,10, 6, 0, 6, 4, 0,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 8, 3, 1, 8, 1, 6, 8, 6, 4, 6, 1,10,-1,-1,-1,-1},
        new int[16]{ 1, 4, 9, 1, 2, 4, 2, 6, 4,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 3, 0, 8, 1, 2, 9, 2, 4, 9, 2, 6, 4,-1,-1,-1,-1},
        new int[16]{ 0, 2, 4, 4, 2, 6,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 8, 3, 2, 8, 2, 4, 4, 2, 6,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{10, 4, 9,10, 6, 4,11, 2, 3,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 0, 8, 2, 2, 8,11, 4, 9,10, 4,10, 6,-1,-1,-1,-1},
        new int[16]{ 3,11, 2, 0, 1, 6, 0, 6, 4, 6, 1,10,-1,-1,-1,-1},
        new int[16]{ 6, 4, 1, 6, 1,10, 4, 8, 1, 2, 1,11, 8,11, 1,-1},
        new int[16]{ 9, 6, 4, 9, 3, 6, 9, 1, 3,11, 6, 3,-1,-1,-1,-1},
        new int[16]{ 8,11, 1, 8, 1, 0,11, 6, 1, 9, 1, 4, 6, 4, 1,-1},
        new int[16]{ 3,11, 6, 3, 6, 0, 0, 6, 4,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 6, 4, 8,11, 6, 8,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 7,10, 6, 7, 8,10, 8, 9,10,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 0, 7, 3, 0,10, 7, 0, 9,10, 6, 7,10,-1,-1,-1,-1},
        new int[16]{10, 6, 7, 1,10, 7, 1, 7, 8, 1, 8, 0,-1,-1,-1,-1},
        new int[16]{10, 6, 7,10, 7, 1, 1, 7, 3,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 1, 2, 6, 1, 6, 8, 1, 8, 9, 8, 6, 7,-1,-1,-1,-1},
        new int[16]{ 2, 6, 9, 2, 9, 1, 6, 7, 9, 0, 9, 3, 7, 3, 9,-1},
        new int[16]{ 7, 8, 0, 7, 0, 6, 6, 0, 2,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 7, 3, 2, 6, 7, 2,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 2, 3,11,10, 6, 8,10, 8, 9, 8, 6, 7,-1,-1,-1,-1},
        new int[16]{ 2, 0, 7, 2, 7,11, 0, 9, 7, 6, 7,10, 9,10, 7,-1},
        new int[16]{ 1, 8, 0, 1, 7, 8, 1,10, 7, 6, 7,10, 2, 3,11,-1},
        new int[16]{11, 2, 1,11, 1, 7,10, 6, 1, 6, 7, 1,-1,-1,-1,-1},
        new int[16]{ 8, 9, 6, 8, 6, 7, 9, 1, 6,11, 6, 3, 1, 3, 6,-1},
        new int[16]{ 0, 9, 1,11, 6, 7,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 7, 8, 0, 7, 0, 6, 3,11, 0,11, 6, 0,-1,-1,-1,-1},
        new int[16]{ 7,11, 6,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 7, 6,11,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 3, 0, 8,11, 7, 6,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 0, 1, 9,11, 7, 6,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 8, 1, 9, 8, 3, 1,11, 7, 6,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{10, 1, 2, 6,11, 7,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 1, 2,10, 3, 0, 8, 6,11, 7,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 2, 9, 0, 2,10, 9, 6,11, 7,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 6,11, 7, 2,10, 3,10, 8, 3,10, 9, 8,-1,-1,-1,-1},
        new int[16]{ 7, 2, 3, 6, 2, 7,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 7, 0, 8, 7, 6, 0, 6, 2, 0,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 2, 7, 6, 2, 3, 7, 0, 1, 9,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 1, 6, 2, 1, 8, 6, 1, 9, 8, 8, 7, 6,-1,-1,-1,-1},
        new int[16]{10, 7, 6,10, 1, 7, 1, 3, 7,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{10, 7, 6, 1, 7,10, 1, 8, 7, 1, 0, 8,-1,-1,-1,-1},
        new int[16]{ 0, 3, 7, 0, 7,10, 0,10, 9, 6,10, 7,-1,-1,-1,-1},
        new int[16]{ 7, 6,10, 7,10, 8, 8,10, 9,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 6, 8, 4,11, 8, 6,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 3, 6,11, 3, 0, 6, 0, 4, 6,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 8, 6,11, 8, 4, 6, 9, 0, 1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 9, 4, 6, 9, 6, 3, 9, 3, 1,11, 3, 6,-1,-1,-1,-1},
        new int[16]{ 6, 8, 4, 6,11, 8, 2,10, 1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 1, 2,10, 3, 0,11, 0, 6,11, 0, 4, 6,-1,-1,-1,-1},
        new int[16]{ 4,11, 8, 4, 6,11, 0, 2, 9, 2,10, 9,-1,-1,-1,-1},
        new int[16]{10, 9, 3,10, 3, 2, 9, 4, 3,11, 3, 6, 4, 6, 3,-1},
        new int[16]{ 8, 2, 3, 8, 4, 2, 4, 6, 2,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 0, 4, 2, 4, 6, 2,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 1, 9, 0, 2, 3, 4, 2, 4, 6, 4, 3, 8,-1,-1,-1,-1},
        new int[16]{ 1, 9, 4, 1, 4, 2, 2, 4, 6,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 8, 1, 3, 8, 6, 1, 8, 4, 6, 6,10, 1,-1,-1,-1,-1},
        new int[16]{10, 1, 0,10, 0, 6, 6, 0, 4,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 4, 6, 3, 4, 3, 8, 6,10, 3, 0, 3, 9,10, 9, 3,-1},
        new int[16]{10, 9, 4, 6,10, 4,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 4, 9, 5, 7, 6,11,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 0, 8, 3, 4, 9, 5,11, 7, 6,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 5, 0, 1, 5, 4, 0, 7, 6,11,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{11, 7, 6, 8, 3, 4, 3, 5, 4, 3, 1, 5,-1,-1,-1,-1},
        new int[16]{ 9, 5, 4,10, 1, 2, 7, 6,11,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 6,11, 7, 1, 2,10, 0, 8, 3, 4, 9, 5,-1,-1,-1,-1},
        new int[16]{ 7, 6,11, 5, 4,10, 4, 2,10, 4, 0, 2,-1,-1,-1,-1},
        new int[16]{ 3, 4, 8, 3, 5, 4, 3, 2, 5,10, 5, 2,11, 7, 6,-1},
        new int[16]{ 7, 2, 3, 7, 6, 2, 5, 4, 9,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 9, 5, 4, 0, 8, 6, 0, 6, 2, 6, 8, 7,-1,-1,-1,-1},
        new int[16]{ 3, 6, 2, 3, 7, 6, 1, 5, 0, 5, 4, 0,-1,-1,-1,-1},
        new int[16]{ 6, 2, 8, 6, 8, 7, 2, 1, 8, 4, 8, 5, 1, 5, 8,-1},
        new int[16]{ 9, 5, 4,10, 1, 6, 1, 7, 6, 1, 3, 7,-1,-1,-1,-1},
        new int[16]{ 1, 6,10, 1, 7, 6, 1, 0, 7, 8, 7, 0, 9, 5, 4,-1},
        new int[16]{ 4, 0,10, 4,10, 5, 0, 3,10, 6,10, 7, 3, 7,10,-1},
        new int[16]{ 7, 6,10, 7,10, 8, 5, 4,10, 4, 8,10,-1,-1,-1,-1},
        new int[16]{ 6, 9, 5, 6,11, 9,11, 8, 9,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 3, 6,11, 0, 6, 3, 0, 5, 6, 0, 9, 5,-1,-1,-1,-1},
        new int[16]{ 0,11, 8, 0, 5,11, 0, 1, 5, 5, 6,11,-1,-1,-1,-1},
        new int[16]{ 6,11, 3, 6, 3, 5, 5, 3, 1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 1, 2,10, 9, 5,11, 9,11, 8,11, 5, 6,-1,-1,-1,-1},
        new int[16]{ 0,11, 3, 0, 6,11, 0, 9, 6, 5, 6, 9, 1, 2,10,-1},
        new int[16]{11, 8, 5,11, 5, 6, 8, 0, 5,10, 5, 2, 0, 2, 5,-1},
        new int[16]{ 6,11, 3, 6, 3, 5, 2,10, 3,10, 5, 3,-1,-1,-1,-1},
        new int[16]{ 5, 8, 9, 5, 2, 8, 5, 6, 2, 3, 8, 2,-1,-1,-1,-1},
        new int[16]{ 9, 5, 6, 9, 6, 0, 0, 6, 2,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 1, 5, 8, 1, 8, 0, 5, 6, 8, 3, 8, 2, 6, 2, 8,-1},
        new int[16]{ 1, 5, 6, 2, 1, 6,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 1, 3, 6, 1, 6,10, 3, 8, 6, 5, 6, 9, 8, 9, 6,-1},
        new int[16]{10, 1, 0,10, 0, 6, 9, 5, 0, 5, 6, 0,-1,-1,-1,-1},
        new int[16]{ 0, 3, 8, 5, 6,10,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{10, 5, 6,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{11, 5,10, 7, 5,11,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{11, 5,10,11, 7, 5, 8, 3, 0,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 5,11, 7, 5,10,11, 1, 9, 0,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{10, 7, 5,10,11, 7, 9, 8, 1, 8, 3, 1,-1,-1,-1,-1},
        new int[16]{11, 1, 2,11, 7, 1, 7, 5, 1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 0, 8, 3, 1, 2, 7, 1, 7, 5, 7, 2,11,-1,-1,-1,-1},
        new int[16]{ 9, 7, 5, 9, 2, 7, 9, 0, 2, 2,11, 7,-1,-1,-1,-1},
        new int[16]{ 7, 5, 2, 7, 2,11, 5, 9, 2, 3, 2, 8, 9, 8, 2,-1},
        new int[16]{ 2, 5,10, 2, 3, 5, 3, 7, 5,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 8, 2, 0, 8, 5, 2, 8, 7, 5,10, 2, 5,-1,-1,-1,-1},
        new int[16]{ 9, 0, 1, 5,10, 3, 5, 3, 7, 3,10, 2,-1,-1,-1,-1},
        new int[16]{ 9, 8, 2, 9, 2, 1, 8, 7, 2,10, 2, 5, 7, 5, 2,-1},
        new int[16]{ 1, 3, 5, 3, 7, 5,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 0, 8, 7, 0, 7, 1, 1, 7, 5,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 9, 0, 3, 9, 3, 5, 5, 3, 7,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 9, 8, 7, 5, 9, 7,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 5, 8, 4, 5,10, 8,10,11, 8,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 5, 0, 4, 5,11, 0, 5,10,11,11, 3, 0,-1,-1,-1,-1},
        new int[16]{ 0, 1, 9, 8, 4,10, 8,10,11,10, 4, 5,-1,-1,-1,-1},
        new int[16]{10,11, 4,10, 4, 5,11, 3, 4, 9, 4, 1, 3, 1, 4,-1},
        new int[16]{ 2, 5, 1, 2, 8, 5, 2,11, 8, 4, 5, 8,-1,-1,-1,-1},
        new int[16]{ 0, 4,11, 0,11, 3, 4, 5,11, 2,11, 1, 5, 1,11,-1},
        new int[16]{ 0, 2, 5, 0, 5, 9, 2,11, 5, 4, 5, 8,11, 8, 5,-1},
        new int[16]{ 9, 4, 5, 2,11, 3,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 2, 5,10, 3, 5, 2, 3, 4, 5, 3, 8, 4,-1,-1,-1,-1},
        new int[16]{ 5,10, 2, 5, 2, 4, 4, 2, 0,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 3,10, 2, 3, 5,10, 3, 8, 5, 4, 5, 8, 0, 1, 9,-1},
        new int[16]{ 5,10, 2, 5, 2, 4, 1, 9, 2, 9, 4, 2,-1,-1,-1,-1},
        new int[16]{ 8, 4, 5, 8, 5, 3, 3, 5, 1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 0, 4, 5, 1, 0, 5,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 8, 4, 5, 8, 5, 3, 9, 0, 5, 0, 3, 5,-1,-1,-1,-1},
        new int[16]{ 9, 4, 5,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 4,11, 7, 4, 9,11, 9,10,11,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 0, 8, 3, 4, 9, 7, 9,11, 7, 9,10,11,-1,-1,-1,-1},
        new int[16]{ 1,10,11, 1,11, 4, 1, 4, 0, 7, 4,11,-1,-1,-1,-1},
        new int[16]{ 3, 1, 4, 3, 4, 8, 1,10, 4, 7, 4,11,10,11, 4,-1},
        new int[16]{ 4,11, 7, 9,11, 4, 9, 2,11, 9, 1, 2,-1,-1,-1,-1},
        new int[16]{ 9, 7, 4, 9,11, 7, 9, 1,11, 2,11, 1, 0, 8, 3,-1},
        new int[16]{11, 7, 4,11, 4, 2, 2, 4, 0,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{11, 7, 4,11, 4, 2, 8, 3, 4, 3, 2, 4,-1,-1,-1,-1},
        new int[16]{ 2, 9,10, 2, 7, 9, 2, 3, 7, 7, 4, 9,-1,-1,-1,-1},
        new int[16]{ 9,10, 7, 9, 7, 4,10, 2, 7, 8, 7, 0, 2, 0, 7,-1},
        new int[16]{ 3, 7,10, 3,10, 2, 7, 4,10, 1,10, 0, 4, 0,10,-1},
        new int[16]{ 1,10, 2, 8, 7, 4,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 4, 9, 1, 4, 1, 7, 7, 1, 3,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 4, 9, 1, 4, 1, 7, 0, 8, 1, 8, 7, 1,-1,-1,-1,-1},
        new int[16]{ 4, 0, 3, 7, 4, 3,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 4, 8, 7,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 9,10, 8,10,11, 8,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 3, 0, 9, 3, 9,11,11, 9,10,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 0, 1,10, 0,10, 8, 8,10,11,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 3, 1,10,11, 3,10,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 1, 2,11, 1,11, 9, 9,11, 8,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 3, 0, 9, 3, 9,11, 1, 2, 9, 2,11, 9,-1,-1,-1,-1},
        new int[16]{ 0, 2,11, 8, 0,11,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 3, 2,11,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 2, 3, 8, 2, 8,10,10, 8, 9,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 9,10, 2, 0, 9, 2,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 2, 3, 8, 2, 8,10, 0, 1, 8, 1,10, 8,-1,-1,-1,-1},
        new int[16]{ 1,10, 2,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 1, 3, 8, 9, 1, 8,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 0, 9, 1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{ 0, 3, 8,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        new int[16]{-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1}});
        #endregion
    }
}