using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleManager : MonoBehaviour {


    public ParticleSystem MajorParticleSys;

    public ParticleSystem ParticleSys1;
    public ParticleSystem ParticleSys2;


    private void Awake()
    {
        Quiet.FingerSnap += ActivateParticleSystem;
        Quiet.Yelling += YellingParticle;
        Quiet.NoSound += NoParticle;
    }



    public void ActivateParticleSystem()
    {
        ParticleSys1.GetComponent<ParticleSystem>().Play();
        ParticleSys2.GetComponent<ParticleSystem>().Play();
    }

    public void YellingParticle()
    {
        var emit = MajorParticleSys.emission;
        emit.rateOverTime = 5f;
    }


    public void NoParticle()
    {
        var emit = MajorParticleSys.emission;
        emit.rateOverTime = 0f;
    }

}
