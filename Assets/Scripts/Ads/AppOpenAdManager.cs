using System;
using UnityEngine;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;

[AddComponentMenu("GoogleMobileAds/AppOpenAdController")]
public class AppOpenAdController : MonoBehaviour
{
#if UNITY_ANDROID
    [SerializeField] private string _adUnitId = "ca-app-pub-3940256099942544/9257395921"; // ✅ Use this for testing


#elif UNITY_IPHONE
    private string _adUnitId = "ca-app-pub-3940256099942544/5575463023";
#else
    private string _adUnitId = "unused";
#endif

    private AppOpenAd _appOpenAd;
    private DateTime _expireTime;

    /// <summary>
    /// Returns true if the ad is loaded and has not expired.
    /// </summary>
    public bool IsAdAvailable
    {
        get
        {
            return _appOpenAd != null &&
                   _appOpenAd.CanShowAd() &&
                   DateTime.Now < _expireTime;
        }
    }

    private void Awake()
    {
        // Listen for app foreground/background state changes
        AppStateEventNotifier.AppStateChanged += OnAppStateChanged;
    }

    private void Start()
    {
        MobileAds.Initialize((InitializationStatus initStatus) =>
        {
            Debug.Log("Google Mobile Ads initialized.");
            LoadAppOpenAd();
        });
    }

    private void OnDestroy()
    {
        // Unsubscribe from app state changes when destroyed
        AppStateEventNotifier.AppStateChanged -= OnAppStateChanged;
    }

    /// <summary>
    /// Called when the app state changes (background ↔ foreground).
    /// </summary>
    private void OnAppStateChanged(AppState state)
    {
        Debug.Log("App State changed to: " + state);

        if (state == AppState.Foreground && IsAdAvailable)
        {
            ShowAppOpenAd();
        }
    }

    /// <summary>
    /// Loads a new App Open Ad.
    /// </summary>
    public void LoadAppOpenAd()
    {
        if (_appOpenAd != null)
        {
            _appOpenAd.Destroy();
            _appOpenAd = null;
        }

        Debug.Log("Loading App Open Ad...");

        var adRequest = new AdRequest();

        AppOpenAd.Load(_adUnitId, adRequest, (AppOpenAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogError("Failed to load App Open Ad: " + error);
                return;
            }

            Debug.Log("App Open Ad loaded.");
            _appOpenAd = ad;
            _expireTime = DateTime.Now + TimeSpan.FromHours(4); // Set expiration

            RegisterEventHandlers(ad);
        });
    }

    /// <summary>
    /// Shows the App Open Ad if it's valid.
    /// </summary>
    public void ShowAppOpenAd()
    {
        if (_appOpenAd != null && _appOpenAd.CanShowAd())
        {
            Debug.Log("Showing App Open Ad.");
            _appOpenAd.Show();
        }
        else
        {
            Debug.LogError("App Open Ad not ready or expired.");
        }
    }

    /// <summary>
    /// Registers event handlers for ad lifecycle events.
    /// </summary>
    private void RegisterEventHandlers(AppOpenAd ad)
    {
        ad.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log($"App Open Ad paid: {adValue.Value} {adValue.CurrencyCode}");
        };

        ad.OnAdImpressionRecorded += () =>
        {
            Debug.Log("App Open Ad recorded impression.");
        };

        ad.OnAdClicked += () =>
        {
            Debug.Log("App Open Ad clicked.");
        };

        ad.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("App Open Ad full screen opened.");
        };

        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("App Open Ad closed.");
            _appOpenAd = null;
            LoadAppOpenAd(); // Preload next one
        };

        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("App Open Ad failed to show: " + error);
            _appOpenAd = null;
        };
    }
}
