using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 判断 UI 是否应拦截玩法输入（菜单、暂停等），避免点击按钮穿透到炮台。
/// </summary>
public static class UIInputGuard
{
    public static bool ShouldBlockGameplayInput()
    {
        if (UIManager.Instance != null && UIManager.Instance.IsBlockingGameplayInput)
            return true;

        return IsPointerOverUI();
    }

    public static bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
            return false;

        return EventSystem.current.IsPointerOverGameObject();
    }
}
