using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBehaviour : MonoBehaviour {
    
    [Header("Movement")]
    public float maxSpeed = 1f;
    public float speedRotationSmooth = 0.5f;
    public float collisionDistance = 0.7f;
    public float decalRaycastDistance = 0.35f;

    Camera currentCamera;
    Transform cameraDirection;
    Transform playerRotation;
    Vector3 cameraDirectionPosition;
    Vector3 stickDirection;

    [Header("Item list")]
    public List<Item> playerItems;
    int equiped = -1;
    public Transform itemHandledPosition;

    [Header("Shooting Level 1")]
    GameObject mousePlaneTransform;
    Ray rayMouseToFloor;
    RaycastHit castInfo;

    void Start () {
        
        //Mostly for testing, waiting for Cinemachine brain
        if (currentCamera == null) currentCamera = Camera.main;

        //Util to calculate input direction regarding the rotation of the camera
        cameraDirection = InstanceManager.instance.CreateEmptyObject(InstanceManager.InstanceType.Utils, "CameraDirection").transform;

        //Util for applying a smooth rotation to player
        playerRotation = InstanceManager.instance.CreateEmptyObject(InstanceManager.InstanceType.Utils, "PlayerRotation").transform;

        //Setting up the plane to catch the mouse direction for aiming
        mousePlaneTransform = InstanceManager.instance.CreateEmptyObject(InstanceManager.InstanceType.Utils, "Raycatcher");

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
        if ((Input.GetButton("Aim") || (Mathf.Approximately(Input.GetAxisRaw("Horizontal"), 0f) && Mathf.Approximately(Input.GetAxisRaw("Vertical"), 0f))) && currentCamera != null)
        {
            //Positioning pointer behind camera, following forward and project it on xz
            cameraDirectionPosition = currentCamera.transform.position - currentCamera.transform.forward;
            cameraDirectionPosition.y = currentCamera.transform.position.y;
            cameraDirection.position = cameraDirectionPosition;

            //Looking the camera, camera will never be upside down
            cameraDirection.LookAt(currentCamera.transform, Vector3.up);
            playerVelocity = 0f;
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
        if (GetEquipedWeapon() == null) return;

        switch (level)
        {
            case 1:
            UpdateAimMGS1();
                break;
            case 2:
            UpdateAimMGS2();
                break;
            case 3:
                //ToDo
                break;
        }
    }

    //Aiming like MGS 1 style, you can't move and you have to rotate the stick (or move the mouse) to aim, with an upper view.
    //Actually in MGS 1 you can move while shooting, but I want a progression in gameplay. In MGS2 you can't move while aim in 1st person, so I've decided to not move while shooting at third person.
    //This way, you have a progression accros the 3 styles : 3rd person static > 1st person static > 1st person moving. Sorry Hideo :(
    public void UpdateAimMGS1()
    {
        if (Input.GetButton("Aim"))
        {
            if (Input.GetJoystickNames().Length == 0)
            {
                //If there's no joystick, the mouse pointer is helping aiming
                //We just do a raycast from the mouse to the plane we set up at Start()
                rayMouseToFloor = currentCamera.ScreenPointToRay(Input.mousePosition);
                mousePlaneTransform.transform.position = transform.position;
                mousePlaneTransform.SetActive(true);

                //If the mouse catch the plane (it always cast), we just get the point and we have or direction to aim !
                if (Physics.Raycast(rayMouseToFloor, out castInfo, 100f, 1 << LayerMask.NameToLayer("MouseEvent")))
                {
                    playerRotation.position = transform.position;
                    playerRotation.LookAt(castInfo.point);
                    transform.rotation = Quaternion.Slerp(transform.rotation, playerRotation.rotation, speedRotationSmooth);

                    GetEquipedWeapon().CanonLookForward();
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

            //Release
            if(Input.GetButtonUp("Shoot"))
            {
                GetEquipedWeapon().Release();
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

    //Aiming like in MGS2 style, 1st person, not movement and the gun is centered on screen.
    //Actually in MGS2 you can have both 1st and 3rd person shooting, but since I want to keep the controls simple, I choose only one option.
    public void UpdateAimMGS2()
    {
        //To do
    }

    //Aiming like MGS 3 style, 1st person moving
    //Again, there's a change here, to be accurate it's more like MGS4 style, cause MGS3 style is basicly MGS2 style, but hey, after 2, it's 3. No ?
    public void UpdateAimMGS3()
    {
        //To do
    }

    public void UpdateShootBehavior()
    {
        Weapon.ShootOutput output = GetEquipedWeapon().Shoot();

        switch(output)
        {
            case Weapon.ShootOutput.VALID:
                SoundManager.instance.play(GetEquipedWeapon().shootSound, transform.position, SoundManager.AudioType.SOUND);
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
            if (!Physics.Raycast(transform.position + cameraDirection.forward*decalRaycastDistance, cameraDirection.right * Input.GetAxisRaw("Horizontal"), collisionDistance, GetCollisionLayers())
                && !Physics.Raycast(transform.position - cameraDirection.forward * decalRaycastDistance, cameraDirection.right * Input.GetAxisRaw("Horizontal"), collisionDistance, GetCollisionLayers()))
            {
                stickDirection += cameraDirection.right * Input.GetAxisRaw("Horizontal");
            }
        }

        if (!Mathf.Approximately(Input.GetAxisRaw("Vertical"), 0f))
        {
            if (!Physics.Raycast(transform.position + cameraDirection.right * decalRaycastDistance, cameraDirection.forward * Input.GetAxisRaw("Vertical"), collisionDistance, GetCollisionLayers())
                && !Physics.Raycast(transform.position - cameraDirection.right * decalRaycastDistance, cameraDirection.forward * Input.GetAxisRaw("Vertical"), collisionDistance, GetCollisionLayers()))
            {
                stickDirection += cameraDirection.forward * Input.GetAxisRaw("Vertical");
            }
        }

        //This is a way to have a stickDirection point that is inside a circle, and not a square. This prevent the player to go beyond max speed.
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

    //When we equip an item, we discard the previous and equip the one selected
    public void Equip(Item item)
    {
        Unequip();
        equiped = playerItems.IndexOf(item);
        GetEquipedItem().Equip(itemHandledPosition);
    }

    //Unequip the player
    public void Unequip()
    {
        if (equiped != -1)
        {
            GetEquipedItem().Unequip();
        }
        equiped = -1;
    }

    public Item GetEquipedItem()
    {
        if (equiped < 0) return null;
        return playerItems[equiped];
    }

    public Weapon GetEquipedWeapon()
    {
        if(GetEquipedItem() is Weapon)
        {
            return (Weapon)playerItems[equiped];
        }
        return null;
    }
}
