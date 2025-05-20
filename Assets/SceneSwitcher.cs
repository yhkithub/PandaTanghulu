// SceneSwitcher.cs (새로운 C# 스크립트)
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    public static SceneSwitcher Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬 전환 시에도 이 오브젝트 유지
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 과일 꽂기 게임 씬을 로드하는 함수
    // 씬 이름은 Unity Build Settings에 추가된 이름과 정확히 일치해야 합니다.
    public void LoadFruitCatchingScene(string sceneName = "FruitCatchingGameScene") // 기본 씬 이름을 설정
    {
        SceneManager.LoadScene(sceneName);
    }

    public void LoadDialogueScene(string sceneName = "DialogueScene")
    {
        SceneManager.LoadScene(sceneName);
    }

    // 기타 필요한 씬 로드 함수들...
}