using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Xml;
using UnityEngine.XR;

namespace Mbryonic.Panotour {


	public class PanotourEditor : MonoBehaviour {

		#region InspectorVars
		public PanotourPlayer player;
		public float rotateSteps = 0.2f;
		public float scaleSteps = 0.001f;
		#endregion

		public const float shiftButtonSlowDown = 0.1f;      // if holding shift go at this lower speed (10th)

		public static bool EditorEnabled { get { return enableEditor; } }
		private static bool enableEditor = false;

		private string editingPatch = "";

		// Start is called before the first frame update
		void Start() {
		}


		private void OnGUI() {
			if (!enableEditor) return;

			GUI.Box(new Rect(0, 0, 145, 80), "Editor");
			if (GUI.Button(new Rect(10, 30, 120, 20), "CANCEL")) {
				enableEditor = false;
				editingPatch = "";
				player.ReloadCurrentLocation();		// overwrite all our changes
			}
			if (GUI.Button(new Rect(10,55,120,20), "SAVE")) {
				Debug.Log("SAVE");
				enableEditor = false;
				editingPatch = "";
				using (FileStream fs = new FileStream(player.ActiveTourPath, FileMode.Create)) {
					using (StreamWriter writer = new StreamWriter(fs)) {
						writer.WriteLine(JsonUtility.ToJson(player.ActiveTour));
					}
				}
			}

			foreach (Patch patch in player.ActiveLocation.patches) {
				if (patch.gameObject == null) continue;

				Vector3 screenPos = Camera.main.WorldToScreenPoint(patch.gameObject.transform.position);
				Vector2 guiPosition = new Vector2(screenPos.x, Screen.height - screenPos.y);
				if (XRSettings.enabled) {
					guiPosition = new Vector2(screenPos.x * ((float)Screen.width / (float)XRSettings.eyeTextureWidth),
						Screen.height - (screenPos.y * ((float)Screen.height / (float)XRSettings.eyeTextureHeight)));
				}
				if (editingPatch != patch.media) {
					GUI.backgroundColor = Color.black;
					if (GUI.Button(new Rect(guiPosition.x - 75, guiPosition.y - 75, 150, 50), patch.media)) {
						editingPatch = patch.media;
					}
				}
				else {
					GUI.backgroundColor = Color.red;
					if (GUI.Button(new Rect(guiPosition.x - 75, guiPosition.y - 75, 150, 50), patch.media)) {
						editingPatch = ""; // click off
					}
				}
			}
		}

		// Update is called once per frame
		void Update() {

			if (Input.GetKeyDown(KeyCode.E)) {
				enableEditor = !enableEditor;
				if (enableEditor) player.Pause(); else player.Unpause();
			}

			if (enableEditor) {

				if (editingPatch != "") {
					ref Patch patch = ref player.ActiveLocation.GetPatch(editingPatch);
					bool shift = Input.GetKey(KeyCode.LeftShift);
					if (Input.GetKeyDown(KeyCode.DownArrow)) {
						patch.rotx += rotateSteps * (shift ? shiftButtonSlowDown : 1f);
						player.UpdateLocation();
					}
					if (Input.GetKeyDown(KeyCode.UpArrow)) {
						patch.rotx -= rotateSteps * (shift ? shiftButtonSlowDown : 1f); 
						player.UpdateLocation();
					}
					if (Input.GetKeyDown(KeyCode.LeftArrow)) {
						patch.roty += rotateSteps * (shift ? shiftButtonSlowDown : 1f); 
						player.UpdateLocation();
					}
					if (Input.GetKeyDown(KeyCode.RightArrow)) {
						patch.roty -= rotateSteps * (shift ? shiftButtonSlowDown : 1f); 
						player.UpdateLocation();
					}
					if (Input.GetKeyDown(KeyCode.KeypadPlus) || Input.GetKeyDown(KeyCode.Plus)) {
						patch.scale += scaleSteps * (shift ? shiftButtonSlowDown : 1f);
						player.UpdateLocation();
					}
					if (Input.GetKeyDown(KeyCode.KeypadMinus) || Input.GetKeyDown(KeyCode.Minus)) {
						patch.scale -= scaleSteps * (shift ? shiftButtonSlowDown : 1f);
						player.UpdateLocation();
					}

				}
			}
		}
	}

}