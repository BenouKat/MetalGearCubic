using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitManager : MonoBehaviour {

    public static UnitManager instance;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        charArray = "AZERTYUIOPQSDFGHJKLMWXCVBN".ToCharArray();

        intruderLayer = LayerMask.NameToLayer("Player");
        friendLayer = LayerMask.NameToLayer("Enemy");
        wallLayer = LayerMask.NameToLayer("Unmovable");
    }

    Transform currentOfficer;
    List<string> unitIDs = new List<string>();
    char[] charArray;

    [HideInInspector]
    public int intruderLayer;
    [HideInInspector]
    public int friendLayer;
    [HideInInspector]
    public int wallLayer;

    public void SetOfficer(Transform officer)
    {
        currentOfficer = officer;
    }

    public Transform GetCurrentOfficer()
    {
        return currentOfficer;
    }

    public List<string> GetAllUnits()
    {
        return unitIDs;
    }

    public string GetNewUnitID(string prefix)
    {
        string newUnit = prefix + Random.Range(0, 1000).ToString("000") + "-" + charArray[Random.Range(0, charArray.Length)];
        if(unitIDs.Contains(newUnit))
        {
            return GetNewUnitID(prefix);
        }
        unitIDs.Add(newUnit);
        return newUnit;
    }

    public void RemoveUnitID(string unit)
    {
        unitIDs.Remove(unit);
    }
}
