using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundFXManager : MonoBehaviour {

    public AudioClip clip1;
    public AudioClip clip2;


	// Use this for initialization
	void Start () {
        Quiet.FingerSnap += PlayAudio1;
        Quiet.Yelled += PlayAudio2;
    }

    private void PlayAudio1()
    {
        GetComponent<AudioSource>().clip = clip1;
        GetComponent<AudioSource>().Play();
    }
    private void PlayAudio2()
    {
        GetComponent<AudioSource>().clip = clip2;
        GetComponent<AudioSource>().Play();
    }

}
