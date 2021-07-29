using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using static AKCondinoO.Util;using static AKCondinoO.Voxels.TerrainChunk;using static AKCondinoO.Voxels.World;using static AKCondinoO.Buildings.Buildings;
namespace AKCondinoO.Buildings{public class DeliciousDonut:Furniture{
[SerializeField]MeshFilter meshFilter;[NonSerialized]Mesh mesh;
[NonSerialized]NavMeshBuildSource navMeshSource;
[NonSerialized]NavMeshBuildMarkup navMeshMarkup;
protected override void Awake(){
                   base.Awake();

//...

mesh=meshFilter.mesh;
navMeshSource=new NavMeshBuildSource{
transform=transform.localToWorldMatrix,
shape=NavMeshBuildSourceShape.Mesh,
sourceObject=mesh,
component=meshFilter,
area=0,//  walkable because it's tiny and doesn't block the passage
};
navMeshMarkup=new NavMeshBuildMarkup{
root=meshFilter.transform,
area=0,//  walkable
overrideArea=false,
ignoreFromBuild=false,
};

//...

}
}
}