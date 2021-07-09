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
[NonSerialized]public static Buildings staticScript;
void Awake(){staticScript=this;
buildingsFolder=string.Format("{0}{1}",savePath,"buildings");
Directory.CreateDirectory(buildingsPath=string.Format("{0}/",buildingsFolder));

            

}



}
}