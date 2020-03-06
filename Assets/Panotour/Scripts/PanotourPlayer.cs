using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using Mbryonic;
using DG.Tweening;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using UnityEngine.Video;
using System.IO;
using System.Xml;


/**
 * 
 * PANOTOUR UNITY PLAYER
 * (C) Mbryonic 2019. All Rights Reserved
 * 
 */


namespace Mbryonic.Panotour {

/* This class is used to playback the Panotours */

	public class PanotourPlayer : MonoBehaviour {

		#region Inspector Vars
		[Header("Setup")]
		[Tooltip("Put all your tours in here")]
		public string[] tours;
		[Tooltip("This is the name of the first one to load")]
		public string startTourID;
		[Tooltip("You can specify a start location within the tour (or leave blank)")]
		public string startLocationID;
		[Tooltip("Will automatically start the tour when the scene loads")]
		public bool startTourOnAwake;


		[Tooltip("Used to rotate the player to the correct orientation")]
		public Transform orientPlayer;

		[Header("Hotspots")]
		public Transform hotspotParent = null;
		public float hotspotDistanceFromCamera = 4.8f;
		public float hotspotScale = 5f;
		public bool disableEmptyLinks = false;

		[Header("References")]
		public Material panoramaMaterial;
		public VideoPlayer panoVideoPlayer;
		public PanotourSkinManager skinManager;

		[Header("Video Patches")]
		public GameObject patchPrefab;
		public GameObject patchUI;
        public Material PatchUIMaterial;
        [Tooltip("Set this to scale all patches up and down")]
		public float patchScale; 

		[Header("Audio")]
		public AudioClip transitionSound;
		public AudioClip clickSound;

		[Header("Popup Player")]
		public PopupViewer popupPlayer;
		public float popupDistanceFromCamera = 0.6f;
		public bool closePopupOnAnyClick = true;


		#endregion

		#region EventHooks
		// Use these to add flow to your application
		public delegate void TourEvent(string tour);
		public delegate void LocationEvent(int index);      // index of location
		public delegate void HotspotEvent(string link);

		public TourEvent OnTourLoaded;      // called when the tour has loaded
		public TourEvent OnTourFinished;        // Called when the tour has finished (most tours don't have a finish state)
		public LocationEvent OnLocationLoaded;    // Called when a location has been loaded but not visible to user yet
		public LocationEvent OnLocationReady;  // Called when a location is now visible to the user
		public HotspotEvent OnHotspotClicked;  // Called when any hotspot in the location is selected
		public LocationEvent OnLocationUnload;   // Called prior to the location being unloaded
		public TourEvent OnTourChangeRequested;     // Called if a location leads to another tour being loaded
		
		
		public Tour ActiveTour { get { return activeTour;  } }
		public TourLocation ActiveLocation {  get { return activeTour.locations[activeLocationIndex]; } }
		#endregion

		#region Protected Vars
		private int activeTourIndex;
		protected Tour activeTour;
		private int activeLocationIndex;

		private bool fileViewerOpened = false;
		private bool openingOrClosingFileViewer = false;

		private Coroutine proc;
		private VideoPlayer masterPatchPlayer;
		private Vector2 originalPanoScale;
		#endregion

		#region Public Methods
		// This starts a tour and returns when its complete - which could be never - unless there is a linear flow

		private bool tourCompleted = false;
		public IEnumerator RunTour(string startLocation = "") {
			tourCompleted = false;
			string loc = startLocation == "" ? (activeTour.startLocation != "" ? activeTour.startLocation : activeTour.locations[0].id) : startLocation;
			yield return GotoLocationCR(loc);
			while (!tourCompleted) {
				yield return null;
			}
		}

        // publicly accesible location changer to enable non hostspot ui to trigger location changes
        public void GoToLocationSimple(string location)
        {
            Debug.Log("gotolocationsimple called towards " + location);
            StartCoroutine(GotoLocationCR(location));
        }


