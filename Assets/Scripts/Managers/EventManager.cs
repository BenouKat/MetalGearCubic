using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventManager : MonoBehaviour {

    public static EventManager instance;
    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
    }

    /*
    //Handlers
    public delegate void HitboxEventHandler(Hitbox hitbox);

    //Events
    public event HitboxEventHandler OnHitboxHit;
    public void InvokeHitboxHit(Hitbox hitbox)
    {
        if (OnHitboxHit != null) OnHitboxHit.Invoke(hitbox);
    }*/
}
