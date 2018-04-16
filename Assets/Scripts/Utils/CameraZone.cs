using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraZone : MonoBehaviour
{
    public GameObject virtualCamera;
    public bool startingCamera = false;
    private void Awake()
    {
        if (virtualCamera == null)
        {
            enabled = false;
        }
        else
        {
            virtualCamera.SetActive(startingCamera);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        virtualCamera.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        virtualCamera.SetActive(false);
    }
}
