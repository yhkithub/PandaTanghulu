using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 이 스크립트는 드래그 가능한 UI Image GameObject에 직접 추가해야 합니다.
[RequireComponent(typeof(Image))] // Image 컴포넌트가 반드시 필요합니다.
public class DraggableTanghuluItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup; // 드래그 중 투명도 조절 등에 사용 (선택 사항)
    private Vector3 originalPosition; // 드래그 시작 전 원래 위치 (월드 좌표)
    private bool 원래RaycastTarget상태; // 드래그 시작 전 원래 Raycast Target 상태

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        // 가장 가까운 부모 Canvas를 찾습니다. UI 요소는 Canvas 하위에 있어야 합니다.
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("DraggableTanghuluItem: 부모 Canvas를 찾을 수 없습니다! 이 UI 요소가 Canvas 하위에 있는지 확인해주세요.");
            enabled = false; // Canvas 없이는 작동 불가
            return;
        }

        // CanvasGroup은 드래그 중인 아이템이 다른 UI 요소 위에 그려지도록 하거나,
        // Raycast를 무시하도록 하는 데 유용합니다. (선택 사항)
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!enabled) return; // 스크립트 비활성화 시 작동 안 함

        Debug.Log(gameObject.name + " 드래그 시작!");
        originalPosition = rectTransform.position; // 현재 월드 위치 저장

        Image imageComponent = GetComponent<Image>();
        if (imageComponent != null)
        {
            원래RaycastTarget상태 = imageComponent.raycastTarget;
            imageComponent.raycastTarget = false; // 드래그 중에는 다른 UI 요소와의 상호작용을 위해 잠시 비활성화
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.7f; // 드래그 중 약간 투명하게 (선택 사항)
            canvasGroup.blocksRaycasts = false; // 드롭 대상이 이벤트를 받을 수 있도록 Raycast 차단 해제
        }

        // CustomerPresentationManager에 드래그 시작을 알릴 수 있습니다 (필요하다면).
        // CustomerPresentationManager.Instance?.NotifyDragStarted(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!enabled) return;

        // Canvas Render Mode에 따라 좌표 변환
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            // Screen Space - Overlay 모드에서는 eventData.position을 직접 사용
            rectTransform.position = eventData.position;
        }
        else
        {
            // Screen Space - Camera 또는 World Space 모드
            Vector2 localPointerPosition;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform, // Canvas의 RectTransform
                eventData.position,                // 현재 마우스/터치 스크린 좌표
                canvas.worldCamera,                // Canvas에 연결된 카메라 (Screen Space - Camera) 또는 씬 카메라 (World Space)
                out localPointerPosition))         // 변환된 로컬 좌표
            {
                rectTransform.localPosition = localPointerPosition;
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!enabled) return;

        Debug.Log(gameObject.name + " 드래그 종료!");

        Image imageComponent = GetComponent<Image>();
        if (imageComponent != null)
        {
            imageComponent.raycastTarget = 원래RaycastTarget상태; // 원래 Raycast Target 상태로 복원
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f; // 원래 투명도로 복원
            canvasGroup.blocksRaycasts = true; // Raycast 차단 원래대로
        }

        // CustomerPresentationManager에게 드롭 처리를 요청합니다.
        // CustomerPresentationManager.Instance가 null이 아닐 때만 호출해야 합니다.
        if (CustomerPresentationManager.Instance != null)
        {
            CustomerPresentationManager.Instance.HandleTanghuluDropped(this, eventData);
        }
        else
        {
            Debug.LogError("DraggableTanghuluItem: CustomerPresentationManager.Instance를 찾을 수 없습니다. 드롭 처리를 할 수 없습니다.");
            // 드롭 처리를 못하면 원래 위치로 되돌리는 것이 안전합니다.
            rectTransform.position = originalPosition;
        }
    }

    // CustomerPresentationManager에서 호출하여 드래그 실패 시 원래 위치로 되돌립니다.
    public void ResetToOriginalPosition()
    {
        if (rectTransform != null)
        {
            rectTransform.position = originalPosition;
            Debug.Log(gameObject.name + " 원래 위치로 복귀.");
        }
    }
}
