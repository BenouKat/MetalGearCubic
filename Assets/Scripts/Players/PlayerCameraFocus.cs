using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCameraFocus : MonoBehaviour {

    public PlayerBehaviour playerBehaviour;
    public GameObject virtualCamera;
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

    }
	
	// Update is called once per frame
	void Update () {
		if(!isOffset && playerBehaviour.GetPlayerWallMovement() == PlayerBehaviour.PlayerWallMovement.LOOK)
        {
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

        }else if(isOffset && playerBehaviour.GetPlayerWallMovement() != PlayerBehaviour.PlayerWallMovement.LOOK)
        {
            virtualCamera.SetActive(false);
            frontCameraPosition.transform.localPosition = startCameraPosition;
            frontCameraLookAt.transform.localPosition = startCameraFollowerPosition;

            isOffset = false;
        }
	}
}
