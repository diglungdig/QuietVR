using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class VoiceRipple : Ripple {

    public static VoiceRipple Instance;

    public ParticleSystem MajorParticleSys;
    public ParticleSystem ParticleSys1;
    public ParticleSystem ParticleSys2;

    public ParticleSystem LoadingParticle;

    public GameObject RippleCircleBorder;

    private ParticleSystem.EmissionModule em;

    [SerializeField]
    private Vector3 Offset;
    [SerializeField]
    private float KlakValue = 0f;
    [SerializeField]
    private Vector3 LocalScale;
    [SerializeField]
    private Material mat;
    [SerializeField]
    private float BloomEmissionValue1 = 0.08f;
    [SerializeField]
    private float BloomEmissionValue2 = 0.53f;
    [SerializeField]
    private Image image;
    [SerializeField]
    private Quiet quiet;

    private bool RippleLock = false;

    void Awake()
    {
        if (Instance == null)
            Instance = this;

        Quiet.PopsOut += ActivateParticleSystem;
        Quiet.PopsOut += ScaleToZero;

        Quiet.Yelling += YellingParticle;
        Quiet.Mute += NoParticle;

    }
    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
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


    // Use this for initialization
    public override void Start()
    {
        //set the size and the color
        LocalScale = transform.localScale;
        transform.localScale = new Vector3(0f, 0f, 0f);
        GetComponent<Image>().color = new Color(StartColor.r, StartColor.g, StartColor.b, 1f);
        mat = GetComponent<Image>().material;

        if(Quiet.Instance == null)
        {
            Debug.LogError("This is null");
        }

        em = LoadingParticle.emission;
    }

    // Update is called once per frame
    public override void Update()
    {

        RippleCircleBorder.SetActive(quiet.ProcessingLockHandle);

        if (!quiet.LockHandle && quiet.Mode == Quiet.QuietMode.RandomMode)
        {
            KlakValue = quiet.KlakHandle;
            //lerp the scale and the color
            transform.localScale = Vector3.Lerp(transform.localScale, KlakValue * LocalScale, Time.deltaTime * Speed) + Offset;
        }
        else if(quiet.LockHandle && quiet.Mode == Quiet.QuietMode.SearchMode && quiet.ProcessingLockHandle)
        {
            KlakValue = quiet.KlakHandle;
            //lerp the scale and the color
            transform.localScale = Vector3.Lerp(transform.localScale, KlakValue * LocalScale, Time.deltaTime * Speed) + Offset;
         }

        if (quiet.TimeStampHandle > 0f)
        {
            image.color = Color.green;
        }
        else
        {
            image.color = Color.white;
        }
    }

    public void SetCircleBorderActive(bool active)
    {
        if (RippleCircleBorder != null)
        {
            RippleCircleBorder.SetActive(active);
        }
    }

    public void ScaleToZero()
    {
        transform.localScale = Vector3.zero;
    }


    public void OverBloom()
    {
        Debug.Log("Over Bloom is activated");
        StartCoroutine(OverBloomRipple());
    }

    IEnumerator OverBloomRipple()
    {
        float temp = mat.GetFloat("_EmissionGain");
        while (temp < BloomEmissionValue2 )
        {
            temp += Time.deltaTime * 2f;
            mat.SetFloat("_EmissionGain", temp);
            yield return null;
        }
        yield return new WaitForSeconds(5f);
        while (temp > BloomEmissionValue1)
        {
            temp -= Time.deltaTime * 2.5f;
            mat.SetFloat("_EmissionGain", temp);
            yield return null;
        }
        mat.SetFloat("_EmissionGain", BloomEmissionValue1);
        transform.localScale = Vector3.zero;
    }

}
