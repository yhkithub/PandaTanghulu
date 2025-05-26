// StageDataManager.cs (새로운 C# 스크립트)
using UnityEngine;
using System.Collections.Generic; // List 사용 시

public class StageDataManager : MonoBehaviour
{
    public static StageDataManager Instance { get; private set; }

    // 각 손님(스테이지)의 클리어 여부를 저장할 키 값의 접두사
    private const string STAGE_CLEARED_PREFIX = "StageCleared_";
    // 총 스테이지(손님) 수 (CustomerOrderManager의 allCustomerOrders.Count와 동기화 필요)
    public int totalStages = 8; // 예시: 총 8명의 손님

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

    // 특정 스테이지가 클리어되었는지 확인하는 함수
    public bool IsStageCleared(int stageIndex)
    {
        if (stageIndex < 0 || stageIndex >= totalStages) return false;
        return PlayerPrefs.GetInt(STAGE_CLEARED_PREFIX + stageIndex, 0) == 1;
    }

    // 특정 스테이지를 클리어 상태로 저장하는 함수
    public void SetStageCleared(int stageIndex)
    {
        if (stageIndex < 0 || stageIndex >= totalStages) return;
        PlayerPrefs.SetInt(STAGE_CLEARED_PREFIX + stageIndex, 1);
        PlayerPrefs.Save();
        Debug.Log("Stage " + stageIndex + " 클리어 저장됨.");

        // 다음 스테이지 해금 로직 (선택 사항)
        int nextStageIndex = stageIndex + 1;
        if (nextStageIndex < totalStages && !IsStageUnlocked(nextStageIndex))
        {
            UnlockStage(nextStageIndex);
        }
    }

    // 특정 스테이지가 해금되었는지 확인하는 함수 (첫 스테이지는 항상 해금)
    public bool IsStageUnlocked(int stageIndex)
    {
        if (stageIndex == 0) return true; // 첫 번째 스테이지는 항상 해금
        if (stageIndex < 0 || stageIndex >= totalStages) return false;
        // 이전 스테이지가 클리어되었으면 해금된 것으로 간주
        return IsStageCleared(stageIndex - 1);
    }

    // 특정 스테이지를 해금 상태로 만드는 (실제 저장되는 값은 없음, IsStageCleared를 통해 간접 확인)
    public void UnlockStage(int stageIndex)
    {
        if (stageIndex < 0 || stageIndex >= totalStages) return;
        // 여기서는 별도로 저장할 필요 없이, 이전 스테이지 클리어 여부로 판단합니다.
        // 만약 명시적인 "해금" 상태 저장이 필요하면 PlayerPrefs에 추가 키 사용.
        Debug.Log("Stage " + stageIndex + " 해금됨 (이전 스테이지 클리어 기준).");
    }

    // 모든 스테이지 진행 상황 초기화 (새로하기 시 호출)
    public void ResetAllStageProgress()
    {
        for (int i = 0; i < totalStages; i++)
        {
            PlayerPrefs.DeleteKey(STAGE_CLEARED_PREFIX + i);
        }
        PlayerPrefs.Save();
        Debug.Log("모든 스테이지 진행 상황이 초기화되었습니다.");
        // 남은 하트 수도 초기화 (HeartManager에서 처리하거나 여기서 PlayerPrefs로 직접)
        // PlayerPrefs.DeleteKey("CurrentHearts"); // 예시
    }
}