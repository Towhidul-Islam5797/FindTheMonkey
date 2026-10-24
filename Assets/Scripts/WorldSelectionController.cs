using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class WorldSelectionController : MonoBehaviour
{
    private const string StartSelectedLevelKey = "StartSelectedLevel";
    private const string LevelButtonPrefix = "LevelButton_";

    private const int JungleWorld = 0;
    private const int BeachWorld = 1;
    private const int SnowWorld = 2;
    private const int WorldCount = 3;

    // -------------------------------------------------------
    // PAGES
    // -------------------------------------------------------

    [Header("Pages")]
    [Tooltip("Assign MapWindow/MapPanel/MapSelectionPage.")]
    [SerializeField]
    private GameObject mapSelectionPage;

    [Tooltip("Assign MapWindow/MapPanel/LevelSelectionPage. Its LevelButton_N buttons are found automatically.")]
    [SerializeField]
    private GameObject levelSelectionPage;

    [Tooltip("Assign LevelSelectionPage/SelectedMapTitle.")]
    [SerializeField]
    private TMP_Text selectedMapTitle;

    // -------------------------------------------------------
    // BUTTONS
    // -------------------------------------------------------

    [Header("World Buttons")]
    [Tooltip("Clicks are connected by code; do not add OnClick entries.")]
    [SerializeField]
    private Button jungleMapButton;

    [SerializeField]
    private Button beachMapButton;

    [SerializeField]
    private Button snowMapButton;

    [Header("World Action Texts")]
    [Tooltip("Shows the open or locked label on each world button.")]
    [SerializeField]
    private TMP_Text jungleActionText;

    [SerializeField]
    private TMP_Text beachActionText;

    [SerializeField]
    private TMP_Text snowActionText;

    [Header("Back Buttons")]
    [Tooltip("MapSelectionPage/BackButton. Loads the main menu.")]
    [SerializeField]
    private Button mapPageBackButton;

    [Tooltip("LevelSelectionPage/BackButton. Returns to the world page.")]
    [SerializeField]
    private Button levelPageBackButton;

    // -------------------------------------------------------
    // WORLDS
    // -------------------------------------------------------

    [Header("Jungle Map")]
    [Tooltip("Must match MapLevelManager Map Id inside the Jungle scene.")]
    [SerializeField]
    private string jungleMapId = "Jungle";

    [Tooltip("Exact Jungle scene name without .unity.")]
    [SerializeField]
    private string jungleSceneName = "JungleMap";

    [Tooltip("Total Jungle levels, including locked levels.")]
    [SerializeField, Min(1)]
    private int jungleLevelCount = 5;

    [SerializeField]
    private bool jungleAvailable = true;

    [Tooltip("Optional. Element 0 = Level 1 background. Empty keeps the current button art.")]
    [SerializeField]
    private Sprite[] jungleLevelButtonBackgrounds;

    [Header("Beach Map")]
    [Tooltip("Must match MapLevelManager Map Id inside the Beach scene.")]
    [SerializeField]
    private string beachMapId = "Beach";

    [Tooltip("Exact Beach scene name without .unity.")]
    [SerializeField]
    private string beachSceneName = "BeachMap";

    [Tooltip("Total Beach levels, including locked levels.")]
    [SerializeField, Min(1)]
    private int beachLevelCount = 5;

    [SerializeField]
    private bool beachAvailable = true;

    [Tooltip("Optional. Element 0 = Level 1 background. Empty keeps the current button art.")]
    [SerializeField]
    private Sprite[] beachLevelButtonBackgrounds;

    [Header("Snow Map")]
    [Tooltip("Must match MapLevelManager Map Id inside the Snow scene.")]
    [SerializeField]
    private string snowMapId = "Snow";

    [Tooltip("Exact Snow scene name without .unity.")]
    [SerializeField]
    private string snowSceneName = "SnowMap";

    [Tooltip("Total Snow levels, including locked levels.")]
    [SerializeField, Min(1)]
    private int snowLevelCount = 5;

    [SerializeField]
    private bool snowAvailable = true;

    [Tooltip("Optional. Element 0 = Level 1 background. Empty keeps the current button art.")]
    [SerializeField]
    private Sprite[] snowLevelButtonBackgrounds;

    // -------------------------------------------------------
    // LABELS
    // -------------------------------------------------------

    [Header("Labels")]
    [SerializeField]
    private string openLabel = "CONTINUE";

    [SerializeField]
    private string lockedLabel = "LOCKED";

    [Tooltip("Added after the map id in the level page title, for example JUNGLE LEVELS.")]
    [SerializeField]
    private string levelsTitleSuffix = " LEVELS";

    // -------------------------------------------------------
    // SCENES
    // -------------------------------------------------------

    [Header("Scenes")]
    [SerializeField]
    private string mainMenuSceneName = "MainMenu";

    // -------------------------------------------------------
    // TESTING
    // -------------------------------------------------------

    [Header("Testing")]
    [Tooltip("Opens every available world without finishing the world before. Keep off in release builds.")]
    [SerializeField]
    private bool unlockAllWorlds = false;

    // -------------------------------------------------------
    // DEBUG
    // -------------------------------------------------------

    [Header("Debug")]
    [SerializeField]
    private bool showDebugLogs = true;

    private Button[] levelButtons;
    private int selectedWorld = -1;
    private bool isLoading;

    // -------------------------------------------------------
    // UNITY
    // -------------------------------------------------------

    private void Awake()
    {
        levelButtons = FindLevelButtons();

        ConnectButtons();
        ShowMapSelectionPage();
    }

    private void ConnectButtons()
    {
        if (jungleMapButton != null)
        {
            jungleMapButton.onClick.AddListener(() => SelectWorld(JungleWorld));
        }

        if (beachMapButton != null)
        {
            beachMapButton.onClick.AddListener(() => SelectWorld(BeachWorld));
        }

        if (snowMapButton != null)
        {
            snowMapButton.onClick.AddListener(() => SelectWorld(SnowWorld));
        }

        if (mapPageBackButton != null)
        {
            mapPageBackButton.onClick.AddListener(BackToMainMenu);
        }

        if (levelPageBackButton != null)
        {
            levelPageBackButton.onClick.AddListener(ShowMapSelectionPage);
        }

        for (int i = 0; i < levelButtons.Length; i++)
        {
            int levelNumber = GetLevelNumberFromButton(levelButtons[i]);
            levelButtons[i].onClick.AddListener(() => SelectLevel(levelNumber));
        }
    }

    // -------------------------------------------------------
    // WORLD PAGE
    // -------------------------------------------------------

    private void ShowMapSelectionPage()
    {
        selectedWorld = -1;

        if (levelSelectionPage != null)
        {
            levelSelectionPage.SetActive(false);
        }

        if (mapSelectionPage != null)
        {
            mapSelectionPage.SetActive(true);
        }

        RefreshWorldButtons();
    }

    private void RefreshWorldButtons()
    {
        for (int world = 0; world < WorldCount; world++)
        {
            bool isUnlocked = IsWorldUnlocked(world);

            Button worldButton = GetWorldButton(world);

            if (worldButton != null)
            {
                worldButton.interactable = isUnlocked;
            }

            TMP_Text actionText = GetActionText(world);

            if (actionText != null)
            {
                actionText.text = isUnlocked ? openLabel : lockedLabel;
            }
        }
    }

    private bool IsWorldUnlocked(int world)
    {
        if (!IsWorldAvailable(world))
        {
            return false;
        }

        if (unlockAllWorlds || world == JungleWorld)
        {
            return true;
        }

        int previousWorld = world - 1;

        string previousCompletedKey =
            GetMapId(previousWorld) +
            "_LevelCompleted_" +
            GetLevelCount(previousWorld);

        return PlayerPrefs.GetInt(previousCompletedKey, 0) == 1;
    }

    private void SelectWorld(int world)
    {
        if (isLoading)
        {
            return;
        }

        if (!IsWorldUnlocked(world))
        {
            DebugLog($"{GetMapId(world)} is locked.");
            return;
        }

        selectedWorld = world;

        if (mapSelectionPage != null)
        {
            mapSelectionPage.SetActive(false);
        }

        if (levelSelectionPage != null)
        {
            levelSelectionPage.SetActive(true);
        }

        if (selectedMapTitle != null)
        {
            selectedMapTitle.text = GetMapId(world).ToUpperInvariant() + levelsTitleSuffix;
        }

        ApplyLevelButtonBackgrounds();
        RefreshLevelButtons();

        DebugLog($"World selected: {GetMapId(world)}.");
    }

    // -------------------------------------------------------
    // LEVEL PAGE
    // -------------------------------------------------------

    private Button[] FindLevelButtons()
    {
        if (levelSelectionPage == null)
        {
            return new Button[0];
        }

        Button[] foundButtons = levelSelectionPage.GetComponentsInChildren<Button>(true);

        List<Button> buttonList = new List<Button>();

        for (int i = 0; i < foundButtons.Length; i++)
        {
            if (GetLevelNumberFromButton(foundButtons[i]) > 0)
            {
                buttonList.Add(foundButtons[i]);
            }
        }

        return buttonList.ToArray();
    }

    private int GetLevelNumberFromButton(Button button)
    {
        if (button == null)
        {
            return -1;
        }

        string buttonName = button.gameObject.name;

        if (!buttonName.StartsWith(LevelButtonPrefix))
        {
            return -1;
        }

        if (int.TryParse(buttonName.Substring(LevelButtonPrefix.Length), out int levelNumber))
        {
            return levelNumber;
        }

        return -1;
    }

    private int GetUnlockedLevel(int world)
    {
        int unlockedLevel = PlayerPrefs.GetInt(GetMapId(world) + "_UnlockedLevel", 1);

        return Mathf.Clamp(unlockedLevel, 1, GetLevelCount(world));
    }

    private void RefreshLevelButtons()
    {
        if (selectedWorld < 0)
        {
            return;
        }

        int levelCount = GetLevelCount(selectedWorld);
        int unlockedLevel = GetUnlockedLevel(selectedWorld);

        for (int i = 0; i < levelButtons.Length; i++)
        {
            Button levelButton = levelButtons[i];
            int levelNumber = GetLevelNumberFromButton(levelButton);

            bool levelExists = levelNumber <= levelCount;

            levelButton.gameObject.SetActive(levelExists);

            if (!levelExists)
            {
                continue;
            }

            bool isUnlocked = levelNumber <= unlockedLevel;

            levelButton.interactable = isUnlocked;

            TMP_Text buttonText = levelButton.GetComponentInChildren<TMP_Text>(true);

            if (buttonText == null)
            {
                continue;
            }

            if (!isUnlocked)
            {
                buttonText.text = $"LEVEL {levelNumber}\n{lockedLabel}";
                continue;
            }

            int savedStars = Mathf.Clamp(
                PlayerPrefs.GetInt(GetMapId(selectedWorld) + "_LevelStars_" + levelNumber, 0),
                0,
                3
            );

            buttonText.text = $"LEVEL {levelNumber}\n{savedStars}/3 STARS";
        }
    }

    private void ApplyLevelButtonBackgrounds()
    {
        Sprite[] backgrounds = GetLevelButtonBackgrounds(selectedWorld);

        if (backgrounds == null || backgrounds.Length == 0)
        {
            return;
        }

        for (int i = 0; i < levelButtons.Length; i++)
        {
            Button levelButton = levelButtons[i];
            int spriteIndex = GetLevelNumberFromButton(levelButton) - 1;

            if (spriteIndex < 0 ||
                spriteIndex >= backgrounds.Length ||
                backgrounds[spriteIndex] == null)
            {
                continue;
            }

            Image backgroundImage = levelButton.targetGraphic as Image;

            if (backgroundImage == null)
            {
                backgroundImage = levelButton.GetComponent<Image>();
            }

            if (backgroundImage != null)
            {
                backgroundImage.sprite = backgrounds[spriteIndex];
            }
        }
    }

    private void SelectLevel(int levelNumber)
    {
        if (isLoading || selectedWorld < 0)
        {
            return;
        }

        string mapId = GetMapId(selectedWorld);
        string sceneName = GetSceneName(selectedWorld);

        if (levelNumber < 1 ||
            levelNumber > GetLevelCount(selectedWorld) ||
            levelNumber > GetUnlockedLevel(selectedWorld))
        {
            DebugLog($"{mapId} Level {levelNumber} is locked or does not exist.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                $"[WorldSelectionController] Cannot load scene '{sceneName}'. " +
                "Check the Scene Name and File > Build Profiles > Scene List.",
                this
            );

            return;
        }

        PlayerPrefs.SetInt(mapId + "_SelectedLevel", levelNumber);
        PlayerPrefs.SetInt(StartSelectedLevelKey, 1);
        PlayerPrefs.Save();

        isLoading = true;

        DebugLog($"Loading {sceneName}, Level {levelNumber}.");

        SceneManager.LoadScene(sceneName);
    }

    private void BackToMainMenu()
    {
        if (isLoading)
        {
            return;
        }

        isLoading = true;

        SceneManager.LoadScene(mainMenuSceneName);
    }

    // -------------------------------------------------------
    // WORLD SETTINGS
    // -------------------------------------------------------

    private string GetMapId(int world)
    {
        switch (world)
        {
            case BeachWorld: return beachMapId.Trim();
            case SnowWorld: return snowMapId.Trim();
            default: return jungleMapId.Trim();
        }
    }

    private string GetSceneName(int world)
    {
        switch (world)
        {
            case BeachWorld: return beachSceneName.Trim();
            case SnowWorld: return snowSceneName.Trim();
            default: return jungleSceneName.Trim();
        }
    }

    private int GetLevelCount(int world)
    {
        switch (world)
        {
            case BeachWorld: return Mathf.Max(1, beachLevelCount);
            case SnowWorld: return Mathf.Max(1, snowLevelCount);
            default: return Mathf.Max(1, jungleLevelCount);
        }
    }

    private bool IsWorldAvailable(int world)
    {
        switch (world)
        {
            case BeachWorld: return beachAvailable;
            case SnowWorld: return snowAvailable;
            default: return jungleAvailable;
        }
    }

    private Sprite[] GetLevelButtonBackgrounds(int world)
    {
        switch (world)
        {
            case BeachWorld: return beachLevelButtonBackgrounds;
            case SnowWorld: return snowLevelButtonBackgrounds;
            default: return jungleLevelButtonBackgrounds;
        }
    }

    private Button GetWorldButton(int world)
    {
        switch (world)
        {
            case BeachWorld: return beachMapButton;
            case SnowWorld: return snowMapButton;
            default: return jungleMapButton;
        }
    }

    private TMP_Text GetActionText(int world)
    {
        switch (world)
        {
            case BeachWorld: return beachActionText;
            case SnowWorld: return snowActionText;
            default: return jungleActionText;
        }
    }

    // -------------------------------------------------------
    // DEBUG
    // -------------------------------------------------------

    private void DebugLog(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[WorldSelectionController] {message}", this);
        }
    }
}