		public void FinishTour() {
			ScreenFade.Active.FadeOut(0.5f, () => {
					tourCompleted = true;
					if (OnTourFinished != null) OnTourFinished(activeTour.name);
			});
		}

		public void Pause() {
			if (panoVideoPlayer) panoVideoPlayer.Pause();
			if (popupPlayer) { popupPlayer.Pause(); }

			foreach (Patch patch in ActiveLocation.patches) {
				if (patch.gameObject != null) {
					patch.gameObject.GetComponent<VideoPlayer>().Pause();
				}
			}
		}

		public void Unpause() {
			if (fileViewerOpened && popupPlayer.IsPaused()) {
				popupPlayer.Resume();
			}
			else {
				if (panoVideoPlayer && panoVideoPlayer.isPaused)
					panoVideoPlayer.Play();

				foreach (Patch patch in ActiveLocation.patches) {
					if (patch.gameObject != null) {
						patch.gameObject.GetComponent<VideoPlayer>().Play();
					}
				}
			}
		}

		// Reloads the current location - this called by the editor to reset any changes
		public void ReloadCurrentLocation() {
			StartCoroutine(ReloadCR());
		}

		public string ActiveTourPath { get { return activeTourPath; } }
		private string activeTourPath;

		// Loads a tour from storage but doesn't run it yet
		public IEnumerator LoadTour(int tourIndex) {

			activeTour = null; // delete old one
			activeTourIndex = tourIndex;

			if (skinManager) {
				yield return skinManager.LoadSkin(tours[activeTourIndex] + "_skin.json");
			}

#if UNITY_ANDROID && !UNITY_EDITOR
			// Android file loading has to use a Web request and this doesn't return immediately
			string jsonPath = Application.streamingAssetsPath + "/" + tours[activeTourIndex] + ".json";
			WWW www = new WWW(jsonPath);
			yield return www;
			Debug.Log("Loaded tour from " + jsonPath);
			activeTour = JsonUtility.FromJson<Tour>(www.text);
#else
			activeTourPath = Application.streamingAssetsPath + "/" + tours[activeTourIndex] + ".json";
			using (FileStream fs = new FileStream(activeTourPath, FileMode.Open)) {
				using (StreamReader reader = new StreamReader(fs)) {
					activeTour = JsonUtility.FromJson<Tour>(reader.ReadToEnd());
				}
			}
#endif
			activeTour.name = tours[activeTourIndex];
		}

		public void UpdateLocation() {
			foreach (Patch patch in activeTour.locations[activeLocationIndex].patches) {  // not sure about performance on this??
				Vector3 scale = new Vector3(patch.width, patch.height, 1f) * patch.scale * patchScale;
				if (patch.gameObject == null) Debug.LogError("Patch Gameobject empty!");
				SetTransform(patch.gameObject.transform, patch.tilt, patch.pan, transform.localScale.x, new Vector3(patch.rotx, patch.roty, patch.rotz), scale);
			}
			foreach (TourHotspot hotspot in activeTour.locations[activeLocationIndex].hotspots) {
				if (hotspot.gameObject == null) Debug.LogError("Patch Gameobject empty!");
                if (hotspot.id == "Menu_Prototype") //--------------------------------------------------instantiates a low level hotspot at the user start orientation.
                {
                    float startpan = activeTour.locations[activeLocationIndex].startpan;
                    float starttilt = (activeTour.locations[activeLocationIndex].starttilt - 60.0f);
                    SetTransform(hotspot.gameObject.transform, starttilt, startpan, hotspotDistanceFromCamera, Vector3.zero, (Vector3.one * hotspotScale * hotspot.scale));
                }
                else
                {
                    SetTransform(hotspot.gameObject.transform, hotspot.tilt, hotspot.pan, hotspotDistanceFromCamera, Vector3.zero, (Vector3.one * hotspotScale * hotspot.scale));
                }
			}
		}


		#endregion

		#region Popup Methods

