using UnityEngine;

public class Skewer2DController : MonoBehaviour
{
    public float moveSpeed = 5f;

    void Update()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        Vector2 moveDirection = new Vector2(horizontalInput, 0f).normalized;
        GetComponent<Rigidbody2D>().linearVelocity = moveDirection * moveSpeed;

        // 이동 범위 제한 (선택 사항)
        float clampX = Mathf.Clamp(transform.position.x, -8f, 8f);
        transform.position = new Vector2(clampX, transform.position.y);
    }
}