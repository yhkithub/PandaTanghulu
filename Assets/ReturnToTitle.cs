using UnityEngine;

public class ReturnToTitle : MonoBehaviour
{
    // 게임 오버 씬의 버튼에서는 체크, 클리어 씬에서는 체크 해제
    public bool resetOnReturn = false; 

    public void GoToTitleScene()
    {
        if (resetOnReturn)
        {
            Debug.Log("게임을 초기화합니다.");

            // 1. 영구적인 스테이지 클리어 데이터 초기화
            if (StageDataManager.Instance != null)
            {
                // StageDataManager에 있는 실제 함수 호출
                StageDataManager.Instance.ResetAllStageProgress();
            }

            // 2. 하트 초기화
            if (HeartManager.Instance != null)
            {
                HeartManager.Instance.InitializeHearts();
            }

            // 3. 과일 꼬치 임시 데이터 초기화 (SkewerManager가 담당)
            // SkewerManager 스크립트에 ClearSkewer() 함수가 있어야 합니다.
            // if (SkewerManager.Instance != null)
            // {
            //     SkewerManager.Instance.ClearSkewer();
            // }

            // 참고: 만약 다른 매니저에 탕후루 제작 관련 임시 데이터가 있다면
            // 이 곳에서 해당 매니저의 초기화 함수도 호출해주어야 합니다.
        }

        if (SceneSwitcher.Instance != null)
        {
            SceneSwitcher.Instance.LoadScene("TitleScene");
        }
    }
}