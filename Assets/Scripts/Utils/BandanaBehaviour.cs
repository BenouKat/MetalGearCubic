using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BandanaBehaviour : MonoBehaviour {

    public GameObject quadMesh;
    public int sectionCount;
    public float sectionLength;
    public float sectionWidth;

    GameObject root;
    Vector2 oldPosition;
    Vector2 currentPosition;
    Transform goalRotation;
    public float speedRotationUpward;
    public float speedRotationDownward;
    List<Transform> allJoints = new List<Transform>();
    Dictionary<Transform,Quaternion> jointRotation = new Dictionary<Transform, Quaternion>();

    public float maxSpeed;
    float lastMovementSpeed = 0f;
    float newMovementSpeed;
    Transform tempTrans;
    Quaternion tempRot;
	// Use this for initialization
	void Start () {
        GenerateBandana();
	}
	
	// Update is called once per frame
	void Update () {
        currentPosition.x = transform.position.x;
        currentPosition.y = transform.position.z;

        newMovementSpeed = (Vector2.Distance(oldPosition, currentPosition) / Time.deltaTime) / maxSpeed;

        goalRotation.transform.rotation = Quaternion.identity;
        goalRotation.LookAt(Vector3.Lerp(goalRotation.transform.position + transform.parent.forward, goalRotation.transform.position - Vector3.up, newMovementSpeed / maxSpeed));

        root.transform.rotation = Quaternion.Slerp(root.transform.rotation, 
            goalRotation.transform.rotation, 
            newMovementSpeed > lastMovementSpeed ? speedRotationUpward : speedRotationDownward);


        for (int i=0; i<allJoints.Count;i++)
        {
            tempTrans = allJoints[i];
            tempRot = jointRotation[tempTrans];
            tempTrans.rotation = Quaternion.Slerp(tempRot, tempTrans.parent.rotation, lastMovementSpeed >= newMovementSpeed ? speedRotationUpward : speedRotationDownward);
            jointRotation[tempTrans] = tempTrans.rotation;
        }

        oldPosition = currentPosition;
        lastMovementSpeed = newMovementSpeed;
    }

    void GenerateBandana()
    {
        root = new GameObject("Root");
        root.transform.SetParent(transform);
        root.transform.localScale = Vector3.one;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localPosition = Vector3.zero;
        oldPosition = new Vector2(root.transform.position.x, root.transform.position.z);

        goalRotation = (new GameObject("RotationHelper")).transform;
        goalRotation.transform.SetParent(transform);
        goalRotation.transform.localPosition = Vector3.zero;
        goalRotation.transform.rotation = root.transform.localRotation;

        Transform currentParent = null;
        for(int i=0; i < sectionCount; i++)
        {
            GameObject join = new GameObject("Join");
            join.transform.SetParent(currentParent ?? root.transform);
            join.transform.localScale = Vector3.one;
            join.transform.localRotation = Quaternion.identity;
            join.transform.localPosition = currentParent != null ? - (Vector3.up*sectionLength*2f) : Vector3.zero;
            
            currentParent = join.transform;
            allJoints.Add(join.transform);
            jointRotation.Add(join.transform, join.transform.rotation);

            GameObject model = Instantiate(quadMesh) as GameObject;
            model.transform.SetParent(join.transform);
            model.transform.localScale = new Vector3(1f, 1f, sectionLength);
            model.transform.localRotation = Quaternion.identity;
            //model.transform.Rotate(-Vector3.right * 90f);
            model.transform.localPosition = -Vector3.up * sectionLength;
        }
    }
}
