using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using DG.Tweening;

namespace Mbryonic {
	public class ScreenFade : MonoBehaviour {

		#region InspectorVars
		[Range(0, 1)]
		[SerializeField] private float _amount = 0f;
		[SerializeField] private Color _color = new Color(0.0f, 0.0f, 0.0f, 1.0f);
		[SerializeField] private AnimationCurve _curve;
		[SerializeField] private Material _fadeMaterial = null;

		[Header("Automatic")]
		[SerializeField] private bool _fadeOnStart = true;
		[SerializeField] private float _fadeTime = 2.0f;

		[Header("Audio")]
		[Tooltip("Add a mixer if you want it to audio dip the audio as you fade. Otherwise leave blank")]
		[SerializeField] private AudioMixer _audioMixer;
		[SerializeField] private string _audioParam = "Master";

		#endregion



		#region ProtectedVars
		private bool _animating = false;

		private YieldInstruction _fadeInstruction = new WaitForEndOfFrame();
		private Coroutine _proc;
		private Material _fadeMaterialInstance = null;
		#endregion

		#region PublicAccessors
		public float Amount { get { return _amount; } set { _amount = value; } }
		public event Action OnFadeComplete = null;

		public static ScreenFade Active { get { return _active; } }
		public static ScreenFade _active = null;
		#endregion


		void OnEnable() {
			_active = this;
		}

		public void OnDisable() {
			if (_active == this) _active = null;
		}
		/// <summary>
		/// Initialize.
		/// </summary>
		void Awake() {

			if (_active == null) _active = this;

			// create the fade material
			_fadeMaterialInstance = Instantiate(_fadeMaterial) as Material;
			if (_audioMixer != null) {
				_audioMixer.SetFloat(_audioParam, -80f);
			}
			if (_fadeOnStart) {
				_color.a = 1f;
				_fadeMaterialInstance.color = _color;
			}
		}

		/// <summary>
		/// Starts a fade in when a new level is loaded
		/// </summary> 
		void Start() {
			if (_fadeMaterialInstance == null) {
				_fadeMaterialInstance = Instantiate(_fadeMaterial) as Material;
			}
			if (_fadeOnStart) {
				FadeIn(_fadeTime);
			}
		}

		/// <summary>
		/// Cleans up the fade material
		/// </summary>
		void OnDestroy() {

		}


		/// <summary>
		/// Renders the fade overlay when attached to a camera object
		/// </summary>
		void OnPostRender() {
			if (_fadeMaterialInstance.color.a > 0f) {
				_fadeMaterialInstance.SetPass(0);
				GL.PushMatrix();
				GL.LoadOrtho();
				GL.Color(_fadeMaterialInstance.color);
				GL.Begin(GL.QUADS);
				GL.Vertex3(0f, 0f, 0f); // -12f);
				GL.Vertex3(1f, 0f, 0f); //-12f);
				GL.Vertex3(1f, 1f, 0f); //-12f);
				GL.Vertex3(0f, 1f, 0f); //-12f);
				GL.End();
				GL.PopMatrix();
			}
		}

		public void FadeIn(float duration = .5f, Action onComplete = null) {
			_animating = true;
			DOTween.To(() => { return _amount; }, (x) => { _amount = x; }, 0f, duration).OnComplete(() => { _animating = false; if (onComplete != null) onComplete(); });
		}


		public void FadeOut(float duration = .5f, Action onComplete = null) {
			_animating = true;
			DOTween.To(() => { return _amount; }, (x) => { _amount = x; }, 1f, duration).OnComplete(() => { _animating = false; if (onComplete != null) onComplete(); });
		}

		public void SetColor(Color color) {
			_color = color;
		}


		public IEnumerator FadeInCR(float fadeTime = .5f) {
			FadeIn();
			yield return new WaitForSeconds(fadeTime);
		}


		public IEnumerator FadeOutCR(float fadeTime = .5f) {
			FadeOut();
			yield return new WaitForSeconds(fadeTime);
		}


		public void SetFadeInstant(Color col) {
			if (_fadeMaterial) _fadeMaterialInstance.color = col;
		}

		public void SetFadeInstantAlpha(float alpha) {
			if (_fadeMaterial) _fadeMaterialInstance.color = new Color(_fadeMaterial.color.r, _fadeMaterial.color.g, _fadeMaterial.color.b, alpha);
		}


		public void Update() {
			//        GetComponent<Camera>().enabled = (_amount > 0.5);
			//Camera.main.enabled = (_amount > 0.5);

			Color color = _color;
			if (_curve != null)
				color.a = _curve.Evaluate(_amount);
			else
				color.a = _amount;
			_fadeMaterialInstance.color = color;

			if (_audioMixer != null) {
				_audioMixer.SetFloat(_audioParam, Mathf.Lerp(0f, -80.0f, _amount));
			}


		}

	}

}