using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using UnityEngine.Events;
using Mbryonic;
using UnityEngine.Video;


namespace Mbryonic.Panotour {

	public class PopupViewer : MonoBehaviour {

		#region InspectorVars
		public RectTransform scaleScreen;
		public Button closeButton;
		public Button nextButton;
		public Button previousButton;

		[Header("Image Viewer")]
		public Image imageScreen;
		[Header("Video Viewer")]
		public GameObject videoScreen;
		public VideoPlayer videoPlayer;
		public Vector2 widthHeightVideo = new Vector2(1f, 1.6f);
		#endregion

		#region PrivateVars
		private CanvasGroup canvasGroup;
		private List<string> filePaths;
		private int currentFileIndex;
		private Vector2 maxWidthHeight;
		#endregion

		public void Awake() {
			maxWidthHeight = new Vector2(scaleScreen.rect.width, scaleScreen.rect.height);
			filePaths = new List<string>();
			videoPlayer.gameObject.SetActive(true);

			canvasGroup = GetComponentInChildren<CanvasGroup>();
			CleanUp();
			gameObject.SetActive(false);
		}

		public void Start() {
			nextButton.onClick.AddListener(NextFile);
			previousButton.onClick.AddListener(PreviousFile);
		}

		public void AddToOnClose(UnityAction call) {
			closeButton.onClick.AddListener(call);
		}

		#region General File Viewer functions
		public void AddFileToViewer(string path) {
			filePaths.Add(path);
			UpdateButtonStates();
		}

		void LoadFile(int index = 0) {
			if (filePaths == null || index >= filePaths.Count)
				return;
			CleanUp();
			currentFileIndex = index;
			if (filePaths[index].Contains(".png") || filePaths[index].Contains(".jpg") || filePaths[index].Contains(".jpeg"))
				LoadImage(index);
			else if (filePaths[index].Contains(".mp4") || filePaths[index].Contains(".mov"))
				LoadVideo(index);
		}

		void CleanUp() {
			imageScreen.gameObject.SetActive(false);
			videoScreen.gameObject.SetActive(false);
			if (IsPlaying())
				videoPlayer.Stop();
		}
		void UpdateButtonStates() {
			nextButton.gameObject.SetActive(currentFileIndex < filePaths.Count - 1);
			previousButton.gameObject.SetActive(currentFileIndex > 0);
		}

		void NextFile() {
			if (currentFileIndex < filePaths.Count - 1) {
				canvasGroup.DOFade(0, 0.5f);
				scaleScreen.DOAnchorPosX(-10, 0.5f).SetEase(Ease.InCirc).OnComplete(() => {
					LoadFile(currentFileIndex + 1);
					scaleScreen.anchoredPosition = new Vector2(10, 0);
					canvasGroup.DOFade(1, 0.5f);
					scaleScreen.DOAnchorPosX(0, 0.5f);
				});
			}
		}
		void PreviousFile() {
			if (currentFileIndex > 0) {
				canvasGroup.DOFade(0, 0.5f);
				scaleScreen.DOAnchorPosX(10, 0.5f).SetEase(Ease.OutCirc).OnComplete(() => {
					LoadFile(currentFileIndex - 1);
					scaleScreen.anchoredPosition = new Vector2(-10, 0);
					canvasGroup.DOFade(1, 0.5f);
					scaleScreen.DOAnchorPosX(0, 0.5f);
				});
			}
		}

		public void OpenFileViewer() {
			currentFileIndex = 0;
			LoadFile();
		}

		public void CloseFileViewer() {
			currentFileIndex = 0;
			filePaths.Clear();
			CleanUp();
		}

		public void ResizeViewer(float width, float height) {
			float widthToHeightRatio = width / height;
			float heightToWidthRatio = height / width;
			scaleScreen.sizeDelta = new Vector2(maxWidthHeight.x * (widthToHeightRatio > heightToWidthRatio ? 1 : widthToHeightRatio),
				maxWidthHeight.y * (heightToWidthRatio > widthToHeightRatio ? 1 : heightToWidthRatio));
			scaleScreen.gameObject.GetComponent<BoxCollider>().size = scaleScreen.sizeDelta;
		}
		#endregion


		#region Image Viewer specific functions
		void LoadImage(int index = 0) {
			imageScreen.gameObject.SetActive(true);
			string path = filePaths[index].Substring(0, filePaths[index].IndexOf("."));
			Sprite sprite = Resources.Load<Sprite>(path) as Sprite;
			if (sprite != null) {
				imageScreen.sprite = sprite;
				ResizeViewer(sprite.texture.width, sprite.texture.height);
				UpdateButtonStates();
			}
			else {
				Debug.LogError("Could not load sprite " + path);
			}
		}
		#endregion



		#region Video Viewer specific functions
		public string GetResourceName(string path) {
			string substring = path.Substring(0, path.IndexOf('.'));
			return substring;
		}

		void LoadVideo(int index = 0) {
			videoScreen.gameObject.SetActive(true);
			videoPlayer.clip = Resources.Load(GetResourceName(filePaths[index])) as VideoClip;
			if (videoPlayer.clip != null) {
				// Clear previous contents
				RenderTexture rt = UnityEngine.RenderTexture.active;
				UnityEngine.RenderTexture.active = videoPlayer.targetTexture;
				GL.Clear(true, true, Color.clear);
				UnityEngine.RenderTexture.active = rt;

				ResizeViewer(widthHeightVideo.x, widthHeightVideo.y);
				UpdateButtonStates();
				videoPlayer.time = 0f;
				StartCoroutine(PlayVideo());
				Debug.Log("Started to play video " + videoPlayer.clip.name);
			}
			else{
				Debug.LogError("Could not find video resource: " + GetResourceName(filePaths[index]));
			}
		}

		protected IEnumerator PlayVideo() {
			yield return null;
			videoPlayer.Play();
		}




		public bool IsPlaying() {
			return IsPaused() == false;
		}
		public bool IsPaused() {
			return videoPlayer && videoPlayer.isPaused;
		}
		public void Resume() {
			if (IsPaused())
				videoPlayer.Play();
		}

		public void Pause() {
			if (IsPlaying()) {
				videoPlayer.Pause();
			}
		}
		#endregion

	}

}