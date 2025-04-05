using UnityEngine;

[RequireComponent(typeof(TrailRenderer), typeof(BoxCollider2D))]
public class MouseTrailController : MonoBehaviour
{
    private TrailRenderer trail;
    private BoxCollider2D boxCollider;

    void Awake()
    {
        trail = GetComponent<TrailRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();

        // 충돌 판정을 위해 Trigger로 설정
        boxCollider.isTrigger = true;
    }

    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            // 마우스 위치를 월드 좌표로 변환
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0f;
            // 오브젝트(팁) 위치 이동
            transform.position = mousePos;
        }
        else
        {
            // 클릭을 떼면 오브젝트를 화면 밖으로 보내서 충돌 방지
            transform.position = new Vector3(0, -1000, 0);
        }
    }
}
