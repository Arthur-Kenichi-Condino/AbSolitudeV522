using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AKCondinoO{public class MainCamera:MonoBehaviour{public bool LOG=true;public int LOG_LEVEL=-50;public int GIZMOS_ENABLED=1;
void Awake(){
Camera.main.transparencySortMode=TransparencySortMode.Default;

//...

}
[NonSerialized]Vector3 inputViewRotationEuler;[NonSerialized]float ViewRotationSmoothValue=.05f;[NonSerialized]Vector3 tgtRot,tgtRot_Pre;[NonSerialized]float tgtRotLerpTime;[NonSerialized]float tgtRotLerpInterval=.05f;[NonSerialized]float tgtRotLerpVal;[NonSerialized]Quaternion tgtRotLerpA,tgtRotLerpB;[NonSerialized]float tgtRotLerpSpeed=25f;
[NonSerialized]Vector3 inputMoveSpeed;[NonSerialized]Vector3 MoveAcceleration=new Vector3(1.0f,1.0f,1.0f);[NonSerialized]Vector3 MaxMoveSpeed=new Vector3(10.0f,10.0f,10.0f);
void Update(){

//...

#region ROTATE
inputViewRotationEuler.x+=-Enabled.MOUSE_ROTATION_DELTA_Y[0]*ViewRotationSmoothValue;
inputViewRotationEuler.y+= Enabled.MOUSE_ROTATION_DELTA_X[0]*ViewRotationSmoothValue;
inputViewRotationEuler.x=inputViewRotationEuler.x%360;
inputViewRotationEuler.y=inputViewRotationEuler.y%360;
#endregion
#region FORWARD BACKWARD
if((bool)Enabled.FORWARD [0]){inputMoveSpeed.z+=MoveAcceleration.z;} 
if((bool)Enabled.BACKWARD[0]){inputMoveSpeed.z-=MoveAcceleration.z;}
if(!(bool)Enabled.FORWARD[0]&&!(bool)Enabled.BACKWARD[0]){inputMoveSpeed.z=0;}
if( inputMoveSpeed.z>MaxMoveSpeed.z){inputMoveSpeed.z= MaxMoveSpeed.z;}
if(-inputMoveSpeed.z>MaxMoveSpeed.z){inputMoveSpeed.z=-MaxMoveSpeed.z;}
#endregion

//...

if(inputViewRotationEuler!=Vector3.zero){
tgtRot+=inputViewRotationEuler;
inputViewRotationEuler=Vector3.zero;
}
if(tgtRotLerpTime==0){
if(tgtRot!=tgtRot_Pre){
if(LOG&&LOG_LEVEL<=-50)Debug.Log("input rotation detected:start rotating to tgtRot:"+tgtRot);
tgtRotLerpVal=0;
tgtRotLerpA=transform.rotation;
tgtRotLerpB=Quaternion.Euler(tgtRot);
tgtRotLerpTime+=Time.deltaTime;
tgtRot_Pre=tgtRot;
}
}else{
tgtRotLerpTime+=Time.deltaTime;
}
if(tgtRotLerpTime!=0){
tgtRotLerpVal+=tgtRotLerpSpeed*Time.deltaTime;
if(tgtRotLerpVal>=1){
tgtRotLerpVal=1;
tgtRotLerpTime=0;
if(LOG&&LOG_LEVEL<=-50)Debug.Log("tgtRot:"+tgtRot+" reached");
}
if(LOG&&LOG_LEVEL<=-100)Debug.Log("tgtRotLerpA:"+tgtRotLerpA+";tgtRotLerpB:"+tgtRotLerpB);
transform.rotation=Quaternion.Lerp(tgtRotLerpA,tgtRotLerpB,tgtRotLerpVal);
if(tgtRotLerpTime>tgtRotLerpInterval){
if(tgtRot!=tgtRot_Pre){
if(LOG&&LOG_LEVEL<=-50)Debug.Log("get new tgtRot:"+tgtRot+";don't need to lerp all the way to old target before going to a new one");
tgtRotLerpTime=0;
}
}
}

//...

}
}
}