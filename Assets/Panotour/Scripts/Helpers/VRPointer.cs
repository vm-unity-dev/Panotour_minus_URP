using UnityEngine;
using System.Collections;
using System;

namespace Mbryonic {

	/// <summary>
	/// Can create a behaviour that is attached to a pointer
	/// </summary>


	/* VRPointer is a class that represents a pointing device the user will use.
	   Typically this is a HMD Gaze or a hand device such as Oculus Touch, HTC Vive
	   This is used to abstract the different types of pointers away.
	   Pointers don't only point, they also have position and a mass so can be used for other touch interactions
	   We will probably need to split this in the future to support more sophisticated touch devices.
	   VRPointers can be used with VRInteractiveItems or with the standard Unity UI
	*/
	public class VRPointer : MonoBehaviour {

		#region PublicTypes
		public enum PointerType { Head, Hand_Left, Hand_Right };
		public enum ButtonID { Trigger, Grip, Joystick, Menu, ControllerSpecificButton1, ControllerSpecificButton2, ControllerSpecificButton3}
		#endregion

		#region InspectorVars
		[Tooltip("Pointer or reticule")]
		[SerializeField] private PointerGuide m_pointerGuide;			// This is the new reticule
		[Tooltip("Which hand or head this belongs to")]
		[SerializeField] private PointerType m_type;
		[Tooltip("Optional transform to use for the raycast")]
		[SerializeField] private LayerMask m_ExclusionLayers;           // Layers to exclude from the raycast.
		[Tooltip("Set to zero to disable autoclick")]
		[SerializeField] private float m_autoClickTime = 2f;
		[SerializeField] private float m_RayLength = 500f;              // How far into the scene the ray is cast.

		[Header("Debug Options")]
		[SerializeField] private bool m_ShowDebugRay;                   // Optionally show the debug ray.
		#endregion

		#region PublicVars
		public PointerGuide Guide {
			get { return m_pointerGuide; }
			set {
				m_pointerGuide = value;
				if (m_pointerGuide!=null) m_pointerGuide.Own (this);
			}
		}
		public PointerType Type { get { return m_type; } }
		public virtual Ray Ray() { return new UnityEngine.Ray(); }
		public virtual LayerMask RayMask { get { return m_ExclusionLayers; } set { m_ExclusionLayers = value; }}
		public event Action<RaycastHit> OnRaycasthit;                   // This event is called every frame that the user's gaze is over a collider.
		public virtual Collider Collider { get { return GetComponentInChildren<Collider>(); } }
		public virtual Transform Transform { get { return transform; } }
		public virtual Vector3 Position { get { return transform.position; } }
		public virtual Quaternion Rotation { get { return transform.rotation; } }
		public virtual VRPlayer Owner { get { return m_owner; } }
		public bool Interactive { get { return m_interactive; } set { m_interactive = value; if (m_pointerGuide) m_pointerGuide.SetState(value ? PointerGuide.State.Normal : PointerGuide.State.Hidden); } }
		public bool IsHand() { return m_type == PointerType.Hand_Left || m_type == PointerType.Hand_Right; }
		public virtual void Haptic(ButtonID buttonID, float length = 0.4f, float frequency = 30f, float strength = 1f) { }
		public VRInteractiveItem HoveringOver { get { return m_CurrentInteractible; } }
		#endregion

		#region PrivateVars
		private VRInteractiveItem m_CurrentInteractible;                //The current interactive item
		private VRInteractiveItem m_LastInteractible;                   //The last interactive item
		private bool m_TriggerDown = false;
		private float m_TriggerTime = 0.0f;
		protected Vector2 m_thumbStickDirection = Vector2.zero;
		protected Collider m_Collider = null;
		protected VRPlayer m_owner;
		protected float m_hoverTimer = 0f;
		protected bool m_interactive = true;           // are we allowed to interact with anything
		protected bool m_kinematic = false;
		private bool m_clickLatch = false;
		private bool m_UIReportedHit = false;
		private VRInteractiveItem m_UIHit = null;
		#endregion

		#region PublicMethods
		// STore the player this pointer belongs to
		public void Own(VRPlayer owner) {
			m_owner = owner;
		}

		// Returns if the trigger has just been pressed
		public virtual bool TriggerDown() { 
			return m_TriggerDown && m_TriggerTime == 0f;
		}

		// Returns if the trigger has just been released
		public virtual bool TriggerUp() {
			return !m_TriggerDown && m_TriggerTime > 0f;
		}

		// Returns if the trigger is currently down
		public virtual bool Trigger() {
			return m_TriggerDown;
		}

		// Returns how long the trigger has been held for
		public virtual float TriggerHeldTime() {
			return m_TriggerTime;
		}

		public virtual float TriggerAmount() {
			return m_TriggerDown ? 1f : 0f;
		}

		// Returns another generic pointer button
		public virtual bool Button(ButtonID buttonID) {return false; }
		// Returns another generic pointer button
		public virtual bool ButtonDown(ButtonID buttonID) { return false; }
		// Returns another generic pointer button
		public virtual bool ButtonUp(ButtonID buttonID) { return false; }

		// Utility for other classes to get the current interactive item
		public VRInteractiveItem CurrentInteractible {
			get { return m_CurrentInteractible; }
		}

		public virtual Vector2 Thumbstick {
			get { return m_thumbStickDirection; }
		}

