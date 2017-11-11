using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomObject : MonoBehaviour
{
    [SerializeField]
    public bool UseRotate = true;
    public Transform RotateAround;
    public float Speed = 2f;

    [SerializeField]
    private bool UseSelfRotate = true;

    public float lifecycle;
    public float FadeSpeed = 0.3f;
    public bool faceCamera = false;
    public RoomobjectType type = RoomobjectType.basic;

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
        if (type == RoomobjectType.advanced)
        {
            transform.localScale = Vector3.zero;
        }
        transform.position = pos;
        transform.Rotate(Random.value * new Vector3(30, 40, 100));
        Speed = Random.Range(2f, 4f);
        lifecycle = value;
        StartCoroutine(In());
    }


    public IEnumerator In()
    {
        gameObject.SetActive(true);

        if (type == RoomobjectType.basic)
        {
            for (float t = 0; t < 1; t += Time.deltaTime / 2)
            {
                transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
                yield return null;
            }
            transform.localScale = Vector3.one;

        }
        else if (type == RoomobjectType.advanced)
        {
            for (float t = 0; t < 1; t += Time.deltaTime / 2)
            {
                transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
                yield return null;
            }
            transform.localScale = Vector3.one;

        }
    }
    public IEnumerator Out()
    {
        if (type == RoomobjectType.basic)
        {
            for (float t = 0; t < 1; t += Time.deltaTime / 1)
            {
                transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);
                yield return null;
            }
            transform.localScale = Vector3.zero;
        }
        else if (type == RoomobjectType.advanced)
        {
            for (float t = 0; t < 1; t += Time.deltaTime / 1)
            {
                transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);
                yield return null;
            }
            transform.localScale = Vector3.zero;
        }

        gameObject.SetActive(false);
    }


    public virtual void FixedUpdate()
    {
        if (UseRotate && RotateAround != null)
        {
            transform.RotateAround(RotateAround.position, Vector3.up, -1 * Speed * Time.deltaTime);
        }

        if (UseSelfRotate)
        {
            transform.Rotate(transform.up * 10f * Time.deltaTime);
        }

        lifecycle -= Time.deltaTime;

        if (lifecycle <= 0)
        {
            StartCoroutine(Out());
        }

        if (faceCamera)
        {
            transform.LookAt(Camera.main.transform);
        }

    }

}

public enum RoomobjectType
{
    basic,
    advanced
}



