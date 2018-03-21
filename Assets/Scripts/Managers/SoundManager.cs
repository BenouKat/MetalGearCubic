using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour {

    public static SoundManager instance;
    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
    }
    public enum AudioType { MUSIC,
                            AMBIANT,
                            SOUND,
                            VOICE }

    public struct AudioChannel
    {
        public AudioType type;
        public AudioMixerGroup audioGroup;
    }
    [SerializeField]
    List<AudioChannel> audioChannels;

    //All sound prefab
    [SerializeField]
    List<GameObject> soundPrefab;

    //Instance the prefab at the right place and assign it to the right audio group
    public void instanceSoundPrefab(GameObject soundPrefab, Vector3 position, AudioType type)
    {
        GameObject audioInst = InstanceManager.instance.InstanceObject(InstanceManager.InstanceType.Audio, soundPrefab, position, Quaternion.identity);
        AudioSource source = audioInst.AddComponent<AudioSource>();
        source.outputAudioMixerGroup = audioChannels.Find(c => c.type == type).audioGroup;
    }

    //Play method to go fast with sound
    public void play(string sound, AudioType type)
    {
        play(sound, Vector3.zero, type);
    }

    //Same but we set the position
    public void play(string sound, Vector3 position, AudioType type)
    {
        GameObject audioPrefab = soundPrefab.Find(c => c.name == sound);
        if(audioPrefab != null)
        {
            instanceSoundPrefab(audioPrefab, position, type);
        }
        else
        {
            Debug.LogWarning("The sound " + sound + " is missing in the prefab list, not played");
        }
    }

    //Get the audio mixer group
    public AudioMixerGroup GetAMGChannel(AudioType type)
    {
        return audioChannels.Find(c => c.type == type).audioGroup;
    }
}
