using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraZone : MonoBehaviour
{
    public GameObject virtualCamera;
    public bool startingCamera = false;
    public bool isTracking = false;

    public List<CameraZone> connectedZone;

    private void Awake()
    {
        //If not virtual camera is set, the script doesn't start
        if (virtualCamera == null)
        {
            enabled = false;
        }
        else
        {
            //If it's the starting camera
            if(startingCamera)
            {
                //We enable it
                virtualCamera.SetActive(true);
                CameraManager.instance.EnableZoneCam();
                isTracking = true;
            }
            else
            {
                //Else we don't (heh !)
                virtualCamera.SetActive(false);
                isTracking = false;
            }
        }
    }

    //If the player enters a zone, it enable the camera associated and notice the camera manager
    private void OnTriggerEnter(Collider other)
    {
        if(!virtualCamera.activeInHierarchy)
        {
            virtualCamera.SetActive(true);
            CameraManager.instance.EnableZoneCam();
        }
        isTracking = true;
    }

    //If the player exist a zone and no other zone linked to this camera have the player tracked, we disable the camera
    private void OnTriggerExit(Collider other)
    {
        if(virtualCamera.activeInHierarchy)
        {
            if(!connectedZone.Exists(c => c.isTracking))
            {
                virtualCamera.SetActive(false);
                CameraManager.instance.DisableZoneCam();
            }
            isTracking = false;
        }
        
    }
}
