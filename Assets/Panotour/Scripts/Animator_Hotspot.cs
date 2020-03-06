using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mbryonic;

namespace Mbryonic.Panotour {
	public class Animator_Hotspot : MonoBehaviour {

		Animator animator;

		// Use this for initialization
		void Start() {

			animator = GetComponent<Animator>();
			VRInteractiveItem interactive = GetComponent<VRInteractiveItem>();
			interactive.OnOver += Interactive_OnOver;
			interactive.OnOut += Interactive_OnOut;
			interactive.OnClick += Interactive_OnClick;
		}

		void Interactive_OnOut(VRInteractiveItem origin, VRPointer pointer) {
			animator.SetBool("Over", false);
		}

		void Interactive_OnOver(VRInteractiveItem origin, VRPointer pointer) {
			animator.SetBool("Over", true);
		}

		void Interactive_OnClick(VRInteractiveItem origin, VRPointer pointer) {
			animator.SetBool("Over", false);
		}

	}

}