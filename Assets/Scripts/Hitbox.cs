using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hitbox : MonoBehaviour {

    public float maxLife;
    float currentLife;
    public float shieldPercentAtSides;
    public GameObject bloodEffect;

    public void OnTriggerEnter(Collider other)
    {
        
    }
}
