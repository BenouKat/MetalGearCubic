using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour {

    public GameObject collectibleModel;
    public GameObject handledModel;

    private void Start()
    {
        collectibleModel.SetActive(true);
        handledModel.SetActive(false);
    }

    //On Trigger enter with the "collectible item" (the box in MGS), the player has it.
    void OnTriggerEnter(Collider other)
    {
        PlayerBehaviour player;
        if((player = other.GetComponent<PlayerBehaviour>()) != null)
        {
            //We don't need to detect the object anymore
            Destroy(GetComponent<BoxCollider>());
            Destroy(GetComponent<Rigidbody>());
            //Removing the collectible graphics
            collectibleModel.SetActive(false);

            //Add the item to the player. If the player isn't equiped with anything, we auto equip
            player.playerItems.Add(this);
            if (player.GetEquipedItem() == null)
            {
                player.Equip(this);
            }
            else //Else, we put it in the instance garbage
            {
                InstanceManager.instance.MoveTo(InstanceManager.InstanceType.Items, gameObject);
            }
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
        transform.localScale = Vector3.one;
    }

    //Unequipe the item just hide the handled model and set it back to the instance garbage
    public void Unequip()
    {
        handledModel.SetActive(false);
        InstanceManager.instance.MoveTo(InstanceManager.InstanceType.Items, gameObject);
    }
}
