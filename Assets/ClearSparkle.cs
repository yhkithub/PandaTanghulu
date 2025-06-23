// Assets/ClearSparkle.cs
using UnityEngine;
using System.Collections;

public class ClearSparkle : MonoBehaviour
{
    [Header("배경 애니메이션 관련 변수")]
    public SpriteRenderer background; // 배경 이미지를 표시할 SpriteRenderer 컴포넌트
    public Sprite[] ClearSprites;
    public float frameRate = 0.2f; // 이미지 전환 속도

    void Start()
    {
        StartCoroutine(AnimateBackground());
    }

    IEnumerator AnimateBackground()
    {

        Sprite[] selectedSprites = ClearSprites;
        int currentIndex = 0;

        while (true)
        {
            if (selectedSprites.Length > 0)
            {
                background.sprite = selectedSprites[currentIndex];
                // 좌우 반전 적용
                background.flipX = (currentIndex % 2 == 1);
                currentIndex = (currentIndex + 1) % selectedSprites.Length;
            }
            yield return new WaitForSeconds(frameRate);
        }
    }
}