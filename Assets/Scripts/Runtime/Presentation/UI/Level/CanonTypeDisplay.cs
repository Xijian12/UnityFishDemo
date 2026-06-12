/// <summary>
/// 炮台类型展示文案。
/// </summary>
public static class CanonTypeDisplay
{
    public static string GetLevelText(CanonType type)
    {
        return type switch
        {
            CanonType.Single => "Single Cannon Lv.1",
            CanonType.Double => "Double Cannon Lv.2",
            CanonType.Triple => "Triple Cannon Lv.3",
            CanonType.Quadruple => "Quad Cannon Lv.4",
            CanonType.Quintuple => "Quint Cannon Lv.5",
            _ => "Cannon"
        };
    }

    public static int GetLevelNumber(CanonType type) => (int)type + 1;
}
