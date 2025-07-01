// DraggableTopping.cs
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableTopping : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private CircleCollider2D draggedObjectCollider; // 분신의 콜라이더
    private Rigidbody2D draggedObjectRigidbody;   // 분신의 리지드바디
    public float toppingColliderRadius = 0.5f; // 분신 콜라이더 크기 (Inspector에서 조절 가능)

    private DraggedToppingCollider draggedObjectColliderScript;


    [HideInInspector] public FruitType toppingType;

    private RectTransform draggedObjectRectTransform; // 분신의 RectTransform
    private Image originalImage;                      // 원본 UI 아이템의 Image 컴포넌트

    private Vector3 startPosition;
    private CanvasGroup canvasGroup;
    private Canvas canvas; // 드래그 중인 UI 요소가 속한 캔버스
    private RectTransform rectTransform;
    private Camera mainCamera; // 월드 좌표 변환을 위해 메인 카메라 참조 추가


    // 토핑을 놓을 수 있는 상태인지 확인하는 플래그
    private bool canPlaceTopping = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        // GetComponentInParent<Canvas>() 는 그대로 둡니다.
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        // 메인 카메라를 캐싱합니다.
        mainCamera = Camera.main;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 시작 위치를 월드 좌표 기준으로 저장합니다.
        startPosition = transform.position;
        canvasGroup.blocksRaycasts = false;
        transform.SetAsLastSibling();
        canPlaceTopping = false;
        
        Debug.Log(toppingType + " 토핑 드래그 시작 (원본 이동 방식)");
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 🟩🟩🟩 이 부분이 핵심적인 수정 부분입니다! 🟩🟩🟩
        // 마우스의 스크린 좌표를 게임 월드 좌표로 변환합니다.
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(eventData.position);
        // z 좌표는 0으로 고정하여 2D 평면에 있도록 합니다.
        worldPosition.z = 0f;
        
        // 아이템의 실제 월드 위치를 업데이트합니다.
        transform.position = worldPosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        if (canPlaceTopping)
        {
            Debug.Log(toppingType + " 토핑이 꼬치 영역에 놓임.");
            ToppingPlacementManager.Instance.PlaceToppingOnSkewer(toppingType);
            Destroy(gameObject); // 성공 시 파괴
        }
        else
        {
            Debug.Log(toppingType + " 토핑이 꼬치 영역 바깥에 놓임. UI를 리셋합니다.");
            
            // ❌ 기존 위치 복귀 코드 삭제
            // transform.position = startPosition; 

            // ✅ 실패 시에도 ToppingPlacementManager가 UI를 다시 그리도록 하고, 자신은 파괴
            ToppingPlacementManager.Instance.ResetToppingChoices(); 
            Destroy(gameObject); 
        }
    }

    // --- OnTriggerEnter2D, OnTriggerExit2D는 기존 코드 그대로 둡니다. ---
    void OnTriggerEnter2D(Collider2D other)
    {
        // [수정] "TanghuluSkewer" 또는 "TanghuluDropZone" 태그를 모두 감지합니다.
        if (other.CompareTag("TanghuluSkewer") || other.CompareTag("TanghuluDropZone"))
        {
            Debug.Log(toppingType + "이(가) " + other.name + " 영역에 들어옴.");
            canPlaceTopping = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        // [수정] "TanghuluSkewer" 또는 "TanghuluDropZone" 태그를 모두 감지합니다.
        if (other.CompareTag("TanghuluSkewer") || other.CompareTag("TanghuluDropZone"))
        {
            Debug.Log(toppingType + "이(가) " + other.name + " 영역에서 나옴.");
            canPlaceTopping = false;
        }
    }
}