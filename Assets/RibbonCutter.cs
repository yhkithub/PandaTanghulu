using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))] // Collider는 여전히 필요 (Raycast Target용)
public class RibbonCutter : MonoBehaviour
{
    public Sprite cutSprite;
    private SpriteRenderer sr;
    private bool isCut = false;
    private Camera mainCamera; // 카메라 캐싱
    private Collider2D ribbonCollider; // 자신의 콜라이더 참조

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        ribbonCollider = GetComponent<Collider2D>(); // 자신의 콜라이더 가져오기
        mainCamera = Camera.main;
        // ... 오류 검사 ...
        isCut = false;
    }

    // OnTriggerStay2D 대신 Update 사용
    void Update()
    {
        // 이미 잘렸거나, 카메라가 없거나, 마우스 버튼 안 눌렀으면 종료
        if (isCut || mainCamera == null || !Input.GetMouseButton(0))
        {
            return;
        }

        // 마우스 위치에서 Raycast 발사
        Vector2 mousePosWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePosWorld, Vector2.zero); // Vector2.zero는 점 형태의 Raycast

        // Raycast 결과 확인
        if (hit.collider != null)
        {
             // Debug.Log($"Raycast hit: {hit.collider.name}"); // 무엇과 충돌했는지 확인

             // 충돌한 Collider가 이 오브젝트(리본)의 Collider인지 확인
             if (hit.collider == ribbonCollider)
             {
                 Debug.Log("RibbonCutter (Raycast): 마우스가 리본 위에 있고 버튼 눌림! 리본 자르기 실행.");
                 isCut = true;

                 if (cutSprite != null)
                     sr.sprite = cutSprite;
                 else
                     Debug.LogWarning("RibbonCutter Warning: 'cutSprite'가 할당되지 않음.", this.gameObject);

                 Invoke("LoadShopScene", 0.5f);
             }
        }
    }

    void LoadShopScene()
    {
        Debug.Log("RibbonCutter: 'ShopScene' 로드를 시도합니다.");
        SceneManager.LoadScene("ShopScene");
    }
}