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
    }
}
#endif

public class Zone : MonoBehaviour {

#if UNITY_EDITOR

    public Color zoneColor;

    Vector2[] signs = new Vector2[4] {
            new Vector2(1f, 1f),
            new Vector2(1f, -1f),
            new Vector2(-1f, -1f),
            new Vector2(-1f, 1f) };

    Vector3[] vects = new Vector3[4];

    public void DrawZone()
    {
        float zoneHeigth = transform.parent.GetComponent<ZoneManager>().zoneHeigth;
        
        for (int i = 0; i < signs.Length; i++)
        {
            for (int j = 0; j < vects.Length; j++)
            {
                int signIndex = (j > 0 && j < vects.Length - 1) ? i : i + 1;
                if (signIndex >= signs.Length) signIndex = 0;

                vects[j] = new Vector3(transform.position.x + (transform.localScale.x * 0.5f * signs[signIndex].x),
                    (j < 2 ? zoneHeigth : 0),
                    transform.position.z + (transform.localScale.z * 0.5f * signs[signIndex].y));
            }
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Less;
            Handles.DrawSolidRectangleWithOutline(vects, new Color(zoneColor.r, zoneColor.g, zoneColor.b, 0.1f), Color.clear);

            Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
            Handles.DrawSolidRectangleWithOutline(vects, Color.clear, new Color(zoneColor.r, zoneColor.g, zoneColor.b, 0.5f));
        }

        foreach(ZoneEntry entry in zoneEntries)
        {
            if(entry != null)
            {
                for (int i = 0; i < signs.Length; i++)
                {
                    vects[i] = new Vector3(entry.transform.position.x + (1f * signs[i].x), 0.01f, entry.transform.position.z + (1f * signs[i].y));
                }
                Handles.zTest = UnityEngine.Rendering.CompareFunction.Less;
                Handles.DrawSolidRectangleWithOutline(vects, new Color(1f, entry.isDoor ? 1f : 0f, 0f, 0.5f), Color.clear);

                Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
                Handles.DrawSolidRectangleWithOutline(vects, Color.clear, new Color(1f, entry.isDoor ? 1f : 0f, 0f, 0.5f));
            }
        }
    }

    public void GenerateEntry()
    {
        StartCoroutine(RoutineGenerateEntry());
    }

    IEnumerator RoutineGenerateEntry()
    {
        foreach(ZoneEntry entry in zoneEntries)
        {
            DestroyImmediate(entry.gameObject);
        }

        zoneEntries = new List<ZoneEntry>();
        float t = 0f;

        for(int i=0; i<signs.Length; i++)
        {
            int nextSign = i + 1;
            if (nextSign >= signs.Length) i = 0;

            bool reachEndPoint = false;
            bool exitFound = false;
            Vector3 startPoint = new Vector3(transform.position.x + (transform.localScale.x * 0.5f * signs[i].x), 1f, transform.position.z + (transform.localScale.z * 0.5f * signs[i].y));
            Vector3 endPoint = new Vector3(transform.position.x + (transform.localScale.x * 0.5f * signs[nextSign].x), 1f, transform.position.z + (transform.localScale.z * 0.5f * signs[nextSign].y));
            RaycastHit info;
            while (!reachEndPoint)
            {
                t = Time.time;
                while (Time.time < t + 5f)
                {
                    Debug.DrawLine(startPoint, endPoint, Color.yellow);
                    yield return 0;
                }

                if (Physics.Raycast(startPoint, endPoint - startPoint, out info, Vector3.Distance(startPoint, endPoint) + 0.001f, 1 << LayerMask.NameToLayer("Unmovable")))
                {
                    t = Time.time;
                    while(Time.time < t + 5f)
                    {
                        Debug.DrawLine(startPoint, info.point, Color.red);
                        yield return 0;
                    }

                    

                    exitFound = true;
                    Vector3 touchPoint = info.point;

                    t = Time.time;
                    while (Time.time < t + 5f)
                    {
                        Debug.DrawLine(touchPoint, startPoint, Color.cyan);
                        yield return 0;
                    }

                    if (Physics.Raycast(touchPoint, startPoint - touchPoint, out info, Vector3.Distance(startPoint, touchPoint) + 0.001f, 1 << LayerMask.NameToLayer("Unmovable")))
                    {
                        t = Time.time;
                        while (Time.time < t + 5f)
                        {
                            Debug.DrawLine(touchPoint, info.point, Color.green);
                            yield return 0;
                        }

                        InstanceEntry(Vector3.Lerp(touchPoint, info.point, 0.5f));
                    }
                    else
                    {
                        InstanceEntry(Vector3.Lerp(touchPoint, startPoint, 0.5f));
                    }
                    startPoint = touchPoint;
                }
                else
                {
                    if (!exitFound)
                    {
                        t = Time.time;
                        while (Time.time < t + 5f)
                        {
                            Debug.DrawLine(startPoint, endPoint, Color.magenta);
                            yield return 0;
                        }

                        if (Physics.Raycast(endPoint, startPoint - endPoint, out info, Vector3.Distance(startPoint, endPoint) + 0.001f, 1 << LayerMask.NameToLayer("Unmovable")))
                        {
                            t = Time.time;
                            while (Time.time < t + 5f)
                            {
                                Debug.DrawLine(endPoint, info.point, Color.blue);
                                yield return 0;
                            }

                            InstanceEntry(Vector3.Lerp(endPoint, startPoint, 0.5f));
                        }
                    }

                    reachEndPoint = true;
                }
            }
        }
    }

    public void InstanceEntry(Vector3 point)
    {
        GameObject entry = new GameObject("Entry");
        entry.transform.SetParent(transform);
        entry.transform.localScale = Vector3.one;
        entry.transform.position = point;
        entry.transform.rotation = Quaternion.identity;

        ZoneEntry entryComp = entry.AddComponent<ZoneEntry>();
        zoneEntries.Add(entryComp);
    }
#endif

    public string zoneName;
    public List<ZoneEntry> zoneEntries;

    private void Start()
    {
        
    }

    private void Update()
    {

    }
}
