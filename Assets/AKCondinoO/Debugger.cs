using AKCondinoO.Buildings;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AKCondinoO{public class Debugger:MonoBehaviour{
[SerializeField]int        DEBUG_TEST_CREATE_TONS_OF_GAME_OBJECT_COUNT=0;
[SerializeField]GameObject DEBUG_TEST_CREATE_TONS_OF_GAME_OBJECT=null;
[SerializeField]int       DEBUG_TEST_CREATE_TONS_OF_SIM_OBJECT_COUNT=0;
[SerializeField]SimObject DEBUG_TEST_CREATE_TONS_OF_SIM_OBJECT=null;
void Update(){
if(DEBUG_TEST_CREATE_TONS_OF_GAME_OBJECT){var instantiate=DEBUG_TEST_CREATE_TONS_OF_GAME_OBJECT;DEBUG_TEST_CREATE_TONS_OF_GAME_OBJECT=null;
for(int i=0;i<DEBUG_TEST_CREATE_TONS_OF_GAME_OBJECT_COUNT;++i){
Instantiate(instantiate);
}
}
if(DEBUG_TEST_CREATE_TONS_OF_SIM_OBJECT){var instantiate=DEBUG_TEST_CREATE_TONS_OF_SIM_OBJECT;DEBUG_TEST_CREATE_TONS_OF_SIM_OBJECT=null;
for(int i=0;i<DEBUG_TEST_CREATE_TONS_OF_SIM_OBJECT_COUNT;++i){
Instantiate(instantiate);
}
}
}
}
}