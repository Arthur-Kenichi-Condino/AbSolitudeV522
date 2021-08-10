using MLAPI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace AKCondinoO{public class UIHandler:MonoBehaviour{
public void OnPause(){

//...

}
public void OnResume(){
MenuPanel.SetActive(false);

//...

}
public GameObject MenuPanel;
public void OnMenuClick(){
MenuPanel.SetActive(!MenuPanel.activeSelf);
}
[SerializeField]Button btnHost;
public void OnHostClick(){
NetworkManager.Singleton.StartHost();
btnHost  .interactable=false;
btnClient.interactable=false;
OnResume();
}
[SerializeField]Button btnClient;
public void OnClientClick(){
NetworkManager.Singleton.StartClient();
btnHost  .interactable=false;
btnClient.interactable=false;
OnResume();
}
}
}