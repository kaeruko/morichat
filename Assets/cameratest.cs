using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cameratest : MonoBehaviour {
	public Camera mainCamera;



	// Use this for initialization
	void Start () {

	}

	// Update is called once per frame
	void Update () {

		if(Input.touchCount > 0){
			// Debug.Log("=========tap start========");
			Touch touch = Input.GetTouch(0);
			Vector3 dist = touch.position;
			// Vector3 forwardVector = new Vector3(0,0,0);
			Vector3 scrn = mainCamera.ScreenToWorldPoint(new Vector3(dist.x, dist.y, mainCamera.nearClipPlane));
			// forwardVector = new Vector3( scrn.x ,  0, scrn.z) - new Vector3( current.x , 0, current.z  );
			//dist x=10 current x= 5の場合;
			// +5  +(dist - current)
			//dist x=-5 current x= 5の場合;
			// -10 +(dist - current)

			// if(){

			// }


			Debug.Log("dist:"+scrn);


			// Debug.Log("current:"+current);
			// Debug.Log("forwardVector:"+forwardVector);



		}




	}
}
