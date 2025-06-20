// 🟩 수정 후 코드
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D), typeof(AudioSource))]
public class RibbonCutter : MonoBehaviour
{
    public Sprite cutSprite;
    public float fadeDuration = 0.25f;
    public float delayAfterCut = 0.25f;
    public AudioClip ribbonCutSound;

    private SpriteRenderer sr;
    private Collider2D ribbonCollider;
    private AudioSource audioSource;

    private bool isCut = false;
    private bool isDraggingRibbon = false; // 리본을 드래그 중인지 확인하는 플래그
    private Vector2 dragStartPosition; // 드래그 시작 마우스 위치
    private const float MIN_DRAG_DISTANCE_Y = 50f; // 리본을 자르기 위해 필요한 최소 Y축 이동 거리

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        ribbonCollider = GetComponent<Collider2D>();
        audioSource = GetComponent<AudioSource>();
        // Camera.main은 성능상 Awake/Start에서 캐싱하는 것이 좋습니다.
    }

    void Update()
    {
        if (isCut) return;

        // 1. 마우스 버튼을 처음 눌렀을 때
        if (Input.GetMouseButtonDown(0))
        {
            // 마우스 위치에 있는 콜라이더 확인
            RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            if (hit.collider != null && hit.collider == ribbonCollider)
            {
                // 리본 위에서 클릭했으므로 드래그 시작
                isDraggingRibbon = true;
                dragStartPosition = Input.mousePosition;
                Debug.Log("리본 드래그 시작");
            }
        }

        // 2. 마우스 버튼을 누르고 있는 동안 (드래그 중)
        if (isDraggingRibbon && Input.GetMouseButton(0))
        {
            float dragDistanceY = dragStartPosition.y - Input.mousePosition.y;

            // 현재 마우스 위치가 시작 위치보다 일정 거리 이상 아래에 있다면
            if (dragDistanceY > MIN_DRAG_DISTANCE_Y)
            {
                Debug.Log("리본 자르기 성공!");
                StartCuttingSequence();
                isDraggingRibbon = false; // 한 번만 실행되도록 플래그 초기화
            }
        }

        // 3. 마우스 버튼에서 손을 떼면 드래그 상태 초기화
        if (Input.GetMouseButtonUp(0))
        {
            if(isDraggingRibbon)
            {
                Debug.Log("리본 드래그 취소");
                isDraggingRibbon = false;
            }
        }
    }

    void StartCuttingSequence()
    {
        if (isCut) return;
        isCut = true;
        StartCoroutine(CutRibbonSequence());
    }

    IEnumerator CutRibbonSequence()
    {
        // (기존의 CutRibbonSequence 코드는 그대로 유지)
        // ... 페이드 아웃, 사운드 재생, 스프라이트 교체, 씬 전환 로직 ...
        float timer = 0f;
        Color originalColor = sr.color;

        while (timer < fadeDuration)
        {
            float alpha = Mathf.Lerp(originalColor.a, 0f, timer / fadeDuration);
            sr.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            timer += Time.deltaTime;
            yield return null;
        }
        sr.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);

        if (ribbonCutSound != null && AudioManager.Instance != null && AudioManager.Instance.IsSfxEnabled)
        {
            audioSource.PlayOneShot(ribbonCutSound);
        }

        if (cutSprite != null)
        {
            sr.sprite = cutSprite;
            sr.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1f);
        }

        if (delayAfterCut > 0)
        {
            yield return new WaitForSeconds(delayAfterCut);
        }

        LoadShopScene();
    }

    void LoadShopScene()
    {
        SceneManager.LoadScene("ShopScene");
    }
}