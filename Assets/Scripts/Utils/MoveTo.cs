using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveTo : MonoBehaviour {

    public InstanceManager.InstanceType moveToType;

    private void Start()
    {
        InstanceManager.instance.MoveTo(moveToType, gameObject);
    }
}
