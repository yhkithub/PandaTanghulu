using UnityEngine;

public class CloudMover : MonoBehaviour
{
    public float speed = 50f; // 이동 속도
    public float resetX = -1000f; // 왼쪽 시작 지점
    public float endX = 1000f;    // 오른쪽 끝 지점

    RectTransform rect;

    void Start()
    {
        rect = GetComponent<RectTransform>();
    }

    void Update()
    {
        rect.anchoredPosition += Vector2.right * speed * Time.deltaTime;

        if (rect.anchoredPosition.x > endX)
        {
            rect.anchoredPosition = new Vector2(resetX, rect.anchoredPosition.y);
        }
    }
}
