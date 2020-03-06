using UnityEngine;
using System.Collections;
using UnityEngine.XR;


namespace Mbryonic {
	/* Represents a player in VR and is a convenience object in which we can gather all input for the user in one handy location.
		It also abstracts which type of VR rig we are using */
	public class VRPlayer : MonoBehaviour {
		/* Instance */
		public static VRPlayer main;

		public float eyeResolutionScale = 1.5f;

		/* Public types */
		public enum VRPositionalType { None, Standing, Room };

		/* Private vars */
		private VRPointer head;
		private VRPointer leftHand;
		private VRPointer rightHand;

		private bool VRReady = false;
		private VRPositionalType m_VRPositionalType;

		/* Public Accessors */
		public VRPositionalType PositionalType { get { return m_VRPositionalType; } }
		public VRPointer Head { get { if (head && head.isActiveAndEnabled) return head; else return null; } }
		public VRPointer LeftHand { get { if (leftHand && leftHand.isActiveAndEnabled) return leftHand; else return null; } }
		public VRPointer RightHand { get { if (rightHand && rightHand.isActiveAndEnabled) return rightHand; else return null; } }
		public Transform Position { get { return Head.transform; } }
		/* Haptics */

		public VRPointer Pointer(VRPointer.PointerType type) {
			switch (type) {
				case VRPointer.PointerType.Head:
					return head;
				case VRPointer.PointerType.Hand_Right:
					return rightHand;
				case VRPointer.PointerType.Hand_Left:
					return leftHand;
			}
			return null;
		}

		/* For the moment we assume just one player, but this should be a nice way to extend it to multiple in the future */
		void Awake() {
			if (main == null)
				main = this;

			EnumerateVR();
		}

		protected void EnumerateVR() {
			VRReady = false;
			string vrDevice = UnityEngine.XR.XRSettings.loadedDeviceName;
			m_VRPositionalType = VRPositionalType.None;

			// Gather all our VR pointers
			VRPointer[] pointers = FindObjectsOfType<VRPointer>();
			for (int i = 0; i < pointers.Length; i++) {
				if (pointers[i].Type == VRPointer.PointerType.Head)
					head = pointers[i];
				else if (pointers[i].Type == VRPointer.PointerType.Hand_Right)
					rightHand = pointers[i];
				else if (pointers[i].Type == VRPointer.PointerType.Hand_Left)
					leftHand = pointers[i];
			}
			activePointer = pointers[0];

			if (vrDevice == "Oculus") {
				Debug.Log("Detected Oculus Rig");
				if (GetComponent<OVRManager>() == false) {
					gameObject.AddComponent<OVRManager>();
				}

				m_VRPositionalType = VRPositionalType.Standing;
				VRReady = true;
			}

			if (vrDevice == "OpenVR") {
				Debug.Log("This application does not support Steam currently");
			}

			XRSettings.eyeTextureResolutionScale = eyeResolutionScale;

			if (!VRReady) {
				// Reset the camera in the scene to be none VR 
				Camera.main.stereoTargetEye = StereoTargetEyeMask.None;
				Camera.main.fieldOfView = 60f;
			}
		}

		public VRPointer ActivePointer { get { return activePointer; } }
		private VRPointer activePointer = null;

		// Update is called once per frame
		void Update() {

			// Manage controllers being available
			if (VRReady) {
				bool leftControllerAvailable = OVRInput.IsControllerConnected(OVRInput.Controller.LTouch) || OVRInput.IsControllerConnected(OVRInput.Controller.LTrackedRemote);
				bool rightControllerAvailable = OVRInput.IsControllerConnected(OVRInput.Controller.RTouch) || OVRInput.IsControllerConnected(OVRInput.Controller.RTrackedRemote);

				if (leftHand) leftHand.enabled = leftControllerAvailable;
				if (rightHand) rightHand.enabled = rightControllerAvailable;
				if (head) head.enabled = !(leftControllerAvailable || rightControllerAvailable);

				// The activePointer is the last one pressed, or if none then the head
				if (leftHand && leftHand.TriggerDown()) { activePointer = leftHand; }
				if (rightHand && rightHand.TriggerDown()) { activePointer = rightHand; }
				if (head & head.enabled) activePointer = head;

			}
		}
	}

}