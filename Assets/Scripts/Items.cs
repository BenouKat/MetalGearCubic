using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Items : MonoBehaviour {

    public GameObject objectModel;

    public void OnTriggerEnter(Collider other)
    {
        PlayerBehaviour player;
        if((player = other.GetComponent<PlayerBehaviour>()) != null)
        {
            player.playerItems.Add(this);
            objectModel.SetActive(false);
            InstanceManager.instance.MoveTo(InstanceManager.InstanceType.Items, gameObject);
        }
    }
}
