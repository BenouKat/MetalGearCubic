using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour {

    public GameObject collectibleModel;
    public GameObject handledModel;

    //On Trigger enter with the "collectible item" (the box in MGS), the player has it.
    public void OnTriggerEnter(Collider other)
    {
        PlayerBehaviour player;
        if((player = other.GetComponent<PlayerBehaviour>()) != null)
        {
            //We remove the collectible graphics, and put it in the instance manager, waiting the player equip it
            player.playerItems.Add(this);
            collectibleModel.SetActive(false);
            InstanceManager.instance.MoveTo(InstanceManager.InstanceType.Items, gameObject);
        }
    }
    
    //Equip the item active its "handled" graphics in the hand of the player
    public void Equip(Transform hand)
    {
        handledModel.SetActive(true);

        //Take the item in hand
        transform.SetParent(hand);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    //Unequipe the item just hide the handled model and set it back to the instance garbage
    public void Unequip()
    {
        handledModel.SetActive(false);
        InstanceManager.instance.MoveTo(InstanceManager.InstanceType.Items, gameObject);
    }
}
