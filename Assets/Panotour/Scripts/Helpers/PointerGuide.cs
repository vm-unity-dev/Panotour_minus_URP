using UnityEngine;
using System.Collections;

namespace Mbryonic {

	
	public abstract class PointerGuide : MonoBehaviour {

		public enum State { Hidden,		// Doesn't work currently - use Show method instead
							Inactive,	// Show but can't interact with anything
							Normal,		// Normal
							Hover,		// Hovering over an interactive item
							Activated }; // Activated an interactive item

		public abstract void SetState(State state, float timerIndication = 0f);

		public abstract State GetState();
		public abstract void Show(bool state);

		public abstract void Own(VRPointer pointer);
		// The target is the end point of the Pointer. This could be an eye recitle or a 'laser' end point
		public abstract void SetTarget();
		public abstract void SetTarget(RaycastHit hit);
	}

}