// Skewer2DController.cs
using UnityEngine;

public class Skewer2DController : MonoBehaviour
{
    public float moveSpeed = 5f; // 마우스 민감도 등으로 활용 가능 (현재는 직접 위치 지정)
    public float fixedYPosition = -4f;
    public float minXClamp = -8f; // 꼬치 이동 가능 최소 X
    public float maxXClamp = 8f;  // 꼬치 이동 가능 최대 X

    private bool isInTrashZone = false; // 꼬치가 현재 쓰레기통 영역에 있는지 여부
    private SkewerManager skewerManager; // 꼬치 내용물 관리를 위해 참조

    void Start()
    {
        skewerManager = GetComponent<SkewerManager>(); // 같은 게임오브젝트에 있다고 가정
        if (skewerManager == null)
        {
            Debug.LogError("Skewer2DController: SkewerManager를 찾을 수 없습니다!");
        }
    }

    void Update()
    {
        // 1. 마우스 위치로 꼬치 이동 (X축만)
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float targetX = Mathf.Clamp(mousePosition.x, minXClamp, maxXClamp);
        transform.position = new Vector2(targetX, fixedYPosition);

        // 2. 버리기 입력 처리 (마우스 왼쪽 버튼 클릭)
        if (isInTrashZone && Input.GetMouseButtonDown(0)) // 마우스 왼쪽 버튼 클릭 시
        {
            if (skewerManager != null && skewerManager.collectedFruitsOnSkewer.Count > 0) // 꽂힌 과일이 있을 때만
            {
                Debug.Log("플레이어가 꼬치를 쓰레기통에 버립니다.");

                // 튜토리얼 중이 아닐 때만 하트 차감 (SkewerManager에서 처리 또는 여기서 직접)
                if (CustomerOrderManager.Instance != null && !CustomerOrderManager.Instance.isTutorialActive)
                {
                    if (HeartManager.Instance != null)
                    {
                        HeartManager.Instance.LoseHeart();
                    }
                }
                else if (CustomerOrderManager.Instance != null && CustomerOrderManager.Instance.isTutorialActive)
                {
                    // 튜토리얼 메시지 (예: "잘못된 과일을 버렸네요! 다시 시도해보세요.")
                    // CustomerOrderManager.Instance.ShowTutorialMessage("꼬치를 비웠어요! 다시 만들어보세요.");
                    Debug.Log("튜토리얼 중: 꼬치 내용물을 버렸지만 하트는 차감되지 않습니다.");
                }
                skewerManager.ClearSkewer(); // 꼬치 내용물 비우기
            }
        }
    }

    // 꼬치가 쓰레기통 영역(Trigger)에 들어갔을 때 호출됨
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("TrashCan"))
        {
            isInTrashZone = true;
            Debug.Log("꼬치가 쓰레기통 영역에 들어감");
            // (선택 사항) 쓰레기통 위에 있을 때 시각적 피드백 (예: 꼬치 색 변경, 쓰레기통 하이라이트)
        }
    }

    // 꼬치가 쓰레기통 영역(Trigger)에서 나왔을 때 호출됨
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("TrashCan"))
        {
            isInTrashZone = false;
            Debug.Log("꼬치가 쓰레기통 영역에서 나옴");
            // (선택 사항) 시각적 피드백 해제
        }
    }
}