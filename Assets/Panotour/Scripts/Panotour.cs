using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

namespace Mbryonic.Panotour {

	[Serializable]
	public class TourHotspot {
		public string id;
		public string link;
		public float pan;
		public float tilt;
		public float scale;
		[NonSerialized] public GameObject gameObject;			// reference to runtime representation
	}

	[Serializable]
	public class Patch {
		public string media;
		public float pan;
		public float tilt;
		public float height;
		public float width;
		public float rotx;
		public float roty;
		public float rotz;
		public float scale = 1.621621f;
		public bool master = false;
        public bool autoPlay = false;
		public float volume = 0.20f;
		[NonSerialized] public GameObject gameObject;
	}

	[Serializable]
	public class TourLocation {
		public string id;
		public string panoramaPath;
		public float startpan;
		public float starttilt;
		public float levelpitch;
		public float levelroll;
		public string nextLocation = "";
		public Patch[] patches;
		public TourHotspot[] hotspots;

		public ref Patch GetPatch(string name) {
			for (int i = 0; i < patches.Length; i++) {
				if (patches[i].media == name) return ref patches[i];
			}
			return ref patches[0];
		}

		public ref TourHotspot GetHotspot(string name) {
			for (int i = 0; i < hotspots.Length; i++) {
				if (hotspots[i].id == name) return ref hotspots[i];
			}
			return ref hotspots[0];
		}


	}

	[Serializable]
	public class Tour {
		public string name;
		public string startLocation;
		public TourLocation[] locations;
		public bool autoCycle = false;

		public ref TourLocation GetLocation(string name) {
			for (int i = 0; i < locations.Length; i++) {
				if (locations[i].id == name) return ref locations[i];
			}
			return ref locations[0];
		}


	}

}
 