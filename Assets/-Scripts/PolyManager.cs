using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using PolyToolkit;

public class PolyManager : MonoBehaviour
{
    public static PolyManager Instance;
    public Text DescriptionText;

    //This flag is set to true when model is being imported from Poly
    [SerializeField, Header("Importing Model")]
    private bool IsImporting = false;

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

    public void FetchBlockObjectBySearch(string word)
    {
        Debug.Log("Eneter FetchBlockObjectBySearch(string word)");
        DescriptionText.text = "Requesting...";
        PolyListAssetsRequest request = new PolyListAssetsRequest();
        request.keywords = word;
        request.curated = true;
        request.orderBy = PolyOrderBy.BEST;
        request.maxComplexity = PolyMaxComplexityFilter.MEDIUM;
        //request.formatFilter = PolyFormatFilter.BLOCKS;

        PolyApi.ListAssets(request, MyListAssetCallback);
    }

    public void FetchTheFirstBlockObjectBySearch(string word)
    {
        Debug.Log("Eneter FetchTheFirstBlockObjectBySearch(string word)");
        DescriptionText.text = "Requesting...";
        PolyListAssetsRequest request = new PolyListAssetsRequest();
        request.keywords = word;
        request.curated = true;
        request.orderBy = PolyOrderBy.NEWEST;
        request.formatFilter = PolyFormatFilter.BLOCKS;

        PolyApi.ListAssets(request, MyListAssetCallback);
    }

    private void MyListAssetCallback(PolyStatusOr<PolyListAssetsResult> result)
    {
        if (!result.Ok)
        {
            Debug.LogError("Failed to get featured assets. :( Reason: " + result.Status);
            DescriptionText.text = "ERROR: " + result.Status;
            return;
        }
        Debug.Log("Successfully got featured assets!");
        DescriptionText.text = "Importing...";

        PolyImportOptions options = PolyImportOptions.Default();

        options.rescalingMode = PolyImportOptions.RescalingMode.FIT;
        options.desiredSize = 1.0f;
        options.recenter = true;

        PolyAsset assetInUse = new PolyAsset();

        //Randomly select one

        if (result.Value.assets.Count > 0)
        {
            int seed = Random.Range(0, result.Value.assets.Count);
            IsImporting = true;
            PolyApi.Import(result.Value.assets[seed], options, ImportAssetCallback);
            assetInUse = result.Value.assets[seed];
            DescriptionText.text = PolyApi.GenerateAttributions(includeStatic: false, runtimeAssets: new List<PolyAsset>() { assetInUse });
        }
        else
        {
            //No Result
            VoiceRipple.Instance.ScaleToZero();
            PolyVRPort.Instance.ErrorFallback(PolyVRPort.ItemErrorType.Non_Existance_In_Database);
            
            DescriptionText.text = "Such thing doesn't exist yet";
        }
    }

    // Callback invoked when an asset has just been imported.
    private void ImportAssetCallback(PolyAsset asset, PolyStatusOr<PolyImportResult> result)
    {
        IsImporting = false;
        if (!result.Ok)
        {
            Debug.LogWarning("Failed to import asset. :( Reason: " + result.Status);
            PolyVRPort.Instance.ErrorFallback(PolyVRPort.ItemErrorType.Fail_To_Import);
            return;
        }
        PolyVRPort.Instance.ReceivePoly(result.Value.gameObject);
    }

    #region RandomMode
    public void RandomlyFetchBlockObject(PolyVRPort.ItemComplexity complexity)
    {
        Debug.Log("Eneter RandomlyFetchBlockObject(Quiet.QuietItemComplexity complexity)");
        DescriptionText.text = "Requesting...";

        //Setting up request
        PolyListAssetsRequest request = new PolyListAssetsRequest();
        request.curated = true;
        request.orderBy = PolyOrderBy.BEST;
        request.formatFilter = PolyFormatFilter.BLOCKS;
        switch (complexity)
        {
            case PolyVRPort.ItemComplexity.SIMPLE:
                request.maxComplexity = PolyMaxComplexityFilter.SIMPLE;
                break;
            case PolyVRPort.ItemComplexity.MEDIUM:
                request.maxComplexity = PolyMaxComplexityFilter.MEDIUM;
                break;
            case PolyVRPort.ItemComplexity.COMPLEX:
                request.maxComplexity = PolyMaxComplexityFilter.COMPLEX;
                break;
        }
        PolyApi.ListAssets(request, MyListAssetCallbackRandomMode);
    }
    private void MyListAssetCallbackRandomMode(PolyStatusOr<PolyListAssetsResult> result)
    {
        if (!result.Ok)
        {
            Debug.LogError("Failed to get featured assets. :( Reason: " + result.Status);
            DescriptionText.text = "ERROR: " + result.Status;
            return;
        }
        Debug.Log("Successfully got featured assets!");
        DescriptionText.text = "Importing...";

        PolyImportOptions options = PolyImportOptions.Default();

        options.rescalingMode = PolyImportOptions.RescalingMode.FIT;
        options.desiredSize = 1.0f;
        options.recenter = true;

        PolyAsset assetInUse = new PolyAsset();

        //Randomly select one
        int seed = Random.Range(0, result.Value.assets.Count);

        IsImporting = true;
        PolyApi.Import(result.Value.assets[seed], options, ImportAssetCallback);
        assetInUse = result.Value.assets[seed];

        DescriptionText.text = PolyApi.GenerateAttributions(includeStatic: false, runtimeAssets: new List<PolyAsset>() { assetInUse });
    }
    #endregion



}
