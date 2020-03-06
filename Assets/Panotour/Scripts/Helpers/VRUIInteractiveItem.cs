using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Mbryonic
{
    // This class should be added to any gameobject in the scene
    // that should react to input based on the user's gaze.
    // It contains events that can be subscribed to by classes that
    // need to know about input specifics to this gameobject.
    public class VRUIInteractiveItem : VRInteractiveItem
    {
		private BoxCollider m_collider;
		private RectTransform m_rect;

		public void Awake(){
			// Create box collider
			m_rect = GetComponent<RectTransform>();
			if (GetComponent<BoxCollider> () == null) {
				m_collider = gameObject.AddComponent<BoxCollider> ();
				m_collider.size = new Vector3 ((m_rect.rect.size.x * 1.5f), (m_rect.rect.size.y * 1.5f), 1f);
				m_collider.center = m_rect.rect.center;
			} else {
				Debug.Log ("Box Collider already present");
			}
		
		}

		public void Update(){
			m_collider.size = new Vector3((m_rect.rect.size.x * 1.5f), (m_rect.rect.size.y * 1.5f), 1f);
            m_collider.center = m_rect.rect.center;
		}

		public override void Click(VRPointer pointer)
		{
			base.Click (pointer);

			Button button = GetComponent<Button> ();
			if (button!=null && button.onClick != null) {
				button.onClick.Invoke ();
			}
		}

    }
}