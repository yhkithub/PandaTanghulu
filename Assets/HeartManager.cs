// HeartManager.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class HeartManager : MonoBehaviour
{
    public int maxHearts = 3;
    private int currentHearts;
    public List<Image> heartImages; // 하트 UI 이미지 리스트 (Inspector에서 연결)
    public Sprite fullHeart;
    public Sprite emptyHeart;
    public string gameOverSceneName = "GameOverScene"; // 게임 오버 씬 이름

    private static HeartManager _instance;
    public static HeartManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<HeartManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("HeartManager");
                    _instance = go.AddComponent<HeartManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        currentHearts = maxHearts;
        UpdateHeartUI();
    }

    // HeartManager.cs - LoseHeart() 함수 수정
    public void LoseHeart()
    {
        // GameManager 또는 CustomerOrderManager를 통해 튜토리얼 상태 확인
        // 예시: if (GameManager.Instance.isTutorialActive)
        if (CustomerOrderManager.Instance.isTutorialActive) // CustomerOrderManager에 isTutorialActive가 있다고 가정
        {
            Debug.Log("튜토리얼 중이므로 하트가 차감되지 않습니다.");
            // 튜토리얼 중 실패 피드백은 줄 수 있지만, 실제 게임오버로 이어지지 않음
            // 예를 들어, 실패했다는 UI 메시지만 잠깐 보여주고 계속 진행
            return; // 하트 차감 및 게임오버 로직 실행 안 함
        }

        currentHearts--;
        UpdateHeartUI();

        if (currentHearts <= 0)
        {
            Debug.Log("게임 오버!");
            SceneManager.LoadScene(gameOverSceneName);
        }
    }

    void UpdateHeartUI()
    {
        for (int i = 0; i < heartImages.Count; i++)
        {
            if (i < currentHearts)
            {
                heartImages[i].sprite = fullHeart;
            }
            else
            {
                heartImages[i].sprite = emptyHeart;
            }
        }
    }
}