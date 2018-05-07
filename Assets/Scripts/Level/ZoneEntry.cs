using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
[CustomEditor(typeof(ZoneEntry))]
class ZoneEntryEditor : Editor
{
    public void OnSceneGUI()
    {
        //Draw all connected zone
        foreach(Zone zone in ((ZoneEntry)target).zoneConnected)
        {
            if(zone != null)
            {
                zone.DrawZone();
            }
        }
    }
}
#endif

public class ZoneEntry : MonoBehaviour {

    public bool isEnabled = true;
    public bool isDoor;
    public List<Zone> zoneConnected;

    //Set connected zone connects a zone with an entry
    public void SetZoneConnected(Zone zone)
    {
        if (zoneConnected == null) zoneConnected = new List<Zone>();
        zoneConnected.Add(zone);
    }

    //The object is only destroy if there's no zone attached to this entry
    public void DestroySafe(Zone zone)
    {
        if(zoneConnected != null)
        {
            zoneConnected.Remove(zone);
            if (zoneConnected.Count == 0) DestroyImmediate(gameObject);
        }
        else
        {
            DestroyImmediate(gameObject);
        }
    }
}
