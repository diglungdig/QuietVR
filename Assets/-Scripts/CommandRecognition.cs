using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Text;


using UnityEngine.Windows.Speech;


/// <summary>
/// Command Recognition is KeywordRecognizer(Command) + DictationRecognizer(Command Content)
/// </summary>
public class CommandRecognition : MonoBehaviour {

    [SerializeField]
    private float AutoSilenceTimer = 2f;
    [SerializeField]
    private string[] m_Keywords;

    //Static variables
    [SerializeField]
    public static string HypoString = "";
    [SerializeField]
    public static string RecogString = "";
    [SerializeField]
    public static bool VoiceRecognitionGotResult = false;

    //privates
    private static DictationRecognizer m_DictationRecognizer;
    private static KeywordRecognizer m_KeywordRecognizer;

    public static Boolean KeywordRecognitionRunning
    {
        get
        {
            return m_KeywordRecognizer.IsRunning;
        }
    }

    //Delegate events
    public delegate void CommandEvent(string RecognitionText);
    public static event CommandEvent SccucessTrigger;
    public static event CommandEvent FailureTrigger;
    public static event CommandEvent TimeOutTrigger;
    public static event CommandEvent CommandTrigger;

    private void OnDisable()
    {
        m_DictationRecognizer.Dispose();
        m_KeywordRecognizer.Dispose();
    }

    void Start () {
        //Set up both Recognizers
        //First, Dictation
        m_DictationRecognizer = new DictationRecognizer();

        m_DictationRecognizer.AutoSilenceTimeoutSeconds = AutoSilenceTimer;
        m_DictationRecognizer.InitialSilenceTimeoutSeconds = AutoSilenceTimer;

        m_DictationRecognizer.DictationResult += (text, confidence) =>
        {
            Debug.LogFormat("Dictation result: {0}", text);
            RecogString = text;

            VoiceRecognitionGotResult = true;
        };

        m_DictationRecognizer.DictationHypothesis += (text) =>
        {
            Debug.LogFormat("Dictation hypothesis: {0}", text);
            HypoString = text;
        };

        m_DictationRecognizer.DictationComplete += (completionCause) =>
        {
            if (completionCause != DictationCompletionCause.Complete)
            {
                if (completionCause == DictationCompletionCause.TimeoutExceeded)
                {
                    Debug.LogWarningFormat("Completed unsuccessfully: {0}.", completionCause);

                    TimeOutTrigger(RecogString);
                }
            }
            else
            {
                //Subscription to the poly event
                if (VoiceRecognitionGotResult)
                {
                    Debug.Log("Got result");
                    SccucessTrigger(RecogString);
                }
                else
                {
                    Debug.Log("DOESNT Get result");
                    FailureTrigger(RecogString);
                }
                Debug.Log("[Completed successfully!!]");
            }
            StartCoroutine(OnRestartCommandRecognition());
        };

        m_DictationRecognizer.DictationError += (error, hresult) =>
        {
            Debug.LogWarningFormat("Dictation error: {0}; HResult = {1}.", error, hresult);
        };

        //Keyword
        m_KeywordRecognizer = new KeywordRecognizer(m_Keywords);
        m_KeywordRecognizer.OnPhraseRecognized += OnPhraseRecognized;

    }

    public static void StartKeywordListening()
    {
        VoiceRecognitionGotResult = false;
        m_KeywordRecognizer.Start();
    }

    private void OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendFormat("{0} ({1}){2}", args.text, args.confidence, Environment.NewLine);
        builder.AppendFormat("\tTimestamp: {0}{1}", args.phraseStartTime, Environment.NewLine);
        builder.AppendFormat("\tDuration: {0} seconds{1}", args.phraseDuration.TotalSeconds, Environment.NewLine);
        Debug.Log(builder.ToString());

        CommandTrigger(args.text);
        PhraseRecognitionSystem.Shutdown();
        m_DictationRecognizer.Start();
        StartCoroutine(OnStallingDictation(3f));
    }

    IEnumerator OnStallingDictation(float timer)
    {
        yield return new WaitForSeconds(timer);
        m_DictationRecognizer.Stop();
    }

    IEnumerator OnRestartCommandRecognition()
    {
        while (m_DictationRecognizer.Status == SpeechSystemStatus.Running)
        {
            Debug.Log("SHITZ STILL RUNNING");
            yield return null;
        }

        Debug.Log("Not Running anymore");
        m_KeywordRecognizer.Dispose();
        m_KeywordRecognizer = new KeywordRecognizer(m_Keywords);
        m_KeywordRecognizer.OnPhraseRecognized += OnPhraseRecognized;
    }

    #region DEBUG

    private void Update()
    {
        switch (m_DictationRecognizer.Status)
        {
            case SpeechSystemStatus.Stopped:
                Debug.LogWarning("STOPPED");
                break;
            case SpeechSystemStatus.Running:
                Debug.LogWarning("RUNNING");
                break;
            case SpeechSystemStatus.Failed:
                Debug.LogWarning("FAILED");
                break;
        }
        if (m_KeywordRecognizer.IsRunning)
        {
            Debug.Log("KWR is running");
        }
        else
        {
            Debug.Log("KWR has stopped");
        }
    }
#endregion
}
