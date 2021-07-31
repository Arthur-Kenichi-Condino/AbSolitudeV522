using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;
namespace AKCondinoO{public class InputHandler:MonoBehaviour{public bool LOG=true;public int LOG_LEVEL=1;public int GIZMOS_ENABLED=1;
public static Dictionary<string,object[]>AllCommands=new Dictionary<string,object[]>();public static Dictionary<string,object[]>AllStates=new Dictionary<string,object[]>();
void Awake(){
foreach(FieldInfo field in typeof(Commands).GetFields(BindingFlags.Public|BindingFlags.Static)){
if(field.GetValue(null)is object[]command){
if(LOG&&LOG_LEVEL<=1)Debug.Log("add command input to handle:"+field.Name);
AllCommands.Add(field.Name,command);
}
}
foreach(FieldInfo field in typeof(Enabled).GetFields(BindingFlags.Public|BindingFlags.Static)){
if(field.GetValue(null)is object[]state){
if(LOG&&LOG_LEVEL<=1)Debug.Log("add command input state status field:"+field.Name);
AllStates.Add(field.Name,state);
}
}
foreach(MethodInfo method in GetType().GetMethods(BindingFlags.NonPublic|BindingFlags.Instance)){
if(method.Name=="Get"){var inputType=method.GetParameters()[1].ParameterType;

//...

Delegate result;
if(inputType==typeof(KeyCode))result=method.CreateDelegate(typeof(Func<Func<KeyCode,bool>,KeyCode,bool>),this);else
if(inputType==typeof(int    ))result=method.CreateDelegate(typeof(Func<Func<int    ,bool>,int    ,bool>),this);else
                              result=method.CreateDelegate(typeof(Func<Func<string ,bool>,string ,bool>),this);
                        
//...

GetMethods[inputType]=result;
}
}
Gets.Add(typeof(KeyCode),  keyboardGets);
Gets.Add(typeof(int    ),     mouseGets);
Gets.Add(typeof(string ),controllerGets);
}
[NonSerialized]readonly Dictionary<Type,Delegate>GetMethods=new Dictionary<Type,Delegate>();[NonSerialized]readonly Dictionary<Type,object[]>Gets=new Dictionary<Type,object[]>();
#pragma warning disable IDE0051 // ignore: Remover membros privados não utilizados
bool Get(Func<KeyCode,bool>  keyboardGet,KeyCode   key){return   keyboardGet(   key);}[NonSerialized]readonly Func<KeyCode,bool>[]  keyboardGets=new Func<KeyCode,bool>[3]{Input.GetKey        ,Input.GetKeyUp        ,Input.GetKeyDown        ,};
bool Get(Func<int    ,bool>     mouseGet,int    button){return      mouseGet(button);}[NonSerialized]readonly Func<int    ,bool>[]     mouseGets=new Func<int    ,bool>[3]{Input.GetMouseButton,Input.GetMouseButtonUp,Input.GetMouseButtonDown,};
bool Get(Func<string ,bool>controllerGet,string button){return controllerGet(button);}[NonSerialized]readonly Func<string ,bool>[]controllerGets=new Func<string ,bool>[3]{Input.GetButton     ,Input.GetButtonUp     ,Input.GetButtonDown     ,};
#pragma warning restore IDE0051 
[NonSerialized]public bool Focus=true;
void Update(){
foreach(var command in AllCommands){string name=command.Key;Type type=command.Value[0].GetType();string mode=command.Value[1]as string;object[]state=AllStates[name];state[1]=state[0];UpdateCommandState();
void UpdateCommandState(){bool get(int getsType){if(type==typeof(KeyCode))return((Func<Func<KeyCode,bool>,KeyCode,bool>)GetMethods[type]).Invoke((Func<KeyCode,bool>)Gets[type][getsType],(KeyCode)command.Value[0]);else
                                                 if(type==typeof(int    ))return((Func<Func<int    ,bool>,int    ,bool>)GetMethods[type]).Invoke((Func<int    ,bool>)Gets[type][getsType],(int    )command.Value[0]);else
                                                                          return((Func<Func<string ,bool>,string ,bool>)GetMethods[type]).Invoke((Func<string ,bool>)Gets[type][getsType],(string )command.Value[0]);}
if(mode=="holdDelay"){
if(get(0)){float heldTime=(float)state[2];heldTime+=Time.deltaTime;state[2]=heldTime;}else{state[2]=0f;}

//...
Debug.LogWarning(state[2]);

}

//...

}

//...

}

//...

}
}
public static class Enabled{
public static readonly object[]PAUSE={true,true};
public static readonly object[]FORWARD ={false,false};
public static readonly object[]BACKWARD={false,false};
public static readonly object[]RIGHT   ={false,false};
public static readonly object[]LEFT    ={false,false};
public static readonly object[]JUMP    ={false,false};
public static readonly object[]CROUCH  ={false,false};
public static readonly float[]MOUSE_ROTATION_DELTA_X={0,0};
public static readonly float[]MOUSE_ROTATION_DELTA_Y={0,0};
public static readonly object[]SWITCH_CAMERA_MODE={false,false};//  Free camera, or follow and command a player
public static readonly object[]ACTION_1={false,false};
public static readonly object[]ACTION_2={false,false};
public static readonly object[]INTERACT={false,false,0f};
}
public static class Commands{
public static object[]PAUSE={KeyCode.Tab,"alternateDown"};
public static object[]FORWARD ={KeyCode.W,"activeHeld"};
public static object[]BACKWARD={KeyCode.S,"activeHeld"};
public static object[]RIGHT   ={KeyCode.D,"activeHeld"};
public static object[]LEFT    ={KeyCode.A,"activeHeld"};
public static object[]JUMP    ={KeyCode.E,"whenUp"};
public static object[]CROUCH  ={KeyCode.Q,"whenUp"};
public static float ROTATION_SENSITIVITY_X=360.0f;
public static float ROTATION_SENSITIVITY_Y=360.0f;
public static object[]SWITCH_CAMERA_MODE={KeyCode.RightAlt,"alternateDown"};  //  Free camera, or following the player
public static object[]ACTION_1={(int)0,"activeHeld"};
public static object[]ACTION_2={(int)1,"activeHeld"};
public static object[]INTERACT={KeyCode.G,"holdDelay",2f};//  holdDelay so you can't, for example, "steal an item" instantly
}
}