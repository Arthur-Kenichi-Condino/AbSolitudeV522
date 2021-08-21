using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AKCondinoO{public static class PhysHelper{
public static int AllInteractableLayers{get;private set;}public static int NoCharacterLayer{get;private set;}public static int NoCharacterNoTerrainLayer{get;private set;}
public static void Awake(){
AllInteractableLayers=~(1<<LayerMask.NameToLayer("Sky")|1<<LayerMask.NameToLayer("Ignore Raycast"));NoCharacterLayer=~(1<<LayerMask.NameToLayer("Character")|1<<LayerMask.NameToLayer("TinyFurniture")|1<<LayerMask.NameToLayer("Sky")|1<<LayerMask.NameToLayer("Ignore Raycast"));NoCharacterNoTerrainLayer=~(1<<LayerMask.NameToLayer("Character")|1<<LayerMask.NameToLayer("TinyFurniture")|1<<LayerMask.NameToLayer("Terrain")|1<<LayerMask.NameToLayer("Sky")|1<<LayerMask.NameToLayer("Ignore Raycast"));
}
}
}