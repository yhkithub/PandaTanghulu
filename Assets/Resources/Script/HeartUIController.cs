using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class HeartUIController : MonoBehaviour
{
    public List<Image> heartImagesInScene; // Inspector에서 해당 씬의 하트 Image UI들을 연결
    public Sprite fullHeartSpriteInScene;   // Inspector에서 해당 씬의 꽉 찬 하트 스프라이트 연결
    public Sprite emptyHeartSpriteInScene;  // Inspector에서 해당 씬의 빈 하트 스프라이트 연결

    void Start()
    {
        if (HeartManager.Instance != null)
        {
            HeartManager.Instance.OnHeartsChanged += UpdateDisplay; // 이벤트 구독
            UpdateDisplay(HeartManager.Instance.CurrentHearts); // 초기 UI 업데이트
        }
        else
        {
            Debug.LogError("HeartUIController: HeartManager.Instance를 찾을 수 없습니다.");
            gameObject.SetActive(false); // 매니저 없으면 UI 컨트롤러 비활성화
        }
    }

    void OnDestroy() // 오브젝트 파괴 시 이벤트 구독 해제 (메모리 누수 방지)
    {
        if (HeartManager.Instance != null)
        {
            HeartManager.Instance.OnHeartsChanged -= UpdateDisplay;
        }
    }

    void UpdateDisplay(int currentHeartCount)
    {
        if (heartImagesInScene == null || fullHeartSpriteInScene == null || emptyHeartSpriteInScene == null)
        {
            Debug.LogError("HeartUIController: UI 요소 또는 스프라이트가 Inspector에 연결되지 않았습니다.");
            return;
        }

        for (int i = 0; i < heartImagesInScene.Count; i++)
        {
            if (i < currentHeartCount)
            {
                heartImagesInScene[i].sprite = fullHeartSpriteInScene;
            }
            else
            {
                heartImagesInScene[i].sprite = emptyHeartSpriteInScene;
            }
            heartImagesInScene[i].gameObject.SetActive(i < HeartManager.Instance.maxHearts); // 최대 하트 개수만큼만 보이도록
        }
    }
}