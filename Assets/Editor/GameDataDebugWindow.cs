#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

public class GameDataDebugWindow : EditorWindow
{
    private const string TotalScoreKey = "TotalScore";
    private const string TotalCoinsKey = "TotalCoins";
    private const string TotalBananasKey = "TotalBananas";

    private const string UnlockedLevelKey = "UnlockedLevel";
    private const string LevelStarsKeyPrefix = "LevelStars_";
    private const string LevelCompletedKeyPrefix = "LevelCompleted_";

    private int scoreValue;
    private int coinValue;
    private int bananaValue;
    private int unlockedLevelValue = 1;
    private int maximumLevelCount = 100;

    [MenuItem("Tools/Game Data Control")]
    public static void OpenWindow()
    {
        GetWindow<GameDataDebugWindow>("Game Data Control");
    }

    private void OnEnable()
    {
        LoadCurrentValues();
    }

    private void OnGUI()
    {
        GUILayout.Space(10);

        EditorGUILayout.LabelField(
            "Saved Game Data",
            EditorStyles.boldLabel
        );

        EditorGUILayout.HelpBox(
            "Stop Play Mode before changing or resetting saved data.",
            MessageType.Warning
        );

        GUILayout.Space(5);

        scoreValue = EditorGUILayout.IntField(
            "Total Score",
            scoreValue
        );

        coinValue = EditorGUILayout.IntField(
            "Total Coins",
            coinValue
        );

        bananaValue = EditorGUILayout.IntField(
            "Total Bananas",
            bananaValue
        );

        unlockedLevelValue = EditorGUILayout.IntField(
            "Unlocked Level",
            unlockedLevelValue
        );

        maximumLevelCount = EditorGUILayout.IntField(
            "Maximum Level Count",
            maximumLevelCount
        );

        scoreValue = Mathf.Max(0, scoreValue);
        coinValue = Mathf.Max(0, coinValue);
        bananaValue = Mathf.Max(0, bananaValue);
        unlockedLevelValue = Mathf.Max(1, unlockedLevelValue);
        maximumLevelCount = Mathf.Max(1, maximumLevelCount);

        GUILayout.Space(15);

        if (GUILayout.Button("Load Current Saved Values", GUILayout.Height(32)))
        {
            LoadCurrentValues();
        }

        if (GUILayout.Button("Apply Manual Values", GUILayout.Height(38)))
        {
            ApplyManualValues();
        }

        GUILayout.Space(15);

        EditorGUILayout.LabelField(
            "Reset Options",
            EditorStyles.boldLabel
        );

        if (GUILayout.Button(
                "Reset Score, Coins And Bananas",
                GUILayout.Height(35)
            ))
        {
            ResetCurrency();
        }

        if (GUILayout.Button(
                "Reset Stars And Progression",
                GUILayout.Height(35)
            ))
        {
            ResetProgression();
        }

        if (GUILayout.Button(
                "Reset First Completion Rewards",
                GUILayout.Height(35)
            ))
        {
            ResetFirstCompletionRewards();
        }

        GUILayout.Space(10);

        GUI.backgroundColor = new Color(1f, 0.45f, 0.45f);

        if (GUILayout.Button(
                "RESET EVERYTHING",
                GUILayout.Height(45)
            ))
        {
            ResetEverythingWithConfirmation();
        }

        GUI.backgroundColor = Color.white;

        GUILayout.Space(15);

        DrawCurrentSavedValues();
    }

    private void LoadCurrentValues()
    {
        scoreValue = PlayerPrefs.GetInt(TotalScoreKey, 0);
        coinValue = PlayerPrefs.GetInt(TotalCoinsKey, 0);
        bananaValue = PlayerPrefs.GetInt(TotalBananasKey, 0);
        unlockedLevelValue =
            PlayerPrefs.GetInt(UnlockedLevelKey, 1);

        Repaint();

        Debug.Log(
            "[GameDataDebugWindow] Loaded saved values. " +
            $"Score={scoreValue}, " +
            $"Coins={coinValue}, " +
            $"Bananas={bananaValue}, " +
            $"UnlockedLevel={unlockedLevelValue}."
        );
    }

    private void ApplyManualValues()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError(
                "[GameDataDebugWindow] Stop Play Mode before applying saved values."
            );

