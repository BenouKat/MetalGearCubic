using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BloodManager : MonoBehaviour {

    public static BloodManager instance;
    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
    }

    struct BloodTimer
    {
        public Material bloodMaterial;
        public string colorID;
        public Color startColor;
        public float timeStart;
    }
    List<BloodTimer> bloodTimes = new List<BloodTimer>();
    Material lastBloodMaterial;

    public float timeBodyDisappear;
    public float timeBloodDisappear;
    float timeBeforeStartDisappear;
    public Color dryBloodColor;
    public float timeBloodDry;

    //Just object pooling
    BloodTimer bt;
    // Update is called once per frame
    void Update () {

        //If there's some blood materials running
		if(bloodTimes.Count > 0)
        {
            for(int i=0; i<bloodTimes.Count; i++)
            {
                //We set the color of the shared material to dry over time
                bt = bloodTimes[i];
                bt.bloodMaterial.SetColor(bt.colorID, Color.Lerp(bt.startColor, dryBloodColor, (Time.time - bt.timeStart) / timeBloodDry));
                if(Time.time >= bt.timeStart + timeBloodDry)
                {
                    //When it's done, we remove it from the list
                    bloodTimes.Remove(bt);
                    i--;
                }
            }
        }
	}

    public void setTimeBeforeDisappear(float time)
    {
        timeBeforeStartDisappear = time;
    }

    //Blood material is a material cube of red color that change color over time
    public void registerNewBloodMaterial(Material bloodMaterial, string colorID = "_Color")
    {
        lastBloodMaterial = bloodMaterial;

        BloodTimer timer;
        timer.timeStart = Time.time;
        timer.colorID = colorID;
        timer.startColor = lastBloodMaterial.GetColor(colorID);
        timer.bloodMaterial = lastBloodMaterial;

        bloodTimes.Add(timer);
    }

    //Blood object is blood cubes
    public void registerNewBloodObject(BloodObject bloodObject)
    {
        bloodObject.Init(timeBloodDisappear, timeBeforeStartDisappear, lastBloodMaterial);
    }

    //Body part is not blood cubes, they last shorter than the blood
    public void registerNewBodyPartObject(BloodObject bloodObject)
    {
        bloodObject.Init(timeBodyDisappear, timeBeforeStartDisappear, null);
    }
}
