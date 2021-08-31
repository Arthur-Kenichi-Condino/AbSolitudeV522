using AKCondinoO.Voxels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
namespace AKCondinoO.Buildings.Biomes.Plains{public class Cnidoscolus_quercifolius:Plant{
[SerializeField]SphereCollider radiusCollider;[NonSerialized]public static float radius;
[SerializeField]BoxCollider rootsCollider;[NonSerialized]public static float buryRootsDepth;
[NonSerialized]public const int maxDepth=1;
[NonSerialized]public static readonly ReadOnlyCollection<(Type type,float chance,Vector3 minScale,Vector3 maxScale)>Biomes=new ReadOnlyCollection<(Type,float,Vector3,Vector3)>(new (Type,float,Vector3,Vector3)[1]{
(typeof(World.Plains),.75f,Vector3.one*.1f,Vector3.one*.5f),
});

//...

}
}