using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AKCondinoO{public class MainCamera:MonoBehaviour{public bool LOG=true;public int LOG_LEVEL=-50;public int GIZMOS_ENABLED=1;
void Awake(){
Camera.main.transparencySortMode=TransparencySortMode.Default;

//...

tgtRot=tgtRot_Pre=transform.eulerAngles;
tgtPos=tgtPos_Pre=transform.position;
}
[NonSerialized]Vector3 inputViewRotationEuler;[NonSerialized]float ViewRotationSmoothValue=.025f;[NonSerialized]Vector3 tgtRot,tgtRot_Pre;[NonSerialized]float tgtRotLerpTime;[NonSerialized]float tgtRotLerpMaxTime=.05f;[NonSerialized]float tgtRotLerpVal;[NonSerialized]Quaternion tgtRotLerpA,tgtRotLerpB;[NonSerialized]float tgtRotLerpSpeed=25f;
[NonSerialized]Vector3 inputMoveSpeed;[NonSerialized]Vector3 MoveAcceleration=new Vector3(.01f,.01f,.01f);[NonSerialized]Vector3 MaxMoveSpeed=new Vector3(.1f,.1f,.1f);[NonSerialized]Vector3 tgtPos,tgtPos_Pre;[NonSerialized]float tgtPosLerpTime;[NonSerialized]float tgtPosLerpMaxTime=.05f;[NonSerialized]float tgtPosLerpVal;[NonSerialized]Vector3 tgtPosLerpA,tgtPosLerpB;[NonSerialized]float tgtPosLerpSpeed=25f;
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

#region ROTATION LERP
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
if(tgtRotLerpTime>tgtRotLerpMaxTime){
if(tgtRot!=tgtRot_Pre){
if(LOG&&LOG_LEVEL<=-50)Debug.Log("get new tgtRot:"+tgtRot+";don't need to lerp all the way to old target before going to a new one");
tgtRotLerpTime=0;
}
}
}
#endregion
#region POSITION LERP
if(inputMoveSpeed!=Vector3.zero){
tgtPos+=(transform.rotation*inputMoveSpeed);
}
if(tgtPosLerpTime==0){
if(tgtPos!=tgtPos_Pre){
if(LOG&&LOG_LEVEL<=-50)Debug.Log("input movement detected:start going to tgtPos:"+tgtPos);
tgtPosLerpVal=0;
tgtPosLerpA=transform.position;
tgtPosLerpB=tgtPos;

//...

tgtPosLerpTime+=Time.deltaTime;
tgtPos_Pre=tgtPos;
}
}else{

//..

tgtPosLerpTime+=Time.deltaTime;
}
if(tgtPosLerpTime!=0){
tgtPosLerpVal+=tgtPosLerpSpeed*Time.deltaTime;
if(tgtPosLerpVal>=1){
tgtPosLerpVal=1;
tgtPosLerpTime=0;
if(LOG&&LOG_LEVEL<=-50)Debug.Log("tgtPos:"+tgtPos+" reached");
}
transform.position=Vector3.Lerp(tgtPosLerpA,tgtPosLerpB,tgtPosLerpVal);

//...

if(tgtPosLerpTime>tgtPosLerpMaxTime){
if(tgtPos!=tgtPos_Pre){
if(LOG&&LOG_LEVEL<=-50)Debug.Log("get new tgtPos:"+tgtPos+";don't need to lerp all the way to old target before going to a new one");
tgtPosLerpTime=0;
}

//...

}
}

//...                
                
#endregion

//...

}
}
}