using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
[CustomEditor(typeof(ZoneManager))]
class ZoneManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        //Generate all the zone entries
        ZoneManager zoneManager = (ZoneManager)target;
        if (GUILayout.Button("Generate All Entries"))
        {
            for (int i = 0; i < zoneManager.transform.childCount; i++)
            {
                Zone zone = zoneManager.transform.GetChild(i).GetComponent<Zone>();
                if (zone != null)
                {
                    zone.GenerateEntry();
                }
            }
        }

        //Clear all the entries
        if (GUILayout.Button("Clear All Entries"))
        {
            for (int i = 0; i < zoneManager.transform.childCount; i++)
            {
                Zone zone = zoneManager.transform.GetChild(i).GetComponent<Zone>();
                if (zone != null)
                {
                    zone.ClearEntry();
                }
            }
        }

        //Generate all the zone entries
        if (GUILayout.Button("Generate All Checkers"))
        {
            for (int i = 0; i < zoneManager.transform.childCount; i++)
            {
                Zone zone = zoneManager.transform.GetChild(i).GetComponent<Zone>();
                if (zone != null)
                {
                    zone.GenerateCheckers();
                }
            }
        }

        //Clear all the entries
        if (GUILayout.Button("Clear All Checkers"))
        {
            for (int i = 0; i < zoneManager.transform.childCount; i++)
            {
                Zone zone = zoneManager.transform.GetChild(i).GetComponent<Zone>();
                if (zone != null)
                {
                    zone.ClearCheckers();
                }
            }
        }

        //Random colorize the zones
        if (GUILayout.Button("Colorize"))
        {
            for (int i = 0; i < zoneManager.transform.childCount; i++)
            {
                Zone zone = zoneManager.transform.GetChild(i).GetComponent<Zone>();
                if (zone != null)
                {
                    zone.zoneColor = Color.HSVToRGB(Mathf.Lerp(0.1f, 0.8f, Random.Range(0f, 1f)), 1f, 1f);
                }
            }
        }
    }

    private void OnSceneGUI()
    {
        //On GUI, draw all zones inside the Zone Manager
        ZoneManager zoneManager = (ZoneManager)target;
        for(int i=0; i<zoneManager.transform.childCount; i++)
        {
            Zone zone = zoneManager.transform.GetChild(i).GetComponent<Zone>();
            if (zone != null)
            {
                zone.DrawZone();
            }
        }
    }
    
}

#endif

public class ZoneManager : MonoBehaviour {

    public static ZoneManager instance;

    [Range(1f, 5f)]
    public float zoneHeigth = 3f;
    [Range(0f, 2f)]
    public float entryDistanceMatch = 1f;
    [Range(1f, 5f)]
    public float checkLength = 1f;

    [HideInInspector]
    public List<Zone> allZones;
    [HideInInspector]
    public List<ZoneEntry> allEntry;
    
	// Use this for initialization
	void Awake () {

        if (instance == null)
        {
            instance = this;
        }

        allZones = new List<Zone>();
        allZones.AddRange(GetComponentsInChildren<Zone>());

        allEntry = new List<ZoneEntry>();
        allEntry.AddRange(GetComponentsInChildren<ZoneEntry>());
    }


    //Get the zone for a given position

    float minDistance = 0f;
    float tempDistance = 0f;
    Zone closestZone;
    public Zone GetZone(Vector3 position)
    {
        //Check inside
        foreach(Zone zone in allZones)
        {
            if(zone.IsInsideZone(position))
            {
                return zone;
            }
        }

        //If not found, check zone entry
        foreach (ZoneEntry zoneEntry in allEntry)
        {
            if(Vector3.Distance(zoneEntry.transform.position, position) < 1f)
            {
                return zoneEntry.zoneConnected[0];
            }
        }

        //If not found, check closest zone
        minDistance = Mathf.Infinity;
        closestZone = null;
        foreach (Zone zone in allZones)
        {
            tempDistance = (position - zone.transform.position).sqrMagnitude;
            if (tempDistance < minDistance)
            {
                minDistance = tempDistance;
                closestZone = zone;
            }
        }

        return closestZone;
    }

    
}
