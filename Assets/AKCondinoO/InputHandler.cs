using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AKCondinoO{public class InputHandler:MonoBehaviour{
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
public static object[]INTERACT={KeyCode.G,"holdDelay",1f};//  holdDelay so you can't, for example, "steal an item" instantly
}
}