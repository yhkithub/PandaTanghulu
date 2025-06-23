using UnityEngine;

// 이름: TrashCanHoverFinal.cs
[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class TrashCanHoverFinal : MonoBehaviour
{
    public Sprite openLidSprite;
    public Sprite closedLidSprite;

    private SpriteRenderer spriteRenderer;
    private int layerMaskToHit; // 마우스 광선에 맞아야 할 레이어들을 지정하는 변수

    void Start()
    {

        if (AudioManager.Instance != null)
        {
            // 현재 재생 중인 BGM을 중지하려면
            // AudioManager.Instance.StopBackgroundMusic();
            // AudioManager.Instance.PlayBgm("MainGameBGM"); // "ShopBGM"으로 교체
        }
    }

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        // 'Skewer' 레이어의 번호를 가져옵니다.
        int skewerLayer = LayerMask.NameToLayer("Skewer");

        // 모든 레이어에서 'Skewer' 레이어만 제외하는 마스크를 생성합니다.
        // `~`는 비트 NOT 연산자로, 마스크를 반전시켜 'Skewer' 레이어만 끄는 효과를 줍니다.
        layerMaskToHit = ~(1 << skewerLayer);
    }

    void Update()
    {
        // 마우스 위치로 직접 광선을 쏩니다. 단, 'Skewer' 레이어는 무시합니다.
        RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero, Mathf.Infinity, layerMaskToHit);

        // 광선에 맞은 것이 이 쓰레기통 오브젝트인지 확인합니다.
        if (hit.collider != null && hit.collider.gameObject == this.gameObject)
        {
            // 마우스가 위에 있을 때, 현재 스프라이트가 '열린' 이미지가 아니면 변경합니다.
            if (spriteRenderer.sprite != openLidSprite)
            {
                spriteRenderer.sprite = openLidSprite;
            }
        }
        else
        {
            // 마우스가 밖에 있을 때, 현재 스프라이트가 '닫힌' 이미지가 아니면 변경합니다.
            if (spriteRenderer.sprite != closedLidSprite)
            {
                spriteRenderer.sprite = closedLidSprite;
            }
        }
    }
}