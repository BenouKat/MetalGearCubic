using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class PlayerCameraFocus : MonoBehaviour {

    public PlayerBehaviour playerBehaviour;
    public Transform frontCameraPosition;
    public Transform frontCameraLookAt;
    
    public float distanceAngle = 2f;
    Vector3 startCameraPosition;
    Vector3 startCameraFollowerPosition;
    Vector3 startCameraAngle;
    Quaternion startCameraAngleRotation;
    bool isOffset = false;
    bool wasOffset = false;
    Vector3 oldPosition;

    public AnimationCurve blendIn;
    public AnimationCurve blendOut;
    float timeAnimationBlendIn;
    float timeAnimationBlendOut;
    float animationTimer;

	// Use this for initialization
	void Start () {
        if (playerBehaviour == null) playerBehaviour = GetComponent<PlayerBehaviour>();
        startCameraPosition = frontCameraPosition.transform.localPosition;
        startCameraFollowerPosition = frontCameraLookAt.transform.localPosition;
        timeAnimationBlendIn = blendIn.keys[blendIn.length - 1].time;
        timeAnimationBlendOut = blendOut.keys[blendOut.length - 1].time;
    }
	
	// Update is called once per frame
	void Update () {
        
        //If the player is looking at corner or is aiming from the wall
        if(playerBehaviour.GetPlayerWallMovement() >= PlayerBehaviour.PlayerWallMovement.LOOK || playerBehaviour.IsAimingFromWall())
        {
            //If the offset is not set, we set the start of the angle camera position
            if(!isOffset)
            {
                CameraManager.instance.angleCam.SetActive(true);
                CameraManager.instance.angleCam.transform.position = CameraManager.instance.mainCam.transform.position;
                CameraManager.instance.angleCam.transform.rotation = CameraManager.instance.mainCam.transform.rotation;
                oldPosition = Vector3.zero;
                isOffset = true;
                wasOffset = true;
            }
            else
            {
                oldPosition = frontCameraPosition.localPosition;
            }
            
            //We constantly set the camera position focus, to prevent the camera go forth and back in case of a tiny wall
            frontCameraPosition.localPosition = startCameraPosition + (Vector3.right * GetDirectionSigned(distanceAngle));
            frontCameraLookAt.localPosition = startCameraFollowerPosition + (Vector3.right * GetDirectionSigned(distanceAngle));
            frontCameraPosition.LookAt(frontCameraLookAt, Vector3.up);

            //If the front camera position move since the last time, and it's not aiming the wall, the animation starts again
            if (oldPosition != frontCameraPosition.localPosition && !playerBehaviour.IsAimingFromWall())
            {
                animationTimer = 0f;
                startCameraAngle = CameraManager.instance.angleCam.transform.position;
                startCameraAngleRotation = CameraManager.instance.angleCam.transform.rotation;
            }

            //If the animation timer didn't reach the max, we do the animation. If the player is aiming from the wall, we freeze it to prevent bad aiming.
            if(animationTimer < timeAnimationBlendIn && !playerBehaviour.IsAimingFromWall())
            {
                animationTimer += Time.deltaTime;
                CameraManager.instance.angleCam.transform.position = Vector3.Lerp(startCameraAngle, frontCameraPosition.position, blendIn.Evaluate(animationTimer));
                CameraManager.instance.angleCam.transform.rotation = Quaternion.Slerp(startCameraAngleRotation, frontCameraPosition.rotation, blendIn.Evaluate(animationTimer));
            }

        }
        //If the camera was already offset, it's time to go back !
        else if(wasOffset)
        {
            //Initialization of the camera start position
            if(isOffset)
            {
                animationTimer = 0f;
                startCameraAngle = CameraManager.instance.angleCam.transform.position;
                startCameraAngleRotation = CameraManager.instance.angleCam.transform.rotation;
                isOffset = false;
            }

            //Blending out to the current active camera location
            if (animationTimer < timeAnimationBlendOut)
            {
                animationTimer += Time.deltaTime;
                CameraManager.instance.angleCam.transform.position = Vector3.Lerp(startCameraAngle, CameraManager.instance.GetCurrentActiveCamera().transform.position, blendOut.Evaluate(animationTimer));
                CameraManager.instance.angleCam.transform.rotation = Quaternion.Slerp(startCameraAngleRotation, CameraManager.instance.GetCurrentActiveCamera().transform.rotation, blendOut.Evaluate(animationTimer));
            }
            else
            {
                //Once it's done, the camera is disabled and cinemachine takes the control again
                CameraManager.instance.angleCam.SetActive(false);
                wasOffset = false;
            }
        }
	}

    public float GetDirectionSigned(float distance)
    {
        return distance * (playerBehaviour.isPlayerMovingRight() ? 1f : -1f);
    }
}
