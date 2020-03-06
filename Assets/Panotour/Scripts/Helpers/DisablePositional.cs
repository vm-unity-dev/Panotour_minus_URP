using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR;

public class DisablePositional : MonoBehaviour {
		public GameObject cameraGameObject;

	void Start() {
	}


	void Update(){
		if (Input.GetKeyDown (KeyCode.R)) {
			UnityEngine.XR.InputTracking.Recenter ();
		}
	}

		// Disable positional tracking by applying an opposite offset from the camera that's being
		// moved by the HMD. This script should be applied on a n empty parent object of the camera.
		void LateUpdate() {
			if (cameraGameObject != null) {
				Vector3 offset = new Vector3(
					-cameraGameObject.transform.localPosition.x,
					-cameraGameObject.transform.localPosition.y,
					-cameraGameObject.transform.localPosition.z);
				transform.localPosition = offset;
			}
		}
	
}
