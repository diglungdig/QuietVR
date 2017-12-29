using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.Windows.Speech;

/// <summary>
/// Quiet class connects voice responsive interface(like Klak and WinSpeech) to Google's Poly Api
/// </summary>
public class Quiet : PolyVRPort {

    //The Mode Quiet is running on
    //1. Search means searching 3D asset based on voice recognition
    //2. Random mode is the OG mode of QuietVR. Randomly getting stuff
    // Search mode by default

    public QuietMode Mode = QuietMode.SearchMode;

    //Some local low poly 3D objects to generate when online fetching fails
    public List<GameObject> BasicObject;

    [Space, Header("Transform Properties")]
    public Transform SummonTransform;
    public Transform Origin;

    [Space, Header("Object Managers")]
    public ObjectManager GenericShapeManager;
    public ObjectManager SpecialObjectManager;
    public ObjectManager SurpriseManager;

    [Space, Header("Cosmetics")]
    public GameObject VoiceRippleGameObject;
    public Text CountdownText;
    public Text ModeIndicator;
    public Text SwitchModeText;
    public RuntimeAnimatorController animator;
    public Color SkyboxColor1;
    public Color SkyboxColor2;
    public Color SkyboxQuietColor;

    public Text InstructionText;
    public Text HypoText;

    [Space, Header("------SerializeField Privates------")]
    [Header("Random Mode Variables")]
    [SerializeField]
    private float Timer = 0f;
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
    private float CachedTimer = 0f;

    [SerializeField]
    private bool PlayerFacingCamera = false;

    //Two flags used for disabling voice input
    //This lock is set to true when the voice input is completed
    [SerializeField, Header("Set to true when Quiet is running")]
    private bool Lock = false;
    //This lock is set to true when player is yelling(RandomMode) and set to true when voice recognition gets result(SearchMode)
    [SerializeField, Header("Set to true when player is yelling(RandomMode) and set to true when voice recognition gets proper result(SearchMode)")]
    private bool ProcessingLock = false;

    //Events
    public delegate void QuietEvent();
    public static event QuietEvent PopsOut;
    public static event QuietEvent Yelled;
    public static event QuietEvent Yelling;
    public static event QuietEvent Mute;


    #region Monobehavior Functions
    // Use this for initialization
    void Start()
    {
        //Random mode setups
        AudioGapOffset = AudioGapOffsetCached;
        CachedTimer = CountTimer;

        //Search mode setups
        CommandRecognition.CommandTrigger += CommandRecogitionGotCommand;
        CommandRecognition.SccucessTrigger += CommandRecognitionSucceeded;
        CommandRecognition.FailureTrigger += CommandRecogitionFailed;
        CommandRecognition.TimeOutTrigger += CommandRecogitionTimeOut;

        //Set Skybox Color based on mode
        if (Mode == QuietMode.SearchMode)
        {
            RenderSettings.skybox.SetColor("_Tint", SkyboxColor2);
        }
        else
        {
            RenderSettings.skybox.SetColor("_Tint", SkyboxColor1);
        }

        Mute();
    }

