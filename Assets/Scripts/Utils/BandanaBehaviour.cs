using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BandanaBehaviour : MonoBehaviour {

    //Initial parameters
    public GameObject quadMesh;
    public int sectionCount;
    public float sectionLength;
    public float sectionWidth;

    //Wind
    public AnimationCurve windAnimation;
    public AnimationCurve windRunAnimation;
    public float minWindAngle;
    public float maxWindAngle;
    float timeWind;
    public float minSpeedWind;
    public float maxSpeedWind;

    //Positions and jointure
    GameObject root;
    Transform goalRotation;
    public float speedRotationUpward;
    public float speedRotationDownward;
    public float speedRotationJoint;
    public float scalePerAngle;

    struct TransformJoint
    {
        public Transform joint;
        public Quaternion oldRotation;
        public Transform model;
    }
    List<TransformJoint> allJoints = new List<TransformJoint>();

    //Temp positional variables
    Vector2 oldPosition;
    Vector2 currentPosition;

    //Speed
    public float minSpeed;
    public float maxSpeed;
    public float maxAngleZ;
    float lastMovementSpeed = 0f;
    float newMovementSpeed;

    //Temp variables
    TransformJoint tempTransJoint;
    Vector3 tempEuler;
    Vector3 tempScale;
    float tempEulerZ;

    // Use this for initialization
    void Start () {
        GenerateCloth();
	}
	
	// Update is called once per frame
	void Update () {
        //Convert the current position to a Vector2, we don't do this over Y
        currentPosition.x = transform.position.x;
        currentPosition.y = transform.position.z;
        
        //Movement speed is calculated to know how many wind the player do by just moving. Goes 0 to 1 (from minSpeed to maxSpeed)
        newMovementSpeed = Mathf.Clamp(((Vector2.Distance(oldPosition, currentPosition) / Time.deltaTime) - minSpeed) / maxSpeed, 0f, 1f);
        
        //The goal rotation look down regarding the movement speed.
        goalRotation.localRotation = Quaternion.identity;
        goalRotation.Rotate(Vector3.right * Mathf.Lerp(0f, 90f, newMovementSpeed), Space.Self);

        //We add the wind
        goalRotation.Rotate(Vector3.right * GetWind() * Mathf.Lerp(minWindAngle, maxWindAngle, newMovementSpeed), Space.Self);

        //The root rotation is following the goalRotation. Depending of if we are accelerate or decelerate, it's not the same speed.
        root.transform.rotation = Quaternion.Slerp(root.transform.rotation, goalRotation.transform.rotation, GetSpeedRotation());

        //Time wind on the Animation Curve
        timeWind += Mathf.Lerp(minSpeedWind, maxSpeedWind, newMovementSpeed) * Time.deltaTime;

        //For all joints
        for (int i=0; i<allJoints.Count;i++)
        {
            //We take the joint and Slerp to the parent jointure. This is doing the "decal" between jointure, simulating cloth.
            tempTransJoint = allJoints[i];
            tempScale = tempTransJoint.model.localScale;
            tempTransJoint.joint.rotation = Quaternion.Slerp(tempTransJoint.oldRotation, tempTransJoint.joint.parent.rotation, GetSpeedRotation());
            
            //Let's talk about angles
            tempEuler = tempTransJoint.joint.localEulerAngles;

            //We don't rotate on Y. It's ugly.
            tempEuler.y = 0f;

            //We limit the Z axis to prevent to see "holes" in the quads queue.
            tempEulerZ = tempEuler.z;
            if (tempEulerZ >= 180)
            {
                tempEuler.z = Mathf.Clamp(tempEuler.z, 360f - maxAngleZ, 360f);
                tempEulerZ = 360f - tempEulerZ;
            }else if(tempEulerZ <= -180f)
            {
                tempEuler.z = Mathf.Clamp(tempEuler.z, -360f , -360f + maxAngleZ);
                tempEulerZ = tempEulerZ + 360f;
            }
            else
            {
                tempEuler.z = Mathf.Clamp(tempEuler.z, -maxAngleZ, maxAngleZ);
            }
            
            //To do this, we increase a little bit the scale of the quads
            tempScale.y = sectionLength * (1f + (scalePerAngle * Mathf.Abs(tempEulerZ)));
            tempTransJoint.model.localScale = tempScale;

            //And tada.
            tempTransJoint.joint.localEulerAngles = tempEuler;
            tempTransJoint.oldRotation = tempTransJoint.joint.rotation;
            allJoints[i] = tempTransJoint;
        }

        oldPosition = currentPosition;
        lastMovementSpeed = newMovementSpeed;
    }

    public float GetWind()
    {
        return Mathf.Lerp(windAnimation.Evaluate(Mathf.Repeat(timeWind, 1f)), windRunAnimation.Evaluate(Mathf.Repeat(timeWind, 1f)), newMovementSpeed);
    }

    //It's a very basic way to simulate gravity and cloth weight.
    public float GetSpeedRotation()
    {
        if (newMovementSpeed > lastMovementSpeed)
        {
            return speedRotationUpward;
        }else if(newMovementSpeed < lastMovementSpeed)
        {
            return speedRotationDownward;
        }
        //Else, it's equal
        return Mathf.Lerp(speedRotationDownward, speedRotationUpward, newMovementSpeed);
    }

    void GenerateCloth()
    {
        //Generating the root rotation
        root = new GameObject("Root");
        root.transform.SetParent(transform);
        root.transform.localScale = Vector3.one;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localPosition = Vector3.zero;
        oldPosition = new Vector2(root.transform.position.x, root.transform.position.z);

        //Rotation helper will help the root to target its rotation
        goalRotation = (new GameObject("RotationHelper")).transform;
        goalRotation.transform.SetParent(transform);
        goalRotation.transform.localPosition = Vector3.zero;
        goalRotation.transform.rotation = root.transform.localRotation;

        Transform currentParent = null;
        for(int i=0; i < sectionCount; i++)
        {
            //Jointure instance
            GameObject join = new GameObject("Join");
            join.transform.SetParent(currentParent ?? root.transform);
            join.transform.localScale = Vector3.one;
            join.transform.localRotation = Quaternion.identity;
            join.transform.localPosition = currentParent != null ? - (Vector3.up*sectionLength) : Vector3.zero;
            
            currentParent = join.transform;

            //Model instance
            GameObject model = Instantiate(quadMesh) as GameObject;
            model.transform.SetParent(join.transform);
            model.transform.localScale = new Vector3(sectionWidth, sectionLength, 1f);
            model.transform.localRotation = Quaternion.identity;
            model.transform.localPosition = -Vector3.up * sectionLength/2f;
            
            TransformJoint transJoint;
            transJoint.joint = join.transform;
            transJoint.oldRotation = join.transform.rotation;
            transJoint.model = model.transform;
            allJoints.Add(transJoint);
        }
    }
}
