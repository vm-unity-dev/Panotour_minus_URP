using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Xml;

#if UNITY_EDITOR
namespace Mbryonic.Panotour {

	public class PanotourImporterEditorWindow : EditorWindow {
		string inputPath;
		string outputPath;
		bool overwriteSkin = true;
		bool completed = false;

		[MenuItem("Panotour/Panotour Convertor")]
		// Start is called before the first frame update
		public static void Init() {
			PanotourImporterEditorWindow window = (PanotourImporterEditorWindow)EditorWindow.GetWindow(typeof(PanotourImporterEditorWindow));
			window.Show();
		}

		private void OnGUI() {
			// Window code goes here

			GUILayout.BeginArea(new Rect(10, 10, 400, 500));
			GUILayout.Label("P2VR Importer", EditorStyles.boldLabel);
			GUILayout.Space(10);

			inputPath = GUILayout.TextField(inputPath);
			if (GUILayout.Button("Browse P2VR")) {
				string path = EditorUtility.OpenFilePanel("Select P2VR Tour", "", "p2vr");
				if (path.Length != 0) {
					inputPath = path;
				}
			}

			overwriteSkin = GUILayout.Toggle(overwriteSkin,"Overwrite Skin File");
			
			if (GUILayout.Button("Convert")) {
				XmlDocument doc = new XmlDocument();
				using (FileStream fs = new FileStream(inputPath, FileMode.Open)) {
					using (StreamReader reader = new StreamReader(fs)) {
						doc.Load(reader);
					}
				}

				Skin hotspots;
				Tour tour = PanotourConvertor.CreateTourFromP2VR(doc, out hotspots);

				// Bring up save dialogue
				string outPath = EditorUtility.SaveFilePanel("Save Panotour", Application.streamingAssetsPath, "panotour.json", "json");
				if (outPath.Length != 0) {
					using (FileStream fs = new FileStream(outPath, FileMode.Create)) {
						using (StreamWriter writer = new StreamWriter(fs)) {
							writer.WriteLine(JsonUtility.ToJson(tour));
						}
					}

					if (overwriteSkin){
						string hotspotPath = outPath.Substring(0, outPath.LastIndexOf('.')) + "_skin.json";
						using (FileStream fs = new FileStream(hotspotPath, FileMode.Create)) {
							using (StreamWriter writer = new StreamWriter(fs)) {
								writer.WriteLine(JsonUtility.ToJson(hotspots));
							}
						}
					}
					completed = true;
				}

				
			}
			if (completed) GUILayout.Label("Completed!");
			GUILayout.EndArea();
		}
	}
}
#endif