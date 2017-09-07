using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class VoiceRipple : Ripple {
    

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

    private bool RippleLock = false;

    private void Awake()
    {
        Quiet.Yelled += OverBloom;
        Quiet.FingerSnap += ScaleToZero;
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
    }

    // Update is called once per frame
    public override void Update()
    {
        if (!Quiet.Instance.LockHandle)
        {
            KlakValue = Quiet.Instance.KlakHandle;
            //lerp the scale and the color
            transform.localScale = Vector3.Lerp(transform.localScale, KlakValue * LocalScale, Time.deltaTime * Speed) + Offset;
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
        yield return new WaitForSeconds(2f);
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