    // FixedUpdate function used for listening and intializing voice responsive Api(Klak and WinSpeech in this case)
    void FixedUpdate()
    {
        //Check if player is facing at the camera
        PlayerFacingCamera = FacingCamera(0.05F);

        if (Lock)
        {
            if (Mode == QuietMode.SearchMode && !CommandRecognition.VoiceRecognitionGotResult)
            {
                HypoText.text = CommandRecognition.HypoString;
            }
        }
        else
        {
            if (PlayerFacingCamera)
            {
                if (Mode == QuietMode.SearchMode)
                {
                    if (!CommandRecognition.KeywordRecognitionRunning)
                    {
                        SetInstructionText("Say \"Quiet\"! ");
                        VoiceRippleGameObject.SetActive(false);
                        CommandRecognition.StartKeywordListening();
                    }
                }
                else if (Mode == QuietMode.RandomMode)
                {
                    SetInstructionText("Keep Yelling...");
                    VoiceRippleGameObject.SetActive(true);
                    TestOnAudioInputWithKlak();
                }
            }
            else if (Mode == QuietMode.RandomMode)
            {
                //For random mode if the player is not facing at the camera then simply disable the ripple 
                VoiceRippleGameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Update function used for acceptting user's keyboard input like mode switching(space)
    /// </summary>
    private void Update()
    {
        //Exit the application
        if (Input.GetKey("escape"))
            Application.Quit();
        
        //Switch mode
        if (!Lock && !ProcessingLock)
        {
            SwitchModeText.enabled = true;
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (Mode == QuietMode.SearchMode)
                {
                    Mode = QuietMode.RandomMode;
                    StartCoroutine(SkyboxColorChanging(0.2f, 0.01f, SkyboxColor2, SkyboxColor1));
                }
                else
                {
                    Mode = QuietMode.SearchMode;
                    StartCoroutine(SkyboxColorChanging(0.2f, 0.01f, SkyboxColor1, SkyboxColor2));
                }
            }
        }
        else
        {
            SwitchModeText.enabled = false;
        }
    }

    //Update the text indicator of mode here
    private void LateUpdate()
    {
        if (Mode == QuietMode.SearchMode)
        {
            ModeIndicator.text = "Current Mode: [Search Mode]";
        }
        else
        {
            ModeIndicator.text = "Current Mode: [Random Mode]";
        }
    }
    #endregion

    #region Set/Get Handles
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
        get
        {
            return Lock;
        }   
    }
    public bool ProcessingLockHandle {
        get
        {
            return ProcessingLock;
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
    #endregion

    #region Override Functions from the parent class "Poly_VRPort"
    public override void ColliderProcessing(GameObject o)
    {
        MeshFilter[] MeshList = o.GetComponentsInChildren<MeshFilter>();

        for (int i = 0; i < MeshList.Length; i++)
        {
            MeshCollider mc = MeshList[i].gameObject.AddComponent<MeshCollider>();

            mc.sharedMesh = MeshList[i].mesh;
            mc.convex = true;
        }

    }

    public override void ReceivePoly(GameObject o)
    {
        o.SetActive(false);
        SearchMode_ClearHypothesisStringsAndTexts();

        //Callback
        StartCoroutine(DelayAction(2f, () =>
        {
            o.SetActive(true);
            //Particle effects and cosmetics
            o.transform.position = SummonTransform.position + new Vector3(0f, 0f, -1f);
            o.AddComponent<Animator>().runtimeAnimatorController = animator;
            o.GetComponent<Animator>().applyRootMotion = true;
            PopsOut();
            if (Mode == QuietMode.SearchMode)
            {
                SetInstructionText("Here is your " + CommandRecognition.RecogString);
                SearchMode_Cosmetics(false);
            }
            else if (Mode == QuietMode.RandomMode)
            {
                SetInstructionText("Nice Yell!");
            }

            StartCoroutine(SendObjectAndReleaseLock(o, SpecialObjectManager, 6f));
        }));
    }


    public override void ErrorFallback(ItemErrorType ErrorType)
    {
        GameObject o;
        o = Instantiate(BasicObject[UnityEngine.Random.Range(0, BasicObject.Count)], SummonTransform.position + new Vector3(0f, 0f, -1f), Quaternion.identity);
        o.AddComponent<Animator>().runtimeAnimatorController = animator;
        o.GetComponent<Animator>().applyRootMotion = true;
        if (o.GetComponent<MeshRenderer>() == null)
        {
            o.GetComponentInChildren<MeshRenderer>().material.color = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
        }
        else
        {
            o.GetComponent<MeshRenderer>().material.color = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
        }

        PopsOut();

        if (ErrorType == ItemErrorType.Non_Existance_In_Database)
        {
            SetInstructionText("No such thing exists yet");
        }

        if (Mode == QuietMode.SearchMode)
        {
            SearchMode_ClearHypothesisStringsAndTexts();
            SearchMode_Cosmetics(false);
        }

        StartCoroutine(SendObjectAndReleaseLock(o, GenericShapeManager, 4f));
    }

    IEnumerator SendObjectAndReleaseLock(GameObject o, ObjectManager manager, float Delay)
    {
        //Let player take a good look at the object
        Vector3 randomAngle = UnityEngine.Random.insideUnitSphere;
        float seconds = Delay;
        while(seconds > 0f)
        {
            o.transform.Rotate(randomAngle * 100f * Time.deltaTime);
            seconds -= Time.deltaTime;
            yield return null;
        }

        Destroy(o.GetComponent<Animator>());

        //Send Object to Object Manager
        o.transform.localScale = Vector3.one;
        manager.ReceiveObject(o);

        //Give it a wait.
        yield return new WaitForSeconds(1.5f);

        CountdownText.enabled = true;
        while (CountTimer >= 0f)
        {
            CountdownText.text = Mathf.RoundToInt(CountTimer).ToString();
            CountTimer -= Time.deltaTime;
            yield return null;
        }
        CountdownText.enabled = false;
        CountTimer = CachedTimer;

        //Release the lock
        Lock = false;
        ProcessingLock = false;
    }
#endregion

    #region Random Mode Functions
    void TestOnAudioInputWithKlak()
    {
        if (KlakValue > ThreholdToStartCounting)
        {
            //Reach Threhold
            if (ProcessingLock == false)
            {
                TimeStamp = Time.time;
            }
            ProcessingLock = true;

            AudioGapOffset = AudioGapOffsetCached;

            //Yelling
            Yelling();
        }
        else
        {
            //Fall off
            if (TimeStamp > 0f && AudioGapOffset <= 0f)
            {
                float duration = Time.time - TimeStamp;

                Lock = true;
                //Here is where it decides its following behaviours
                if (duration < Timer)
                {
                    SetInstructionText("Try yelling longer next time");
                    ErrorFallback(ItemErrorType.Fail_To_Import);
                    Debug.Log("This is basic");
                }
                else if (duration <= 10f)
                {
                    PolyManager.Instance.RandomlyFetchBlockObject(ItemComplexity.MEDIUM);
                    Debug.Log("WOWO THIS IS ADVANDED");
                }
                else
                {
                    //Over 10 secs, Summon animated objects
                    SurpriseManager.Rebirth();
                    Lock = false;
                }

                TimeStamp = 0;
                AudioGapOffset = AudioGapOffsetCached;
                //release the lock 
                ProcessingLock = false;
            }
            else if (TimeStamp > 0f)
            {
                //There is tiny time gap between the audio input
                AudioGapOffset -= Time.fixedDeltaTime;
            }
            //Turning off particles
            Mute();
        }
    }

    #endregion

    #region Search Mode Functions
    void CommandRecognitionSucceeded(string RecoString)
    {
        ProcessingLock = false;

        SetInstructionText("You want " + RecoString + "?");
        PolyManager.Instance.FetchBlockObjectBySearch(RecoString);
    }

    void CommandRecogitionFailed(string RecoString)
    {
        ProcessingLock = false;

        SetInstructionText("A bit too quick there......");
        ErrorFallback(ItemErrorType.Fail_To_Import);
    }

    void CommandRecogitionTimeOut(string RecoString)
    {
        ProcessingLock = false;

        if (CommandRecognition.VoiceRecognitionGotResult)
        {
            Debug.Log("Time out : Got Result" + RecoString);

            PolyManager.Instance.FetchBlockObjectBySearch(RecoString);
        }
        else if(CommandRecognition.HypoString.Length == 0)
        {
            SetInstructionText("You can't be that quiet.....");
            ErrorFallback(ItemErrorType.Fail_To_Import);
            Debug.Log("Time out : No Result");
        }
        Debug.Log("HYPOTSTRING" + CommandRecognition.HypoString);
    }
    void CommandRecogitionGotCommand(string RecoString)
    {
        Lock = true;
        ProcessingLock = true;
        VoiceRippleGameObject.SetActive(true);
        SetInstructionText("Good. Now Say Anything.....");
        SearchMode_Cosmetics(true);
    }

    /// <summary>
    /// Clear voice recognition hypothesis text string
    /// </summary>
    void SearchMode_ClearHypothesisStringsAndTexts()
    {
        HypoText.text = "";
        CommandRecognition.HypoString = "";
    }

    void SearchMode_Cosmetics(Boolean quiet)
    {
        //Cosmetics, animations, skybox color changing when "quiet" is said, objects stop rotating
        if (quiet)
        {
            SoundFXManager.Instance.PauseMusic();
            StartCoroutine(SkyboxColorChanging(0.5f, 0.01f, SkyboxColor2, SkyboxQuietColor));
            RoomObject[] roomobjects = FindObjectsOfType(typeof(RoomObject)) as RoomObject[];
            foreach (var item in roomobjects)
            {
                item.UseRotate = false;
            }
        }
        else
        {
            SoundFXManager.Instance.ResumeMusic();
            StartCoroutine(SkyboxColorChanging(0.5f, 0.01f, SkyboxQuietColor, SkyboxColor2));
            RoomObject[] roomobjects = FindObjectsOfType(typeof(RoomObject)) as RoomObject[];
            foreach (var item in roomobjects)
            {
                item.UseRotate = true;
            }
        }

    }

#endregion

    #region Shared Utilities in two modes
    /// <summary>
    /// Set instruction UI
    /// </summary>
    /// <param name="text"></param>
    public void SetInstructionText(string text)
    {
        InstructionText.text = text;
    }

    /// <summary>
    /// Check if the camera is facing at the origin
    /// </summary>
    /// <param name="bias"></param>
    /// <returns></returns>
    bool FacingCamera(float bias)
    {
        if (Vector3.Dot(Camera.main.transform.forward, Origin.forward) < (-1f + bias))
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Used to change Skybox color
    /// </summary>
    /// <param name="duration"></param>
    /// <param name="smoothness"></param>
    /// <param name="StartingColor"></param>
    /// <param name="DestinationColor"></param>
    /// <returns></returns>
    IEnumerator SkyboxColorChanging(float duration, float smoothness, Color StartingColor, Color DestinationColor)
    {
        float progress = 0; //This float will serve as the 3rd parameter of the lerp function.
        float increment = smoothness / duration; //The amount of change to apply.
        while (progress < 1)
        {

            RenderSettings.skybox.SetColor("_Tint", Color.Lerp(StartingColor, DestinationColor, progress));
            progress += increment;
            yield return new WaitForSeconds(smoothness);
        }
    }

    IEnumerator DelayAction(float timer, Action OnComplete)
    {
        yield return new WaitForSeconds(timer);

        OnComplete();
    }

    public enum QuietMode
    {
        SearchMode = 0,
        RandomMode = 1
    }
    #endregion
}
