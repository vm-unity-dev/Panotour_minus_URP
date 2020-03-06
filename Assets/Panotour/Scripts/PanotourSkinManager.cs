using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using UnityEngine.UI;
using Mbryonic;
using TMPro;

namespace Mbryonic.Panotour {

	[Serializable]
	public class HotspotConfig {
		public string ID;
		public string prefab;			// if blank reverts to default
		public string sprite;
		public string text;
		public float scale = 1f;
	}

	[Serializable]
	public class Skin {
		public HotspotConfig[] hotspotConfigs;
	}

	public class PanotourSkinManager : MonoBehaviour {

		#region PublicVars
		private Skin skin;
		public Skin Skin { get { return skin; } }

		public GameObject defaultHotspotPrefab;
		#endregion

		#region ProtectedVars
		protected string tourName;
		#endregion


		public IEnumerator LoadSkin(string path) {

			string name = path.Split('_')[0];
#if UNITY_ANDROID && !UNITY_EDITOR

		string jsonPath = Application.streamingAssetsPath + "/" + path;
		WWW www = new WWW(jsonPath);
		yield return www;
		Debug.Log("Loaded skin from " + jsonPath);

		skin = JsonUtility.FromJson<Skin>(www.text);
#else
			if (File.Exists(Application.streamingAssetsPath + "/" + path)) {
				using (FileStream fs = new FileStream(Application.streamingAssetsPath + "/" + path, FileMode.Open)) {
					using (StreamReader reader = new StreamReader(fs)) {
						string fileContent = reader.ReadToEnd();
						if (fileContent != null) {
							if (JsonUtility.FromJson<Skin>(fileContent) != null) {
								skin = JsonUtility.FromJson<Skin>(fileContent);
							}
						}
					}
				}
			}
#endif
			tourName = name;
			yield return null;
		}

		protected void Save(string path) {
			string name = path.Split('_')[0];
			using (FileStream fs = new FileStream(Application.streamingAssetsPath + "/" + name + "/" + path, FileMode.Create)) {
				using (StreamWriter writer = new StreamWriter(fs)) {
					writer.WriteLine(JsonUtility.ToJson(skin));
				}
			}
		}


		// Search for a icon
		HotspotConfig GetHotspotConfig(string location, string id) {
			if (skin != null && skin.hotspotConfigs != null) {
				string searchString = location + "/" + id;
				for (int i = 0; i < skin.hotspotConfigs.Length; i++) {
					if (searchString == skin.hotspotConfigs[i].ID) {
						return skin.hotspotConfigs[i];
					}
				}
				searchString = id;
				for (int i = 0; i < skin.hotspotConfigs.Length; i++) {
					if (searchString == skin.hotspotConfigs[i].ID) {
						return skin.hotspotConfigs[i];
					}
				}
			}
			return null;
		}

		// Recursive
		private GameObject FindRecursive(GameObject parent, string name) {
			GameObject obj = null;
			for (int i = 0; i < parent.transform.childCount; i++) {
				obj = FindRecursive(parent.transform.GetChild(i).gameObject, name);
				if (obj != null) return obj;
				if (parent.transform.GetChild(i).gameObject.name == name) return parent.transform.GetChild(i).gameObject;
			}
			return obj;
		}

		public GameObject CreateHotspot(string location, string id) {
			// Look up this icon
			HotspotConfig config = GetHotspotConfig(location, id);
			if (config == null) {
				Debug.LogError("Cannot load hotspot config: " + location + "/" + id);
				return null;
			}

			GameObject hotspot = null;

			if (config.prefab == "" && defaultHotspotPrefab) {
				hotspot = Instantiate(defaultHotspotPrefab.gameObject) as GameObject;
			}
			else {
				hotspot = Instantiate(Resources.Load(tourName + "/Skin/" + config.prefab) as GameObject);
			}

			// Setup the hotspot - we look for specific object names
			if (hotspot) {
				hotspot.gameObject.name = "Hotspot_" + config.ID; // give the object a name so we can find easily in the editor

				hotspot.transform.localScale = Vector3.one * config.scale;

				GameObject item = null;
				item = FindRecursive(hotspot, "iconDefault");
				if (item && config.sprite != "") {
					Image image = item.GetComponent<Image>();
					string path = tourName + "/Skin/" + config.sprite;
					image.sprite = Resources.Load<Sprite>(path);
					if (image.sprite == null) Debug.LogError("Could not load icon resource: " + path);
					image.preserveAspect = true;
				}

				item = FindRecursive(hotspot, "Text");
				if (item && config.text != "") {
					item.GetComponent<TextMeshProUGUI>().text = config.text;
				}
			}

			return hotspot;
		}
	}

}