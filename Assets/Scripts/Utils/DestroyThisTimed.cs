using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyThisTimed : MonoBehaviour {

    public float lifetime;
    
	void Awake () {
        Destroy(gameObject, lifetime);
	}
}
