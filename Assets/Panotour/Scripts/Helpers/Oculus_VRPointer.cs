using UnityEngine;
using System.Collections;
using Mbryonic;

namespace Mbryonic {
	public class Oculus_VRPointer : VRPointer {

		protected OVRInput.Controller m_ovrController;
		public float m_triggerThreshold = 0.9f;

		OVRInput.Button[] controllerMappings = { OVRInput.Button.PrimaryIndexTrigger, OVRInput.Button.PrimaryHandTrigger, OVRInput.Button.PrimaryThumbstick, OVRInput.Button.Start, OVRInput.Button.One, OVRInput.Button.Two, OVRInput.Button.Any };

		// Use this for initialization
		public override void Start() {
			base.Start();
			m_Collider = GetComponent<Collider>();

			if (OVRPlugin.GetSystemHeadsetType() == OVRPlugin.SystemHeadset.Oculus_Go) { //Application.platform == RuntimePlatform.Android) {
				if (Type == PointerType.Hand_Right) m_ovrController = OVRInput.Controller.RTrackedRemote;
				if (Type == PointerType.Hand_Left) m_ovrController = OVRInput.Controller.LTrackedRemote;
				if (Type == PointerType.Head) m_ovrController = OVRInput.Controller.None;
			}
			else {
				if (Type == PointerType.Hand_Right) m_ovrController = OVRInput.Controller.RTouch;
				if (Type == PointerType.Hand_Left) m_ovrController = OVRInput.Controller.LTouch;
				if (Type == PointerType.Head) m_ovrController = OVRInput.Controller.None;
			}
		}

		public override float TriggerAmount() {
			return OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger);
		}

		// Update is called once per frame
		public override void Update() {
			base.Update();


			if (Type != PointerType.Head) {
				if (!m_kinematic) {
					transform.localRotation = OVRInput.GetLocalControllerRotation(m_ovrController);
					transform.localPosition = OVRInput.GetLocalControllerPosition(m_ovrController);

					if (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, m_ovrController)) {
						SetTrigger(true);
					}
					else {
						SetTrigger(false);
					}

					//Note this is already normalised 
					m_thumbStickDirection = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, m_ovrController);
				}
			}

		}

		public override Ray Ray() {
			return new Ray(transform.position, transform.forward);
		}

		public override bool Button(ButtonID buttonID) {
			if ((int)buttonID >= controllerMappings.Length)
				return false;
			return OVRInput.Get(controllerMappings[(int)buttonID], m_ovrController);

		}

		public override bool ButtonDown(ButtonID buttonID) {
			if ((int)buttonID >= controllerMappings.Length)
				return false;
			return OVRInput.GetDown(controllerMappings[(int)buttonID], m_ovrController);
		}

		public override bool ButtonUp(ButtonID buttonID) {
			if ((int)buttonID >= controllerMappings.Length)
				return false;
			return OVRInput.GetUp(controllerMappings[(int)buttonID], m_ovrController);
		}

		protected IEnumerator HapticPulse(float length, float frequency, float strength) {
			OVRInput.SetControllerVibration(frequency, strength, m_ovrController);
			yield return new WaitForSeconds(length);
			OVRInput.SetControllerVibration(0f, 0f, m_ovrController);
		}

		public override void Haptic(ButtonID buttonID, float length = 0.4f, float frequency = 30f, float strength = 1f) {
			// We ignore the button ID
			StartCoroutine(HapticPulse(length, frequency, strength));
		}

	}

}