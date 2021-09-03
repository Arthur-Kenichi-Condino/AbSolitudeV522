using AKCondinoO.Voxels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
namespace AKCondinoO.Buildings.Biomes.Plains{public class Urochloa_brizantha:Plant{
[NonSerialized]public static readonly ReadOnlyCollection<(Type type,float chance,Vector3 minScale,Vector3 maxScale)>Biomes=new ReadOnlyCollection<(Type,float,Vector3,Vector3)>(new (Type,float,Vector3,Vector3)[1]{
(typeof(World.Plains),.85f,Vector3.one*.5f,Vector3.one*1f),
});[NonSerialized]public const int maxDepth=1;
[NonSerialized]public const float radius=4.07f;
[NonSerialized]public const float spacingMultiplier=.25f;
[NonSerialized]public const bool ignoreCollisions=true;
[NonSerialized]public const float buryRootsDepth=.1f;

//...

}
}