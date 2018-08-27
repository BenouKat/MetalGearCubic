using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBehaviour : MonoBehaviour {
    
    [Header("Movement")]
    public float speedAgainstWall = 0.3f;
    public float maxSpeed = 1f;
    public float speedRotationSmooth = 0.5f;
    public float collisionDistance = 0.7f;
    public float decalRaycastDistance = 0.35f;
    public float wallDirectionDeadZone = 0.01f;

    //Inside variables
    //Player direction
    Camera currentCamera;
    Transform cameraDirection;
    Transform playerRotation;
    Vector3 cameraDirectionPosition;
    Vector3 stickDirection;
    Vector3 stickNormalDirection;

    //Colliding wall
    public enum PlayerWallMovement { NONE, COLLIDE, MOVE, LOOK }
    PlayerWallMovement playerWallMovement = PlayerWallMovement.NONE;
    Vector3 wallDirection;
    Vector3 wallRightDirection;
    bool aimingFromWall;
    Vector3 startDecalPosition;
    Vector3 aimingDecalPosition;

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

        LateUpdatePosition();
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

            //If we are not targeting on a corner, player wall movement is none
            if(!(Input.GetButton("Aim") && playerWallMovement == PlayerWallMovement.LOOK) && !aimingFromWall)
            {
                stickDirection = Vector3.zero;
                playerWallMovement = PlayerWallMovement.NONE;
            }
        }
        else
        {
            //Calculate direction point from the center of the joystick
            RefreshStickDirection();

            //Player move in the direction and write velocity (if not aiming from the wall)
            //If the player is against a wall, we reduce its speed
            if(!aimingFromWall)
            {
                switch (playerWallMovement)
                {
                    case PlayerWallMovement.NONE:
                        transform.position += stickDirection * Time.deltaTime * maxSpeed;
                        playerVelocity = Vector3.Distance(Vector3.zero, stickDirection * maxSpeed);
                        break;
                    case PlayerWallMovement.MOVE:
                        transform.position += stickDirection * Time.deltaTime * speedAgainstWall;
                        playerVelocity = Vector3.Distance(Vector3.zero, stickDirection * speedAgainstWall);
                        break;
                    default:
                        playerVelocity = 0f;
                        break;
                }
            }

            //Orientation regarding if the player is against a wall or not
            playerRotation.position = transform.position;
            if(playerWallMovement >= PlayerWallMovement.COLLIDE)
            {
                //The player turns his back on the wall
                playerRotation.LookAt(transform.position + wallDirection);
            }
            else
            {
                playerRotation.LookAt(transform.position + stickDirection);
            }
        }
    }

    //Upload player position after aim/shoot
    public void LateUpdatePosition()
    {
        //Player look in the direction (with a quick smooth)
        transform.rotation = Quaternion.Slerp(transform.rotation, playerRotation.rotation, speedRotationSmooth);

        if (Input.GetButton("Aim") && GetEquipedWeapon() != null)
        {
            //If the player is looking at the corner and he wants to shoot from the corner
            if (!aimingFromWall && playerWallMovement == PlayerWallMovement.LOOK && Vector3.Angle(playerRotation.forward, -wallDirection) < 90f)
            {
                aimingFromWall = true;
                startDecalPosition = transform.position;
                aimingDecalPosition = transform.position + (stickDirection * decalRaycastDistance * 2f);
            }
           
            //If we are aiming from the wall, the player decal his position
            if (aimingFromWall)
            {
                transform.position = Vector3.Lerp(transform.position, aimingDecalPosition, speedRotationSmooth);
                playerWallMovement = PlayerWallMovement.NONE;
            }

        }else
        {
            //If we stop aiming from the wall, the player returns to its original position (we force him to)
            if (aimingFromWall)
            {
                playerRotation.position = transform.position;
                playerRotation.forward = wallDirection;

                transform.position = Vector3.Lerp(transform.position, startDecalPosition, speedRotationSmooth);
                transform.rotation = Quaternion.Slerp(transform.rotation, playerRotation.rotation, speedRotationSmooth);

                //To prevent the animation being long
                if (Vector3.Distance(transform.position, startDecalPosition) < 0.01f)
                {
                    transform.position = startDecalPosition;
                    aimingFromWall = false;
                    playerWallMovement = PlayerWallMovement.LOOK;
                }
            } 
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
            UpdateAimMGS3();
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

                    GetEquipedWeapon().CanonLookForward();
                }
            }
            else
            {
                //It's basicly stick direction calculation without raycasts. We allow the player to shoot against the wall if we want to.
                playerRotation.position = transform.position;
                playerRotation.LookAt(transform.position + (cameraDirection.right * Input.GetAxisRaw("Horizontal")) + cameraDirection.forward * Input.GetAxisRaw("Vertical"));
            }
            
            //Shoot
            if (Input.GetButtonDown("Shoot")
#if UNITY_EDITOR
                || Input.GetKeyDown(KeyCode.Space)
#endif
                )
            {
                UpdateShootBehavior();
            }

            //Release
            if(Input.GetButtonUp("Shoot")
#if UNITY_EDITOR
                || Input.GetKeyUp(KeyCode.Space)
#endif
                )
            {
                GetEquipedWeapon().Release();
            }
        }
        else
        {
            //Shoot
            if (Input.GetButtonDown("Shoot")
#if UNITY_EDITOR
                || Input.GetKeyDown(KeyCode.Space)
#endif
                )
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


    bool frontDetection, rightDetection, leftDetection;
    RaycastHit frontHit, rightHit, leftHit;
    //Refresh stick position related to the camera orientation.
    //Most of this is basicly a way to redone the Rigidbody.MovePosition, because I never trust fucking physics.
    //Use 3 very short raycast at least, 4 at worst. Pretty optimized.
    void RefreshStickDirection()
    {
        //First, let's calculate the stick orientation and his normal
        stickDirection = (cameraDirection.right * Input.GetAxisRaw("Horizontal")) + (cameraDirection.forward * Input.GetAxisRaw("Vertical"));
        stickNormalDirection = (cameraDirection.forward * Input.GetAxisRaw("Horizontal")) - (cameraDirection.right * Input.GetAxisRaw("Vertical"));

        //This is a way to have a stickDirection point that is inside a circle, and not a square.
        if (stickDirection.magnitude > 1f)
        {
            stickDirection.Normalize();
            stickNormalDirection.Normalize();
        }

        Debug.DrawLine(transform.position, transform.position + (stickDirection*collisionDistance), Color.blue, Time.deltaTime, false);
        Debug.DrawLine(transform.position, transform.position + (stickNormalDirection * collisionDistance), Color.gray, Time.deltaTime, false);
        
        //Wall is detected in front of the user, or on the right, or on the left, in this order
        //If the player is already against a wall, we double the collision distance to check out the wall collision
        frontDetection = HitWallTest(stickDirection, Vector3.zero, playerWallMovement == PlayerWallMovement.NONE ? collisionDistance : collisionDistance * 2f, out frontHit, Color.red);
        rightDetection = HitWallTest(stickDirection, stickNormalDirection, collisionDistance, out rightHit, Color.magenta);
        leftDetection = HitWallTest(stickDirection, -stickNormalDirection, collisionDistance, out leftHit, Color.cyan);

        //If one of these detection has been made
        if (frontDetection || rightDetection || leftDetection)
        {
            //If the player is not against a wall, we do a second raycast but longer to catch the wall
            if(playerWallMovement == PlayerWallMovement.NONE)
                frontDetection = HitWallTest(stickDirection, Vector3.zero, collisionDistance*2f, out frontHit, Color.red);

            //If there's a wall in front of the player, we get the normal and the right vector of the wall
            if (frontDetection)
            {
                wallDirection = frontHit.normal;
            }
            wallRightDirection = Vector3.Cross(wallDirection, Vector3.up);

            //Projecting the stick direction into the wall to know in which direction the player can go
            stickDirection = Vector3.ProjectOnPlane(stickDirection, wallDirection);
            
            //If the stick is nearly the opposite of the wall, we notice that the player collide with the wall
            if (Vector3.Distance(stickDirection, Vector3.zero) <= wallDirectionDeadZone) 
            {
                stickDirection = Vector3.zero;
                //If there's a front detection, it's a wall, if not, it's just a corner, the player won't turn against the wall
                playerWallMovement = frontDetection ? PlayerWallMovement.COLLIDE : PlayerWallMovement.NONE;
                
            //Else, if the player was already against a wall...
            }else if(playerWallMovement != PlayerWallMovement.NONE)
            {
                //Wall left and right are not the same, we are at a corner, we stop the player
                if ((rightDetection && Vector3.Angle(stickDirection, wallRightDirection) > 90f && rightHit.normal != wallDirection)
                || (leftDetection && Vector3.Angle(stickDirection, wallRightDirection) < 90f && leftHit.normal != wallDirection))
                {
                    stickDirection = Vector3.zero;
                    playerWallMovement = PlayerWallMovement.COLLIDE;
                }
                //If we are moving along the wall, we test if we are at a side of a wall or not. We need to test the current direction to prevent player being stucked at wall side.
                else if ((Vector3.Angle(stickDirection, wallRightDirection) < 90f && !HitWallTest(-wallDirection, wallRightDirection, collisionDistance * 2f, Color.yellow))
                        || (Vector3.Angle(stickDirection, wallRightDirection) > 90f && !HitWallTest(-wallDirection, -wallRightDirection, collisionDistance * 2f, Color.yellow)))
                {
                    //If there's a hit missing, the player is looking on the side of the wall
                    playerWallMovement = PlayerWallMovement.LOOK;
                }

                //Else the player is moving along the wall
                else
                {
                    
                    playerWallMovement = PlayerWallMovement.MOVE;
                }
            }

            //If the player wasn't already against a wall, we let the player run along the wall (without being against it)
            else
            {
                //Huh ! A Wild wall angle appears ! If both of the right and left cast has touch, we have nowhere to go
                if(rightDetection && leftDetection)
                {
                    stickDirection = Vector3.zero;
                }
                else
                {
                    //We normalize again the stick direction (maybe shorten by the projection)
                    stickDirection.Normalize();
                }
                playerWallMovement = PlayerWallMovement.NONE;
            }
        }

        //No detection ? All right !
        else{
            playerWallMovement = PlayerWallMovement.NONE;
        }

        Debug.DrawLine(transform.position, transform.position + (stickDirection * collisionDistance), Color.green, Time.deltaTime, false);

    }

    public bool HitWallTest(Vector3 direction, Vector3 decalDirection, float distance, out RaycastHit info, Color debugColor)
    {
        Debug.DrawLine(transform.position + (decalDirection * decalRaycastDistance), (transform.position + (decalDirection * decalRaycastDistance)) + (direction * distance), debugColor, Time.deltaTime, false);
        return Physics.Raycast(transform.position + (decalDirection * decalRaycastDistance), direction, out info, distance, GetCollisionLayers());
    }

    public bool HitWallTest(Vector3 direction, Vector3 decalDirection, float distance, Color debugColor)
    {
        Debug.DrawLine(transform.position + (decalDirection * decalRaycastDistance), (transform.position + (decalDirection * decalRaycastDistance)) + (direction * distance), debugColor, Time.deltaTime, false);
        return Physics.Raycast(transform.position + (decalDirection * decalRaycastDistance), direction, distance, GetCollisionLayers());
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
    
    public PlayerWallMovement GetPlayerWallMovement()
    {
        return playerWallMovement;
    }

    public bool isPlayerMovingRight()
    {
        return Vector3.Angle(playerRotation.right, stickDirection) < 180f;
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

    public bool IsAimingFromWall()
    {
        return aimingFromWall;
    }
}
