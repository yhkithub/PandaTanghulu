// GameModeManager.cs
using UnityEngine;

public static class GameModeManager
{
    public static bool IsEndlessMode { get; set; } = false;

    // 게임이 타이틀 화면으로 돌아올 때 호출하여 초기화합니다.
    public static void ResetMode()
    {
        IsEndlessMode = false;
    }
}