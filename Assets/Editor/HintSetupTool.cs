using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class HintSetupTool
{
    private static readonly string[] MapScenePaths =
    {
        "Assets/Scenes/Gameplay/JungleMap.unity",
        "Assets/Scenes/Gameplay/BeachMap.unity",
        "Assets/Scenes/Gameplay/SnowMap.unity"
    };

    private const float ButtonSize = 240f;
    private const float ButtonMargin = 40f;

    [MenuItem("Tools/Monkey Rush/Add Hint Button")]
    public static void AddHintButton()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError(
                "[HintSetupTool] Stop Play Mode before adding the Hint button."
            );

            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.Log(
                "[HintSetupTool] Stopped: scene save was cancelled. " +
                "No scene was changed."
            );

            return;
        }

        SceneSetup[] previousSetup =
            EditorSceneManager.GetSceneManagerSetup();

        for (int i = 0; i < MapScenePaths.Length; i++)
        {
            AddHintButtonToScene(MapScenePaths[i]);
        }

        if (previousSetup.Length > 0)
        {
            EditorSceneManager.RestoreSceneManagerSetup(
                previousSetup
            );
        }
    }

    // -------------------------------------------------------
    // ONE SCENE
    // -------------------------------------------------------

    private static void AddHintButtonToScene(string scenePath)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
        {
            Debug.Log(
                $"[HintSetupTool] {scenePath}: skipped: scene not found."
            );

            return;
        }

        Scene scene = EditorSceneManager.OpenScene(
            scenePath,
            OpenSceneMode.Single
        );

        if (SceneHasLevelHint(scene))
        {
            Debug.Log(
                $"[HintSetupTool] {scene.name}: already set."
            );

            return;
        }

        Transform hud = FindHud(scene);

        if (hud == null)
        {
            Debug.Log(
                $"[HintSetupTool] {scene.name}: skipped: Canvas/HUD not found."
            );

            return;
        }

        CreateHintButton(hud);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log(
            $"[HintSetupTool] {scene.name}: done " +
            "(Canvas/HintButton added after Levels and saved)."
        );
    }

    private static bool SceneHasLevelHint(Scene scene)
    {
        GameObject[] rootObjects = scene.GetRootGameObjects();

        for (int i = 0; i < rootObjects.Length; i++)
        {
            if (rootObjects[i].GetComponentInChildren<LevelHint>(true) != null)
                return true;
        }

        return false;
    }

    private static Transform FindHud(Scene scene)
    {
        GameObject[] rootObjects = scene.GetRootGameObjects();

        for (int i = 0; i < rootObjects.Length; i++)
        {
            if (rootObjects[i].name != "Canvas")
                continue;

            Transform hud = rootObjects[i].transform.Find("HUD");

            if (hud != null)
                return hud;
        }

        return null;
    }

    // -------------------------------------------------------
    // BUTTON
    // -------------------------------------------------------

    private static void CreateHintButton(Transform hud)
    {
        Transform canvas = hud.parent;

        GameObject buttonObject = new GameObject(
            "HintButton",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button)
        );

        buttonObject.layer = hud.gameObject.layer;

        RectTransform buttonRect =
            buttonObject.GetComponent<RectTransform>();

        buttonRect.SetParent(canvas, false);
        Transform levels = canvas.Find("Levels");

        buttonRect.SetSiblingIndex(
            levels != null
                ? levels.GetSiblingIndex() + 1
                : hud.GetSiblingIndex()
        );

        buttonRect.anchorMin = new Vector2(1f, 0f);
        buttonRect.anchorMax = new Vector2(1f, 0f);
        buttonRect.pivot = new Vector2(1f, 0f);
        buttonRect.sizeDelta = new Vector2(ButtonSize, ButtonSize);
        buttonRect.anchoredPosition =
            new Vector2(-ButtonMargin, ButtonMargin);

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.sprite =
            AssetDatabase.GetBuiltinExtraResource<Sprite>(
                "UI/Skin/UISprite.psd"
            );
        buttonImage.type = Image.Type.Sliced;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = buttonImage;

        CreateText(
            buttonRect,
            "HintLabel",
            "HINT",
            new Vector2(0f, 0.5f),
            new Vector2(1f, 1f),
            56f
        );

        TMP_Text costText = CreateText(
            buttonRect,
            "CostText",
            "3",
            new Vector2(0f, 0f),
            new Vector2(1f, 0.5f),
            72f
        );

        buttonObject.AddComponent<UIButtonTapSound>();

        LevelHint levelHint =
            buttonObject.AddComponent<LevelHint>();

        SerializedObject serializedHint =
            new SerializedObject(levelHint);

        serializedHint.FindProperty("hintButton").objectReferenceValue =
            button;

        serializedHint.FindProperty("costText").objectReferenceValue =
            costText;

        serializedHint.ApplyModifiedPropertiesWithoutUndo();
    }

    private static TMP_Text CreateText(
        RectTransform parent,
        string objectName,
        string text,
        Vector2 anchorMin,
        Vector2 anchorMax,
        float fontSize
    )
    {
        GameObject textObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );

        textObject.layer = parent.gameObject.layer;

        RectTransform textRect =
            textObject.GetComponent<RectTransform>();

        textRect.SetParent(parent, false);
        textRect.anchorMin = anchorMin;
        textRect.anchorMax = anchorMax;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label =
            textObject.GetComponent<TextMeshProUGUI>();

        if (TMP_Settings.defaultFontAsset != null)
        {
            label.font = TMP_Settings.defaultFontAsset;
        }

        label.text = text;
        label.fontSize = fontSize;
        label.color = Color.black;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;

        return label;
    }
}
