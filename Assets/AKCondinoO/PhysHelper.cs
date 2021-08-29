using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AKCondinoO{public static class PhysHelper{
public static int AllInteractableLayers{get;private set;}public static int TerrainOnlyLayer{get;private set;}public static int NoTerrainLayer{get;private set;}public static int NoPlantsLayer{get;private set;}public static int NoCharacterLayer{get;private set;}public static int NoCharacterNoTerrainLayer{get;private set;}
public static void Awake(){
AllInteractableLayers    =~(                                                                                                                                                      1<<LayerMask.NameToLayer("Sky")|1<<LayerMask.NameToLayer("Ignore Raycast"));
  TerrainOnlyLayer       = (1<<LayerMask.NameToLayer("Terrain")                                                                                                                                                                                             );
NoTerrainLayer           =~(1<<LayerMask.NameToLayer("Terrain")|                                                                                                                  1<<LayerMask.NameToLayer("Sky")|1<<LayerMask.NameToLayer("Ignore Raycast"));
NoPlantsLayer            =~(                                    1<<LayerMask.NameToLayer("Plant")|                                                                                1<<LayerMask.NameToLayer("Sky")|1<<LayerMask.NameToLayer("Ignore Raycast"));
NoCharacterLayer         =~(                                                                      1<<LayerMask.NameToLayer("Character")|1<<LayerMask.NameToLayer("TinyFurniture")|1<<LayerMask.NameToLayer("Sky")|1<<LayerMask.NameToLayer("Ignore Raycast"));
NoCharacterNoTerrainLayer=~(1<<LayerMask.NameToLayer("Terrain")|                                  1<<LayerMask.NameToLayer("Character")|1<<LayerMask.NameToLayer("TinyFurniture")|1<<LayerMask.NameToLayer("Sky")|1<<LayerMask.NameToLayer("Ignore Raycast"));
}
}
}