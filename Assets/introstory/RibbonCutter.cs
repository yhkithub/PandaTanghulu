// 파일: Assets/introstory/RibbonCutter.cs

using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D), typeof(AudioSource))]
public class RibbonCutter : MonoBehaviour
{
    [Header("잘린 후 설정")]
    public Sprite cutSprite;
    public float fadeDuration = 0.25f;
    public float delayAfterCut = 0.25f;
    public AudioClip ribbonCutSound;

    private SpriteRenderer sr;
    private Collider2D ribbonCollider;
    private AudioSource audioSource;
    private Camera mainCamera;

    private bool isCut = false;
    // 이전 프레임의 마우스 위치를 저장하기 위한 변수
    private Vector2? lastMousePosition = null; 

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        ribbonCollider = GetComponent<Collider2D>();
        audioSource = GetComponent<AudioSource>();
        mainCamera = Camera.main;

        // 필수 컴포넌트 확인
        if (sr == null || ribbonCollider == null || audioSource == null || mainCamera == null)
        {
            Debug.LogError("RibbonCutter: 필수 컴포넌트 중 하나가 없습니다!", this.gameObject);
            enabled = false;
        }
    }

    void Update()
    {
        if (isCut) return;

        // 마우스 왼쪽 버튼을 누르고 있는 동안
        if (Input.GetMouseButton(0))
        {
            Vector2 currentMousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);

            // 이전 프레임의 마우스 위치가 기록되어 있다면
            if (lastMousePosition.HasValue)
            {
                // ✨✨✨ 핵심 수정 부분! ✨✨✨
                // 이전 위치와 현재 위치 사이에 약간의 거리라도 있는지 확인합니다.
                // 거리가 거의 없다면(제자리 클릭이라면) Linecast를 실행하지 않습니다.
                if (Vector2.Distance(lastMousePosition.Value, currentMousePosition) > 0.01f) 
                {
                    // 이전 위치와 현재 위치 사이에 선을 그어 충돌하는 오브젝트가 있는지 확인
                    RaycastHit2D hit = Physics2D.Linecast(lastMousePosition.Value, currentMousePosition, 1 << gameObject.layer);
                    
                    // 만약 충돌한 오브젝트가 바로 이 리본이라면
                    if (hit.collider != null && hit.collider == this.ribbonCollider)
                    {
                        Debug.Log("리본 자르기 성공!");
                        StartCuttingSequence(); // 자르는 시퀀스 시작
                    }
                }
            }
            
            // 다음 프레임에서 사용하기 위해 현재 위치를 "이전 위치"로 저장
            lastMousePosition = currentMousePosition;
        }
        else // 마우스 버튼에서 손을 떼면
        {
            // "이전 위치" 기록을 리셋
            lastMousePosition = null;
        }
    }

    void StartCuttingSequence()
    {
        if (isCut) return;
        isCut = true; // 중복 실행 방지
        StartCoroutine(CutRibbonSequence());
    }

    // CutRibbonSequence 코루틴은 기존 코드를 그대로 사용합니다.
    IEnumerator CutRibbonSequence()
    {
        // ... (기존의 페이드 아웃, 사운드 재생, 스프라이트 교체, 씬 전환 로직) ...
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