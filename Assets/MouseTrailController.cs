using UnityEngine;

// TrailRenderer와 BoxCollider2D 컴포넌트가 반드시 필요함을 명시
[RequireComponent(typeof(TrailRenderer), typeof(BoxCollider2D))]
public class MouseTrailController : MonoBehaviour
{
    private TrailRenderer trail;        // TrailRenderer 참조
    private Camera mainCamera;          // 메인 카메라 참조 캐싱

    void Awake()
    {
        trail = GetComponent<TrailRenderer>();
        mainCamera = Camera.main; // Awake에서 메인 카메라 찾아두기

        // 컴포넌트 존재 확인
        if (trail == null)
        {
            Debug.LogError("MouseTrailController Error: TrailRenderer 컴포넌트를 찾을 수 없습니다!", this.gameObject);
            enabled = false;
        }
        if (GetComponent<BoxCollider2D>() == null)
        {
             Debug.LogError("MouseTrailController Error: BoxCollider2D 컴포넌트를 찾을 수 없습니다!", this.gameObject);
             enabled = false;
        }
        if (mainCamera == null)
        {
             Debug.LogError("MouseTrailController Error: 씬에 Main Camera가 없거나 'MainCamera' 태그가 지정되지 않았습니다.");
             enabled = false;
        }

        // 초기화 시 트레일 숨기기 (필요하다면)
        // trail.enabled = false; // 또는 trail.Clear();
    }

    void Update()
    {
        // 메인 카메라가 유효한지 매 프레임 확인 (씬 전환 등 고려)
        if (mainCamera == null) return;

        // 마우스 왼쪽 버튼을 새로 클릭했을 때
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("MouseTrailController: 마우스 클릭 감지! 위치 이동 후 트레일 초기화 시도.");
            if (trail != null && mainCamera != null)
            {
                // 1. 먼저 새 위치로 이동
                Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
                mousePos.z = 0f;
                transform.position = mousePos;

                // 2. 그 다음 초기화
                trail.Clear();
            }
            else
            {
                Debug.LogError("MouseTrailController Error: TrailRenderer 또는 MainCamera 참조가 null입니다.");
            }
        }

        // 마우스 왼쪽 버튼을 누르고 있는 동안 (매 프레임 발생)
        if (Input.GetMouseButton(0))
        {
            // 마우스 커서 위치를 월드 좌표로 변환
            Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0f; // 2D 게임이므로 Z 좌표는 0으로 설정

            // 이 오브젝트(MouseTrail)의 위치를 마우스 위치로 이동
            transform.position = mousePos;

            // 트레일이 비활성화 상태였다면 활성화 (클릭 시작 시)
            if (trail != null && !trail.emitting) {
                trail.emitting = true;
            }
        }

        // 마우스 버튼을 뗐을 때 (단 한 번 발생)
        // if (Input.GetMouseButtonUp(0))
        // {
        //     // 필요 시 트레일 중지 로직 추가 (Time 속성에 의해 자동으로 사라지므로 보통 불필요)
        //     // if (trail != null) {
        //     //     trail.emitting = false;
        //     // }
        // }
    }
}