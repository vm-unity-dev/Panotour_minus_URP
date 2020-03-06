using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Xml;

namespace Mbryonic.Panotour {

	public class PanotourConvertor {

		static XmlDocument doc;

		// This is a magic number to go from the P2VR 'fov' to our own 'scale'.
		private const float FOVtoScale = 0.0190779014f * 0.282f;

		public static Tour CreateTourFromP2VR(XmlDocument doc, out Skin hotspotIcons) {
			Tour tour = new Tour();
			var root = doc.DocumentElement;
			XmlNodeList xmlLocations = doc.SelectNodes("/pano2vrconfig/tour/panorama");
			tour.locations = new TourLocation[xmlLocations.Count];

			hotspotIcons = new Skin();
			hotspotIcons.hotspotConfigs = new HotspotConfig[doc.SelectNodes("/pano2vrconfig/tour/panorama/hotspots/hotspot").Count];
			int iconCounter = 0;
			for (int i = 0; i < xmlLocations.Count; i++) {
				TourLocation location = new TourLocation();
				// Parse Tour Location
				location.id = xmlLocations[i]["id"].InnerText;

				// Get panorama path - a little complex, also these may be renamed by human error
				string tile = xmlLocations[i]["input"]["filename"].InnerText;
				tile = tile.Substring(tile.LastIndexOf("/") + 1);
				tile = tile.Substring(0, tile.LastIndexOf("."));
				location.panoramaPath = tile;
				location.startpan = float.Parse(xmlLocations[i]["viewingparameter"]["pan"]["start"].InnerText);
				location.starttilt = float.Parse(xmlLocations[i]["viewingparameter"]["tilt"]["start"].InnerText);
				location.levelpitch = float.Parse(xmlLocations[i]["input"]["levelpitch"].InnerText);
				location.levelroll = float.Parse(xmlLocations[i]["input"]["levelroll"].InnerText);

				// LOAD Hotspots
				XmlNodeList xmlHotspots = xmlLocations[i].SelectNodes("hotspots/hotspot");
				location.hotspots = new TourHotspot[xmlLocations[i].SelectNodes("hotspots/hotspot/skinid").Count];
				int validHotSpotCounter = 0;
				for (int j = 0; j < xmlHotspots.Count; j++) {
					bool isNotValidHotSpot = (xmlHotspots[j].SelectSingleNode("skinid") == null);
					if (isNotValidHotSpot)
						continue;

					TourHotspot hotspot = new TourHotspot();
					HotspotConfig hotspotIcon = new HotspotConfig();
					// CONVERT ICON PATH

					if (xmlHotspots[j]["url"] != null) {
						hotspot.link = xmlHotspots[j]["url"].InnerText;
						hotspot.link = hotspot.link.Replace("{", "");
						hotspot.link = hotspot.link.Replace("}", "");
					}

					hotspot.id = xmlHotspots[j]["skinid"].InnerText;
					hotspotIcon.ID = hotspot.id;

					XmlElement position = xmlHotspots[j]["position"];
					if (position.HasChildNodes) {
						hotspot.pan = float.Parse(position["pan"].InnerText);
						hotspot.tilt = float.Parse(position["tilt"].InnerText);
					}
					hotspot.scale = 1.0f;
					hotspotIcon.prefab = "";
					hotspotIcons.hotspotConfigs[iconCounter++] = hotspotIcon;
					location.hotspots[validHotSpotCounter++] = hotspot;
				}

				// LOAD Patches
				XmlNodeList xmlPatches = xmlLocations[i].SelectNodes("sounds/sound");
				Patch[] patches = new Patch[xmlPatches.Count];

				int numberOfPatches = 0;
				for (int j = 0; j < xmlPatches.Count; j++) {
					Patch patch = new Patch();
					string filename = xmlPatches[j]["filename"].InnerText;
					if (filename.Contains(".png") || filename.Contains(".jpg") || filename.Contains(".jpeg"))
						continue;
					patch.media = filename;
					patch.media = patch.media.Substring(patch.media.LastIndexOf('/') + 1);
					patch.pan = float.Parse(xmlPatches[j]["position"]["pan"].InnerText);
					patch.tilt = float.Parse(xmlPatches[j]["position"]["tilt"].InnerText);
					patch.width = float.Parse(xmlPatches[j]["videorect"]["dimx"].InnerText);
					patch.height = float.Parse(xmlPatches[j]["videorect"]["dimy"].InnerText);
					patch.scale = float.Parse(xmlPatches[j]["videorect"]["fov"].InnerText) * FOVtoScale; //+ 35f;
					patch.rotx = float.Parse(xmlPatches[j]["videorect"]["rotx"].InnerText);
					patch.roty = float.Parse(xmlPatches[j]["videorect"]["roty"].InnerText);
					patch.rotz = float.Parse(xmlPatches[j]["videorect"]["rotz"].InnerText);
					numberOfPatches++;
					patches[j] = patch;
				}

				location.patches = new Patch[numberOfPatches];
				int p = 0;
				foreach (Patch patch in patches) {
					if (patch != null)
						location.patches[p++] = patch;
				}

				XmlNode xmlVideo = xmlLocations[i]["input"].SelectSingleNode("videofile");
				if (xmlVideo != null) {
					string path = xmlVideo.InnerText;
					if (path.Contains("/"))
						path = path.Split('/')[path.Split('/').Length - 1];
					location.panoramaPath = path;
				}

				XmlNode nextNode = xmlLocations[i]["input"].SelectSingleNode("nextnodeid");
				if (xmlVideo != null) {
					string next = nextNode.InnerText;
					next = next.TrimStart('{');
					next = next.TrimEnd('}');
					location.nextLocation = next;
				}

				tour.locations[i] = location;
			}
			return tour;
		}

	}

}