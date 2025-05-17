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

    public void LoseHeart()
    {
        currentHearts--;
        UpdateHeartUI();

        if (currentHearts <= 0)
        {
            Debug.Log("게임 오버!");
            SceneManager.LoadScene(gameOverSceneName);
            // 게임 오버 로직
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