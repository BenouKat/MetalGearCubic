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



	public void EnableZoneCam()
    {
        camActive++;
        if (camActive == 1)
        {
            mainCam.SetActive(false);
        }
    }

    public void DisableZoneCam()
    {
        camActive--;
        if(camActive == 0)
        {
            mainCam.SetActive(true);
        }
    }
}