		protected void OpenFileViewer(Transform source) {
			if (!openingOrClosingFileViewer) {
				popupPlayer.gameObject.SetActive(true);
				popupPlayer.OpenFileViewer();
				fileViewerOpened = true;

				if (transitionSound) AudioSource.PlayClipAtPoint(transitionSound, Camera.main.transform.position);

				Pause();
				openingOrClosingFileViewer = true;
				popupPlayer.transform.localPosition = source.localPosition.normalized * popupDistanceFromCamera;
				popupPlayer.transform.localPosition = new Vector3(popupPlayer.transform.localPosition.x, 0f, popupPlayer.transform.localPosition.z);

				popupPlayer.transform.rotation = source.rotation;
				popupPlayer.transform.LookAt(Camera.main.transform.position);
				popupPlayer.transform.Rotate(0f, 180f, 0f);
				Vector3 scale = Vector3.one;
				popupPlayer.transform.localScale = Vector3.zero;
				float duration = 0.5f;
				CanvasGroup cg = popupPlayer.GetComponentInChildren<CanvasGroup>();

				if (cg)	cg.DOFade(1f, duration);

				popupPlayer.transform.DOScale(scale, duration).SetEase(Ease.InCubic).OnComplete(() => {
					openingOrClosingFileViewer = false;
				});
			}
		}

		public void CloseFileViewer() {
			CloseFileViewer(true);
		}

		public void CloseFileViewer(bool animate = true) {
			if (animate) {
				if (!openingOrClosingFileViewer) {
					openingOrClosingFileViewer = true;
					Vector3 scale = popupPlayer.transform.localScale;
					float duration = 0.5f;
					CanvasGroup cg = popupPlayer.GetComponentInChildren<CanvasGroup>();
					if (cg)
						cg.DOFade(0f, duration);

					popupPlayer.transform.DOScale(0, duration).SetEase(Ease.InCubic).OnComplete(() => {
						popupPlayer.CloseFileViewer();
						openingOrClosingFileViewer = false;
						fileViewerOpened = false;
						popupPlayer.gameObject.SetActive(false);
						popupPlayer.transform.localScale = Vector3.one;
						SetPatchVolumes(1f);
						Unpause();
					});
				}
			}
			else {
				popupPlayer.CloseFileViewer();
				openingOrClosingFileViewer = false;
				fileViewerOpened = false;
				popupPlayer.gameObject.SetActive(false);
				SetPatchVolumes(1f);
				Unpause();
			}
		}

		#endregion

		#region Location Methods

		// Call this to transition to a new location - instant return version
		protected void GotoLocation(string locationPath) {
			StartCoroutine(GotoLocationCR(locationPath));
		}
		
		// Call this to transition to a new location (even if in another tour)
		protected IEnumerator GotoLocationCR(string locationPath, float transitionTime = .3f) {

            // Wait for transition / fade out
            if (transitionTime > 0f) {
				yield return ScreenFade.Active.FadeOutCR(transitionTime);
			}

			// Clean up nicely
			UnloadCurrentLocation();

			// Locations can be in other Tours, you just need to add a backslash like MyTour/MyLocation1
			// If so we need to load the other tour first
			if (locationPath.Contains("/")) {
				// Find link

				string url = locationPath.Substring(0, locationPath.IndexOf("/"));
				locationPath = locationPath.Substring(locationPath.IndexOf("/") + 1);

				int index;
				for (index = 0; index < tours.Length; index++) {
					if (url == tours[index]) break;
				}
				if (index == tours.Length) {
					Debug.LogError("Cannot find tour '" + url + "' in locationPath : " + locationPath);
				}
				else {
					yield return LoadTour(index);
				}
			}

			// Load new location
			CreateLocation(GetLocationIndexByName(locationPath));

			// Wait for the videos to spin up so we don't see ugly white patches before fading
			bool videosWaiting;
			do {
				videosWaiting = false;
				foreach (Patch patch in activeTour.locations[activeLocationIndex].patches) {
					if (patch.gameObject.GetComponent<VideoPlayer>().frame <= 0) {
						videosWaiting = true;
					}
				}
				yield return null;
			} while (videosWaiting);


			// Fade back in
			yield return ScreenFade.Active.FadeInCR(transitionTime);

			if (OnLocationReady != null) OnLocationReady(activeLocationIndex);
		}

