using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(IAMemory))]
class IAMemoryEditor : Editor
{
    public void OnSceneGUI()
    {
        IAMemory memory = ((IAMemory)target);

        memory.DrawMemoryEditor();
    }
}
#endif

[RequireComponent(typeof(IABrain))]
public class IAMemory : MonoBehaviour {

    #region Editor Getters
    public void DrawMemoryEditor()
    {
        Handles.color = Color.white;
        Handles.zTest = UnityEngine.Rendering.CompareFunction.Less;

        foreach (IAInformation info in hardMemory)
        {
            if (info.type == IAInformation.InformationType.SEARCHZONE || info.type == IAInformation.InformationType.ZONECLEAR)
            {
                Zone zoneDisplayed = ZoneManager.instance.allZones.Find(c => c.zoneName == info.parameters);
                if (zoneDisplayed != null)
                {
                    Vector3[] vects = new Vector3[4]
                    {
                        new Vector3((zoneDisplayed.transform.position.x + (zoneDisplayed.transform.localScale.x/2f)), 0.1f, (zoneDisplayed.transform.position.z + (zoneDisplayed.transform.localScale.z/2f))),
                        new Vector3((zoneDisplayed.transform.position.x - (zoneDisplayed.transform.localScale.x/2f)), 0.1f, (zoneDisplayed.transform.position.z + (zoneDisplayed.transform.localScale.z/2f))),
                        new Vector3((zoneDisplayed.transform.position.x - (zoneDisplayed.transform.localScale.x/2f)), 0.1f, (zoneDisplayed.transform.position.z - (zoneDisplayed.transform.localScale.z/2f))),
                        new Vector3((zoneDisplayed.transform.position.x + (zoneDisplayed.transform.localScale.x/2f)), 0.1f, (zoneDisplayed.transform.position.z - (zoneDisplayed.transform.localScale.z/2f)))
                    };

                    Color color = info.type == IAInformation.InformationType.SEARCHZONE ? new Color(1f, 1f, 0f, 0.5f) : new Color(0f, 1f, 0f, 0.5f);
                    color.a = (memoryPerformance.Evaluate(Time.time - info.timeReceived) * info.completion) * 0.5f;

                    if (info.type == IAInformation.InformationType.SEARCHZONE)
                    {
                        Handles.DrawSolidRectangleWithOutline(vects, Color.clear, color);
                    }
                    else
                    {
                        Handles.DrawSolidRectangleWithOutline(vects, color, Color.clear);
                    }
                }
            }
        }
    }
    #endregion

    IABrain brain;
    List<IAInformation> hardMemory = new List<IAInformation>();
    List<IAInformation> softMemory = new List<IAInformation>();

    float lastSoftMemoryAccess;
    public int maxMemory;
    public AnimationCurve memoryPerformance;

    // Use this for initialization
    void Start () {
        brain = GetComponent<IABrain>();
        hardMemory = new List<IAInformation>();
        softMemory = new List<IAInformation>();
    }
	
    public void RegisterMemory(IAInformation information, bool directToBrain = false)
    {
        information.timeReceived = Time.time;
        if (directToBrain) information.completion = 1f;
        if (information.completion >= 1f || Random.Range(0f, 1f) <= information.completion)
        {
            if (information.IsRememberNeeded() || directToBrain)
            {
                ReplaceInformation(information);
                hardMemory.Insert(0, information);
            }
            if (!directToBrain) brain.ProcessInformation(information);
        }

        if (hardMemory.Count > maxMemory)
        {
            hardMemory.Remove(hardMemory.FindLast(c => true));
        }
    }

    public void ReplaceInformation(IAInformation information)
    {
        switch (information.type)
        {
            case IAInformation.InformationType.SEARCHZONE:
                hardMemory.RemoveAll(c => c.type == IAInformation.InformationType.SEARCHZONE && c.parameters == information.parameters);
                break;
            case IAInformation.InformationType.ZONECLEAR:
                hardMemory.RemoveAll(c => (c.type == IAInformation.InformationType.SEARCHZONE || c.type == IAInformation.InformationType.ZONECLEAR) && c.parameters == information.parameters);
                break;
        }
    }

    public List<IAInformation> AccessSoftMemory()
    {
        if (lastSoftMemoryAccess < Time.time)
        {
            lastSoftMemoryAccess = Time.time;
            softMemory.Clear();
            ComputeSoftMemory();
        }
        return softMemory;
    }

    public void ComputeSoftMemory()
    {
        float time = Time.time;
        foreach (IAInformation info in hardMemory)
        {
            if (Random.Range(0f, 0.99f) <= memoryPerformance.Evaluate(time - info.timeReceived) * info.completion)
            {
                softMemory.Add(info);
            }
        }
    }

    public bool HasOrderOfTypes(params IAInformation.InformationType[] types)
    {
        return GetOrderOfTypes(types) != null;
    }

    public IAInformation GetOrderOfTypes(params IAInformation.InformationType[] types)
    {
        for(int i=0; i<hardMemory.Count; i++)
        {
            for(int j=0; j<types.Length; j++)
            {
                if (types[j] == hardMemory[i].type) return hardMemory[i];
            }
        }

        return null;
    }

    public List<IAInformation> GetOrdersOfType(IAInformation.InformationType type)
    {
        return hardMemory.FindAll(c => c.toDo && c.type == type);
    }

    public void CleanOrders()
    {
        hardMemory.RemoveAll(c => c.toDo);
    }

    public void CleanOrders(IAInformation.InformationType type, string parameters = "")
    {
        hardMemory.RemoveAll(c => c.toDo && c.type == type && (string.IsNullOrEmpty(parameters) || c.parameters == parameters));
    }

}
