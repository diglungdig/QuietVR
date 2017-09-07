using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Quiet : MonoBehaviour {

    public static Quiet Instance;
    public List<GameObject> BasicObject;
    public List<GameObject> AdvancedObject;
    public float Timer = 0f;
    public RuntimeAnimatorController animator;
    public Transform SummonTransform;

    public ObjectManager PrimitveShapeManager;
    public ObjectManager SpecialObjectManager;
    public ObjectManager SurpriseManager;


    private bool Lock = false;
    private bool InnerLock = false;

    [Space, Header("SerializeField Privates")]
    [SerializeField]
    private float KlakValue = 0f;
    [SerializeField]
    private float TimeStamp = 0f;
    [SerializeField]
    private float ThreholdToStartCounting = 0.8f;
    [SerializeField]
    private float AudioGapOffset = 0f;
    [SerializeField]
    private float AudioGapOffsetCached = 0.2f;
    [SerializeField]
    private float CountTimer = 0f;

    //Events
    public delegate void QuietEvent();
    public static event QuietEvent FingerSnap;
    public static event QuietEvent Yelled;

    public static event QuietEvent Yelling;
    public static event QuietEvent NoSound;
    

    void Awake()
    {
        if (Instance == null)
            Instance = this;
    }
    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }



    public float KlakHandle {

        set
        {
            KlakValue = value;
        }

        get
        {
            return KlakValue;
        }

     }

    public bool LockHandle
    {
        set
        {
            Lock = value;
        }
        get
        {
            return Lock;
        }   
    }

    public float TimeStampHandle
    {
        set
        {
            TimeStampHandle = value;
        }
        get
        {
            return TimeStamp;
        }
    }


	// Use this for initialization
	void Start () {
        AudioGapOffset = AudioGapOffsetCached;
	}


    private void Update()
    {
        if (Input.GetKey("escape"))
            Application.Quit();
    }

    // Update is called once per frame
    void FixedUpdate () {

        if (!Lock)
        {
            TestOnAudioInput();
        }
	}

    void TestOnAudioInput()
    {
        //Do something for the Count Timer and Klak

        if(KlakValue > ThreholdToStartCounting)
        {
            //Reach Threhold
            if (InnerLock == false)
            {
                TimeStamp = Time.time;
            }
            InnerLock = true;
            Debug.Log("0.8 ----------->");


            AudioGapOffset = AudioGapOffsetCached;

            //Yelling
            Yelling();
        }
        else
        {

            //Fall off
            if(TimeStamp > 0f && AudioGapOffset <= 0f)
            {
                float duration = Time.time - TimeStamp;

                Lock = true;
                //Here is where it decides its following behaviours
                if (duration < Timer)
                {
                    FingerSnap();
                    Summon(0f);
                    Debug.Log("This is basic");
                }
                else
                {
                    Yelled();
                    Summon(duration);
                    Debug.Log("WOWO THIS IS ADVANDED");
                }

                TimeStamp = 0;
                AudioGapOffset = AudioGapOffsetCached;
                //release the lock 
                InnerLock = false;
            }
            else if(TimeStamp > 0f)
            {
                //There is tiny time gap between the audio input
                AudioGapOffset -= Time.fixedDeltaTime;
            }
            NoSound();
        }
    }
    /// <summary>
    /// Currently QuietVR only summons based on the length of audio input. Future implemenetations require deeper audio analysization to generate more sophisticate and robust things
    /// </summary>
    /// <param name="duration"></param>
    void Summon(float duration)
    {
        GameObject o;
        if(duration <= 0f)
        {
            o = Instantiate(BasicObject[Random.Range(0, BasicObject.Count)], SummonTransform.position + new Vector3(0f,0f,-1f), Quaternion.identity);
            o.AddComponent<Animator>().runtimeAnimatorController = animator;

            if (o.GetComponent<MeshRenderer>() == null)
            {
                o.GetComponentInChildren<MeshRenderer>().material.color = new Color(Random.value, Random.value, Random.value);
            }
            else
            {
                o.GetComponent<MeshRenderer>().material.color = new Color(Random.value, Random.value, Random.value);
            }
            StartCoroutine(SendObjectAndReleaseLock(o, PrimitveShapeManager));
        }
        else if(duration >= 30f)
        {
            SurpriseManager.ObjectRebirth();
            Lock = false;
        }
        else
        {
            o = Instantiate(AdvancedObject[Random.Range(0, AdvancedObject.Count)], SummonTransform.position + new Vector3(0f, 0f, -1f), Quaternion.identity);
            o.AddComponent<Animator>().runtimeAnimatorController = animator;
            StartCoroutine(SendObjectAndReleaseLock(o, SpecialObjectManager));
        }

    }

    IEnumerator SendObjectAndReleaseLock(GameObject o, ObjectManager manager)
    {
        //Let player take a good look at the object
        yield return new WaitForSeconds(7f);

        Destroy(o.GetComponent<Animator>());

        //Send Object to Object Manager
        manager.ReceiveObject(o);

        //Give it a wait
        yield return new WaitForSeconds(3f);
        
        //Release the lock
        Lock = false;
    }

}
