using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomObject : MonoBehaviour {
    [SerializeField]
    public bool UseRotate = true;

    public Transform RotateAround;

    
    public float Speed = 2f;

    [SerializeField]
    private bool UseSelfRotate = true;

    public float lifecycle = 0f;
    public float FadeSpeed = 0.3f;
    public bool faceCamera = false;


    private Color color;
    private Color FadeInColor;
    private Color FadeOutColor;
    [SerializeField]
    private Renderer ren;

    private void Awake()
    {
        if (ren == null)
        {
            if (GetComponent<MeshRenderer>() == null)
            {
                ren = GetComponentInChildren<MeshRenderer>();
            }
            else
            {
                ren = GetComponent<MeshRenderer>();
            }
        }
    }


    void SetRotate(bool value, Transform t)
    {
        UseRotate = value;
        RotateAround = t;
    }
    void SetSelfRotate(bool value)
    {
        UseSelfRotate = value;
    }

    public virtual void SetLifeCycle(float value, Vector3 pos, Transform origin)
    {
        RotateAround = origin;
        transform.position = pos;
        transform.Rotate(Random.value* new Vector3(30, 40, 100));
        Speed = Random.Range(2f, 4f);
        lifecycle = value;
        color = ren.material.color;
        color = new Color(color.r, color.g, color.b, 0f);
        FadeInColor = new Color(color.r, color.g, color.b, 0.8f);
        FadeOutColor = new Color(color.r, color.g, color.b, 0f);
        ren.material.SetColor("_Color", color);
        StartCoroutine(FadeIn());
    }


    public IEnumerator FadeIn()
    {
        color = ren.material.color;
        while (ren.material.color.a < 0.9f)
        {
            color = Color.Lerp(color, FadeInColor, FadeSpeed * Time.deltaTime);

            ren.material.SetColor("_Color",color);

            yield return null;
        }
    }
    public IEnumerator FadeOut()
    {
        color = ren.material.color;


        while (ren.material.color.a > 0.1f)
        {
            color = new Color(color.r, color.g, color.b, color.a - FadeSpeed * Time.deltaTime);
            ren.material.SetColor("_Color", color);

            yield return null;
        }
        gameObject.SetActive(false);
        transform.parent.GetComponent<ObjectManager>().ObjectRebirth();
    }

    public virtual void FixedUpdate()
    {


        if (UseRotate && RotateAround != null)
        {
            transform.RotateAround(RotateAround.position, Vector3.up, -1*Speed * Time.deltaTime);

        }

        if (UseSelfRotate)
        {
            transform.Rotate(transform.up * 10f * Time.deltaTime);
        }

        lifecycle -= Time.deltaTime;

        if(lifecycle <= 0)
        {
            StartCoroutine(FadeOut());
            lifecycle = 10000;
        }

        if (faceCamera)
        {
            transform.LookAt(Camera.main.transform);
        }
        
    }

}



