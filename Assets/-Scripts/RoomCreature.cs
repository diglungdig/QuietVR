using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomCreature : RoomObject {

    private Vector3 destionation = Vector3.zero;

    public override void SetLifeCycle(float value, Vector3 pos, Transform origin)
    {
        destionation = Random.insideUnitSphere * 200;

        transform.LookAt(destionation);

        base.SetLifeCycle(value, pos, origin);
    }


    public override void FixedUpdate()
    {
        lifecycle -= Time.deltaTime;
        if (lifecycle <= 0)
        {
            StartCoroutine(FadeOut());
            lifecycle = 10000;
        }

        float step = FadeSpeed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, destionation, step);
    }
}