		// Goto a location (using name lookup)
		protected int GetLocationIndexByName(string name) {
			for(int i=0;i<activeTour.locations.Length;i++) {
				if (activeTour.locations[i].id == name) {
					return i;
				}
			}
			return -1;
		}

		// Unload the current location - clean up
		private void UnloadCurrentLocation() {


			// Stop and clear the pano player
			if (panoVideoPlayer != null) {
				panoVideoPlayer.Stop();
				panoVideoPlayer.clip = null;
			}

			// And any coroutines...
			if (proc != null) {
				StopCoroutine(proc);
				proc = null;
			}

			// Remove existing hotspots and patches
			foreach (TourHotspot hotspot in activeTour.locations[activeLocationIndex].hotspots) {
				if (hotspot.gameObject) {
					Destroy(hotspot.gameObject);
					hotspot.gameObject = null;
				}
			}
			foreach (Patch patch in activeTour.locations[activeLocationIndex].patches) {
				if (patch.gameObject) {
					Destroy(patch.gameObject);
					patch.gameObject = null;
				}
			}

			// Close the file viewer
			if (fileViewerOpened) {
				CloseFileViewer();
			}

			if (OnLocationUnload != null) OnLocationUnload(activeLocationIndex);

		}

		private bool hasMasterPatch = false;
        private bool autoPlayPatch = true;

		// Goto a location
		protected void CreateLocation(int locationIndex) {

            ref TourLocation activeLocation = ref activeTour.locations[locationIndex];  // needs to be mutable
			activeLocationIndex = locationIndex;
			string path = tours[activeTourIndex] + "/Panos/" + activeLocation.panoramaPath;
			activeLocationIndex = locationIndex;

			//====== Set up the Panorama
			if (activeLocation.panoramaPath.Contains(".mp4") || activeLocation.panoramaPath.Contains(".m4v")) {
				panoVideoPlayer.clip = Resources.Load(GetResourceName(path)) as VideoClip;
				panoVideoPlayer.Play();
			}
			else {
				Texture2D panorama = Resources.Load(path) as Texture2D;
				if (panorama == null)
					Debug.LogError("Could not load panorama " + path);
				else {
					transform.GetComponent<Renderer>().material = panoramaMaterial;
					panoramaMaterial.SetTexture("_MainTex", panorama);
					panoramaMaterial.mainTextureScale = originalPanoScale;
				}
			}
			transform.rotation = Quaternion.Euler(activeLocation.levelpitch, 180f, activeLocation.levelroll);

			//====== Create Hotspots
			if (activeLocation.hotspots != null) {
				int id = 0;
				foreach (TourHotspot hotspot in activeLocation.hotspots) {
					GameObject go = skinManager.CreateHotspot(activeLocation.id, hotspot.id);
					if (go == null) {
						Debug.LogError("No hotspot created " + hotspot.id);
						continue;
					}
					hotspot.gameObject = go;

					if (hotspotParent) {
						go.transform.parent = hotspotParent;
						go.SetActive(false);
					}

					VRInteractiveItem interactive = go.GetComponent<VRInteractiveItem>();
                    { 
       
                        if (interactive != null)
                        {
                            interactive.OnClick += HotspotClicked; 
                            interactive.Context = hotspot.link;
                            if (disableEmptyLinks && hotspot.link == "")
                                interactive.Disabled = true;
                        }
                    }

                        //go.transform.parent = m_orientPlayer;
                        go.SetActive(true);
					id++;
				}
			}

			//====== Create Patches
			int renderQueue = 2000;
			hasMasterPatch = false;
			if (activeLocation.patches != null) {
				int numPatches = 0;
				foreach (Patch patch in activeLocation.patches) {
					numPatches++;
					if (Application.platform == RuntimePlatform.Android && numPatches > 2)
						continue;
					GameObject go = Instantiate(patchPrefab) as GameObject;
					if (hotspotParent) go.transform.parent = hotspotParent;
					patch.gameObject = go;
					go.SetActive(true);
					go.GetComponent<MeshRenderer>().material.renderQueue = renderQueue++;


                    // ---- JQ  -- TODO - attempt to generate a still image patch to render on top of the video texture in order to mask the edges of the video and improve colour fidelity at edge of video
     


                    if (patch.media != "") {

                        Debug.Log("Patch media is video");

                        VideoPlayer player = go.GetComponent<VideoPlayer>();
						player.clip = Resources.Load(GetResourceName(patch.media)) as VideoClip;
						if (player.clip == null) { Debug.LogError("Cannot load " + GetResourceName(path)); }
						player.SetDirectAudioVolume(0, patch.volume);

						player.isLooping = patch.master ? false : true;         // The patch master is for when a patch is supposed to play to end and continue to the next scene
						if (patch.master) {
							player.loopPointReached += OnMasterPatchComplete;
							if (hasMasterPatch) {
								Debug.LogError("You have multiple video patches listed as master - the shortest video will define length of sequence");
							}
							hasMasterPatch = true;
						}
                        if(patch.autoPlay == false)
                        {
                           // perform actions to spin up the video player and then pause it, 
                           // generate the playback UI system
                        }
                        else
                            player.Play();

                    }

                }
			}

			UpdateLocation();
			orientPlayer.rotation = Quaternion.Euler(-activeLocation.starttilt, -activeLocation.startpan, 0f);

			if (OnLocationLoaded != null) OnLocationLoaded(activeLocationIndex);

		}

