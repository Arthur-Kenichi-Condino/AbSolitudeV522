using AKCondinoO.Voxels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
namespace AKCondinoO.Buildings.Biomes.Plains{public class Cnidoscolus_quercifolius:Plant{[NonSerialized]public const int poolSize=280;
[NonSerialized]public static readonly ReadOnlyCollection<(Type type,float chance,Vector3 minScale,Vector3 maxScale)>Biomes=new ReadOnlyCollection<(Type,float,Vector3,Vector3)>(new (Type,float,Vector3,Vector3)[1]{
(typeof(World.Plains),.3f,Vector3.one*.1f,Vector3.one*.5f),
});[NonSerialized]public const int maxDepth=1;
[NonSerialized]public const float radius=3.75f;
[NonSerialized]public const float spacingMultiplier=1f;
[NonSerialized]public const bool ignoreCollisions=false;
[NonSerialized]public const float buryRootsDepth=.5f;

//...

}
}