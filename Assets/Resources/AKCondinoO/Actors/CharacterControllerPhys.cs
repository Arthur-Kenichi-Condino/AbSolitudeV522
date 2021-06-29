using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AKCondinoO.Actors{public class CharacterControllerPhys:MonoBehaviour{
[NonSerialized]public CharacterController controller;
void Awake(){

//...to do: SimActor GetActors e SimActor IsGrounded e find valid pos on enable e disable this gameObject when out of sight
controller=GetComponent<CharacterController>();
IsGrounded=true;

}
/*  do collider changes based on is grounded or not  */public bool IsGrounded{get{return IsGrounded_v;}protected set{if(IsGrounded_v!=value){

//...

IsGrounded_v=value;
}}
}[NonSerialized]protected bool IsGrounded_v;
[NonSerialized]protected Vector3 inputMoveSpeed=Vector3.zero;
void Update(){

//...
IsGrounded=controller.isGrounded;if(!IsGrounded){
                       
//...

}else{

//...

}
            
//...
controller.SimpleMove(inputMoveSpeed);

}
}
}