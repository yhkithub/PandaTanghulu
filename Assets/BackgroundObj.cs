using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundScaler : MonoBehaviour
{
    void Start()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        // 화면 높이(월드 단위)
        float worldScreenHeight = Camera.main.orthographicSize * 2f;
        // 화면 너비(월드 단위)
        float worldScreenWidth = worldScreenHeight * Screen.width / Screen.height;
        Vector2 spriteSize = sr.sprite.bounds.size;
        // 스케일 계산
        transform.localScale = new Vector3(
            worldScreenWidth / spriteSize.x,
            worldScreenHeight / spriteSize.y,
            1f
        );
    }
}
