// DraggableTopping.cs
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableTopping : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [HideInInspector] public FruitType toppingType;

    private RectTransform rectTransform; // 원본 UI 아이템의 RectTransform
    private CanvasGroup canvasGroup;    // 이제 사용하지 않거나, 원본에만 사용될 수 있음
    // private Vector2 originalPosition; // 이제 사용하지 않음 (원본은 고정)
    // private Transform originalParent; // 이제 사용하지 않음 (원본은 고정)
    private Canvas canvas;

    private GameObject draggedObjectInstance = null;    // 드래그되는 분신 오브젝트
    private RectTransform draggedObjectRectTransform; // 분신의 RectTransform
    private Image originalImage;                      // 원본 UI 아이템의 Image 컴포넌트

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalImage = GetComponent<Image>();
        canvas = GetComponentInParent<Canvas>();

        // CanvasGroup은 드래그되는 분신에만 필요하다면 해당 부분에서 추가하거나,
        // 아예 알파값 조절을 안 할 것이므로 제거해도 무방합니다.
        // 여기서는 분신의 알파를 조절하지 않으므로 CanvasGroup 관련 로직은 제거합니다.
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (ToppingPlacementManager.Instance == null || originalImage == null || originalImage.sprite == null)
        {
            Debug.LogError("드래그 시작 불가: 매니저 또는 원본 이미지가 없습니다.");
            return;
        }

        // 1. 분신 생성
        draggedObjectInstance = new GameObject("Dragged_" + gameObject.name);
        Image draggedImage = draggedObjectInstance.AddComponent<Image>(); // Image 컴포넌트 먼저 추가
        draggedObjectRectTransform = draggedObjectInstance.GetComponent<RectTransform>(); // 그 다음 RectTransform 가져오기

        draggedObjectInstance.transform.SetParent(canvas.transform, true);
        draggedObjectInstance.transform.SetAsLastSibling();

        // 2. 분신에 이미지 및 크기 설정
        draggedImage.sprite = originalImage.sprite;
        
        // ★★★ 분신의 크기를 원본 UI 아이템의 크기와 동일하게 설정 ★★★
        draggedObjectRectTransform.sizeDelta = rectTransform.sizeDelta;
        
        // ★★★ 분신의 스케일을 원본 UI 아이템의 스케일과 동일하게 설정 ★★★
        // UI 요소는 보통 RectTransform의 sizeDelta로 크기를 조절하지만,
        // 만약 원본 UI 아이템 자체가 스케일(Transform.localScale)로 크기가 조절되었다면 이 값도 복사합니다.
        // 일반적으로 UI는 localScale이 (1,1,1)인 경우가 많습니다.
        draggedObjectRectTransform.localScale = rectTransform.localScale;


        // 드래그 시작 시 분신의 위치를 마우스 위치로 설정
        Vector2 localPointerPosition;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, eventData.position, canvas.worldCamera, out localPointerPosition))
        {
            draggedObjectRectTransform.localPosition = localPointerPosition;
        }
        
        draggedImage.raycastTarget = false; // 드롭 감지를 위해 분신은 레이캐스트 타겟에서 제외

        ToppingPlacementManager.Instance.StartDraggingTopping(this);
        Debug.Log(toppingType + " 토핑 드래그 시작 (분신 사용, 크기 복제됨)");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (draggedObjectRectTransform == null || canvas == null) return;

        Vector2 localPointerPosition;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, eventData.position, canvas.worldCamera, out localPointerPosition))
        {
            draggedObjectRectTransform.localPosition = localPointerPosition;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (draggedObjectInstance == null) // 분신이 없으면 아무것도 안 함
        {
            Debug.LogWarning("OnEndDrag: draggedObjectInstance가 null입니다.");
            ToppingPlacementManager.Instance?.StartDraggingTopping(null); // 매니저 상태 초기화
            return;
        }

        // 마우스 위치를 월드 좌표로 변환
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPosition.z = 0; // 2D 게임이므로 Z는 0으로 고정
        Debug.Log($"OnEndDrag: 마우스 월드 위치 = {mouseWorldPosition}");

        // 해당 위치에 있는 모든 2D 콜라이더 가져오기 (디버깅용)
        Collider2D[] hitColliders = Physics2D.OverlapPointAll(mouseWorldPosition);
        if (hitColliders.Length > 0)
        {
            foreach (Collider2D col in hitColliders)
            {
                Debug.Log($"OnEndDrag: OverlapPointAll 감지된 콜라이더: {col.gameObject.name}, 태그: {col.tag}");
            }
        }
        else
        {
            Debug.Log("OnEndDrag: OverlapPointAll 감지된 콜라이더 없음.");
        }

        // 실제로 사용할 콜라이더 (첫 번째 것 또는 특정 조건에 맞는 것)
        Collider2D hitCollider = Physics2D.OverlapPoint(mouseWorldPosition); // 특정 레이어만 검사하려면 LayerMask 추가 가능

        bool droppedOnSkewer = false;
        if (hitCollider != null)
        {
            Debug.Log($"OnEndDrag: OverlapPoint 감지된 단일 콜라이더: {hitCollider.gameObject.name}, 태그: {hitCollider.tag}");
            if (hitCollider.CompareTag("TanghuluDropZone"))
            {
                droppedOnSkewer = true;
            }
        }
        else
        {
            Debug.Log("OnEndDrag: OverlapPoint 감지된 단일 콜라이더 없음.");
        }


        if (droppedOnSkewer)
        {
            Debug.Log(toppingType + " 토핑이 꼬치 영역(" + hitCollider.name + ")에 놓임 (분신).");
            // ToppingPlacementManager.Instance가 null이 아닌지 여기서도 확인
            if (ToppingPlacementManager.Instance != null)
            {
                ToppingPlacementManager.Instance.PlaceToppingOnSkewer(toppingType);
            }
            else
            {
                Debug.LogError("ToppingPlacementManager.Instance가 null입니다. 토핑을 놓을 수 없습니다.");
            }
            if (draggedObjectInstance != null) Destroy(draggedObjectInstance);
        }
        else
        {
            Debug.Log(toppingType + " 토핑이 꼬치 영역 바깥에 놓임 (분신). 분신 파괴.");
            if (draggedObjectInstance != null) Destroy(draggedObjectInstance);
            ToppingPlacementManager.Instance?.StartDraggingTopping(null);
        }

        // 참조 정리
        draggedObjectInstance = null;
        draggedObjectRectTransform = null;
    }
}