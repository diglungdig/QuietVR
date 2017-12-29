using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundFXManager : MonoBehaviour {

    public static SoundFXManager Instance;
    public AudioClip clip1;
    public AudioSource BackgroundMusic;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
    }
    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

	// Use this for initialization
	void Start () {
        Quiet.PopsOut += PopsOutExplosion;
    }

    private void PopsOutExplosion()
    {
        GetComponent<AudioSource>().clip = clip1;
        GetComponent<AudioSource>().Play();
    }

    public void PauseMusic()
    {
        BackgroundMusic.Pause();
    }
    public void ResumeMusic()
    {
        BackgroundMusic.UnPause();
    }
}
