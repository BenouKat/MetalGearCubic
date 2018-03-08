using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBehaviour : MonoBehaviour {
    
    Rigidbody playerRigidbody;

    [Header("Movement")]
    public float maxSpeed = 1f;
    public float speedRotationSmooth = 0.5f;
    Camera currentCamera;
    Transform cameraDirection;
    Transform playerRotation;
    Vector3 cameraDirectionPosition;
    Vector3 stickDirection;

    [Header("Shooting Level 1")]
    GameObject mousePlaneTransform;
    Ray rayMouseToFloor;
    RaycastHit castInfo;

    void Start () {

        playerRigidbody = GetComponent<Rigidbody>();

        //Util to calculate input direction regarding the rotation of the camera
        cameraDirection = InstanceManager.instance.instanceObject(InstanceManager.InstanceType.Utils, 
            new GameObject("CameraDirection")).transform;

        //Util for applying a smooth rotation to player
        playerRotation = InstanceManager.instance.instanceObject(InstanceManager.InstanceType.Utils,
            new GameObject("PlayerRotation")).transform;

        //Setting up the plane to catch the mouse direction for aiming
        mousePlaneTransform = InstanceManager.instance.instanceObject(InstanceManager.InstanceType.Utils,
            new GameObject("Raycatcher"));

        mousePlaneTransform.transform.localScale = Vector3.right * 100f + Vector3.up * 0.01f + Vector3.forward * 100f;
        BoxCollider box = mousePlaneTransform.AddComponent<BoxCollider>();
        box.center = -Vector3.up * 0.5f;
        box.isTrigger = true;
        mousePlaneTransform.layer = LayerMask.NameToLayer("MouseEvent");
        mousePlaneTransform.gameObject.SetActive(false);
    }

    private void Update()
    {
        if(Input.GetButton("Aim"))
        {
            if(Input.GetJoystickNames().Length == 0)
            {
                rayMouseToFloor = currentCamera.ScreenPointToRay(Input.mousePosition);
                mousePlaneTransform.transform.position = transform.position;
                mousePlaneTransform.SetActive(true);

                if(Physics.Raycast(rayMouseToFloor, out castInfo, 100f, 1 << LayerMask.NameToLayer("MouseEvent")))
                {
                    playerRotation.position = transform.position;
                    playerRotation.LookAt(castInfo.point);
                    playerRigidbody.transform.rotation = Quaternion.Slerp(playerRigidbody.transform.rotation, playerRotation.rotation, speedRotationSmooth);
                }
            }
            else
            {
                refreshStickDirection();
                playerRigidbody.transform.LookAt(playerRigidbody.position + stickDirection);
            }
        }

        if(Input.GetButtonUp("Aim"))
        {
            mousePlaneTransform.SetActive(false);
        }

        if(Input.GetButtonDown("Shoot"))
        {
            //Shoot
        }
    }

    void FixedUpdate () {
        
        //Movement
        if(Input.GetButton("Aim") || Mathf.Approximately(Input.GetAxisRaw("Horizontal"), 0f) && Mathf.Approximately(Input.GetAxisRaw("Vertical"), 0f) && currentCamera != null)
        {
            //Positioning pointer behind camera, following forward and project it on xz
            cameraDirectionPosition = currentCamera.transform.position - currentCamera.transform.forward;
            cameraDirectionPosition.y = currentCamera.transform.position.y;
            cameraDirection.position = cameraDirectionPosition;

            //Looking the camera, camera will never be upside down
            cameraDirection.LookAt(currentCamera.transform, Vector3.up);
        }
        else
        {
            //Calculate direction point from the center of the joystick
            refreshStickDirection();

            //Player move in the direction and look in the direction (with a quick smooth)
            playerRigidbody.position += stickDirection * Time.deltaTime * maxSpeed;

            playerRotation.position = playerRigidbody.position;
            playerRotation.LookAt(playerRigidbody.position + stickDirection);
            playerRigidbody.transform.rotation = Quaternion.Slerp(playerRigidbody.transform.rotation, playerRotation.rotation, speedRotationSmooth);
        }
    }

    //Refresh stick position related to the camera orientation
    public void refreshStickDirection()
    {
        stickDirection = ((cameraDirection.right * Input.GetAxisRaw("Horizontal")) + cameraDirection.forward * Input.GetAxisRaw("Vertical"));
        if (stickDirection.magnitude > 1f) stickDirection.Normalize();
    }
}
