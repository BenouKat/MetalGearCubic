using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RadarTracker : MonoBehaviour {

    public enum RadarType { ENEMY, PLAYER }
    public RadarType type;

    Transform model;

    private void Start()
    {
        RadarManager.instance.AddTracker(this);
    }

    public void SetTrackerModel(Transform model)
    {
        this.model = model;
    }

    public Transform GetTrackerModel()
    {
        return model;
    }

    private void OnDestroy()
    {
        RadarManager.instance.RemoveTracker(this);
    }
}
