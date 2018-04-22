using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCameraFocus : MonoBehaviour {

    public PlayerBehaviour playerBehaviour;
    GameObject virtualCamera;
    public GameObject frontCameraPosition;
    public GameObject frontCameraLookAt;

    public float distanceAngle = 2f;
    Vector3 startCameraPosition;
    Vector3 startCameraFollowerPosition;
    bool isOffset = false;

	// Use this for initialization
	void Start () {
        if (playerBehaviour == null) playerBehaviour = GetComponent<PlayerBehaviour>();
        startCameraPosition = frontCameraPosition.transform.localPosition;
        startCameraFollowerPosition = frontCameraLookAt.transform.localPosition;
        virtualCamera = CameraManager.instance.mainCam;

    }
	
	// Update is called once per frame
	void Update () {
        //If the player is looking from a corner of the wall
		if(!isOffset && playerBehaviour.GetPlayerWallMovement() == PlayerBehaviour.PlayerWallMovement.LOOK)
        {
            //The angle camera is set, depending of the side, we displace the target view on the right or on the left
            virtualCamera.SetActive(true);
            if (playerBehaviour.isPlayerMovingRight())
            {
                frontCameraPosition.transform.localPosition = startCameraPosition + (Vector3.right* distanceAngle);
                frontCameraLookAt.transform.localPosition = startCameraFollowerPosition + (Vector3.right * distanceAngle);
            }
            else
            {
                frontCameraPosition.transform.localPosition = startCameraPosition - (Vector3.right * distanceAngle);
                frontCameraLookAt.transform.localPosition = startCameraFollowerPosition - (Vector3.right * distanceAngle);
            }
            isOffset = true;

        //Else if the player stop looking, the angle camera is off
        }else if(isOffset && playerBehaviour.GetPlayerWallMovement() != PlayerBehaviour.PlayerWallMovement.LOOK)
        {
            virtualCamera.SetActive(false);
            frontCameraPosition.transform.localPosition = startCameraPosition;
            frontCameraLookAt.transform.localPosition = startCameraFollowerPosition;

            isOffset = false;
        }
	}
}