		// Child classes can call this to set the trigger state
		public virtual void SetTrigger(bool triggerDown) {
			if (!m_TriggerDown) {
				m_TriggerTime = 0f;
			}

			if (m_TriggerDown && triggerDown) {
				m_TriggerTime += Time.deltaTime;
			}
			
			m_TriggerDown = triggerDown;
		}


		#endregion

		#region PrivateMethods
		// Internal method - should be called on update by subclassed controller once input polled.
		protected virtual void SetThumbstickDirection(Vector2 direction) {
			m_thumbStickDirection = direction;
		}

		// Initialisation
		public virtual void Start() {
			m_Collider = GetComponent<Collider>();
			if (m_pointerGuide) m_pointerGuide.Own(this);

        }

		// Update function called by Unity
		public virtual void Update() {
			// Do the raycast
			if (Interactive) Raycast();
			// If this is a head, we just look at the standard input trigger and map this to this pointer.
			if (m_type == PointerType.Head) {
				SetTrigger(Input.GetMouseButton(0));
				if (TriggerDown()) {
				 HandleClick();
				}
			}
    }


		// Called when component disabled
		private void OnDisable() {
	
			if (m_pointerGuide) m_pointerGuide.SetState(PointerGuide.State.Hidden);
		}

		private void Raycast() {
			// Show the debug ray if required
			if (m_ShowDebugRay) {
				Debug.DrawRay(Transform.position,Transform.forward * m_RayLength, Color.blue);
			}

			// Create a ray that points forwards from the camera.
			Ray ray = new Ray(Transform.position,Transform.forward);
			RaycastHit hit = new RaycastHit();

			// Do the raycast forweards to see if we hit an interactive item
			if (m_UIHit!=null || Physics.Raycast(ray, out hit, m_RayLength, ~m_ExclusionLayers)) {
				//if (m_type == PointerType.Head) Debug.Log(gameObject.name+" "+Time.time + ": Hit, UI hit = " + (m_UIHit!= null));
				if (m_UIHit==null) {
					VRInteractiveItem interactible = hit.collider.GetComponent<VRInteractiveItem>(); //attempt to get the VRInteractiveItem on the hit object
					m_CurrentInteractible = interactible;
				}
				else {
					m_CurrentInteractible = m_UIHit;
				}

				// If we hit an interactive item and it's not the same as the last interactive item, then call Over
				if (m_CurrentInteractible && m_CurrentInteractible != m_LastInteractible) {
					m_CurrentInteractible.Over(this);
					m_hoverTimer = 0f;
					if (m_pointerGuide && !m_CurrentInteractible.Disabled) m_pointerGuide.SetState(PointerGuide.State.Hover, 0f);
					m_clickLatch = false;
				}

				if (m_CurrentInteractible && 
					!m_CurrentInteractible.Disabled && 
					m_CurrentInteractible == m_LastInteractible) {
					// Count up
					m_hoverTimer += Time.deltaTime;
				//	Debug.Log (m_CurrentInteractible.gameObject.name + " " + m_hoverTimer);
					if (!m_clickLatch && (m_hoverTimer >= m_autoClickTime && m_autoClickTime>0f) || TriggerDown()) {
						m_clickLatch = true;
						m_CurrentInteractible.Click(this);
						if (m_pointerGuide) m_pointerGuide.SetState(PointerGuide.State.Activated);
					} else {
						if (m_pointerGuide && (m_pointerGuide.GetState() != PointerGuide.State.Hidden && m_pointerGuide.GetState() != PointerGuide.State.Inactive)) {
							m_pointerGuide.SetState(PointerGuide.State.Hover, m_autoClickTime == 0f ? 0f : (m_hoverTimer / m_autoClickTime));
						}
					}
				}

				// Deactive the last interactive item 
				if (m_CurrentInteractible != m_LastInteractible)
					DeactiveLastInteractible();

				if (!m_CurrentInteractible) {
					if (m_pointerGuide) m_pointerGuide.SetState(PointerGuide.State.Normal);
					m_hoverTimer = 0f;
					m_clickLatch = false;
				}

				m_LastInteractible = m_CurrentInteractible;

				// Something was hit, set at the hit position.
				if (m_pointerGuide)
					m_pointerGuide.SetTarget(hit);
					

				if (OnRaycasthit != null)
					OnRaycasthit(hit);
			}else {
				DeactiveLastInteractible();
				m_CurrentInteractible = null;

				if (m_pointerGuide) {
					m_pointerGuide.SetTarget();
					m_pointerGuide.SetState(PointerGuide.State.Normal);
				}
				m_hoverTimer = 0f;
			}
			
        }

		private void DeactiveLastInteractible() {
			if (m_LastInteractible == null)
				return;
			m_LastInteractible.Out(this);
			m_LastInteractible = null;
		}

		private void HandleUp() {
			if (m_CurrentInteractible != null)
				m_CurrentInteractible.Up(this);
		}

		private void HandleDown() {
			if (m_CurrentInteractible != null)
				m_CurrentInteractible.Down(this);
		}

		private void HandleClick() {
			if (m_CurrentInteractible != null)
				m_CurrentInteractible.Click(this);
		}

		private void HandleDoubleClick() {
			if (m_CurrentInteractible != null)
				m_CurrentInteractible.DoubleClick(this);
		}
		#endregion
	}
}