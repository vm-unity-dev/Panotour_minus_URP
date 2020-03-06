using System;
using UnityEngine;

namespace Mbryonic
{
    // This class should be added to any gameobject in the scene
    // that should react to input based on the user's gaze.
    // It contains events that can be subscribed to by classes that
    // need to know about input specifics to this gameobject.
    public class VRInteractiveItem : MonoBehaviour
    {
		public delegate void PointerAction(VRInteractiveItem origin, VRPointer pointer); 
		[SerializeField] private bool m_hide;
		[SerializeField] private bool m_disabled;

        public event PointerAction OnOver;             // Called when the gaze moves over this object
        public event PointerAction OnOut;              // Called when the gaze leaves this object
        public event PointerAction OnClick;            // Called when click input is detected whilst the gaze is over this object.
        public event PointerAction OnDoubleClick;      // Called when double click input is detected whilst the gaze is over this object.
        public event PointerAction OnUp;               // Called when Fire1 is released whilst the gaze is over this object.
        public event PointerAction OnDown;             // Called when Fire1 is pressed whilst the gaze is over this object.
		public bool Hidden { get { return m_hide; } set { m_hide = value; }}

		public object Context {  get { return m_context; } set { m_context = value; } }

		private object m_context;		// application specific context that can be written / read

        protected bool m_IsOver;

		public bool Disabled { get { return m_disabled; } set { m_disabled = value;  } }

        public bool IsOver
        {
            get { return m_IsOver; }              // Is the gaze currently over this object?
        }


        // The below functions are called by the  when the appropriate input is detected.
        // They in turn call the appropriate events should they have subscribers.
        public void Over(VRPointer pointer)
        {
            m_IsOver = true;
            if (OnOver != null)
                OnOver(this,pointer);
        }

        public void Out(VRPointer pointer)
        {
            m_IsOver = false;

            if (OnOut != null)
                OnOut(this,pointer);
        }

        public virtual void Click(VRPointer pointer)
        {
			if (OnClick != null)
                OnClick(this,pointer);
        }

        public void DoubleClick(VRPointer pointer)
        {
            if (OnDoubleClick != null)
                OnDoubleClick(this,pointer);
        }

        public void Up(VRPointer pointer)
        {
            if (OnUp != null)
                OnUp(this,pointer);
        }

        public void Down(VRPointer pointer)
        {
            if (OnDown != null)
                OnDown(this,pointer);
        }
    }
}