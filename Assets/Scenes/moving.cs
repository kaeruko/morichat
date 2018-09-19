using System;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class moving : MonoBehaviour {

    public Text m_Text;
    // Use this for initialization
    void Start () {

    }

    // Update is called once per frame
    void Update () {

// Debug.Log(Input.GetMouseButtonDown (0));
// Debug.Log(Input.GetMouseButtonUp (0));


      if(Input.touchCount > 0){

        foreach (var t in Input.touches){
//          Debug.Log(t.position);
        }

        // Touch touch = Input.GetTouch(0);
        // Debug.Log(touch.position);
        // Debug.Log(touch.deltaTime);
        // Debug.Log(touch.deltaPosition);
      }

    }
}
