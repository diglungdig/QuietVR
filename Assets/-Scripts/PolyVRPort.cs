using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Abstact parent class for QuietVR
/// </summary>
public abstract class PolyVRPort : MonoBehaviour {

    //Singleton class
    public static PolyVRPort Instance;

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

    /// <summary>
    /// This function handles gameobject sent from Poly Api
    /// </summary>
    /// <param name="o"> GameObject acquired from PolyMananger </param>
    public abstract void ReceivePoly(GameObject o);
    /// <summary>
    /// This function is called when Poly fails to retrieve a desired object
    /// </summary>
    public abstract void ErrorFallback(ItemErrorType ErrorType);
    /// <summary>
    /// This function applies colliders to Gameobject
    /// </summary>
    /// <param name="o"></param>
    public abstract void ColliderProcessing(GameObject o);


    public enum ItemComplexity
    {
        SIMPLE = 0,
        MEDIUM = 1,
        COMPLEX = 2
    }
    public enum ItemErrorType
    {
        Non_Existance_In_Database = 0,
        Fail_To_Import = 1,
    }
}
