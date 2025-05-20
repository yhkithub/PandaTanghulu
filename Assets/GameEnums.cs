// GameEnums.cs
using UnityEngine; // DialogueEntry의 TextArea 때문에 필요

public enum FruitType
{
    None,
    귤,
    바나나,
    키위,
    파인애플,
    딸기,
    토마토,
    샤인머스켓,
    체리,
    블루베리,
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

[System.Serializable]
public class DialogueEntry
{
    public enum Speaker { Kiki, Pupu } // 필요에 따라 다른 화자 추가
    public Speaker speaker;
    [TextArea(3, 10)] public string line;
}

public static class GameInfoHolder
{
    public static int CustomerIndexToLoad = 0;
}