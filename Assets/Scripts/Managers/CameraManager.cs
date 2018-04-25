using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraManager : MonoBehaviour {

    public static CameraManager instance;
    public GameObject mainCam;
    public GameObject mainVCam;
    public GameObject angleCam;
    public int camActive;


    List<GameObject> virtualCameraActive = new List<GameObject>();

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
    }

    private void Start()
    {
        mainVCam.SetActive(virtualCameraActive.Count == 0);
    }

    //Count the number of active cam. If the number is not 0, the main cam is disabled
	public void EnableZoneCam(GameObject virtualCam)
    {
        virtualCameraActive.Add(virtualCam);
        if (virtualCameraActive.Count == 1)
        {
            mainVCam.SetActive(false);
        }
    }

    //If the live cam count is 0, we enable the main cam
    public void DisableZoneCam(GameObject virtualCam)
    {
        virtualCameraActive.Remove(virtualCam);
        if (virtualCameraActive.Count == 0)
        {
            mainVCam.SetActive(true);
        }
    }
    
    //Get the current active camera of cinemachine zone
    public GameObject GetCurrentActiveCamera()
    {
        GameObject highestPriorityCamera = mainVCam;
        int priority = 0;
        int maxPriority = -1;
        foreach(GameObject camera in virtualCameraActive)
        {
            if(maxPriority < (priority = camera.GetComponent<CinemachineVirtualCamera>().Priority))
            {
                maxPriority = priority;
                highestPriorityCamera = camera;
            }
        }
        
        return highestPriorityCamera;
    }
}
