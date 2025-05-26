// HeartManager.cs 수정 예시
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic; // List 사용 시 필요
using System; // Action 사용 시 필요

public class HeartManager : MonoBehaviour
{
    public int maxHearts = 3;
    private int currentHearts;
    // public List<Image> heartImages; // 제거 또는 주석
    // public Sprite fullHeart;       // 제거 또는 주석
    // public Sprite emptyHeart;      // 제거 또는 주석
    public string gameOverSceneName = "GameOverScene";

    public static HeartManager Instance { get; private set; }

    public event Action<int> OnHeartsChanged; // 하트 개수 변경 시 호출될 이벤트

    public int CurrentHearts => currentHearts; // 외부에서 현재 하트 개수를 읽을 수 있도록

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // currentHearts는 게임 시작 시 또는 새로하기 시 초기화 필요
        // 여기서는 DontDestroyOnLoad이므로, 게임의 첫 시작을 감지하는 로직이 없다면
        // PlayerPrefs를 사용하거나, TitleScene 등에서 명시적으로 초기화하는 것이 좋습니다.
        // 지금은 간단하게 maxHearts로 시작한다고 가정합니다.
        InitializeHearts();
    }

    public void InitializeHearts() // 새로하기 또는 게임 시작 시 호출
    {
        currentHearts = maxHearts;
        OnHeartsChanged?.Invoke(currentHearts); // 이벤트 호출
        // UpdateHeartUI(); // 제거
    }

    public void LoseHeart()
    {
        if (CustomerOrderManager.Instance != null && CustomerOrderManager.Instance.isTutorialActive)
        {
            Debug.Log("튜토리얼 중이므로 하트가 차감되지 않습니다.");
            return;
        }

        if (currentHearts > 0) // 0보다 클 때만 감소
        {
            currentHearts--;
            OnHeartsChanged?.Invoke(currentHearts); // 이벤트 호출
            // UpdateHeartUI(); // 제거
            Debug.Log("하트 감소. 현재 하트: " + currentHearts);
        }


        if (currentHearts <= 0)
        {
            Debug.Log("게임 오버!");
            // SceneSwitcher 사용 권장
            if (SceneSwitcher.Instance != null)
            {
                SceneSwitcher.Instance.LoadScene(gameOverSceneName);
            }
            else
            {
                SceneManager.LoadScene(gameOverSceneName);
            }
        }
    }

    // UpdateHeartUI() 함수는 제거됨
}