using UnityEngine;
using System.Collections;

public enum axisTofloat { x, y, z };

public class Floater : MonoBehaviour
{

    public float amplitude;          //Set in Inspector 
    public float speed;                  //Set in Inspector 

    public axisTofloat floatAroundAxis;

    private float tempValx;
    private float tempValy;
    private float tempValz;



    private Vector3 tempPos;
    private Camera main;


    void Start()
    {
        tempValx = transform.position.x;
        tempValy = transform.position.y;
        tempValz = transform.position.z;

        main = Camera.main;
    }

    void Update()
    {
        if (floatAroundAxis == axisTofloat.y)
        {
            transform.position = new Vector3(transform.position.x, tempValy + amplitude * Mathf.Sin(speed * Time.time), transform.position.z);
        }
        else if (floatAroundAxis == axisTofloat.x)
        {
            transform.position = new Vector3(tempValx + amplitude * Mathf.Sin(speed * Time.time), transform.position.y, transform.position.z);
        }
        else if (floatAroundAxis == axisTofloat.z)
        {
            transform.position = new Vector3(transform.position.x, transform.position.y, tempValz + amplitude * Mathf.Sin(speed * Time.time));
        }


        transform.rotation = Quaternion.LookRotation(transform.position - main.transform.position);
    }
}
