using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
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

    [Header("Rewarded Ad Unit IDs")]
    [Tooltip("Android rewarded ad unit ID. The default is Google's public test ID.")]
    [SerializeField]
    private string androidRewardedId = "ca-app-pub-3940256099942544/5224354917";

    [Tooltip("iOS rewarded ad unit ID. The default is Google's public test ID.")]
    [SerializeField]
    private string iosRewardedId = "ca-app-pub-3940256099942544/1712485313";

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

    private RewardedAd rewardedAd;
    private bool isRewardedLoading;
    private bool isRewardedShowing;
    private bool rewardEarned;
    private Action<bool> pendingOnRewardedFinished;

    public bool IsRewardedReady =>
        rewardedAd != null &&
        !isRewardedShowing &&
        rewardedAd.CanShowAd();

    public event Action OnRewardedReadyChanged;

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
        RequestConfiguration requestConfiguration =
            new RequestConfiguration
            {
                AgeRestrictedTreatment =
                    AgeRestrictedTreatment.Child,
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
        isRewardedLoading = true;

        MobileAds.Initialize(initStatus =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                DebugLog("Mobile Ads initialized.");

                LoadInterstitial();
                LoadRewarded();
            });
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

    private string GetRewardedId()
    {
#if UNITY_ANDROID
        return androidRewardedId;
#elif UNITY_IOS
        return iosRewardedId;
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
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
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
                });
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

        AudioListener.pause = true;

        interstitialAd.Show();
    }

    private void HandleAdClosed()
    {
        MobileAdsEventExecutor.ExecuteInUpdate(() =>
        {
            DebugLog("Interstitial closed.");

            FinishAd();
        });
    }

    private void HandleAdFailedToShow(
        AdError error
    )
    {
        MobileAdsEventExecutor.ExecuteInUpdate(() =>
        {
            DebugLog(
                "Interstitial failed to show: " +
                error
            );

            FinishAd();
        });
    }

    private void FinishAd()
    {
        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
            interstitialAd = null;
        }

        isShowing = false;

        AudioListener.pause = false;

        LoadInterstitial();

        Action onFinished = pendingOnFinished;
        pendingOnFinished = null;

        onFinished?.Invoke();
    }

    // =======================================================
    // REWARDED
    // =======================================================

    private void LoadRewarded()
    {
        string adUnitId = GetRewardedId();

        if (string.IsNullOrEmpty(adUnitId))
        {
            isRewardedLoading = false;
            return;
        }

        isRewardedLoading = true;

        RewardedAd.Load(
            adUnitId,
            new AdRequest(),
            (RewardedAd ad, LoadAdError error) =>
            {
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    isRewardedLoading = false;

                    if (error != null ||
                        ad == null)
                    {
                        DebugLog(
                            "Rewarded failed to load: " +
                            error
                        );

                        return;
                    }

                    if (rewardedAd != null)
                    {
                        rewardedAd.Destroy();
                    }

                    rewardedAd = ad;

                    rewardedAd.OnAdFullScreenContentClosed +=
                        HandleRewardedClosed;

                    rewardedAd.OnAdFullScreenContentFailed +=
                        HandleRewardedFailedToShow;

                    DebugLog("Rewarded loaded.");

                    OnRewardedReadyChanged?.Invoke();
                });
            }
        );
    }

    public void ShowRewardedThen(
        Action<bool> onFinished
    )
    {
        /*
         * A second call while a rewarded ad is open is ignored.
         * The first call's callback still runs once.
         */
        if (isRewardedShowing)
            return;

        if (!IsRewardedReady)
        {
            if (rewardedAd == null &&
                !isRewardedLoading)
            {
                LoadRewarded();
            }

            onFinished?.Invoke(false);
            return;
        }

        isRewardedShowing = true;
        rewardEarned = false;
        pendingOnRewardedFinished = onFinished;

        DebugLog("Showing rewarded.");

        OnRewardedReadyChanged?.Invoke();

        AudioListener.pause = true;

        rewardedAd.Show(reward =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                rewardEarned = true;

                DebugLog("Reward earned.");
            });
        });
    }

    private void HandleRewardedClosed()
    {
        MobileAdsEventExecutor.ExecuteInUpdate(() =>
        {
            DebugLog("Rewarded closed.");

            FinishRewarded();
        });
    }

    private void HandleRewardedFailedToShow(
        AdError error
    )
    {
        MobileAdsEventExecutor.ExecuteInUpdate(() =>
        {
            DebugLog(
                "Rewarded failed to show: " +
                error
            );

            FinishRewarded();
        });
    }

    private void FinishRewarded()
    {
        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }

        isRewardedShowing = false;

        AudioListener.pause = false;

        LoadRewarded();

        OnRewardedReadyChanged?.Invoke();

        Action<bool> onFinished = pendingOnRewardedFinished;
        pendingOnRewardedFinished = null;

        onFinished?.Invoke(rewardEarned);
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
