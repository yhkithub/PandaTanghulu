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
        if (CustomerOrderManager.Instance != null && CustomerOrderManager.Instance.isTutorialActive)
        {
            Debug.Log("튜토리얼 중이므로 하트가 차감되지 않습니다.");
            // 여기에 "이런! 순서가 틀렸어요. 주문서를 다시 보고 만들어주세요!" 같은
            // 튜토리얼용 피드백 UI를 잠시 보여주는 로직을 추가할 수 있습니다.
            // CustomerOrderManager.Instance.ShowTutorialMessage("이런! 과일 순서가 다른 것 같아요. 주문서를 잘 보고 다시 시도해보세요!");
            return;
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