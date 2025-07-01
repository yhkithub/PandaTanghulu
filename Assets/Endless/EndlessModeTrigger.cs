// EndlessModeTrigger.cs

using UnityEngine;
using UnityEngine.EventSystems;

public class EndlessModeTrigger : MonoBehaviour, IPointerClickHandler
{
    private int tapCount = 0;
    private const int requiredTaps = 10;

    public void OnPointerClick(PointerEventData eventData)
    {
        // 모든 스테이지가 클리어되었는지 확인
        if (StageDataManager.Instance != null && StageDataManager.Instance.IsGameFullyCleared())
        {
            tapCount++;
            Debug.Log($"무한 모드 트리거 탭: {tapCount}/{requiredTaps}");

            if (tapCount >= requiredTaps)
            {
                Debug.Log("무한 모드 진입 신호 발생!");
                tapCount = 0; // 카운트 초기화

                if (EndlessModeController.Instance != null)
                {
                    EndlessModeController.Instance.StartEndlessMode();
                }
                else
                {
                    Debug.LogError("EndlessModeController 인스턴스를 찾을 수 없습니다!");
                }
            }
        }
        else
        {
            // 아직 모든 스테이지를 클리어하지 않았을 경우
            Debug.Log("무한 모드 진입 조건 미충족: 모든 스테이지를 클리어해야 합니다.");
            tapCount = 0; // 조건을 만족하지 않으면 탭 카운트 초기화
        }
    }
}