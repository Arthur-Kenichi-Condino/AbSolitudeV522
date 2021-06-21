using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AKCondinoO.Actors{public class Actors:MonoBehaviour{public bool LOG=true;public int LOG_LEVEL=1;
[NonSerialized]public static readonly Dictionary<(Type type,int id),SimActor>Get=new Dictionary<(Type,int),SimActor>();[NonSerialized]public static readonly Dictionary<Type,int>Count=new Dictionary<Type,int>();
void Awake(){
            
//...to do: instanciar todos os actors

}
//[NonSerialized]public static readonly List<SimActor>Enabled=new List<SimActor>();[NonSerialized]public static readonly List<SimActor>Disabled=new List<SimActor>();
}
}