using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
[CustomEditor(typeof(ZoneManager))]
class ZoneManagerEditor : Editor
{
    private void OnSceneGUI()
    {
        ZoneManager zoneManager = (ZoneManager)target;
        for(int i=0; i<zoneManager.transform.childCount; i++)
        {
            zoneManager.transform.GetChild(i).GetComponent<Zone>().DrawZone();
        }
    }
    
}

#endif

public class ZoneManager : MonoBehaviour {

    [Range(1f, 5f)]
    public float zoneHeigth = 3f;


	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
