// DraggableTopping.cs
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableTopping : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private CircleCollider2D draggedObjectCollider; // 분신의 콜라이더
    private Rigidbody2D draggedObjectRigidbody;   // 분신의 리지드바디
    private bool isDragging = false;              // 현재 드래그 중인지 여부
    private bool canPlaceTopping = false;         // 꼬치 위에 있는지 여부 (OnTriggerStay2D로 업데이트)
    private Collider2D currentDropZoneCollider = null; // 현재 접촉 중인 드롭존 콜라이더

    public float toppingColliderRadius = 0.5f; // 분신 콜라이더 크기 (Inspector에서 조절 가능)

    private DraggedToppingCollider draggedObjectColliderScript;


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


        CircleCollider2D draggedCollider = draggedObjectInstance.AddComponent<CircleCollider2D>();
        draggedCollider.isTrigger = true;
        draggedCollider.radius = 0.5f; // 적절한 반지름 설정 (또는 toppingColliderRadius 변수 사용)

        Rigidbody2D draggedRigidbody = draggedObjectInstance.AddComponent<Rigidbody2D>();
        draggedRigidbody.bodyType = RigidbodyType2D.Kinematic;

        draggedObjectColliderScript = draggedObjectInstance.AddComponent<DraggedToppingCollider>();
        draggedObjectColliderScript.originalDraggable = this; // 원본 DraggableTopping 참조 전달

        isDragging = true; // isDragging 플래그는 이제 DraggableTopping에 없어도 될 수 있음 (분신 스크립트가 관리)
        // canPlaceTopping 및 currentDropZoneCollider도 분신 스크립트가 관리

        ToppingPlacementManager.Instance.StartDraggingTopping(this);
        Debug.Log(toppingType + " 토핑 드래그 시작 (분신+콜라이더 사용)");

    }

    public void OnDrag(PointerEventData eventData)
    {
        if (draggedObjectRectTransform == null || canvas == null) return;

        Vector2 localPointerPosition;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, eventData.position, canvas.worldCamera, out localPointerPosition))
        {
            // UI 좌표계에서 월드 좌표계로 변환하여 물리 오브젝트 위치 설정
            draggedObjectRectTransform.localPosition = localPointerPosition;
            // 분신의 실제 월드 위치를 물리 시스템이 사용하도록 업데이트
            // draggedObjectInstance.transform.position = canvas.transform.TransformPoint(localPointerPosition); // Canvas가 ScreenSpace-Overlay가 아닐 때
            // ScreenSpace-Overlay인 경우, 마우스 포지션을 직접 월드 좌표로 변환하여 사용해야 함
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            draggedObjectInstance.transform.position = new Vector3(mouseWorldPos.x, mouseWorldPos.y, 0);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        bool droppedOnSkewer = false;
        Collider2D dropZoneCol = null;

        if (draggedObjectColliderScript != null)
        {
            droppedOnSkewer = draggedObjectColliderScript.IsOverDropZone();
            if (droppedOnSkewer)
            {
                dropZoneCol = draggedObjectColliderScript.GetCurrentDropZoneCollider();
            }
        }

        if (droppedOnSkewer && dropZoneCol != null)
        {
            Debug.Log(toppingType + " 토핑이 꼬치 영역(" + dropZoneCol.name + ")에 놓임 (분신 콜라이더 스크립트 사용).");
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
            if (ToppingPlacementManager.Instance != null)
                ToppingPlacementManager.Instance.StartDraggingTopping(null);
        }

        if (draggedObjectInstance != null) Destroy(draggedObjectInstance);
        draggedObjectInstance = null;
        draggedObjectColliderScript = null; // 참조 정리
        ToppingPlacementManager.Instance?.StartDraggingTopping(null);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!isDragging || draggedObjectInstance == null || other.gameObject == draggedObjectInstance) return;

        if (other.CompareTag("TanghuluDropZone"))
        {
            // OnEndDrag에서 최종 판단하므로, 여기서는 canPlaceTopping 상태만 유지
            if (!canPlaceTopping) // 혹시 Enter를 놓쳤을 경우를 대비
            {
                Debug.Log($"DraggableTopping (Stay): {toppingType}이(가) {other.name} 영역에 있음.");
                canPlaceTopping = true;
                currentDropZoneCollider = other;
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!isDragging || draggedObjectInstance == null || other.gameObject == draggedObjectInstance) return;

        if (other.CompareTag("TanghuluDropZone"))
        {
            Debug.Log($"DraggableTopping: {toppingType}이(가) {other.name} 영역에서 나옴.");
            if (other == currentDropZoneCollider) // 현재 감지된 드롭존에서 나간 경우에만 초기화
            {
                canPlaceTopping = false;
                currentDropZoneCollider = null;
            }
        }
    }
}