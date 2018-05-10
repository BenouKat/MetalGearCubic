using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(Zone))]
class ZoneEditor : Editor
{
    public void OnSceneGUI()
    {
        ((Zone)target).DrawZone();
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Zone zone = (Zone)target;
        if (GUILayout.Button("Generate Entries"))
        {
            zone.GenerateEntry();
        }
        
        if (GUILayout.Button("Clear Entries"))
        {
            zone.ClearEntry();
        }

        if (GUILayout.Button("Generate Checkers"))
        {
            zone.GenerateCheckers();
        }

        if (GUILayout.Button("Clear Checkers"))
        {
            zone.ClearCheckers();
        }
    }
}
#endif

public class Zone : MonoBehaviour {

#if UNITY_EDITOR

    public Color zoneColor;

    //To simply rect calculation
    Vector2[] signs = new Vector2[4] {
            new Vector2(1f, 1f),
            new Vector2(1f, -1f),
            new Vector2(-1f, -1f),
            new Vector2(-1f, 1f) };

    //Temp for rect points
    Vector3[] vects = new Vector3[4];

    public void DrawZone()
    {
        //It defines the height of a zone (not controlled by scale)
        float zoneHeigth = transform.parent.GetComponent<ZoneManager>().zoneHeigth;

        //For all signs
        for (int i = 0; i < signs.Length; i++)
        {
            //For all corner
            for (int j = 0; j < vects.Length; j++)
            {
                //Sign index is i+1 but loops
                int signIndex = (j > 0 && j < vects.Length - 1) ? i : i + 1;
                if (signIndex >= signs.Length) signIndex = 0;

                //Set the corner of a rect, based on zone local scale
                vects[j] = new Vector3(transform.position.x + (transform.localScale.x * 0.5f * signs[signIndex].x),
                    (j < 2 ? zoneHeigth : 0),
                    transform.position.z + (transform.localScale.z * 0.5f * signs[signIndex].y));
            }

            //Renders opaque if Z test is <, just outline of Z test is >
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Less;
            Handles.DrawSolidRectangleWithOutline(vects, new Color(zoneColor.r, zoneColor.g, zoneColor.b, 0.1f), Color.clear);

            Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
            Handles.DrawSolidRectangleWithOutline(vects, Color.clear, new Color(zoneColor.r, zoneColor.g, zoneColor.b, 0.5f));
        }

        foreach(ZoneEntry entry in zoneEntries)
        {
            //If zone is a selected game object or if it's the first entry on the list
            if(entry != null && (Selection.activeGameObject == gameObject || entry.zoneConnected.Count > 0 && entry.zoneConnected[0] == this))
            {
                //Rendering the entry box
                for (int i = 0; i < signs.Length; i++)
                {
                    vects[i] = new Vector3(entry.transform.position.x + (1f * signs[i].x), 0.01f, entry.transform.position.z + (1f * signs[i].y));
                }
                
                Color colorEntry = new Color(1f, entry.isDoor ? 1f : 0f, 0f, 0.5f);
                if (!entry.isEnabled)
                {
                    colorEntry = new Color(0.5f, 0.5f, 0.5f, 0.3f);
                }else if(Selection.activeGameObject == entry.gameObject)
                {
                    colorEntry = new Color(entry.isDoor ? 0.5f : 0f, 1f, 0f, 0.5f);
                }

                //Same logic than the zone
                Handles.zTest = UnityEngine.Rendering.CompareFunction.Less;
                Handles.DrawSolidRectangleWithOutline(vects, colorEntry, Color.clear);

                Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
                Handles.DrawSolidRectangleWithOutline(vects, Color.clear, colorEntry);
            }
        }

        Handles.zTest = UnityEngine.Rendering.CompareFunction.Less;
        Handles.color = Color.blue;
        foreach (Transform checker in zoneChecker)
        {
            if(checker != null)
            {
                Handles.DrawSolidDisc(checker.position + Vector3.up*0.5f, checker.up, 0.2f);
            }
        }
    }

