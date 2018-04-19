using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour {

    public static CameraManager instance;
    public GameObject mainCam;
    public int camActive;
    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }

        camActive = 0;
    }

    private void Start()
    {
        mainCam.SetActive(camActive == 0);
    }

    //Count the number of active cam. If the number is not 0, the main cam is disabled
	public void EnableZoneCam()
    {
        camActive++;
        if (camActive == 1)
        {
            mainCam.SetActive(false);
        }
    }

    //If the live cam count is 0, we enable the main cam
    public void DisableZoneCam()
    {
        camActive--;
        if(camActive == 0)
        {
            mainCam.SetActive(true);
        }
    }
}
