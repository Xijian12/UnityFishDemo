using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI 面板基类：统一 Show/Hide，全屏 RaycastBlocker，显示时置顶拦截射线。
/// </summary>
public abstract class UIPanelBase : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private bool useRaycastBlocker = true;

    private Image _raycastBlocker;

    protected GameObject PanelRoot => panelRoot != null ? panelRoot : gameObject;

    public bool IsVisible => PanelRoot.activeSelf;

    protected virtual bool ShouldCreateRaycastBlocker => useRaycastBlocker;

    protected virtual void Awake()
    {
        if (ShouldCreateRaycastBlocker)
            EnsureRaycastBlocker();
    }

    public virtual void Show()
    {
        PanelRoot.transform.SetAsLastSibling();
        PanelRoot.SetActive(true);
        OnShow();
    }

    public virtual void Hide()
    {
        OnHide();
        PanelRoot.SetActive(false);
    }

    protected virtual void OnShow() { }
    protected virtual void OnHide() { }

    private void EnsureRaycastBlocker()
    {
        if (_raycastBlocker != null) return;

        Transform existing = PanelRoot.transform.Find("RaycastBlocker");
        if (existing != null)
        {
            _raycastBlocker = existing.GetComponent<Image>();
            return;
        }

        var go = new GameObject("RaycastBlocker", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(PanelRoot.transform, false);
        go.transform.SetAsFirstSibling();

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        _raycastBlocker = go.GetComponent<Image>();
        _raycastBlocker.color = new Color(0f, 0f, 0f, 0.01f);
        _raycastBlocker.raycastTarget = true;
    }
}
