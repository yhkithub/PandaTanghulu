// GameEnums.cs
using UnityEngine; // DialogueEntry의 TextArea 때문에 필요

public enum FruitType
{
    None,
    귤,
    키위,
    파인애플,
    딸기,
    토마토,
    샤인머스켓,
    체리,
    블루베리,
    바나나,
    뼈다귀쿠키,
    풀,
    당근,
    나비장식,
    생선장식,
    치즈
}

public enum GameState
{
    TutorialDisplay,
    Playing,
    Paused,
    GameOver
}

public enum MiniGameStep
{
    NotStarted,
    FruitSkewering,
    SugarBoiling,
    SugarCoating,
    ToppingPlacement
}

public enum CustomerSpriteState
{
    Default,
    Smiling
}

[System.Serializable]
public class DialogueEntry
{
    public enum Speaker
    {
        customer, // 'Kiki'를 'Customer'로 변경
        Pupu
    }
    public Speaker speaker;
    [TextArea(3, 10)] public string line;

    public CustomerSpriteState spriteState = CustomerSpriteState.Default;

}

public static class GameInfoHolder
{
    public static int CustomerIndexToLoad = 0;
    public static bool OpenStageSelectPanelOnLoad = false;
    public static bool TutorialWasJustCompleted = false; // 이 플래그 추가
}