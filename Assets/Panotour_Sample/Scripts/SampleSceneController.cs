using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mbryonic.Panotour;
using Mbryonic;
using UnityEngine.UI;

public class SampleSceneController : MonoBehaviour
{

    #region singleton
    private static SampleSceneController m_instance = null;
    public static SampleSceneController Instance
    {
        get { return m_instance; }
    }
    #endregion

    public PanotourPlayer player;
    private GameObject navMenu;
    string location;

    private Button[] menuButtons;
    private bool menuClosed;

    void Awake()
    {
        if (m_instance != null && m_instance != this)
            Destroy(this);

        m_instance = this;
    }

    void Start()
    {
        // Use these hooks to add in your own functionality rather than editing PanotourPlayer
        //player.OnLocationReady += (c) => { Debug.Log("Location Ready " + c); };
        //player.OnLocationUnload += (c) => { Debug.Log("Location Completed " + c); };
        //player.OnTourFinished += (c) => { Debug.Log("Tour Finished " + c); };
        //player.OnHotspotClicked += (c) => { Debug.Log("User Pressed a hotspot " + c); };

        player.OnLocationReady += (c) =>
        {
            SetupNavMenu();
        };

        player.OnLocationUnload += (c) =>
        {
            CleanupNavMenu();
        };

        player.OnHotspotClicked += (c) =>
        {
            if (c.Contains("Menu"))
            {
                if (menuClosed)
                    OpenNavMenu();
                else
                    CleanupNavMenu();
            }
        };
    }

    void SetupNavMenu()
    {
        navMenu = GameObject.FindGameObjectWithTag("Menu");
        if (navMenu != null)
        {
            menuButtons = navMenu.GetComponentsInChildren<Button>(true);
            foreach (Button btn in menuButtons)
            {
                string buttonName = btn.gameObject.name;
                Debug.Log("buttonName is " + buttonName);
                btn.onClick.AddListener(() =>
                {
                    ButtonClick(buttonName);
                });
            };
            navMenu.SetActive(false);
        }
    }
    void OpenNavMenu()
    {
        navMenu.GetComponent<Canvas>().enabled = true;
        navMenu.SetActive(true);
        menuClosed = false;
    }

    void CleanupNavMenu()
    {
        if (navMenu != null)
        {
            navMenu.SetActive(false);
            navMenu.GetComponent<Canvas>().enabled = false;
            menuClosed = true;
        }
    }

    public void ButtonClick(string location)
    {
        player.GoToLocationSimple(location);
    }


    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }


}
