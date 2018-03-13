﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBehaviour : MonoBehaviour {
    
    [Header("Movement")]
    public float maxSpeed = 1f;
    public float speedRotationSmooth = 0.5f;
    public float collisionDistance = 0.7f;

    Camera currentCamera;
    Transform cameraDirection;
    Transform playerRotation;
    Vector3 cameraDirectionPosition;
    Vector3 stickDirection;

    [Header("Item list")]
    public List<Items> playerItems;
    int equiped = -1;

    [Header("Shooting Level 1")]
    GameObject mousePlaneTransform;
    Ray rayMouseToFloor;
    RaycastHit castInfo;

    void Start () {
        
        //Mostly for testing, waiting for Cinemachine brain
        if (currentCamera == null) currentCamera = Camera.main;

        //Util to calculate input direction regarding the rotation of the camera
        cameraDirection = InstanceManager.instance.InstanceObject(InstanceManager.InstanceType.Utils, 
            new GameObject("CameraDirection")).transform;

        //Util for applying a smooth rotation to player
        playerRotation = InstanceManager.instance.InstanceObject(InstanceManager.InstanceType.Utils,
            new GameObject("PlayerRotation")).transform;

        //Setting up the plane to catch the mouse direction for aiming
        mousePlaneTransform = InstanceManager.instance.InstanceObject(InstanceManager.InstanceType.Utils,
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
        UpdatePosition();

        //If the player has a weapon equiped, allow him to aim
        if (GetEquipedWeapon() != null)
        {
            UpdateAimBehavior(1);
        }
    }

    //Update player position
    public void UpdatePosition()
    {
        if (Input.GetButton("Aim") || Mathf.Approximately(Input.GetAxisRaw("Horizontal"), 0f) && Mathf.Approximately(Input.GetAxisRaw("Vertical"), 0f) && currentCamera != null)
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
            RefreshStickDirection();

            //Player move in the direction and write velocity
            transform.position += stickDirection * Time.deltaTime * maxSpeed;
            playerVelocity = Vector3.Distance(Vector3.zero, stickDirection * maxSpeed);

            //Player look in the direction (with a quick smooth)
            playerRotation.position = transform.position;
            playerRotation.LookAt(transform.position + stickDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, playerRotation.rotation, speedRotationSmooth);
        }
    }

    public void UpdateAimBehavior(int level)
    {
        switch(level)
        {
            case 1:
            UpdateAimPS1();
                break;
            case 2:
                //ToDo
                break;
            case 3:
                //ToDo
                break;
        }
    }

    public void UpdateAimPS1()
    {
        if (Input.GetButton("Aim"))
        {
            if (Input.GetJoystickNames().Length == 0)
            {
                rayMouseToFloor = currentCamera.ScreenPointToRay(Input.mousePosition);
                mousePlaneTransform.transform.position = transform.position;
                mousePlaneTransform.SetActive(true);

                if (Physics.Raycast(rayMouseToFloor, out castInfo, 100f, 1 << LayerMask.NameToLayer("MouseEvent")))
                {
                    playerRotation.position = transform.position;
                    playerRotation.LookAt(castInfo.point);
                    transform.rotation = Quaternion.Slerp(transform.rotation, playerRotation.rotation, speedRotationSmooth);
                }
            }
            else
            {
                //It's basicly stick direction calculation without raycasts. We allow the player to shoot against the wall if we want to.
                transform.LookAt(transform.position + (cameraDirection.right * Input.GetAxisRaw("Horizontal")) + cameraDirection.forward * Input.GetAxisRaw("Vertical"));
            }

            //Shoot
            if (Input.GetButtonDown("Shoot"))
            {
                UpdateShootBehavior();
            }
        }
        else
        {
            //Shoot
            if (Input.GetButtonDown("Shoot"))
            {
                GetEquipedWeapon().CallReload();
            }
        }

        //Release aim
        if (Input.GetButtonUp("Aim"))
        {
            mousePlaneTransform.SetActive(false);
        }
    }

    public void UpdateShootBehavior()
    {
        Weapon.ShootOutput output = GetEquipedWeapon().Shoot();

        switch(output)
        {
            case Weapon.ShootOutput.VALID:
                SoundManager.instance.play("Shoot" + GetEquipedWeapon().WeaponID, transform.position, SoundManager.AudioType.SOUND);
                break;
            case Weapon.ShootOutput.EMPTY:
                SoundManager.instance.play("EmptyAmmo", transform.position, SoundManager.AudioType.SOUND);
                break;
            case Weapon.ShootOutput.RELOAD:
                SoundManager.instance.play("Reload", transform.position, SoundManager.AudioType.SOUND);
                break;
        }
    }

    //Refresh stick position related to the camera orientation
    void RefreshStickDirection()
    {
        stickDirection = Vector3.zero;

        //We test physics on the two axes to prevent wall hit and redirect the player in the best direction along the wall
        if (!Mathf.Approximately(Input.GetAxisRaw("Horizontal"), 0f))
        {
            if (!Physics.Raycast(transform.position, cameraDirection.right * Input.GetAxisRaw("Horizontal"), collisionDistance, GetCollisionLayers()))
            {
                stickDirection += cameraDirection.right * Input.GetAxisRaw("Horizontal");
            }
        }

        if (!Mathf.Approximately(Input.GetAxisRaw("Vertical"), 0f))
        {
            if (!Physics.Raycast(transform.position, cameraDirection.forward * Input.GetAxisRaw("Vertical"), collisionDistance, GetCollisionLayers()))
            {
                stickDirection += cameraDirection.forward * Input.GetAxisRaw("Vertical");
            }
        }

        if (stickDirection.magnitude > 1f) stickDirection.Normalize();
    }

    int GetCollisionLayers()
    {
        return 1 << LayerMask.NameToLayer("Unmovable");
    }

    float playerVelocity;
    public float GetPlayerVelocity()
    {
        return playerVelocity;
    }

    public Weapon GetEquipedWeapon()
    {
        if(equiped >= 0 && playerItems[equiped] is Weapon)
        {
            return (Weapon)playerItems[equiped];
        }
        return null;
    }
}