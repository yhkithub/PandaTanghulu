// HoneyDipperController.cs
using UnityEngine;

public class HoneyDipperController : MonoBehaviour
{
    [Header("스프라이트 에셋")]
    public Sprite dipperWithoutSugarSprite;
    public Sprite dipperWithSugarSprite;

    private SpriteRenderer spriteRenderer;
    private Camera mainCamera;
    private bool hasSugar = false;
    private bool canDip = true; // 후라이팬에 한번만 담그도록 제어

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCamera = Camera.main;

        if (spriteRenderer == null) Debug.LogError("HoneyDipperController: SpriteRenderer가 없습니다!");
        if (mainCamera == null) Debug.LogError("HoneyDipperController: Main Camera를 찾을 수 없습니다!");
    }

    void Start()
    {
        ResetDipper();
    }

    public void ResetDipper()
    {
        hasSugar = false;
        canDip = true;
        if (spriteRenderer != null && dipperWithoutSugarSprite != null)
        {
            spriteRenderer.sprite = dipperWithoutSugarSprite;
        }
    }

    void Update()
    {
        // 마우스를 따라 허니디퍼 이동
        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        transform.position = new Vector3(mousePos.x, mousePos.y, 0); // Z는 0으로 고정 (2D)

        // 이 부분은 SugarCoatingManager의 RubSkewer로 대체될 수 있음
        // 또는 여기서 계속 충돌을 감지하고 SugarCoatingManager에 알림
        // 여기서는 OnTriggerStay2D를 사용하여 지속적인 문지름을 감지하도록 함
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 디버그 로그 추가하여 어떤 오브젝트와 충돌했는지, 태그는 무엇인지 확인
        Debug.Log("HoneyDipper OnTriggerEnter2D with: " + other.gameObject.name + ", Tag: " + other.tag);

        if (other.CompareTag("SugarPot") && canDip && !hasSugar)
        {
            if (SugarCoatingManager.Instance != null)
            {
                SugarCoatingManager.Instance.DipHoneyDipper();
                hasSugar = true;
                canDip = false; // 한 번만 담그도록
                if (spriteRenderer != null && dipperWithSugarSprite != null)
                {
                    spriteRenderer.sprite = dipperWithSugarSprite;
                    Debug.Log("허니디퍼 스프라이트: 설탕 묻은 상태로 변경됨."); // 로그 메시지 명확하게
                }
                else
                {
                    Debug.LogError("허니디퍼 스프라이트 변경 실패: spriteRenderer 또는 dipperWithSugarSprite가 null입니다.");
                }
            }
            else
            {
                Debug.LogError("SugarCoatingManager.Instance가 null입니다. DipHoneyDipper 호출 불가.");
            }
        }
    }

    // isTrigger가 켜진 콜라이더끼리의 지속적인 충돌 감지
    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("TanghuluSkewer") && hasSugar) // 설탕 묻은 허니디퍼가 탕후루 꼬치에 닿아있을 때
        {
            // SugarCoatingManager에 현재 허니디퍼 위치를 전달하며 문지르기 알림
            if (SugarCoatingManager.Instance != null)
            {
                SugarCoatingManager.Instance.RubSkewer(transform.position);
            }
        }
    }

    public bool HasSugar()
    {
        return hasSugar;
    }
}