using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 关卡选择界面：根据 LevelCatalogConfig 动态生成关卡按钮。
/// </summary>
public class LevelSelectPanel : UIPanelBase
{
    private const float ListItemHeight = 72f;

    [SerializeField] private Transform levelButtonContainer;
    [SerializeField] private Button levelButtonTemplate;
    [SerializeField] private Button backButton;
    [SerializeField] private TextMeshProUGUI titleText;

    private readonly List<Button> _spawnedButtons = new List<Button>(8);

    public event Action OnBackClicked;
    public event Action<LevelConfig> OnLevelSelected;

    protected override void Awake()
    {
        base.Awake();
        if (backButton != null)
            backButton.onClick.AddListener(() => OnBackClicked?.Invoke());

        if (levelButtonTemplate != null)
        {
            ConfigureListItemRect(levelButtonTemplate);
            levelButtonTemplate.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (backButton != null)
            backButton.onClick.RemoveAllListeners();

        ClearButtons();
    }

    public void BindCatalog(LevelCatalogConfig catalog)
    {
        ClearButtons();
        if (catalog == null || catalog.levels == null || levelButtonContainer == null || levelButtonTemplate == null)
            return;

        foreach (LevelConfig level in catalog.levels)
        {
            if (level == null) continue;

            Button btn = Instantiate(levelButtonTemplate, levelButtonContainer);
            ConfigureListItemRect(btn);
            btn.gameObject.SetActive(true);
            btn.onClick.AddListener(() => OnLevelSelected?.Invoke(level));

            TextMeshProUGUI label = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.SetText(level.GetSelectListLabel());

            _spawnedButtons.Add(btn);
        }

        RebuildListLayout();
    }

    protected override void OnShow()
    {
        if (titleText != null)
            titleText.SetText("Select Level");

        RebuildListLayout();
    }

    private void RebuildListLayout()
    {
        if (levelButtonContainer is not RectTransform contentRt) return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt);

        Transform scrollParent = contentRt.parent;
        if (scrollParent is RectTransform viewportRt)
            LayoutRebuilder.ForceRebuildLayoutImmediate(viewportRt);
    }

    private static void ConfigureListItemRect(Button btn)
    {
        if (btn == null) return;

        RectTransform rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, ListItemHeight);
        rt.anchoredPosition = Vector2.zero;

        LayoutElement le = btn.GetComponent<LayoutElement>();
        if (le == null)
            le = btn.gameObject.AddComponent<LayoutElement>();

        le.preferredHeight = ListItemHeight;
        le.minHeight = ListItemHeight;
        le.flexibleWidth = 1f;
    }

    private void ClearButtons()
    {
        for (int i = 0; i < _spawnedButtons.Count; i++)
        {
            if (_spawnedButtons[i] != null)
                Destroy(_spawnedButtons[i].gameObject);
        }

        _spawnedButtons.Clear();
    }
}
