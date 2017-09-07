using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class BorderColorChange : MonoBehaviour {

    private Image image;

	// Use this for initialization
	void Start () {
        image = GetComponent<Image>();
	}
	
	// Update is called once per frame
	void Update () {
		if(Quiet.Instance.TimeStampHandle > 0f)
        {
            image.color = Color.green;
        }
        else
        {
            image.color = Color.white;
        }
	}
}
