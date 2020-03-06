using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR;

public class VRControlSimulator : MonoBehaviour {
	[SerializeField] private Transform m_camera;
	[SerializeField] private float m_mouseSpeed;
	[SerializeField] private bool m_reverseControls = false;

	private Vector2 m_startMousePosition;
	private Vector2 m_lastMousePosition;
	private Quaternion m_startRotation;

	// Use this for initialization
	void Start () {
		if (m_camera == null)
			m_camera = this.transform;
	}
	
	// Update is called once per frame
	void Update () {
		if (UnityEngine.XR.XRSettings.enabled) {
		}

		#if UNITY_EDITOR

		if (Input.GetMouseButtonDown (0)) {
//			m_startMousePosition.x= Input.GetAxis ("Horizontal");
//			m_startMousePosition.y= Input.GetAxis ("Vertical");
			m_lastMousePosition = m_startMousePosition = Input.mousePosition;
			m_startRotation = m_camera.rotation;
		}

		if (Input.GetMouseButton (0)) {
//			float x = Input.GetAxis ("Horizontal");
//			float y = Input.GetAxis ("Vertical");
			Vector3 mousePosition = Input.mousePosition;
			Vector2 sign = new Vector2(1f,-1f);

			if (m_reverseControls) { sign.x = -1f; sign.y = 1f; }
			//m_camera.Rotate (new Vector3( sign.x * (mousePosition.y-m_lastMousePosition.y)*m_mouseSpeed, sign.y * (mousePosition.x-m_lastMousePosition.x)*m_mouseSpeed,0f));
			m_camera.rotation = m_startRotation * Quaternion.Euler(new Vector3( sign.x * (m_startMousePosition.y-m_lastMousePosition.y)*m_mouseSpeed, sign.y * (m_startMousePosition.x-m_lastMousePosition.x)*m_mouseSpeed,0f));

			m_lastMousePosition = mousePosition;
		}
		#endif
	}
}
