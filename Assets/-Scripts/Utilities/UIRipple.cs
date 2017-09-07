using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class UIRipple : MonoBehaviour
{


    /// <summary>
    /// Give an item ripple effect. Based on https://www.assetstore.unity3d.com/en/#!/content/46243
    /// </summary>

    public UnityEngine.Sprite ShapeSprite;

    /// <summary> 
    /// the speed at which the ripple will grow
    /// </summary>
    [Range(0.25f, 5f)]
    public float Speed = 1f;


    /// <summary> 
    /// If true the MaxSize will be set automatically
    /// </summary>
    public bool AutomaticMaxSize = true;

    /// <summary> 
    /// The Maximum Size of the Ripple
    /// </summary>
    public float MaxSize = 4f;

    /// <summary> 
    /// Start Color of Ripple
    /// </summary>
    public Color StartColor = new Color(1f, 1f, 1f, 1f);

    /// <summary> 
    /// End Color of Ripple
    /// </summary>
    public Color EndColor = new Color(1f, 1f, 1f, 1f);

    /// <summary>
    /// If true, the ripple will happen automatically and continuiously
    /// </summary>
    public bool isAutomatically = true;

    /// <summary> 
    /// If true the Ripple will start at the center of the UI Element
    /// </summary>
    public bool StartAtCenter = true;

    /// <summary>
    /// Object Transfom that needs ripple on
    /// </summary>
    public Transform thatTransform;

    /// <summary>
    /// The rate of regenerating new ripple
    /// </summary>
    [Range(0.1f, 2f)]
    public float rate;

    private float tempRate = 0f;

    void Awake()
    {
        //automatically set the MaxSize if needed
        if (AutomaticMaxSize)
        {
            RectTransform RT = gameObject.transform as RectTransform;
            MaxSize = (RT.rect.width > RT.rect.height) ? 4f * ((float)Mathf.Abs(RT.rect.width) / (float)Mathf.Abs(RT.rect.height)) : 4f * ((float)Mathf.Abs(RT.rect.height) / (float)Mathf.Abs(RT.rect.width));

            if (float.IsNaN(MaxSize))
            {
                MaxSize = (transform.localScale.x > transform.localScale.y) ? 4f * transform.localScale.x : 4f * transform.localScale.y;
            }
        }

        MaxSize = Mathf.Clamp(MaxSize, 0.5f, 1000f);
        tempRate = rate;
    }

    void Start()
    {
        transform.position = thatTransform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (isAutomatically)
        {
            if (tempRate >= 0f)
            {
                tempRate -= Time.deltaTime;
            }
            else
            {
                CreateRipple(thatTransform.position);
                tempRate = rate;
            }

        }

    }

    //this will create the Ripple
    public void CreateRipple(Vector3 Position)
    {
        //create the GameObject and add components

        GameObject ThisRipple = new GameObject();
        ThisRipple.AddComponent<Ripple>();
        ThisRipple.AddComponent<Image>();
        ThisRipple.GetComponent<Image>().sprite = ShapeSprite;
        ThisRipple.name = "Ripple";

        //set the parent
        ThisRipple.transform.SetParent(gameObject.transform);

        //set the Ripple at the correct location
        if (StartAtCenter)
        {
            ThisRipple.transform.localPosition = new Vector3(0f, 0f, 0f);
        }
        else
        { ThisRipple.transform.position = Position; }

        //set the parameters in the Ripple
        ThisRipple.GetComponent<Ripple>().Speed = Speed;
        ThisRipple.GetComponent<Ripple>().MaxSize = MaxSize;
        ThisRipple.GetComponent<Ripple>().StartColor = StartColor;
        ThisRipple.GetComponent<Ripple>().EndColor = EndColor;
    }

    public void setTransform(Transform t)
    {
        this.thatTransform = t;
        transform.position = t.position;
    }
}