		protected IEnumerator ReloadCR() {
			TourLocation currentLocation = ActiveLocation;
			UnloadCurrentLocation();
			yield return null;
			yield return LoadTour(activeTourIndex);
			CreateLocation(activeLocationIndex);

		}

		// Goto the next location
		public void GotoNextLocation() {
			if (activeTour.locations[activeLocationIndex].nextLocation == "") {
				if (OnTourFinished != null) OnTourFinished(activeTour.name);
			}
			else {
				GotoLocation(activeTour.locations[activeLocationIndex].nextLocation);
			}
		}

		#endregion

		#region Hotspots and Patches Methods


       
		void HotspotClicked(VRInteractiveItem source, VRPointer pointer) {
            

			if (clickSound) AudioSource.PlayClipAtPoint(clickSound, Camera.main.transform.position);

			if (fileViewerOpened) {
				CloseFileViewer(false);
			}
			if (!openingOrClosingFileViewer) {
				string link = source.Context as string;

				if (OnHotspotClicked != null) OnHotspotClicked(link);

                if (link.Contains("Menu")) //--------------------------This is a bit of a hack
                {
                    return;
                }

				if (link.Contains(".")) {
					bool isFirstLink = true;
					string currentLink = link;
					string remainingLink = "";
					if (link.Contains(";")) {
						currentLink = link.Substring(0, link.IndexOf(";"));
						remainingLink = link.Substring(link.IndexOf(";") + 1);
					}
					int argumentCount = Regex.Matches(link, (";")).Count + 1;
					for (int i = 0; i < argumentCount; i++) {
						if (currentLink.Contains(".")) { //if link is a media file
							if (popupPlayer==null) { Debug.LogError("You need a pop-up player in the scene!"); }

							popupPlayer.AddFileToViewer(activeTour.name + "/Popups/" + currentLink);
							if (isFirstLink) {
								isFirstLink = false;
								OpenFileViewer(source.transform);
							}
						}
						if (remainingLink.Contains(";")) {
							currentLink = remainingLink.Substring(0, remainingLink.IndexOf(";"));
							remainingLink = remainingLink.Substring(remainingLink.IndexOf(";") + 1);
						}
						else {
							currentLink = remainingLink;
							remainingLink = "";
						}
					}
				}
				else if (link != "") {
					StartCoroutine(GotoLocationCR(link));
				}
			}
		}

