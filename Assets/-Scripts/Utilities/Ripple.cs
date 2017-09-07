using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Ripple : MonoBehaviour
{
    /// <summary>
    /// Give an item ripple effect. Based on https://www.assetstore.unity3d.com/en/#!/content/46243
    /// </summary>

    //parameters of the Ripple
    public float Speed;
    public float MaxSize;
    public Color StartColor;
    public Color EndColor;

    // Use this for initialization
    public virtual void Start()
    {
        //set the size and the color
        transform.localScale = new Vector3(0f, 0f, 0f);
        GetComponent<Image>().color = new Color(StartColor.r, StartColor.g, StartColor.b, 1f);
    }

    // Update is called once per frame
    public virtual void Update()
    {
        //lerp the scale and the color
        transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(MaxSize, MaxSize, MaxSize), Time.deltaTime * Speed);
        GetComponent<Image>().color = Color.Lerp(GetComponent<Image>().color, new Color(EndColor.r, EndColor.g, EndColor.b, 0f), Time.deltaTime * Speed);
        transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);

        //destroy at the end of life
        if (transform.localScale.x >= MaxSize * 0.995f)
        {
            Destroy(gameObject);
        }
    }
}

