using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UVmonSlotUI : MonoBehaviour
{
    [SerializeField] private Image portrait;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Button button;

    private UVmonInstance currentUVmon;

    public void Setup(UVmonInstance uvmon, Action<UVmonInstance> onClick)
    {
        currentUVmon = uvmon;

        if (uvmon == null || uvmon.data == null) return;

        portrait.sprite = uvmon.data.portrait;
        nameText.text = uvmon.data.uvmonName;
        levelText.text = "Nv. " + uvmon.level;
        hpText.text = uvmon.currentHP + " / " + uvmon.MaxHP;

        if (hpSlider != null)
        {
            hpSlider.value = uvmon.MaxHP > 0 ? (float)uvmon.currentHP / uvmon.MaxHP : 0f;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick?.Invoke(currentUVmon));
    }
}