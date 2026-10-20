using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AdsSetupTool
{
    private const string SettingsAssetPath =
        "Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset";

    private const string BootstrapScenePath =
        "Assets/Scenes/System/Bootstrap.unity";

    private const string ForceResolveMenuPath =
        "Assets/External Dependency Manager/Android Resolver/Force Resolve";

    private const string TestAppIdPrefix =
        "ca-app-pub-3940256099942544~";

    private const string AndroidTestAppId =
        "ca-app-pub-3940256099942544~3347511713";

    private const string IosTestAppId =
        "ca-app-pub-3940256099942544~1458002511";

    [MenuItem("Tools/Monkey Rush/Set Up Ads")]
    public static void SetUpAds()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError(
                "[AdsSetupTool] Stop Play Mode before setting up ads."
            );

            return;
        }

        SetAppIds();

        if (!AddAdsManagerToBootstrap())
            return;

        SwitchToAndroid();

        ForceResolve();
    }

    // -------------------------------------------------------
    // STEP 1: APP IDS
    // -------------------------------------------------------

    private static void SetAppIds()
    {
        ScriptableObject settings =
            AssetDatabase.LoadAssetAtPath<ScriptableObject>(
                SettingsAssetPath
            );

        if (settings == null)
        {
            Debug.Log(
                "[AdsSetupTool] Step 1 App IDs: skipped: " +
                SettingsAssetPath + " not found."
            );

            return;
        }

        SerializedObject serializedSettings =
            new SerializedObject(settings);

        bool androidChanged = SetAppId(
            serializedSettings,
            "adMobAndroidAppId",
            AndroidTestAppId,
            "Android"
        );

        bool iosChanged = SetAppId(
            serializedSettings,
            "adMobIOSAppId",
            IosTestAppId,
            "iOS"
        );

        if (!androidChanged && !iosChanged)
            return;

        serializedSettings.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
    }

    private static bool SetAppId(
        SerializedObject serializedSettings,
        string fieldName,
        string testAppId,
        string platformName
    )
    {
        SerializedProperty property =
            serializedSettings.FindProperty(fieldName);

        if (property == null)
        {
            Debug.Log(
                $"[AdsSetupTool] Step 1 {platformName} App ID: skipped: " +
                $"field {fieldName} not found."
            );

            return false;
        }

        string currentValue = property.stringValue;

        if (currentValue == testAppId)
        {
            Debug.Log(
                $"[AdsSetupTool] Step 1 {platformName} App ID: already set."
            );

            return false;
        }

        if (!string.IsNullOrEmpty(currentValue) &&
            !currentValue.StartsWith(TestAppIdPrefix))
        {
            Debug.Log(
                $"[AdsSetupTool] Step 1 {platformName} App ID: skipped: " +
                $"holds a real ID ({currentValue}), left unchanged."
            );

            return false;
        }

        property.stringValue = testAppId;

        Debug.Log(
            $"[AdsSetupTool] Step 1 {platformName} App ID: done ({testAppId})."
        );

        return true;
    }

    // -------------------------------------------------------
    // STEP 2: ADSMANAGER IN BOOTSTRAP
    // -------------------------------------------------------

    private static bool AddAdsManagerToBootstrap()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(
                BootstrapScenePath
            ) == null)
        {
            Debug.Log(
                "[AdsSetupTool] Step 2 AdsManager: skipped: " +
                BootstrapScenePath + " not found."
            );

            return true;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.Log(
                "[AdsSetupTool] Step 2 AdsManager: stopped: " +
                "scene save was cancelled. No further steps ran."
            );

            return false;
        }

        SceneSetup[] previousSetup =
            EditorSceneManager.GetSceneManagerSetup();

        Scene bootstrapScene = EditorSceneManager.OpenScene(
            BootstrapScenePath,
            OpenSceneMode.Single
        );

        if (BootstrapHasAdsManager(bootstrapScene))
        {
            Debug.Log(
                "[AdsSetupTool] Step 2 AdsManager: already set."
            );
        }
        else
        {
            GameObject adsManagerObject =
                new GameObject("AdsManager");

            SceneManager.MoveGameObjectToScene(
                adsManagerObject,
                bootstrapScene
            );

            adsManagerObject.AddComponent<AdsManager>();

            EditorSceneManager.MarkSceneDirty(bootstrapScene);
            EditorSceneManager.SaveScene(bootstrapScene);

            Debug.Log(
                "[AdsSetupTool] Step 2 AdsManager: done " +
                "(added to Bootstrap and saved)."
            );
        }

        if (previousSetup.Length > 0)
        {
            EditorSceneManager.RestoreSceneManagerSetup(
                previousSetup
            );
        }

        return true;
    }

    private static bool BootstrapHasAdsManager(
        Scene bootstrapScene
    )
    {
        GameObject[] rootObjects =
            bootstrapScene.GetRootGameObjects();

        for (int i = 0; i < rootObjects.Length; i++)
        {
            if (rootObjects[i].GetComponentInChildren<AdsManager>(true) != null)
                return true;
        }

        return false;
    }

    // -------------------------------------------------------
    // STEP 3: ANDROID PLATFORM
    // -------------------------------------------------------

    private static void SwitchToAndroid()
    {
        if (EditorUserBuildSettings.activeBuildTarget ==
            BuildTarget.Android)
        {
            Debug.Log(
                "[AdsSetupTool] Step 3 Android platform: already set."
            );

            return;
        }

        bool switchPlatform = EditorUtility.DisplayDialog(
            "Switch to Android",
            "The active platform is not Android. Switching reimports " +
            "assets and can take several minutes.",
            "Yes, switch",
            "No"
        );

        if (!switchPlatform)
        {
            Debug.Log(
                "[AdsSetupTool] Step 3 Android platform: skipped: " +
                "not switched by the developer."
            );

            return;
        }

        bool switched =
            EditorUserBuildSettings.SwitchActiveBuildTarget(
                BuildTargetGroup.Android,
                BuildTarget.Android
            );

        Debug.Log(
            switched
                ? "[AdsSetupTool] Step 3 Android platform: done."
                : "[AdsSetupTool] Step 3 Android platform: skipped: " +
                  "switch failed. Is Android Build Support installed?"
        );
    }

    // -------------------------------------------------------
    // STEP 4: FORCE RESOLVE
    // -------------------------------------------------------

    private static void ForceResolve()
    {
        if (EditorApplication.ExecuteMenuItem(ForceResolveMenuPath))
        {
            Debug.Log(
                "[AdsSetupTool] Step 4 Force Resolve: done (started)."
            );

            return;
        }

        Debug.Log(
            "[AdsSetupTool] Step 4 Force Resolve: skipped: " +
            "Run Assets > External Dependency Manager > Android Resolver > " +
            "Force Resolve by hand."
        );
    }
}
