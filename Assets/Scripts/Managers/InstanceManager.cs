using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * This class manage the multiples instances of the game, and sort them by their use case.
 **/
public class InstanceManager : MonoBehaviour {

    public static InstanceManager instance;

    public enum InstanceType { Utils, //Utils object are mostly permanent object used to calculate positions or do some maths
                               Destroyable, //Destroyable are objects that have a lifetime and meant to be destroyed after a period of time
                               Items, //List of player items
                               Audio, //All sounds
                               Graphics //Instancied graphics like hole in the wall, etc...
                            }
    [System.Serializable]
    public struct InstanceContener
    {
        public InstanceType type;
        public Transform target;
    }

    [SerializeField]
    List<InstanceContener> instanceTargets;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
    }

    GameObject tempInstance;
    public GameObject InstanceObject(InstanceType type, GameObject model, Vector3 position, Quaternion rotation)
    {
        tempInstance = Instantiate(model, position, rotation) as GameObject;
        MoveTo(type, tempInstance);
        return tempInstance;
    }

    public GameObject InstanceObject(InstanceType type, GameObject model)
    {
        return InstanceObject(type, model, Vector3.zero, Quaternion.identity);
    }

    public GameObject CreateEmptyObject(InstanceType type, string name, Vector3 position, Quaternion rotation)
    {
        tempInstance = new GameObject(name);
        tempInstance.transform.position = position;
        tempInstance.transform.rotation = rotation;
        MoveTo(type, tempInstance);
        return tempInstance;
    }

    public GameObject CreateEmptyObject(InstanceType type, string name)
    {
        return CreateEmptyObject(type, name, Vector3.zero, Quaternion.identity);
    }

    public void MoveTo(InstanceType type, GameObject model)
    {
        model.transform.SetParent(instanceTargets.Find(c => c.type == type).target);
    }
}
