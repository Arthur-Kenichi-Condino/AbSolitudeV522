using MLAPI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AKCondinoO{public class UIHandler:MonoBehaviour{
public GameObject MenuPanel;
public void OnMenuClick(){
MenuPanel.SetActive(!MenuPanel.activeSelf);
}
public void OnHostClick(){
NetworkManager.Singleton.StartHost();
}
public void OnClientClick(){
NetworkManager.Singleton.StartClient();
}
}
}