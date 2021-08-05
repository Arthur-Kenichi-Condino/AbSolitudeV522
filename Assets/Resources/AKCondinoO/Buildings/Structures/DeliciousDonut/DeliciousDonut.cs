using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using static AKCondinoO.Util;using static AKCondinoO.Voxels.TerrainChunk;using static AKCondinoO.Voxels.World;using static AKCondinoO.Buildings.Buildings;
namespace AKCondinoO.Buildings{public class DeliciousDonut:Furniture{
[SerializeField]MeshFilter meshFilter;[NonSerialized]Mesh mesh;
protected override void Awake(){
                   base.Awake();

//...

}
}
}