using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectManager : MonoBehaviour {

    public ManagerType Type;
    public List<GameObject> Prefab;
    public float DeathRatePerSec;
    public float DeathRateOffset;
    [Range(0f, 1f)]
    public float ChancePerMin = 0;


    [SerializeField, Range(20, 60)]
    private float FarPlane;
    [SerializeField, Range(5, 20)]
    private float NearPlane;
    [SerializeField]
    private int PoolSize;

    private float wRadius;
    private float rRadius;
    private List<GameObject> ObjectPool;
    private float Timer = 0f;



    void Start () {
        ObjectPool = new List<GameObject>();

        if(Type == ManagerType.Massive)
        {
            NearPlane += 60f;
            FarPlane += 80f;
        }
        wRadius = (FarPlane - NearPlane) * 0.5f;
        rRadius = wRadius + NearPlane;

        if (Type == ManagerType.Generic && Prefab.Count == 1)
        {
            for (int i = 0; i < PoolSize; i++)
            {
                GameObject obj = (GameObject)Instantiate(Prefab[0], transform);
                if (obj.GetComponent<RoomObject>() == null)
                {
                    obj.AddComponent<RoomObject>();
                    obj.GetComponent<RoomObject>().SetLifeCycle(Random.Range(DeathRatePerSec, DeathRatePerSec + DeathRateOffset), GetRandomPositionInTorus(rRadius, wRadius), transform);
                }
                else
                {
                    obj.GetComponent<RoomObject>().SetLifeCycle(Random.Range(DeathRatePerSec, DeathRatePerSec + DeathRateOffset), GetRandomPositionInTorus(rRadius, wRadius), transform);
                }
                obj.SetActive(true);
                ObjectPool.Add(obj);
            }
        }
        else if(Prefab.Count > 1){

            switch (Type)
            {
                case ManagerType.Massive:
                    int PoolSizeDived = PoolSize / Prefab.Count;
                    for (int i = 0; i < Prefab.Count; i++)
                    {
                        for (int j = 0; j < PoolSizeDived; j++)
                        {
                            GameObject obj = (GameObject)Instantiate(Prefab[i], transform);
                            if (obj.GetComponent<RoomObject>() == null)
                            {
                                obj.AddComponent<RoomObject>();
                                obj.GetComponent<RoomObject>().SetLifeCycle(Random.Range(DeathRatePerSec, DeathRatePerSec + DeathRateOffset), GetRandomPositionInTorus(rRadius, wRadius), transform);
                            }
                            else
                            {
                                obj.GetComponent<RoomObject>().SetLifeCycle(Random.Range(DeathRatePerSec, DeathRatePerSec + DeathRateOffset), GetRandomPositionInTorus(rRadius, wRadius), transform);
                            }
                            obj.SetActive(true);
                            ObjectPool.Add(obj);
                        }
                    }
                    break;
                case ManagerType.Special:
                    for (int i = 0; i < Prefab.Count; i++)
                    {
                        GameObject obj = (GameObject)Instantiate(Prefab[i], transform);
                        obj.SetActive(false);
                        ObjectPool.Add(obj);
                    }
                    GameObject temp = ObjectPool[Random.Range(0, ObjectPool.Count)];
                    temp.SetActive(true);
                    if (temp.GetComponent<RoomObject>() == null)
                    {
                        temp.AddComponent<RoomObject>();
                        temp.GetComponent<RoomObject>().SetLifeCycle(Random.Range(DeathRatePerSec, DeathRatePerSec + DeathRateOffset), GetRandomPositionInTorus(rRadius, wRadius), transform);
                    }
                    else
                    {
                        temp.GetComponent<RoomObject>().SetLifeCycle(Random.Range(DeathRatePerSec, DeathRatePerSec + DeathRateOffset), GetRandomPositionInTorus(rRadius, wRadius), transform);
                    }
                    break;
                case ManagerType.Surprise:
                    for (int i = 0; i < Prefab.Count; i++)
                    {
                        GameObject obj = (GameObject)Instantiate(Prefab[i], transform);
                        obj.SetActive(false);
                        ObjectPool.Add(obj);
                    }
                    break;
                default:
                    break;
            }
        }
        else
        {
            Debug.LogWarning("NO PREFAB ATTACHED!");
        }
    }

    private void Update()
    {
        Timer += Time.deltaTime;
        if (Type == ManagerType.Surprise && Timer >= 60f)
        {
            Timer = 0f;
            if(Random.value <= ChancePerMin)
            {
                ObjectRebirth();
            }
        }

    }


    public void ReceiveObject(GameObject o)
    {
        RoomObject ro = o.AddComponent<RoomObject>();
        ro.Speed = 3f;
        ro.FadeSpeed = 2f;
        o.transform.SetParent(transform);

        if(ObjectPool.Count < PoolSize)
        {
            ObjectPool.Add(o);
            
            ObjectRebirth();
        }
        else
        {
            if(ObjectPool.Count == PoolSize)
            {
                GameObject MisfortunateOne = ObjectPooling();



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
                ObjectRebirth();
            }
            else
            {
                // > never gonna happen
                Debug.LogError("This is never gonna happen: ObjectPool.Count > PoolSize");
            }
        }
    }


    public void ObjectRebirth()
    {
        StartCoroutine(Rebirth());
    }

    private IEnumerator Rebirth()
    {
        yield return new WaitForSeconds(1.5f);
        GameObject o = ObjectPooling();
        o.SetActive(true);
        if (o.GetComponent<RoomObject>() == null)
        {
            o.AddComponent<RoomObject>();
            o.GetComponent<RoomObject>().SetLifeCycle(Random.Range(DeathRatePerSec, DeathRatePerSec + DeathRateOffset), GetRandomPositionInTorus(rRadius, wRadius), transform);
        }
        else
        {
            o.GetComponent<RoomObject>().SetLifeCycle(Random.Range(DeathRatePerSec, DeathRatePerSec + DeathRateOffset), GetRandomPositionInTorus(rRadius, wRadius), transform);
        }
    }

    Vector3 GetRandomPositionInTorus(float ringRadius, float wallRadius)
    {
        // get a random angle around the ring
        float rndAngle = Random.value * 6.28f; // use radians, saves converting degrees to radians

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
        if (Prefab.Count == 1 && Type == ManagerType.Generic)
        {
            for (int i = 0; i < ObjectPool.Count; i++)
            {
                if (!ObjectPool[i].activeInHierarchy)
                {
                    return ObjectPool[i];
                }
            }

        }
        else if(Type == ManagerType.Surprise)
        {
            GameObject o = ObjectPool[Random.Range(0, ObjectPool.Count)];
            return o;
        }
        else if(Type == ManagerType.Massive)
        {
            for (int i = 0; i < ObjectPool.Count; i++)
            {
                if (!ObjectPool[i].activeInHierarchy)
                {
                    return ObjectPool[i];
                }
            }
        }
        else if(Type == ManagerType.Special)
        {
            GameObject o = ObjectPool[Random.Range(0, ObjectPool.Count)];
            return o;

        }
        GameObject p = ObjectPool[Random.Range(0, ObjectPool.Count)];
        return p;
    }
}


public enum ManagerType
{
    /*
     1.Generic: Prefab.Count = 1, Generate a lot of the prefab at the same time, used by primitive shapes in the scene
     2.Speical: Prefab.Count > 1, Geneate only one object per life cycle. （This is meant for web assetbundle 3D models.）
     3.Massive: Prefab.Count > 1, Generate a lot of the prefabs at the same time
     4.Surprise: Prefab.Count > 1, Geneate one per lifecycle by chance per min. This serves as a surprise to the player. Usually used on animal/creatures that have animation on them.
    */
    Massive, Generic, Special, Surprise

}
