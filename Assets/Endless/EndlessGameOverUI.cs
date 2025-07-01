using UnityEngine;
using TMPro; // TextMeshPro를 사용하기 위해 필요

public class EndlessGameOverUI : MonoBehaviour
{
    [Header("UI 연결")]
    public TextMeshProUGUI finalScoreText; // 최종 점수를 표시할 UI 텍스트
    public TextMeshProUGUI highScoreText; // 최고 점수를 표시할 UI 텍스트

    void Start()
    {
        // PlayerPrefs에서 마지막 점수와 최고 점수를 불러옵니다.
        // 저장된 값이 없을 경우 기본값으로 0을 사용합니다.
        int lastScore = PlayerPrefs.GetInt("LastEndlessScore", 0);
        int highScore = PlayerPrefs.GetInt("EndlessHighScore", 0);

        // 불러온 점수를 UI 텍스트에 반영합니다.
        if (finalScoreText != null)
        {
            finalScoreText.text = "SCORE : " + lastScore;
        }

        if (highScoreText != null)
        {
            highScoreText.text = "HIGH SCORE : " + highScore;
        }
    }
}