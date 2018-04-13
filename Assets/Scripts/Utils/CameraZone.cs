using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraZone : MonoBehaviour
{
    public GameObject virtualCamera;

    private void Awake()
    {
        if (virtualCamera == null) enabled = false;
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
