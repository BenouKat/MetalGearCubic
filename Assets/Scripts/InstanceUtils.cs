﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * This class manage the multiples instances of the game, and sort them by their use case.
 **/
public class InstanceManager : MonoBehaviour {

    public static InstanceManager instance;

    public enum InstanceType { Utils, //Utils object are mostly permanent object used to calculate positions or do some maths
                               Destroyable //Destroyable are objects that have a lifetime and meant to be destroyed after a period of time
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
    public GameObject instanceObject(InstanceType type, GameObject model, Vector3 position, Quaternion rotation)
    {
        tempInstance = Instantiate(model, position, rotation) as GameObject;
        tempInstance.transform.SetParent(instanceTargets.Find(c => c.type == type).target);
        return tempInstance;
    }

    public GameObject instanceObject(InstanceType type, GameObject model)
    {
        return instanceObject(type, model, Vector3.zero, Quaternion.identity);
    }
}
