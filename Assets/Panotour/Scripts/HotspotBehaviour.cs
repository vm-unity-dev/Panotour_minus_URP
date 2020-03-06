using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HotSpotBehaviour : MonoBehaviour {

	public string m_link = "";
	static public float m_hoverActionTime = 1.5f;
	public float m_zoomNormal = 0.8f;
	public float m_zoomOnOver = 1.0f;
	public float m_alphaFade = 0.7f;
    public bool clickOnstart;

	public delegate void ClickAction (HotSpotBehaviour source, string link);
	public event ClickAction m_OnClicked;


	private Vector3 m_origScale;
	private bool m_Over;
	private float m_zoom = 0f;
	private float m_hoverTime = 0f;
	private bool m_clickLatch = false;
	private Material m_material;


	// Use this for initialization
	void Start () {
		m_origScale = transform.localScale;
        if(GetComponent<MeshRenderer>())
		    m_material = GetComponent<MeshRenderer> ().material;
        if (clickOnstart)
            Click();

    }

	public void Over(){
		m_Over = true;
		m_clickLatch = false;
		m_hoverTime = 0f;
	}

	public void Out(){
		m_Over = false;
	}

	public void Click() {
		if (m_OnClicked != null)
			m_OnClicked (this,m_link);
	}
	
	// Update is called once per frame
	void Update () {
		m_zoom = Mathf.Clamp01 (m_zoom + (Mathf.Abs (m_Over ? 1f : 0f - m_zoom) * Time.deltaTime *2f * Mathf.Sign(m_Over ? 1f : 0f - m_zoom)));
		transform.localScale = m_origScale * Mathf.Lerp (m_zoomNormal, m_zoomOnOver, m_zoom);

        if (m_material == null)
            return;
        Color col = Color.white;
		col.a = Mathf.Lerp (m_alphaFade, 1f, m_zoom);
		m_material.SetColor ("_Color", col);
	
	}
}