            return;
        }

        PlayerPrefs.SetInt(
            TotalScoreKey,
            Mathf.Max(0, scoreValue)
        );

        PlayerPrefs.SetInt(
            TotalCoinsKey,
            Mathf.Max(0, coinValue)
        );

        PlayerPrefs.SetInt(
            TotalBananasKey,
            Mathf.Max(0, bananaValue)
        );

        PlayerPrefs.SetInt(
            UnlockedLevelKey,
            Mathf.Max(1, unlockedLevelValue)
        );

        PlayerPrefs.Save();

        Debug.Log(
            "[GameDataDebugWindow] Manual values saved. " +
            $"Score={scoreValue}, " +
            $"Coins={coinValue}, " +
            $"Bananas={bananaValue}, " +
            $"UnlockedLevel={unlockedLevelValue}."
        );
    }

    private void ResetCurrency()
    {
        if (!CanModifyData())
            return;

        PlayerPrefs.SetInt(TotalScoreKey, 0);
        PlayerPrefs.SetInt(TotalCoinsKey, 0);
        PlayerPrefs.SetInt(TotalBananasKey, 0);
        PlayerPrefs.Save();

        scoreValue = 0;
        coinValue = 0;
        bananaValue = 0;

        Debug.Log(
            "[GameDataDebugWindow] Score, coins and bananas reset to zero."
        );

        Repaint();
    }

    private void ResetProgression()
    {
        if (!CanModifyData())
            return;

        PlayerPrefs.SetInt(UnlockedLevelKey, 1);

        for (int level = 1; level <= maximumLevelCount; level++)
        {
            PlayerPrefs.DeleteKey(
                LevelStarsKeyPrefix + level
            );

            PlayerPrefs.DeleteKey(
                LevelCompletedKeyPrefix + level
            );
        }

        PlayerPrefs.Save();

        unlockedLevelValue = 1;

        Debug.Log(
            "[GameDataDebugWindow] Stars and progression reset. " +
            "Only Level 1 is unlocked."
        );

        Repaint();
    }

    private void ResetFirstCompletionRewards()
    {
        if (!CanModifyData())
            return;

        for (int level = 1; level <= maximumLevelCount; level++)
        {
            PlayerPrefs.DeleteKey(
                LevelCompletedKeyPrefix + level
            );
        }

        PlayerPrefs.Save();

        Debug.Log(
            "[GameDataDebugWindow] First-completion reward records reset."
        );
    }

    private void ResetEverythingWithConfirmation()
    {
        if (!CanModifyData())
            return;

        bool confirmed = EditorUtility.DisplayDialog(
            "Reset Everything",
            "This will delete score, coins, bananas, stars, " +
            "completion rewards and unlocked levels.",
            "Reset Everything",
            "Cancel"
        );

        if (!confirmed)
            return;

        ResetEverything();
    }

    private void ResetEverything()
    {
        /*
         * DeleteAll is suitable for development.
         * It guarantees that old test data is removed.
         */
        PlayerPrefs.DeleteAll();

        PlayerPrefs.SetInt(TotalScoreKey, 0);
        PlayerPrefs.SetInt(TotalCoinsKey, 0);
        PlayerPrefs.SetInt(TotalBananasKey, 0);
        PlayerPrefs.SetInt(UnlockedLevelKey, 1);

        PlayerPrefs.Save();

        scoreValue = 0;
        coinValue = 0;
        bananaValue = 0;
        unlockedLevelValue = 1;

        Debug.Log(
            "[GameDataDebugWindow] EVERYTHING RESET. " +
            "Score=0, Coins=0, Bananas=0, UnlockedLevel=1."
        );

        Repaint();
    }

    private bool CanModifyData()
    {
        if (!EditorApplication.isPlaying)
            return true;

        Debug.LogError(
            "[GameDataDebugWindow] Stop Play Mode before resetting saved data."
        );

        EditorUtility.DisplayDialog(
            "Stop Play Mode",
            "Stop Play Mode before modifying saved game data.",
            "OK"
        );

        return false;
    }

    private void DrawCurrentSavedValues()
    {
        EditorGUILayout.LabelField(
            "PlayerPrefs Verification",
            EditorStyles.boldLabel
        );

        EditorGUILayout.LabelField(
            "Saved Score",
            PlayerPrefs.GetInt(TotalScoreKey, 0).ToString()
        );

        EditorGUILayout.LabelField(
            "Saved Coins",
            PlayerPrefs.GetInt(TotalCoinsKey, 0).ToString()
        );

        EditorGUILayout.LabelField(
            "Saved Bananas",
            PlayerPrefs.GetInt(TotalBananasKey, 0).ToString()
        );

        EditorGUILayout.LabelField(
            "Unlocked Level",
            PlayerPrefs.GetInt(UnlockedLevelKey, 1).ToString()
        );
    }
}

#endif