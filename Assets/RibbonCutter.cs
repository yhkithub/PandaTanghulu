using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections; // 코루틴 사용을 위해 추가

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class RibbonCutter : MonoBehaviour
{
    public Sprite cutSprite;            // 잘린 리본 스프라이트 (Inspector에서 할당)
    public float fadeDuration = 0.25f;  // 페이드 아웃에 걸리는 시간 (초)
    public float delayAfterCut = 0.25f; // 이미지 변경 후 씬 전환까지 추가 지연 시간
    // ★ 리본 자르는 사운드 추가
    public AudioClip ribbonCutSound;

    private SpriteRenderer sr;
    private bool isCut = false;
    private Camera mainCamera;
    private Collider2D ribbonCollider;
    // ★ AudioSource 컴포넌트 참조 변수
    private AudioSource audioSource;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        ribbonCollider = GetComponent<Collider2D>();
        mainCamera = Camera.main;
        // ★ AudioSource 컴포넌트 가져오기
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogError("RibbonCutter Error: AudioSource 컴포넌트가 없습니다!");
            enabled = false;
            return;
        }
        // --- 오류 검사 (이전과 동일) ---
        if (sr == null) { Debug.LogError("RibbonCutter Error: SpriteRenderer 없음!"); enabled = false; }
        if (ribbonCollider == null) { Debug.LogError("RibbonCutter Error: Collider2D 없음!"); enabled = false; }
        if (mainCamera == null) { Debug.LogError("RibbonCutter Error: MainCamera 없음!"); enabled = false; }
        isCut = false;
    }

    void Update()
    {
        if (isCut || mainCamera == null || !Input.GetMouseButton(0))
        {
            return;
        }

        Vector2 mousePosWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePosWorld, Vector2.zero);

        if (hit.collider != null && hit.collider == ribbonCollider)
        {
            // 리본 자르기 로직 시작 (코루틴 호출)
            StartCuttingSequence();
        }
    }

    void StartCuttingSequence()
    {
        if (isCut) return; // 이미 잘리는 중이거나 잘렸으면 반환
        isCut = true; // 잘림 플래그 설정

        Debug.Log("RibbonCutter (Raycast): 리본 자르기 시퀀스 시작.");
        // 코루틴 시작
        StartCoroutine(CutRibbonSequence());
    }

    IEnumerator CutRibbonSequence()
    {
        // 1. 페이드 아웃
        float timer = 0f;
        Color originalColor = sr.color; // 초기 색상 저장 (알파값 포함)

        while (timer < fadeDuration)
        {
            // 시간에 따라 알파값을 1에서 0으로 변경
            float alpha = Mathf.Lerp(originalColor.a, 0f, timer / fadeDuration);
            sr.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);

            timer += Time.deltaTime;
            yield return null; // 다음 프레임까지 대기
        }
        // 페이드 아웃 완료 후 알파값 0으로 확실히 설정
        sr.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
        Debug.Log("RibbonCutter: 페이드 아웃 완료.");

        // ★ 2. 사운드 재생 (페이드 아웃 직후 또는 스프라이트 교체 직전에 재생하는 것이 적절)
        if (audioSource != null && ribbonCutSound != null)
        {
            audioSource.PlayOneShot(ribbonCutSound);
            Debug.Log("RibbonCutter: 리본 자르는 사운드 재생.");
        }
        else
        {
            Debug.LogWarning("RibbonCutter Warning: AudioSource가 없거나 ribbonCutSound가 할당되지 않았습니다.");
        }

        // 3. 스프라이트 교체
        if (cutSprite != null)
        {
            sr.sprite = cutSprite;
            Debug.Log("RibbonCutter: 스프라이트를 'cutSprite'으로 교체.");

            // 4. 즉시 다시 보이게 함 (알파값 1로 설정)
            // (만약 페이드 인 효과도 원하면 이 부분을 다시 Lerp 루프로 만들어야 함)
            sr.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1f);
            Debug.Log("RibbonCutter: 잘린 리본 즉시 표시.");
        }
        else
        {
            Debug.LogWarning("RibbonCutter Warning: 'cutSprite'가 할당되지 않음.", this.gameObject);
            // 스프라이트 교체 실패 시에도 씬 전환은 진행되도록 함
        }

        // 5. 추가 지연 시간만큼 대기
        if (delayAfterCut > 0)
        {
            yield return new WaitForSeconds(delayAfterCut);
        }

        // 6. 씬 전환
        LoadShopScene();
    }

    void LoadShopScene()
    {
        Debug.Log("RibbonCutter: 'ShopScene' 로드를 시도합니다.");
        SceneManager.LoadScene("ShopScene");
    }
}