    public void GenerateEntry()
    {
        ClearEntry();
        
        float entryDistanceMatch = transform.parent.GetComponent<ZoneManager>().entryDistanceMatch;

        zoneEntries = new List<ZoneEntry>();

        //For all corners
        for(int i=0; i<signs.Length; i++)
        {
            int nextSign = i + 1;
            if (nextSign >= signs.Length) nextSign = 0;

            bool reachEndPoint = false;
            bool exitFound = false;
            Vector3 startPoint = new Vector3(transform.position.x + (transform.localScale.x * 0.5f * signs[i].x), 1f, transform.position.z + (transform.localScale.z * 0.5f * signs[i].y));
            Vector3 endPoint = new Vector3(transform.position.x + (transform.localScale.x * 0.5f * signs[nextSign].x), 1f, transform.position.z + (transform.localScale.z * 0.5f * signs[nextSign].y));
            Vector3 midPoint = Vector3.zero;
            Vector3 touchPoint = Vector3.zero;
            RaycastHit info;

            //While we didn't reach the next corner
            while (!reachEndPoint)
            {
                //We do a raycast that will check if there's a hole in the wall
                if (Physics.Raycast(startPoint, endPoint - startPoint, out info, Vector3.Distance(startPoint, endPoint) + 0.001f, 1 << LayerMask.NameToLayer("Unmovable")))
                {
                    exitFound = true;
                    touchPoint = info.point;
                    
                    //If yes, we raycast backside to find the other side of the door. If we don't find it, we consider the corner to be the other side.
                    if (Physics.Raycast(touchPoint, startPoint - touchPoint, out info, Vector3.Distance(startPoint, touchPoint) + 0.001f, 1 << LayerMask.NameToLayer("Unmovable")))
                    {
                        midPoint = Vector3.Lerp(touchPoint, info.point, 0.5f);
                    }
                    else
                    {
                        midPoint = Vector3.Lerp(touchPoint, startPoint, 0.5f);
                    }

                    //Once we have found our entry, we just do a raycast from the sky to the ground to check if it's not just 2 walls side by side
                    if(Physics.Raycast(midPoint + Vector3.up*100f, -Vector3.up, out info, 101f, 1 << LayerMask.NameToLayer("Unmovable")))
                    {
                        if(info.collider.tag == "Floor")
                        {
                            InstanceEntry(midPoint, entryDistanceMatch);
                        }
                    }

                    startPoint = touchPoint;
                }
                else
                {
                    //If no door has been found
                    if (!exitFound)
                    {
                        //We raycast back to check if there's at least a wall
                        if (Physics.Raycast(endPoint, startPoint - endPoint, out info, Vector3.Distance(startPoint, endPoint) + 0.001f, 1 << LayerMask.NameToLayer("Unmovable")))
                        {
                            //Double checking if it's a wall or not
                            midPoint = Vector3.Lerp(endPoint, startPoint, 0.5f);
                            if (Physics.Raycast(midPoint + Vector3.up * 100f, -Vector3.up, out info, 101f, 1 << LayerMask.NameToLayer("Unmovable")))
                            {
                                if (info.collider.tag == "Floor")
                                {
                                    InstanceEntry(midPoint, entryDistanceMatch);
                                }
                            }
                        }
                    }

                    reachEndPoint = true;
                }
            }
        }
    }

    public void ClearEntry()
    {
        foreach (ZoneEntry entry in zoneEntries)
        {
            if(entry != null)
            {
                entry.DestroySafe(this);
            }
        }
        zoneEntries.Clear();
    }

    public void InstanceEntry(Vector3 point, float distanceMatch)
    {
        //Isntancing automaticly the entry on the hierarchy
        Transform entryRoot = transform.parent.Find("Entries");
        ZoneEntry entryComp = null;
        if (entryRoot == null)
        {
            entryRoot = (new GameObject("Entries")).transform;
            entryRoot.SetParent(transform.parent);
            entryRoot.localPosition = Vector3.zero;
            entryRoot.rotation = Quaternion.identity;
            entryRoot.localScale = Vector3.one;
        }
        else
        {
            for(int i=0; i<entryRoot.childCount; i++)
            {
                if(Vector3.Distance(point, entryRoot.GetChild(i).position) <= distanceMatch)
                {
                    entryComp = entryRoot.GetChild(i).GetComponent<ZoneEntry>();
                    if (entryComp.isEnabled)
                    {
                        entryComp.SetZoneConnected(this);
                        zoneEntries.Add(entryRoot.GetChild(i).GetComponent<ZoneEntry>());
                        return;
                    }
                }
            }
        }

        GameObject entry = new GameObject("Entry");
        entry.transform.SetParent(entryRoot);
        entry.transform.localScale = Vector3.one;
        entry.transform.position = point;
        entry.transform.rotation = Quaternion.identity;

        entryComp = entry.AddComponent<ZoneEntry>();
        entryComp.SetZoneConnected(this);
        zoneEntries.Add(entryComp);
    }


    public void GenerateCheckers()
    {
        ClearCheckers();

        float checkerLength = transform.parent.GetComponent<ZoneManager>().checkLength;

        float xProgression =  -0.5f + ((checkerLength/transform.localScale.x) / 2f);
        float zProgression = 0f;
        RaycastHit info;

        while (xProgression < 0.5f)
        {
            zProgression = -0.5f + ((checkerLength / transform.localScale.z) / 2f); ;
            while (zProgression < 0.5f)
            {
                if(Physics.Raycast(transform.position + (new Vector3(xProgression*transform.localScale.x, 10f, zProgression*transform.localScale.z)), -Vector3.up, out info, 11f, 1 << LayerMask.NameToLayer("Unmovable")))
                {
                    if(info.collider.tag == "Floor")
                    {
                        InstanceChecker(xProgression, zProgression);
                    }
                }
                zProgression += checkerLength / transform.localScale.z;
            }
            xProgression += checkerLength / transform.localScale.x;
        }
    }

    public void InstanceChecker(float xPos, float zPos)
    {
        Transform checker = (new GameObject("Checker")).transform;
        checker.SetParent(transform);
        checker.transform.localPosition = new Vector3(xPos, 0f, zPos);
        checker.transform.rotation = Quaternion.identity;
        checker.transform.localScale = Vector3.one;
        zoneChecker.Add(checker);
    }

    public void ClearCheckers()
    {
        foreach(Transform trans in zoneChecker)
        {
            DestroyImmediate(trans.gameObject);
        }
        zoneChecker.Clear();
    }

#endif

    public string zoneName;
    public List<ZoneEntry> zoneEntries;
    public List<Transform> zoneChecker;
    Transform trans;

    private void Awake()
    {
        //Just a simple optimisation, because IsInsideZone is going to be called a lot
        trans = transform;
    }

    public bool IsInsideZone(Vector3 position)
    {
        //Check if a position is inside a zone
        return position.x >= (trans.position.x - (trans.localScale.x / 2f))
            && position.x <= (trans.position.x + (trans.localScale.x / 2f))
            && position.z >= (trans.position.z - (trans.localScale.z / 2f))
             && position.z <= (trans.position.z + (trans.localScale.z / 2f));
    }
}
