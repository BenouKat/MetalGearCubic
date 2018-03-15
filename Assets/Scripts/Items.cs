using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour {

    public GameObject collectibleModel;
    public GameObject handledModel;

    public void OnTriggerEnter(Collider other)
    {
        PlayerBehaviour player;
        if((player = other.GetComponent<PlayerBehaviour>()) != null)
        {
            player.playerItems.Add(this);
            collectibleModel.SetActive(false);
            InstanceManager.instance.MoveTo(InstanceManager.InstanceType.Items, gameObject);
        }
    }
    
    public void Equip(Transform hand)
    {
        handledModel.SetActive(true);

        //Take the item in hand
        transform.SetParent(hand);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    public void Unequip()
    {
        handledModel.SetActive(false);
        InstanceManager.instance.MoveTo(InstanceManager.InstanceType.Items, gameObject);
    }
}
