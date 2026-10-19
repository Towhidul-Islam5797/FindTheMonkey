using System;
using GoogleMobileAds.Api;
using UnityEngine;

[DisallowMultipleComponent]
public class AdsManager : MonoBehaviour
{
    public static AdsManager Instance
    {
        get;
        private set;
    }

    // =======================================================
    // AD UNIT IDS
    // =======================================================

    [Header("Interstitial Ad Unit IDs")]
    [Tooltip("Android interstitial ad unit ID. The default is Google's public test ID.")]
    [SerializeField]
    private string androidInterstitialId = "ca-app-pub-3940256099942544/1033173712";

    [Tooltip("iOS interstitial ad unit ID. The default is Google's public test ID.")]
    [SerializeField]
    private string iosInterstitialId = "ca-app-pub-3940256099942544/4411468910";

    // =======================================================
    // FREQUENCY
    // =======================================================

    [Header("Frequency")]
    [Tooltip("An interstitial is shown on every Nth tap of Next. Counted per session only.")]
    [SerializeField, Min(1)]
    private int levelsBetweenInterstitials = 3;

    // =======================================================
    // DEBUG
    // =======================================================

    [Header("Debug")]
    [SerializeField]
    private bool showDebugLogs = true;

    private InterstitialAd interstitialAd;
    private bool isLoading;
    private bool isShowing;
    private int nextLevelCount;
    private Action pendingOnFinished;

    // =======================================================
    // UNITY
    // =======================================================

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
#if UNITY_ANDROID || UNITY_IOS
        MobileAds.RaiseAdEventsOnUnityMainThread = true;

        RequestConfiguration requestConfiguration =
            new RequestConfiguration
            {
                TagForChildDirectedTreatment =
                    TagForChildDirectedTreatment.True,
                MaxAdContentRating =
                    MaxAdContentRating.G
            };

        MobileAds.SetRequestConfiguration(
            requestConfiguration
        );

        /*
         * Blocks the retry in ShowInterstitialThen
         * until the first load has started.
         */
        isLoading = true;

        MobileAds.Initialize(initStatus =>
        {
            DebugLog("Mobile Ads initialized.");

            LoadInterstitial();
        });
#endif
    }

    // =======================================================
    // AD UNIT ID
    // =======================================================

    private string GetInterstitialId()
    {
#if UNITY_ANDROID
        return androidInterstitialId;
#elif UNITY_IOS
        return iosInterstitialId;
#else
        return string.Empty;
#endif
    }

    // =======================================================
    // LOAD
    // =======================================================

    private void LoadInterstitial()
    {
        string adUnitId = GetInterstitialId();

        if (string.IsNullOrEmpty(adUnitId))
        {
            isLoading = false;
            return;
        }

        isLoading = true;

        InterstitialAd.Load(
            adUnitId,
            new AdRequest(),
            (InterstitialAd ad, LoadAdError error) =>
            {
                isLoading = false;

                if (error != null ||
                    ad == null)
                {
                    DebugLog(
                        "Interstitial failed to load: " +
                        error
                    );

                    return;
                }

                if (interstitialAd != null)
                {
                    interstitialAd.Destroy();
                }

                interstitialAd = ad;

                interstitialAd.OnAdFullScreenContentClosed +=
                    HandleAdClosed;

                interstitialAd.OnAdFullScreenContentFailed +=
                    HandleAdFailedToShow;

                DebugLog("Interstitial loaded.");
            }
        );
    }

    // =======================================================
    // SHOW
    // =======================================================

    public void ShowInterstitialThen(
        Action onFinished
    )
    {
        /*
         * A second call while an ad is open is ignored.
         * The first call's callback still runs once.
         */
        if (isShowing)
            return;

        if (interstitialAd == null &&
            !isLoading)
        {
            LoadInterstitial();
        }

        nextLevelCount++;

        if (nextLevelCount < levelsBetweenInterstitials ||
            interstitialAd == null ||
            !interstitialAd.CanShowAd())
        {
            onFinished?.Invoke();
            return;
        }

        nextLevelCount = 0;
        isShowing = true;
        pendingOnFinished = onFinished;

        DebugLog("Showing interstitial.");

        interstitialAd.Show();
    }

    private void HandleAdClosed()
    {
        DebugLog("Interstitial closed.");

        FinishAd();
    }

    private void HandleAdFailedToShow(
        AdError error
    )
    {
        DebugLog(
            "Interstitial failed to show: " +
            error
        );

        FinishAd();
    }

    private void FinishAd()
    {
        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
            interstitialAd = null;
        }

        isShowing = false;

        LoadInterstitial();

        Action onFinished = pendingOnFinished;
        pendingOnFinished = null;

        onFinished?.Invoke();
    }

    // =======================================================
    // DEBUG
    // =======================================================

    private void DebugLog(string message)
    {
        if (!showDebugLogs)
            return;

        Debug.Log(
            "[AdsManager] " +
            message,
            this
        );
    }
}
