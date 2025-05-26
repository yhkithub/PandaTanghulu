// DraggedToppingCollider.cs (분신 오브젝트에 붙일 스크립트)
using UnityEngine;

public class DraggedToppingCollider : MonoBehaviour
{
    public DraggableTopping originalDraggable { get; set; } // 원본 DraggableTopping 참조

    private bool isInDropZone = false;
    private Collider2D currentDropZoneColliderRef = null;

    // 이 스크립트는 분신 오브젝트에 붙으므로, 분신의 콜라이더가 사용됩니다.
    // 분신 생성 시 CircleCollider2D와 Rigidbody2D (Kinematic, IsTrigger=true)를 추가해야 합니다.

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("TanghuluDropZone"))
        {
            Debug.Log($"DraggedToppingCollider: {originalDraggable?.toppingType} 분신이 {other.name} 영역에 진입.");
            isInDropZone = true;
            currentDropZoneColliderRef = other;
            if (originalDraggable != null)
            {
                // DraggableTopping에 현재 드롭존 상태 전달 (선택적)
                // originalDraggable.SetDropZoneStatus(true, other);
            }
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("TanghuluDropZone"))
        {
            if (!isInDropZone) // Enter를 놓쳤을 경우
            {
                isInDropZone = true;
                currentDropZoneColliderRef = other;
                 if (originalDraggable != null)
                {
                    // originalDraggable.SetDropZoneStatus(true, other);
                }
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("TanghuluDropZone"))
        {
            Debug.Log($"DraggedToppingCollider: {originalDraggable?.toppingType} 분신이 {other.name} 영역에서 나옴.");
            if(other == currentDropZoneColliderRef) // 현재 감지된 드롭존에서 나간 경우만
            {
                isInDropZone = false;
                currentDropZoneColliderRef = null;
                 if (originalDraggable != null)
                {
                    // originalDraggable.SetDropZoneStatus(false, null);
                }
            }
        }
    }

    // DraggableTopping의 OnEndDrag에서 이 함수를 호출하여 드롭존에 있는지 확인
    public bool IsOverDropZone()
    {
        return isInDropZone;
    }

    public Collider2D GetCurrentDropZoneCollider()
    {
        return currentDropZoneColliderRef;
    }
}