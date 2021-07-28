using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using static AKCondinoO.Util;using static AKCondinoO.Voxels.TerrainChunk;using static AKCondinoO.Voxels.World;using static AKCondinoO.Buildings.Buildings;
namespace AKCondinoO.Buildings{public class DeliciousDonut:Furniture{
[SerializeField]MeshFilter meshFilter;[NonSerialized]Mesh mesh;
[NonSerialized]NavMeshBuildSource navMeshSource;
protected override void Awake(){
                   base.Awake();

//...

mesh=meshFilter.mesh;
navMeshSource=new NavMeshBuildSource{
transform=transform.localToWorldMatrix,
shape=NavMeshBuildSourceShape.Mesh,
sourceObject=mesh,
component=meshFilter,
area=1,//  not walkable
};

//...

}
}
}