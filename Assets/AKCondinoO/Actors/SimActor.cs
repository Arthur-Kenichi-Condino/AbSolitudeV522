using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AKCondinoO.Actors{public class SimActor:MonoBehaviour{public bool LOG=true;public int LOG_LEVEL=1;public int GIZMOS_ENABLED=1;
public Type type{get;protected set;}public int id{get;protected set;}
protected virtual void Awake(){
      
//...
type=GetType();if(!Actors.Count.ContainsKey(type)){Actors.Count.Add(type,1);}else{id=Actors.Count[type]++;}Actors.Get.Add((type,id),this);
if(LOG&&LOG_LEVEL<=1)Debug.Log("I am.."+type+"..and I got instantiated with id.."+id);

//...

IsOutOfSight=true;

}
protected virtual void OnDestroy(){
            
//...

}
public virtual bool IsOutOfSight{get{return IsOutOfSight_v;}set{if(IsOutOfSight_v!=value){IsOutOfSight_v=value;
if(LOG&&LOG_LEVEL<=1)Debug.Log("IsOutOfSight:"+value,this);
if(value){

//...

}else{

//...

}

}}
}[NonSerialized]protected bool IsOutOfSight_v;
protected virtual void Update(){
            
//...

}
}
}