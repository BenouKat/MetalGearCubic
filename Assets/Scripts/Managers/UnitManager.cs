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
    }
    
    List<string> unitIDs;
    char[] charArray;

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
