// SceneSwitcher.cs
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
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadScene(string sceneName) // ★★★ 일반 씬 로드 함수 ★★★
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("SceneSwitcher: 로드할 씬 이름이 비어있습니다!");
            return;
        }
        Debug.Log("SceneSwitcher: " + sceneName + " 씬 로드 중...");
        SceneManager.LoadScene(sceneName);
    }

    public void LoadFruitCatchingScene(string sceneName = "FruitCatchingGameScene")
    {
        LoadScene(sceneName); // 일반 LoadScene 함수 재활용
    }

    public void LoadDialogueScene(string sceneName = "DialogueScene") // 현재는 ShopScene을 DialogueScene으로 사용 중
    {
        LoadScene(sceneName); // 일반 LoadScene 함수 재활용
    }

    // 필요하다면 다른 특정 씬 로드 함수들...
    // public void LoadSugarBoilingScene(string sceneName = "SugarBoilingScene")
    // {
    //     LoadScene(sceneName);
    // }
}