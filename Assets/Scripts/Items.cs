using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Items : MonoBehaviour {

    public GameObject graphics;

    public void OnTriggerEnter(Collider other)
    {
        PlayerBehaviour player;
        if((player = other.GetComponent<PlayerBehaviour>()) != null)
        {
            player.playerItems.Add(this);
            graphics.SetActive(false);
            InstanceManager.instance.moveTo(InstanceManager.InstanceType.Items, gameObject);
        }
    }
}
