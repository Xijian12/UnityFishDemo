using System;

/// <summary>
/// 关卡 UI 对外接口：菜单、HUD、暂停、结算与 LevelManager 之间的契约。
/// 外部系统（引导、成就、广告等）可订阅事件或调用请求方法。
/// </summary>
public interface ILevelUIService
{
    event Action<LevelConfig> OnLevelPresentationStarted;
    event Action<LevelState, LevelRuntime> OnLevelPresentationStateChanged;
    event Action<LevelRuntime, float> OnHudRefresh;

    void ShowMainMenu();
    void ShowLevelSelect();
    void RequestStartLevel(LevelConfig config);
    void RequestPause();
    void RequestResume();
    void RequestRestart();
    void ReturnToMainMenu();
}
