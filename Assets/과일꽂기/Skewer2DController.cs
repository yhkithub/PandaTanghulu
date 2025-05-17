// skewer2DController.cs 수정
using UnityEngine;

public class Skewer2DController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float fixedYPosition = -4f; // Inspector에서 고정할 Y 위치 설정

    void Update()
    {
        // 기존 이동 코드 (마우스 이동으로 변경 예정)
        // float horizontalInput = Input.GetAxisRaw("Horizontal");
        // Vector2 moveDirection = new Vector2(horizontalInput, 0f).normalized;
        // GetComponent<Rigidbody2D>().linearVelocity = moveDirection * moveSpeed;

        // 이동 범위 제한 (선택 사항)
        // float clampX = Mathf.Clamp(transform.position.x, -8f, 8f);
        // transform.position = new Vector2(clampX, fixedYPosition); // Y 위치 고정

        // 마우스로 이동 (아래 코드 추가/변경)
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float targetX = Mathf.Clamp(mousePosition.x, -8f, 8f); // X축 이동 범위 제한
        transform.position = new Vector2(targetX, fixedYPosition);
    }
}