using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static AKCondinoO.Voxels.TerrainChunk;using static AKCondinoO.Voxels.World;
namespace AKCondinoO.Buildings{public class Buildings:MonoBehaviour{public bool LOG=true;public int LOG_LEVEL=1;
[NonSerialized]public static string buildingsPath;[NonSerialized]public static string buildingsFolder;
[NonSerialized]public static readonly List<object>load_Syn_All=new List<object>();
[NonSerialized]static Dictionary<Type,int>Count;[NonSerialized]static Dictionary<Type,List<int>>Destroyed;
[NonSerialized]public static Buildings staticScript;
void Awake(){staticScript=this;
buildingsFolder=string.Format("{0}{1}",savePath,"buildings");
Directory.CreateDirectory(buildingsPath=string.Format("{0}/",buildingsFolder));

            

}



}
}