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
        if (virtualCamera == null)
        {
            enabled = false;
        }
        else
        {
            if(startingCamera)
            {
                virtualCamera.SetActive(true);
                CameraManager.instance.EnableZoneCam();
                isTracking = true;
            }
            else
            {
                virtualCamera.SetActive(false);
                isTracking = false;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(!virtualCamera.activeInHierarchy)
        {
            virtualCamera.SetActive(true);
            CameraManager.instance.EnableZoneCam();
        }
        isTracking = true;
    }

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
