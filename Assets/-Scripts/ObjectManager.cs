using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectManager : MonoBehaviour {

    public ManagerType Type;
    public List<GameObject> Prefab;
    public float DeathRate;
    public float DeathRateOffset;



    [SerializeField, Range(20, 100)]
    private float FarPlane;
    [SerializeField, Range(5, 60)]
    private float NearPlane;
    [SerializeField]
    private int PoolSize;

    private float wRadius;
    private float rRadius;
    [SerializeField, Header("Debug View")]
    private List<GameObject> ObjectPool;

    public bool Looping = false;
    [SerializeField]
    private float RebirthRate;

    void Start () {
        ObjectPool = new List<GameObject>();
        wRadius = (FarPlane - NearPlane) * 0.5f;
        rRadius = wRadius + NearPlane;
        RebirthRate = DeathRate / PoolSize;

        if (Prefab.Count >= 1)
        {
            if (Type == ManagerType.Generic)
            {
                int PoolSizeDived = PoolSize / Prefab.Count;
                for (int i = 0; i < Prefab.Count; i++)
                {
                    for (int j = 0; j < PoolSizeDived; j++)
                    {
                        GameObject obj = (GameObject)Instantiate(Prefab[i], transform);
                        if (obj.GetComponent<RoomObject>() == null)
                        {
                            RoomObject Rb = obj.AddComponent<RoomObject>();
                            Rb.type = RoomobjectType.basic;
                            Rb.SetLifeCycle(Random.Range(DeathRate, DeathRate + DeathRateOffset), GetRandomPositionInTorus(rRadius, wRadius, false), transform);
                        }
                        else
                        {
                            RoomObject Rb = obj.GetComponent<RoomObject>();
                            Rb.type = RoomobjectType.basic;
                            Rb.SetLifeCycle(Random.Range(DeathRate, DeathRate + DeathRateOffset), GetRandomPositionInTorus(rRadius, wRadius, false), transform);
                        }
                        obj.SetActive(true);
                        ObjectPool.Add(obj);
                    }
                }
            }
            else
            {
                switch (Type)
                {
                    case ManagerType.Special:
                        for (int i = 0; i < Prefab.Count; i++)
                        {
                            GameObject obj = (GameObject)Instantiate(Prefab[i], transform);
                            obj.SetActive(false);
                            ObjectPool.Add(obj);
                            if (obj.GetComponent<RoomObject>() == null)
                            {
                                RoomObject Rb = obj.AddComponent<RoomObject>();
                                Rb.type = RoomobjectType.advanced;
                            }
                            else
                            {
                                RoomObject Rb = obj.GetComponent<RoomObject>();
                                Rb.type = RoomobjectType.advanced;
                            }
                        }
                        break;
                    case ManagerType.Surprise:
                        for (int i = 0; i < Prefab.Count; i++)
                        {
                            GameObject obj = (GameObject)Instantiate(Prefab[i], transform);
                            obj.SetActive(false);
                            ObjectPool.Add(obj);
                            if (obj.GetComponent<RoomObject>() == null)
                            {
                                RoomObject Rb = obj.AddComponent<RoomObject>();
                                Rb.type = RoomobjectType.advanced;
                            }
                            else
                            {
                                RoomObject Rb = obj.GetComponent<RoomObject>();
                                Rb.type = RoomobjectType.advanced;
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
        }
        else
        {
            Debug.Log("NO PREFAB ATTACHED!");
        }
    }

    public void ReceiveObject(GameObject o)
    {
        //Prepare this roomobject before putting it in use
        RoomObject ro;
        if (o.GetComponent<RoomObject>() == null)
        {
            ro = o.AddComponent<RoomObject>();
        }
        else
        {
            ro = o.GetComponent<RoomObject>();
        }

        if(Type == ManagerType.Generic)
        {
            ro.type = RoomobjectType.basic;
        }
        else
        {
            ro.type = RoomobjectType.advanced;
        }
        ro.Speed = 3f;
        ro.FadeSpeed = 2f;
        o.transform.SetParent(transform);


        //Putting the roomobject into the object pool and putting it onto the torus around user
        if(ObjectPool.Count < PoolSize)
        {
            ObjectPool.Add(o);
            StartCoroutine(ObjectRebirth(o));
        }
        else
        {
            if(ObjectPool.Count == PoolSize)
            {
                GameObject MisfortunateOne = ObjectPool[Random.Range(0, ObjectPool.Count)];

                if (ObjectPool.Remove(MisfortunateOne))
                {
                    Debug.Log("Removed from the list");
                }
                else
                {
                    Debug.Log("It didn't remove");
                }

                Destroy(MisfortunateOne);
                ObjectPool.Add(o);
                StartCoroutine(ObjectRebirth(o));
            }
            else
            {
                // > never gonna happen
                Debug.LogError("This is never gonna happen: ObjectPool.Count > PoolSize");
            }
        }
    }



    public void Rebirth()
    {
        GameObject o = ObjectPooling();
        if (o != null)
        {
            o.SetActive(true);
            if (o.GetComponent<RoomObject>() == null)
            {
                o.AddComponent<RoomObject>();
                o.GetComponent<RoomObject>().SetLifeCycle(Random.Range(DeathRate, DeathRate + DeathRateOffset), GetRandomPositionInTorus(rRadius, wRadius, false), transform);
            }
            else
            {
                o.GetComponent<RoomObject>().SetLifeCycle(Random.Range(DeathRate, DeathRate + DeathRateOffset), GetRandomPositionInTorus(rRadius, wRadius, false), transform);
            }
        }
    }

   public IEnumerator ObjectRebirth(GameObject o)
    {
        
        yield return new WaitForSeconds(2f);
        o.SetActive(true);
        if (o.GetComponent<RoomObject>() == null)
        {
            o.AddComponent<RoomObject>();
            o.GetComponent<RoomObject>().SetLifeCycle(Random.Range(DeathRate, DeathRate + DeathRateOffset), GetRandomPositionInTorus(rRadius, wRadius, true), transform);
        }
        else
        {
            o.GetComponent<RoomObject>().SetLifeCycle(Random.Range(DeathRate, DeathRate + DeathRateOffset), GetRandomPositionInTorus(rRadius, wRadius, true), transform);
        }
    }

    Vector3 GetRandomPositionInTorus(float ringRadius, float wallRadius, bool FacingUser)
    {
        // get a random angle around the ring
        Debug.Log("called in torus");
        float rndAngle;
        if (FacingUser)
        {
            float ran = Random.Range(0.012f, 0.015f);
            Debug.Log(ran);
            rndAngle = ran * 6.28f; // use radians, saves converting degrees to radians
        }
        else
        {
            rndAngle = Random.value * 6.28f;
        }
        // determine position
        float cX = Mathf.Sin(rndAngle);
        float cZ = Mathf.Cos(rndAngle);

        Vector3 ringPos = new Vector3(cX, 0, cZ);
        ringPos *= ringRadius;

        // At any point around the center of the ring
        // a sphere of radius the same as the wallRadius will fit exactly into the torus.
        // Simply get a random point in a sphere of radius wallRadius,
        // then add that to the random center point
        Vector3 sPos = Random.insideUnitSphere * wallRadius;

        return (ringPos + sPos);
    }



    public GameObject ObjectPooling()
    {
        GameObject holder = null;
        if (Type == ManagerType.Generic)
        {
            for (int i = 0; i < ObjectPool.Count; i++)
            {
                if (!ObjectPool[i].activeInHierarchy)
                {
                    Debug.Log("someone is not active in the scene");

                    holder = ObjectPool[i];
                }
            }
        }
        else if(Type == ManagerType.Surprise)
        {
            holder = ObjectPool[Random.Range(0, ObjectPool.Count)];
        }
        else if(Type == ManagerType.Special)
        {
            for (int i = 0; i < ObjectPool.Count; i++)
            {
                if (!ObjectPool[i].activeInHierarchy)
                {
                    holder = ObjectPool[i];
                }
            }
        }
        return holder;
    }

    private void Update()
    {
        if (Looping)
        {
            if(RebirthRate >= 0)
            {
                RebirthRate -= Time.deltaTime;
            }
            else
            {
                RebirthRate = DeathRate / PoolSize;
                Rebirth();
            }
        }
    }
}


public enum ManagerType
{
    /*
     1.Generic: Prefab.Count = 1, Generate a lot of the prefab at the same time, used by primitive shapes in the scene
     2.Speical: Prefab.Count > 1, Geneate only one object per life cycle. （This is meant for web assetbundle 3D models.）
     3.Surprise: Prefab.Count > 1, Geneate one per lifecycle by chance per min. This serves as a surprise to the player. Usually used on animal/creatures that have animation on them.
    */
    Generic, Special, Surprise

}
