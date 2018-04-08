using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BandanaBehaviour : MonoBehaviour {

    public GameObject quadMesh;
    public int sectionCount;
    public float sectionLength;
    public float sectionWidth;

    GameObject root;
    Vector3 oldPosition;
    Quaternion neutralRotation;
    public float speedRotationUpward;
    public float speedRotationDownward;
    List<Transform> allJoints = new List<Transform>();
    Dictionary<Transform,Quaternion> jointRotation = new Dictionary<Transform, Quaternion>();

    public float maxSpeed;
    float lastMovementSpeed;
    float newMovementSpeed;
    Transform tempTrans;
    Quaternion tempRot;
	// Use this for initialization
	void Start () {
        GenerateBandana();
	}
	
	// Update is called once per frame
	void Update () {
        newMovementSpeed = (Vector3.Distance(oldPosition, transform.position) / Time.deltaTime) / maxSpeed;
            root.transform.rotation = Quaternion.Slerp(transform.rotation,
            Quaternion.Slerp(neutralRotation, Quaternion.identity, newMovementSpeed),
            newMovementSpeed >= lastMovementSpeed ? speedRotationUpward : speedRotationDownward);

        for(int i=0; i<allJoints.Count;i++)
        {
            tempTrans = allJoints[i];
            tempRot = jointRotation[tempTrans];
            tempTrans.rotation = Quaternion.Slerp(tempRot, tempTrans.parent.rotation, lastMovementSpeed >= newMovementSpeed ? speedRotationUpward : speedRotationDownward);
            jointRotation[tempTrans] = tempRot;
        }
	}

    void GenerateBandana()
    {
        root = new GameObject("Root");
        root.transform.SetParent(transform);
        root.transform.localScale = Vector3.one;
        root.transform.forward = Vector3.up;
        oldPosition = root.transform.position;
        neutralRotation = root.transform.rotation;

        Transform currentParent = null;
        for(int i=0; i < sectionCount; i++)
        {
            GameObject join = new GameObject("Join");
            join.transform.SetParent(currentParent ?? root.transform);
            join.transform.localScale = new Vector3(1f, 1f, sectionLength);
            join.transform.localRotation = Quaternion.identity;
            if (currentParent != null) join.transform.localPosition = -Vector3.forward;
            currentParent = join.transform;
            allJoints.Add(join.transform);
            jointRotation.Add(join.transform, join.transform.rotation);

            GameObject model = Instantiate(quadMesh) as GameObject;
            model.transform.SetParent(join.transform);
            model.transform.localScale = Vector3.one;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localPosition = -Vector3.forward * 0.5f;
        }
    }
}
