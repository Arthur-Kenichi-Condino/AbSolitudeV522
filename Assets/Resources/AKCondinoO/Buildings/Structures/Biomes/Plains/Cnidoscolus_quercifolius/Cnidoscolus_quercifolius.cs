using AKCondinoO.Voxels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
namespace AKCondinoO.Buildings.Biomes.Plains{public class Cnidoscolus_quercifolius:Plant{
[SerializeField]SphereCollider radiusCollider;[NonSerialized]public static float radius;
[NonSerialized]public const int maxDepth=1;
[NonSerialized]public static readonly ReadOnlyCollection<Type>Biomes=new ReadOnlyCollection<Type>(new Type[1]{typeof(World.Plains),});

//...

}
}