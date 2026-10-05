using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MapWindowController : MonoBehaviour
{
    private const string StartSelectedLevelKey =
        "StartSelectedLevel";

    private const string LevelButtonPrefix =
        "LevelButton_";

    // =======================================================
    // WINDOWS
    // =======================================================

    [Header("Windows")]
    [Tooltip("Assign Canvas/MenuWindow.")]
    [SerializeField]
    private GameObject menuWindow;

    [Tooltip("Assign Canvas/MapWindow.")]
    [SerializeField]
    private GameObject mapWindow;

    // =======================================================
    // MAP WINDOW PAGES
    // =======================================================

    [Header("Map Window Pages")]
    [Tooltip(
        "Assign MapWindow/MapPanel/MapSelectionPage."
    )]
    [SerializeField]
    private GameObject mapSelectionPage;

    [Tooltip(
        "Assign MapWindow/MapPanel/LevelSelectionPage."
    )]
    [SerializeField]
    private GameObject levelSelectionPage;

    // =======================================================
    // MAP BUTTONS
    // =======================================================

    [Header("Map Buttons")]
    [SerializeField]
    private Button jungleMapButton;

    [FormerlySerializedAs("desertMapButton")]
    [SerializeField]
    private Button beachMapButton;

    [SerializeField]
    private Button snowMapButton;

    // =======================================================
    // LEVEL SELECTION UI
    // =======================================================

    [Header("Level Selection UI")]
    [Tooltip(
        "Assign LevelSelectionPage/SelectedMapTitle."
    )]
    [SerializeField]
    private TMP_Text selectedMapTitle;

    /*
     * No Level Buttons array is required.
     *
     * The script automatically finds:
     *
     * LevelButton_1
     * LevelButton_2
     * ...
     * LevelButton_10
     *
     * including inactive/locked buttons.
     */

    // =======================================================
    // LEVEL BUTTON BACKGROUNDS
    // =======================================================

    [Header("Level Button Backgrounds")]

    [Tooltip(
        "Element 0 = Jungle Level 1 background, " +
        "Element 1 = Jungle Level 2, etc."
    )]
    [SerializeField]
    private Sprite[] jungleLevelButtonBackgrounds;

    [Tooltip(
        "Element 0 = Beach Level 1 background, " +
        "Element 1 = Beach Level 2, etc."
    )]
    [SerializeField]
    private Sprite[] beachLevelButtonBackgrounds;

    [Tooltip(
        "Element 0 = Snow Level 1 background, " +
        "Element 1 = Snow Level 2, etc."
    )]
    [SerializeField]
    private Sprite[] snowLevelButtonBackgrounds;

    // =======================================================
    // LEVEL ROOT
    // =======================================================

    [Header("Central Level Start")]
    [Tooltip("Assign Canvas/Levels.")]
    [SerializeField]
    private Transform levelsRoot;

    [Tooltip(
        "Wait this many frames for MapLevelManager " +
        "to activate the selected level."
    )]
    [SerializeField, Range(1, 10)]
    private int autoStartWaitFrames = 2;

    // =======================================================
    // COUNTDOWN
    // =======================================================

    [Header("Level Start Countdown")]
    [Tooltip("Assign Canvas/CountdownPanel.")]
    [SerializeField]
    private GameObject countdownPanel;

    [Tooltip(
        "Assign Canvas/CountdownPanel/CountdownText."
    )]
    [SerializeField]
    private TMP_Text countdownText;

    [SerializeField, Range(1, 10)]
    private int countdownStartNumber = 3;

    [SerializeField, Min(0.1f)]
    private float countdownNumberDuration = 1f;

    [SerializeField, Min(0.1f)]
    private float goTextDuration = 0.7f;

    [SerializeField]
    private int countdownSortingOrder = 700;

    // =======================================================
    // JUNGLE MAP
    // =======================================================

    [Header("Jungle Map")]
    [Tooltip(
        "Must match MapLevelManager Map Id " +
        "inside the Jungle scene."
    )]
    [SerializeField]
    private string jungleMapId = "Jungle";

    [Tooltip(
        "Exact Jungle scene name without .unity."
    )]
    [SerializeField]
    private string jungleSceneName = "JungleMap";

    [Tooltip(
        "TOTAL Jungle levels, including locked levels."
    )]
    [SerializeField, Min(1)]
    private int jungleLevelCount = 10;

    [SerializeField]
    private bool jungleAvailable = true;

    // =======================================================
    // BEACH MAP
    // =======================================================

    [Header("Beach Map")]

    [FormerlySerializedAs("desertMapId")]
    [Tooltip(
        "Must match MapLevelManager Map Id " +
        "inside the Beach scene."
    )]
    [SerializeField]
    private string beachMapId = "Beach";

    [FormerlySerializedAs("desertSceneName")]
    [Tooltip(
        "Exact Beach scene name without .unity."
    )]
    [SerializeField]
    private string beachSceneName = "BeachMap";

    [FormerlySerializedAs("desertLevelCount")]
    [Tooltip(
        "TOTAL Beach levels, including locked levels."
    )]
    [SerializeField, Min(1)]
    private int beachLevelCount = 10;

    [FormerlySerializedAs("desertAvailable")]
    [SerializeField]
    private bool beachAvailable = true;

    // =======================================================
    // SNOW MAP
    // =======================================================

    [Header("Snow Map")]
    [Tooltip(
        "Must match MapLevelManager Map Id " +
        "inside the Snow scene."
    )]
    [SerializeField]
    private string snowMapId = "Snow";

    [Tooltip(
        "Exact Snow scene name without .unity."
    )]
    [SerializeField]
    private string snowSceneName = "SnowMap";

    [Tooltip(
        "TOTAL Snow levels, including locked levels."
    )]
    [SerializeField, Min(1)]
    private int snowLevelCount = 10;

    [SerializeField]
    private bool snowAvailable = true;

    // =======================================================
    // SORTING
    // =======================================================

    [Header("Map Window Sorting")]
    [SerializeField]
    private int mapWindowSortingOrder = 500;

    // =======================================================
    // DEBUG
    // =======================================================

    [Header("Debug")]
    [SerializeField]
    private bool showDebugLogs = true;

    // =======================================================
    // RUNTIME
    // =======================================================

    private string selectedMapId =
        string.Empty;

    private string selectedMapSceneName =
        string.Empty;

    private int selectedMapLevelCount;

    private bool isLoadingLevel;
    private bool isCountdownRunning;

    // =======================================================
    // UNITY
    // =======================================================

    private void Awake()
    {
        CleanInspectorValues();

        isLoadingLevel = false;
        isCountdownRunning = false;

        if (mapWindow != null)
        {
            mapWindow.SetActive(false);
        }

        if (countdownPanel != null)
        {
            countdownPanel.SetActive(false);
        }

        ShowMapSelectionPage();
        RefreshMapButtons();
    }

    private IEnumerator Start()
    {
        /*
         * Wait for MapLevelManager to activate the
         * correct selected Level root.
         */
        for (int i = 0;
             i < autoStartWaitFrames;
             i++)
        {
            yield return null;
        }

        yield return StartCoroutine(
            TryStartSelectedLevelWithCountdown()
        );
    }

    private void OnValidate()
    {
        CleanInspectorValues();
    }

    // =======================================================
    // CLEAN VALUES
    // =======================================================

    private void CleanInspectorValues()
    {
        jungleMapId =
            CleanText(
                jungleMapId,
                "Jungle"
            );

        beachMapId =
            CleanText(
                beachMapId,
                "Beach"
            );

        snowMapId =
            CleanText(
                snowMapId,
                "Snow"
            );

        jungleSceneName =
            CleanText(
                jungleSceneName,
                "JungleMap"
            );

        beachSceneName =
            CleanText(
                beachSceneName,
                "BeachMap"
            );

        snowSceneName =
            CleanText(
                snowSceneName,
                "SnowMap"
            );

        jungleLevelCount =
            Mathf.Max(
                1,
                jungleLevelCount
            );

        beachLevelCount =
            Mathf.Max(
                1,
                beachLevelCount
            );

        snowLevelCount =
            Mathf.Max(
                1,
                snowLevelCount
            );

        autoStartWaitFrames =
            Mathf.Clamp(
                autoStartWaitFrames,
                1,
                10
            );

        countdownStartNumber =
            Mathf.Clamp(
                countdownStartNumber,
                1,
                10
            );

        countdownNumberDuration =
            Mathf.Max(
                0.1f,
                countdownNumberDuration
            );

        goTextDuration =
            Mathf.Max(
                0.1f,
                goTextDuration
            );
    }

    private string CleanText(
        string value,
        string defaultValue
    )
    {
        return string.IsNullOrWhiteSpace(value)
            ? defaultValue
            : value.Trim();
    }

    // =======================================================
    // OPEN MAP WINDOW
    // =======================================================

    public void OpenMapWindow()
    {
        if (mapWindow == null)
        {
            Debug.LogError(
                "[MapWindowController] " +
                "MapWindow is not assigned.",
                this
            );

            return;
        }

        Time.timeScale = 0f;

        if (countdownPanel != null)
        {
            countdownPanel.SetActive(false);
        }

        if (menuWindow != null)
        {
            menuWindow.SetActive(false);
        }

        mapWindow.SetActive(true);

        mapWindow.transform.SetAsLastSibling();

        ConfigurePopupCanvas(
            mapWindow,
            mapWindowSortingOrder
        );

        ShowMapSelectionPage();
        RefreshMapButtons();

        Canvas.ForceUpdateCanvases();

        DebugLog(
            "MapWindow opened."
        );
    }

    // =======================================================
    // CLOSE MAP WINDOW
    // =======================================================

    public void CloseMapWindow()
    {
        if (mapWindow != null)
        {
            mapWindow.SetActive(false);
        }

        if (menuWindow != null)
        {
            menuWindow.SetActive(true);

            menuWindow.transform.SetAsLastSibling();
        }

        /*
         * Menu remains open, so game stays paused.
         */
        Time.timeScale = 0f;

        DebugLog(
            "MapWindow closed."
        );
    }

    // =======================================================
    // RESUME
    // =======================================================

    public void ReleaseGameplayInputAfterResume()
    {
        if (mapWindow != null)
        {
            mapWindow.SetActive(false);
        }

        if (countdownPanel != null)
        {
            countdownPanel.SetActive(false);
        }

        Time.timeScale = 1f;

        DebugLog(
            "Gameplay resumed."
        );
    }

    // =======================================================
    // POPUP CANVAS
    // =======================================================

    private void ConfigurePopupCanvas(
        GameObject popup,
        int sortingOrder
    )
    {
        if (popup == null)
        {
            return;
        }

        Canvas popupCanvas =
            popup.GetComponent<Canvas>();

        if (popupCanvas != null)
        {
            popupCanvas.overrideSorting = true;

            popupCanvas.sortingOrder =
                sortingOrder;
        }

        CanvasGroup popupGroup =
            popup.GetComponent<CanvasGroup>();

        if (popupGroup != null)
        {
            popupGroup.alpha = 1f;
            popupGroup.interactable = true;
            popupGroup.blocksRaycasts = true;
        }
    }

    // =======================================================
    // MAP SELECTION PAGE
    // =======================================================

    public void ShowMapSelectionPage()
    {
        if (mapSelectionPage != null)
        {
            mapSelectionPage.SetActive(true);
        }

        if (levelSelectionPage != null)
        {
            levelSelectionPage.SetActive(false);
        }

        selectedMapId =
            string.Empty;

        selectedMapSceneName =
            string.Empty;

        selectedMapLevelCount = 0;
    }

    public void BackToMapSelection()
    {
        ShowMapSelectionPage();

        DebugLog(
            "Returned to map selection."
        );
    }

    // =======================================================
    // SELECT MAP
    // =======================================================

    public void SelectJungleMap()
    {
        ShowLevelsForMap(
            jungleMapId,
            jungleSceneName,
            jungleLevelCount,
            jungleAvailable
        );
    }

    public void SelectBeachMap()
    {
        ShowLevelsForMap(
            beachMapId,
            beachSceneName,
            beachLevelCount,
            beachAvailable
        );
    }

    /*
     * Keep for compatibility with an old
     * Inspector OnClick assignment.
     */
    public void SelectDesertMap()
    {
        SelectBeachMap();
    }

    public void SelectSnowMap()
    {
        ShowLevelsForMap(
            snowMapId,
            snowSceneName,
            snowLevelCount,
            snowAvailable
        );
    }

    // =======================================================
    // SHOW LEVELS
    // =======================================================

    private void ShowLevelsForMap(
        string mapId,
        string sceneName,
        int levelCount,
        bool isAvailable
    )
    {
        if (!isAvailable)
        {
            Debug.LogWarning(
                $"[MapWindowController] " +
                $"Map '{mapId}' is unavailable.",
                this
            );

            return;
        }

        if (string.IsNullOrWhiteSpace(mapId))
        {
            Debug.LogError(
                "[MapWindowController] " +
                "Map ID is empty.",
                this
            );

            return;
        }

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError(
                $"[MapWindowController] " +
                $"Scene name for '{mapId}' is empty.",
                this
            );

            return;
        }

        selectedMapId =
            mapId.Trim();

        selectedMapSceneName =
            sceneName.Trim();

        selectedMapLevelCount =
            Mathf.Max(
                1,
                levelCount
            );

        /*
         * IMPORTANT:
         *
         * Activate LevelSelectionPage BEFORE finding buttons.
         */
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
            selectedMapTitle.text =
                selectedMapId.ToUpperInvariant() +
                " LEVELS";
        }

        /*
         * Change every LevelButton background.
         *
         * Locked/inactive buttons are included.
         */
        ApplyLevelButtonBackgrounds(
            selectedMapId
        );

        /*
         * Then update unlocked/locked states.
         */
        RefreshLevelButtons();

        Debug.Log(
            $"[MapWindowController] MAP SELECTED\n" +
            $"Map = {selectedMapId}\n" +
            $"Scene = {selectedMapSceneName}\n" +
            $"Levels = {selectedMapLevelCount}",
            this
        );
    }

    // =======================================================
    // FIND ALL LEVEL BUTTONS
    // =======================================================

    private Button[] GetAllLevelButtons()
    {
        if (levelSelectionPage == null)
        {
            return new Button[0];
        }

        /*
         * TRUE means inactive children are included.
         *
         * So LevelButton_5 ... LevelButton_10
         * are found even if currently inactive.
         */
        Button[] foundButtons =
            levelSelectionPage
                .GetComponentsInChildren<Button>(true);

        List<Button> levelButtonList =
            new List<Button>();

        for (int i = 0;
             i < foundButtons.Length;
             i++)
        {
            Button button =
                foundButtons[i];

            if (button == null)
            {
                continue;
            }

            if (!button.gameObject.name.StartsWith(
                    LevelButtonPrefix
                ))
            {
                continue;
            }

            int levelNumber =
                GetLevelNumberFromButton(
                    button
                );

            if (levelNumber <= 0)
            {
                continue;
            }

            levelButtonList.Add(
                button
            );
        }

        /*
         * Numeric sort:
         *
         * 1
         * 2
         * 3
         * ...
         * 9
         * 10
         */
        levelButtonList.Sort(
            (a, b) =>
            {
                int aNumber =
                    GetLevelNumberFromButton(a);

                int bNumber =
                    GetLevelNumberFromButton(b);

                return aNumber.CompareTo(
                    bNumber
                );
            }
        );

        return levelButtonList.ToArray();
    }

    // =======================================================
    // GET LEVEL NUMBER
    // =======================================================

    private int GetLevelNumberFromButton(
        Button button
    )
    {
        if (button == null)
        {
            return -1;
        }

        string buttonName =
            button.gameObject.name;

        if (!buttonName.StartsWith(
                LevelButtonPrefix
            ))
        {
            return -1;
        }

        string numberText =
            buttonName.Substring(
                LevelButtonPrefix.Length
            );

        if (int.TryParse(
                numberText,
                out int levelNumber
            ))
        {
            return levelNumber;
        }

        return -1;
    }

    // =======================================================
    // GET BUTTON BACKGROUND IMAGE
    // =======================================================

    private Image GetLevelButtonBackgroundImage(
        Button button
    )
    {
        if (button == null)
        {
            return null;
        }

        /*
         * First try the Button Target Graphic.
         */
        Image image =
            button.targetGraphic as Image;

        /*
         * Otherwise use Image on the Button object.
         */
        if (image == null)
        {
            image =
                button.GetComponent<Image>();
        }

        return image;
    }

    // =======================================================
    // CHANGE LEVEL BUTTON BACKGROUNDS
    // =======================================================

    private void ApplyLevelButtonBackgrounds(
        string mapId
    )
    {
        Sprite[] selectedSprites = null;

        if (mapId == jungleMapId)
        {
            selectedSprites =
                jungleLevelButtonBackgrounds;
        }
        else if (mapId == beachMapId)
        {
            selectedSprites =
                beachLevelButtonBackgrounds;
        }
        else if (mapId == snowMapId)
        {
            selectedSprites =
                snowLevelButtonBackgrounds;
        }

        if (selectedSprites == null)
        {
            Debug.LogWarning(
                $"[MapWindowController] " +
                $"No background sprites found for '{mapId}'.",
                this
            );

            return;
        }

        Button[] allLevelButtons =
            GetAllLevelButtons();

        for (int i = 0;
             i < allLevelButtons.Length;
             i++)
        {
            Button button =
                allLevelButtons[i];

            if (button == null)
            {
                continue;
            }

            int levelNumber =
                GetLevelNumberFromButton(
                    button
                );

            if (levelNumber <= 0)
            {
                continue;
            }

            int spriteIndex =
                levelNumber - 1;

            Image backgroundImage =
                GetLevelButtonBackgroundImage(
                    button
                );

            if (backgroundImage == null)
            {
                Debug.LogWarning(
                    $"[MapWindowController] " +
                    $"{button.name} has no background Image.",
                    button
                );

                continue;
            }

            /*
             * Always keep the actual Image fully opaque.
             */
            Color imageColor =
                backgroundImage.color;

            imageColor.a = 1f;

            backgroundImage.color =
                imageColor;

            /*
             * If this map does not have an image for the
             * level, clear the previous map image.
             */
            if (spriteIndex < 0 ||
                spriteIndex >= selectedSprites.Length)
            {
                backgroundImage.sprite = null;

                Debug.LogWarning(
                    $"[MapWindowController] " +
                    $"No {mapId} background assigned for " +
                    $"Level {levelNumber}.",
                    button
                );

                continue;
            }

            backgroundImage.sprite =
                selectedSprites[spriteIndex];
        }
    }

    // =======================================================
    // KEEP LOCKED BUTTON VISIBLE
    // =======================================================

    private void PrepareLevelButtonVisual(
        Button levelButton
    )
    {
        if (levelButton == null)
        {
            return;
        }

        /*
         * Sprite Swap can replace our map-specific
         * background with an old Disabled Sprite.
         *
         * Automatically use Color Tint instead.
         */
        if (levelButton.transition ==
            Selectable.Transition.SpriteSwap)
        {
            levelButton.transition =
                Selectable.Transition.ColorTint;
        }

        /*
         * Prevent the disabled Color Tint from making
         * locked backgrounds transparent.
         */
        ColorBlock colors =
            levelButton.colors;

        Color disabledColor =
            colors.disabledColor;

        disabledColor.a = 1f;

        colors.disabledColor =
            disabledColor;

        levelButton.colors =
            colors;

        /*
         * Also keep the Image itself fully opaque.
         */
        Image backgroundImage =
            GetLevelButtonBackgroundImage(
                levelButton
            );

        if (backgroundImage != null)
        {
            Color imageColor =
                backgroundImage.color;

            imageColor.a = 1f;

            backgroundImage.color =
                imageColor;
        }
    }

    // =======================================================
    // REFRESH LEVEL BUTTONS
    // =======================================================

    private void RefreshLevelButtons()
    {
        if (string.IsNullOrWhiteSpace(
                selectedMapId
            ))
        {
            return;
        }

        Button[] allLevelButtons =
            GetAllLevelButtons();

        if (allLevelButtons.Length == 0)
        {
            Debug.LogWarning(
                "[MapWindowController] " +
                "No LevelButton_X buttons found.",
                this
            );

            return;
        }

        int unlockedLevel =
            PlayerPrefs.GetInt(
                GetUnlockedLevelKey(
                    selectedMapId
                ),
                1
            );

        unlockedLevel =
            Mathf.Clamp(
                unlockedLevel,
                1,
                selectedMapLevelCount
            );

        int selectedLevel =
            PlayerPrefs.GetInt(
                GetSelectedLevelKey(
                    selectedMapId
                ),
                1
            );

        Scene currentScene =
            SceneManager.GetActiveScene();

        bool currentMapScene =
            currentScene.name ==
            selectedMapSceneName;

        for (int i = 0;
             i < allLevelButtons.Length;
             i++)
        {
            Button levelButton =
                allLevelButtons[i];

            if (levelButton == null)
            {
                continue;
            }

            int levelNumber =
                GetLevelNumberFromButton(
                    levelButton
                );

            if (levelNumber <= 0)
            {
                continue;
            }

            /*
             * TOTAL level count controls whether
             * the button exists for this map.
             */
            bool levelExists =
                levelNumber <=
                selectedMapLevelCount;

            levelButton.gameObject.SetActive(
                levelExists
            );

            if (!levelExists)
            {
                continue;
            }

            /*
             * Make sure the visual remains clear,
             * even when the Button becomes locked.
             */
            PrepareLevelButtonVisual(
                levelButton
            );

            bool isUnlocked =
                levelNumber <=
                unlockedLevel;

            levelButton.interactable =
                isUnlocked;

            /*
             * Call again AFTER changing interactable,
             * so disabled alpha remains fully visible.
             */
            PrepareLevelButtonVisual(
                levelButton
            );

            int savedStars =
                Mathf.Clamp(
                    PlayerPrefs.GetInt(
                        GetStarsKey(
                            selectedMapId,
                            levelNumber
                        ),
                        0
                    ),
                    0,
                    3
                );

            TMP_Text buttonText =
                levelButton
                    .GetComponentInChildren
                        <TMP_Text>(true);

            if (buttonText == null)
            {
                continue;
            }

            if (!isUnlocked)
            {
                buttonText.text =
                    $"LEVEL {levelNumber}\nLOCKED";

                continue;
            }

            bool isCurrentLevel =
                currentMapScene &&
                selectedLevel ==
                levelNumber;

            if (isCurrentLevel)
            {
                buttonText.text =
                    $"LEVEL {levelNumber}\n" +
                    $"CURRENT  {savedStars}/3";
            }
            else
            {
                buttonText.text =
                    $"LEVEL {levelNumber}\n" +
                    $"{savedStars}/3 STARS";
            }
        }
    }

    // =======================================================
    // SELECT LEVEL
    // =======================================================

    public void SelectLevel(
        int levelNumber
    )
    {
        if (isLoadingLevel)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(
                selectedMapId
            ))
        {
            Debug.LogError(
                "[MapWindowController] " +
                "No map selected.",
                this
            );

            return;
        }

        if (string.IsNullOrWhiteSpace(
                selectedMapSceneName
            ))
        {
            Debug.LogError(
                "[MapWindowController] " +
                "Selected map Scene Name is empty.",
                this
            );

            return;
        }

        string targetMapId =
            selectedMapId.Trim();

        string targetSceneName =
            selectedMapSceneName.Trim();

        int targetLevelCount =
            Mathf.Max(
                1,
                selectedMapLevelCount
            );

        // ---------------------------------------------------
        // VALIDATE LEVEL
        // ---------------------------------------------------

        if (levelNumber < 1 ||
            levelNumber > targetLevelCount)
        {
            Debug.LogError(
                $"[MapWindowController] " +
                $"Level {levelNumber} does not exist " +
                $"for '{targetMapId}'.",
                this
            );

            return;
        }

        // ---------------------------------------------------
        // CHECK LOCK
        // ---------------------------------------------------

        int unlockedLevel =
            PlayerPrefs.GetInt(
                GetUnlockedLevelKey(
                    targetMapId
                ),
                1
            );

        unlockedLevel =
            Mathf.Max(
                1,
                unlockedLevel
            );

        if (levelNumber > unlockedLevel)
        {
            Debug.LogWarning(
                $"[MapWindowController] " +
                $"{targetMapId} Level {levelNumber} " +
                "is locked.",
                this
            );

            return;
        }

        // ---------------------------------------------------
        // CHECK SCENE
        // ---------------------------------------------------

        if (!Application.CanStreamedLevelBeLoaded(
                targetSceneName
            ))
        {
            Debug.LogError(
                $"[MapWindowController] " +
                $"Cannot load scene '{targetSceneName}'.\n" +
                $"Map = {targetMapId}\n\n" +
                "Check Scene Name and " +
                "File > Build Profiles > Scene List.",
                this
            );

            return;
        }

        // ---------------------------------------------------
        // SAVE SELECTED LEVEL
        // ---------------------------------------------------

        PlayerPrefs.SetInt(
            GetSelectedLevelKey(
                targetMapId
            ),
            levelNumber
        );

        /*
         * The new scene will use this to start
         * the countdown.
         */
        PlayerPrefs.SetInt(
            StartSelectedLevelKey,
            1
        );

        PlayerPrefs.Save();

        isLoadingLevel = true;

        Debug.Log(
            $"[MapWindowController] LEVEL SELECTED\n" +
            $"Map = {targetMapId}\n" +
            $"Scene = {targetSceneName}\n" +
            $"Level = {levelNumber}",
            this
        );

        StartCoroutine(
            LoadSelectedLevelRoutine(
                targetSceneName,
                targetMapId,
                levelNumber
            )
        );
    }

    // =======================================================
    // LOAD SCENE
    // =======================================================

    private IEnumerator LoadSelectedLevelRoutine(
        string targetSceneName,
        string targetMapId,
        int targetLevelNumber
    )
    {
        if (mapWindow != null)
        {
            mapWindow.SetActive(false);
        }

        if (menuWindow != null)
        {
            menuWindow.SetActive(false);
        }

        if (countdownPanel != null)
        {
            countdownPanel.SetActive(false);
        }

        if (EventSystem.current != null)
        {
            EventSystem.current
                .SetSelectedGameObject(null);
        }

        Time.timeScale = 1f;

        /*
         * Let UI click complete first.
         */
        yield return null;

        Debug.Log(
            $"[MapWindowController] LOADING\n" +
            $"Map = {targetMapId}\n" +
            $"Scene = {targetSceneName}\n" +
            $"Level = {targetLevelNumber}",
            this
        );

        /*
         * Always load the exact selected map scene.
         */
        AsyncOperation loadOperation =
            SceneManager.LoadSceneAsync(
                targetSceneName,
                LoadSceneMode.Single
            );

        if (loadOperation == null)
        {
            HandleSceneLoadFailure(
                targetSceneName
            );

            yield break;
        }

        while (!loadOperation.isDone)
        {
            yield return null;
        }
    }

    // =======================================================
    // SCENE LOAD FAILURE
    // =======================================================

    private void HandleSceneLoadFailure(
        string targetSceneName
    )
    {
        isLoadingLevel = false;

        PlayerPrefs.SetInt(
            StartSelectedLevelKey,
            0
        );

        PlayerPrefs.Save();

        Time.timeScale = 0f;

        if (mapWindow != null)
        {
            mapWindow.SetActive(true);
        }

        Debug.LogError(
            $"[MapWindowController] " +
            $"Failed to load scene '{targetSceneName}'.",
            this
        );
    }

    // =======================================================
    // COUNTDOWN
    // =======================================================

    private IEnumerator TryStartSelectedLevelWithCountdown()
    {
        if (isCountdownRunning)
        {
            yield break;
        }

        int shouldStart =
            PlayerPrefs.GetInt(
                StartSelectedLevelKey,
                0
            );

        /*
         * No level was selected from MapWindow.
         */
        if (shouldStart != 1)
        {
            yield break;
        }

        if (levelsRoot == null)
        {
            Debug.LogError(
                "[MapWindowController] " +
                "Levels Root missing. " +
                "Assign Canvas/Levels.",
                this
            );

            yield break;
        }

        GameObject activeLevel =
            FindActiveLevel();

        if (activeLevel == null)
        {
            Debug.LogError(
                "[MapWindowController] " +
                "No active Level found.",
                this
            );

            yield break;
        }

        CharacterPathMover activePlayer =
            activeLevel
                .GetComponentInChildren
                    <CharacterPathMover>(true);

        if (activePlayer == null)
        {
            Debug.LogError(
                $"[MapWindowController] " +
                $"No CharacterPathMover found inside " +
                $"'{activeLevel.name}'.",
                activeLevel
            );

            yield break;
        }

        if (!activePlayer.gameObject.activeInHierarchy)
        {
            Debug.LogError(
                $"[MapWindowController] " +
                $"Player inside '{activeLevel.name}' " +
                "is inactive.",
                activePlayer
            );

            yield break;
        }

        isCountdownRunning = true;

        /*
         * Clear before countdown starts.
         */
        PlayerPrefs.SetInt(
            StartSelectedLevelKey,
            0
        );

        PlayerPrefs.Save();

        Time.timeScale = 0f;

        // ---------------------------------------------------
        // SHOW COUNTDOWN PANEL
        // ---------------------------------------------------

        if (countdownPanel != null)
        {
            countdownPanel.SetActive(true);

            countdownPanel.transform
                .SetAsLastSibling();

            ConfigurePopupCanvas(
                countdownPanel,
                countdownSortingOrder
            );
        }

        // ---------------------------------------------------
        // 3 2 1
        // ---------------------------------------------------

        for (int number =
                 countdownStartNumber;
             number >= 1;
             number--)
        {
            if (countdownText != null)
            {
                countdownText.text =
                    number.ToString();
            }

            yield return
                new WaitForSecondsRealtime(
                    countdownNumberDuration
                );
        }

        // ---------------------------------------------------
        // GO!
        // ---------------------------------------------------

        if (countdownText != null)
        {
            countdownText.text =
                "GO!";
        }

        yield return
            new WaitForSecondsRealtime(
                goTextDuration
            );

        if (countdownPanel != null)
        {
            countdownPanel.SetActive(false);
        }

        Time.timeScale = 1f;

        isCountdownRunning = false;

        Debug.Log(
            $"[MapWindowController] START\n" +
            $"Scene = " +
            $"{SceneManager.GetActiveScene().name}\n" +
            $"Level = {activeLevel.name}\n" +
            $"Player = {activePlayer.gameObject.name}",
            activePlayer
        );

        activePlayer.StartGame();
    }

    // =======================================================
    // FIND ACTIVE LEVEL
    // =======================================================

    private GameObject FindActiveLevel()
    {
        if (levelsRoot == null)
        {
            return null;
        }

        for (int i = 0;
             i < levelsRoot.childCount;
             i++)
        {
            GameObject level =
                levelsRoot
                    .GetChild(i)
                    .gameObject;

            if (level.activeInHierarchy)
            {
                return level;
            }
        }

        return null;
    }

    // =======================================================
    // LEVEL BUTTON ONCLICK METHODS
    // =======================================================

    public void SelectLevel1()
    {
        SelectLevel(1);
    }

    public void SelectLevel2()
    {
        SelectLevel(2);
    }

    public void SelectLevel3()
    {
        SelectLevel(3);
    }

    public void SelectLevel4()
    {
        SelectLevel(4);
    }

    public void SelectLevel5()
    {
        SelectLevel(5);
    }

    public void SelectLevel6()
    {
        SelectLevel(6);
    }

    public void SelectLevel7()
    {
        SelectLevel(7);
    }

    public void SelectLevel8()
    {
        SelectLevel(8);
    }

    public void SelectLevel9()
    {
        SelectLevel(9);
    }

    public void SelectLevel10()
    {
        SelectLevel(10);
    }

    // =======================================================
    // PLAYERPREF KEYS
    // =======================================================

    private string GetSelectedLevelKey(
        string mapId
    )
    {
        return mapId +
               "_SelectedLevel";
    }

    private string GetUnlockedLevelKey(
        string mapId
    )
    {
        return mapId +
               "_UnlockedLevel";
    }

    private string GetStarsKey(
        string mapId,
        int levelNumber
    )
    {
        return mapId +
               "_LevelStars_" +
               levelNumber;
    }

    // =======================================================
    // MAP BUTTON STATES
    // =======================================================

    public void RefreshMapButtons()
    {
        if (jungleMapButton != null)
        {
            jungleMapButton.interactable =
                jungleAvailable;
        }

        if (beachMapButton != null)
        {
            beachMapButton.interactable =
                beachAvailable;
        }

        if (snowMapButton != null)
        {
            snowMapButton.interactable =
                snowAvailable;
        }
    }

    // =======================================================
    // DEBUG
    // =======================================================

    private void DebugLog(
        string message
    )
    {
        if (!showDebugLogs)
        {
            return;
        }

        Debug.Log(
            "[MapWindowController] " +
            message,
            this
        );
    }
}