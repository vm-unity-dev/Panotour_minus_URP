using System;
using UnityEngine;
using UnityEngine.UI;

namespace Mbryonic
{
    // The reticle is a small point at the centre of the screen.
    // It is used as a visual aid for aiming. The position of the
    // reticle is either at a default position in space or on the
    // surface of a VRInteractiveItem as determined by the VREyeRaycaster.
    public class Reticle : PointerGuide
    {
		#region InspectorVars
        public float defaultDistance = 5f;      
        public bool useNormal = false;
		public Image image = null;
		public Image hoverImage = null;
		public Transform reticleTransform = null;
		public Transform pointerTransform = null;
		public LayerMask layerMask = -1;
		public bool hideWhenNoHit = false;
		public float normalDistance = 0.01f;
        #endregion

        #region PublicVars
        public bool Over { get {return over; }}
		#endregion

		#region ProtectedVars
        private Vector3 originalScale;                            // Since the scale of the reticle changes, the original scale needs to be stored.
        private Quaternion originalRotation;                      // Used to store the original rotation of the reticle.
		private bool over = false;
		protected State currentState;
		#endregion

		public override State GetState() {
			return currentState;
		}


        public bool UseNormal
        {
            get { return useNormal; }
            set { useNormal = value; }
        }


        public Transform ReticleTransform { get { return reticleTransform; } }

		public override void Show(bool state) {
			throw new NotImplementedException();
		}

		public override void Own(VRPointer pointer) {
			pointerTransform = pointer.transform;
		}

		private void Awake()
        {

            // Store the original scale and rotation.
            originalScale = reticleTransform.localScale;
            originalRotation = reticleTransform.localRotation;
        }

        public void Update()
        {
            if (image && hideWhenNoHit){
				if (over) {
					foreach (Image im in GetComponentsInChildren<Image>()) {
						im.enabled = true;
					}
				} else {
					foreach (Image im in GetComponentsInChildren<Image>())
						im.enabled = false;
				}
			}
		}

		
		public override void SetState(State state, float timerIndication = 0f) {
			if (currentState == State.Hidden && state != State.Normal) return;
			if (image) {
				foreach (Image im in GetComponentsInChildren<Image>())
					im.enabled = state != State.Hidden && state != State.Inactive;
			}
			currentState = state;
			if (hoverImage)
				hoverImage.fillAmount = Mathf.Clamp01(timerIndication);
        }

		// This overload of SetPosition is used when the the VREyeRaycaster hasn't hit anything.
		public override void SetTarget ()
        {
            reticleTransform.position = pointerTransform.position + pointerTransform.forward * defaultDistance;
            reticleTransform.localScale = originalScale * defaultDistance;
			reticleTransform.LookAt(VRPlayer.main.transform.position);
			over = false;
        }


        // This overload of SetPosition is used when the VREyeRaycaster has hit something.
        public override void SetTarget (RaycastHit hit)
        {
			if ((layerMask & (1<<hit.collider.gameObject.layer)) != 0) {

				reticleTransform.position = hit.point + (hit.normal * normalDistance);
				reticleTransform.localScale = originalScale * hit.distance;
            
				if (useNormal) {
					Quaternion rotation = Quaternion.FromToRotation (Vector3.forward, hit.normal);
					rotation = Quaternion.Euler (rotation.eulerAngles.x, rotation.eulerAngles.y, 0f);
					reticleTransform.rotation = rotation;
				} else {
					if(VRPlayer.main.Head)
						reticleTransform.LookAt (VRPlayer.main.Head.transform.position);
				}

				over = true;
			}
        }
    }
}