		protected void SetPatchVolumes(float volume) {
			foreach (Patch patch in activeTour.locations[activeLocationIndex].patches) {
				patch.gameObject.GetComponent<VideoPlayer>().SetDirectAudioVolume(0, volume * patch.volume);
			}
		}

		private void OnMasterPatchComplete(VideoPlayer e) {
			hasMasterPatch = false;
			e.Pause();
			if (activeTour.locations[activeLocationIndex].nextLocation == "") {
				FinishTour();
			}
			else {
				GotoLocation(activeTour.locations[activeLocationIndex].nextLocation);
			}
		}

		private IEnumerator OpenUiWhenPatchFinished(VideoPlayer player, GameObject ui) {
			while (!player.isPlaying) {
				yield return null;
			}

			float offset = 1f;
			if ((player.clip.length) < offset)
				offset = 0f;
			float duration = ((float)player.clip.length) - offset;
			while ((player.clip.length) < duration)
				yield return null;
			OpenUi(ui, player.gameObject);
		}

		void OpenUi(GameObject ui, GameObject parent) {
			Pause();
			// Position the UI to where our hotspot is
			ui.transform.parent = parent.transform;
			ui.transform.localPosition = new Vector3(0, 0, 0);
			ui.transform.localEulerAngles = new Vector3(0, 0, 0);
			ui.transform.localScale = new Vector3(1, 1, 1);
			ui.transform.parent = null;

			ui.SetActive(true);
		}

		#endregion

		#region Helpers

		public string GetResourceName(string path) {
			string substring = path.Substring(0, path.IndexOf('.'));
			return activeTour.name + "/Patches/" + substring;
		}

		// Helper function to calculate the correct position of hotspots + patches
		protected static void SetTransform(Transform target, float tilt, float pan, float distance, Vector3 rot, Vector3 scale) {
			Vector3 f = Quaternion.Euler(-tilt, -pan, 0f) * Vector3.forward * distance;
			target.localPosition = f;
			target.rotation = Quaternion.Euler(-tilt + rot.x, -pan + rot.y, rot.z);
			//target.localRotation = Quaternion.LookRotation(f - Camera.main.transform.position) * Quaternion.Euler(rot.x,rot.y,rot.z); 
			target.localScale = scale;
		}

		#endregion

		#region MonoBehaviour

		public IEnumerator Start() {
			masterPatchPlayer = null;
			originalPanoScale = panoramaMaterial.mainTextureScale;

			if (!skinManager) skinManager = GetComponent<PanotourSkinManager>();
			if (popupPlayer) popupPlayer.AddToOnClose(CloseFileViewer);

			ScreenFade.Active.Amount = 1f;          // Set to black at start	

			int tourIndex = 0;
			if (startTourID != "") {
				for (tourIndex = 0; tourIndex < tours.Length; tourIndex++) {
					if (startTourID == tours[tourIndex]) break;
				}
			}

			yield return LoadTour(tourIndex);

			if (startTourOnAwake) {
				yield return RunTour(startLocationID);
			}
		}

		public void Update() {
			if (Input.GetKeyDown(KeyCode.R)) {
				ReloadCurrentLocation();
			}
			/*
			 * TS - TODO fix this??
			if (m_applyToMesh.enabled && m_360MediaPlayer.Control.IsFinished() && m_loadingLocation == false) {
				m_loadingLocation = true;
				StartCoroutine(FadeToLocationOrTour(m_tourLocation.nextLocation));
			}*/


			if (closePopupOnAnyClick && (Input.GetMouseButtonDown(0) || VRPlayer.main.ActivePointer.TriggerDown()) && fileViewerOpened)
				CloseFileViewer(false);
		}

		#endregion

	}

}