using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public class PlayerSkinController : MonoBehaviour
{
    [Serializable]
    public class Skin
    {
        [Tooltip("Must exactly match the ShopItem Item Id.")]
        public string itemId;

        [Tooltip("Animator Override Controller for this skin.")]
        public AnimatorOverrideController animatorOverride;
    }

    [Header("References")]
    [SerializeField]
    private Animator animator;

    [Header("Default Skin")]
    [SerializeField]
    private string defaultSkinId = "ClassicMonkey";

    [Header("Available Skins")]
    [SerializeField]
    private Skin[] skins;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    private void Start()
    {
        ApplySelectedSkin();
    }

    public void ApplySelectedSkin()
    {
        string selectedSkinId =
            PlayerPrefs.GetString(
                ShopManager.SelectedItemKey,
                defaultSkinId
            );

        ApplySkin(selectedSkinId);
    }

    public void ApplySkin(string itemId)
    {
        if (animator == null)
        {
            Debug.LogError(
                "[PlayerSkinController] Animator is missing.",
                this
            );

            return;
        }

        if (skins == null)
            return;

        for (int i = 0; i < skins.Length; i++)
        {
            Skin skin = skins[i];

            if (skin == null)
                continue;

            if (skin.itemId != itemId)
                continue;

            if (skin.animatorOverride == null)
            {
                Debug.LogError(
                    $"[PlayerSkinController] " +
                    $"Animator Override is missing for {itemId}.",
                    this
                );

                return;
            }

            // Remember current state BEFORE changing skin.
            AnimatorStateInfo currentState =
                animator.GetCurrentAnimatorStateInfo(0);

            // Change skin animation set.
            animator.runtimeAnimatorController =
                skin.animatorOverride;

            // Restart the same state with the new skin.
            animator.Play(
                currentState.fullPathHash,
                0,
                0f
            );

            animator.Update(0f);

            Debug.Log(
                $"[PlayerSkinController] Skin applied: {itemId}",
                this
            );

            return;
        }

        Debug.LogWarning(
            $"[PlayerSkinController] " +
            $"No skin found with Item Id: {itemId}",
            this
        );
    }
}