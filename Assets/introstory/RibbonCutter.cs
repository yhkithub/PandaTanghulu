// RibbonCutter.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class RibbonCutter : MonoBehaviour
{
    public Sprite cutSprite;
    public float fadeDuration = 0.25f;
    public float delayAfterCut = 0.25f;
    public AudioClip ribbonCutSound; // 리본 자르는 사운드 AudioClip 직접 참조

    private SpriteRenderer sr;
    private bool isCut = false;
    private Camera mainCamera;
    private Collider2D ribbonCollider;
    private AudioSource audioSource; // RibbonCutter 자체의 AudioSource

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        ribbonCollider = GetComponent<Collider2D>();
        mainCamera = Camera.main;
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            Debug.LogError("RibbonCutter Error: AudioSource 컴포넌트가 없습니다!", this.gameObject);
            enabled = false; // AudioSource 없으면 스크립트 비활성화
            return;
        }

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
            StartCuttingSequence();
        }
    }

    void StartCuttingSequence()
    {
        if (isCut) return;
        isCut = true;
        Debug.Log("RibbonCutter (Raycast): 리본 자르기 시퀀스 시작.");
        StartCoroutine(CutRibbonSequence());
    }

    IEnumerator CutRibbonSequence()
    {
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
        Debug.Log("RibbonCutter: 페이드 아웃 완료.");

        // ★★★ 리본 자르는 사운드 재생 로직 수정 ★★★
        if (ribbonCutSound != null) // AudioClip이 할당되어 있는지 먼저 확인
        {
            // AudioManager가 있고, SFX가 활성화되어 있을 때만 재생
            if (AudioManager.Instance != null && AudioManager.Instance.IsSfxEnabled)
            {
                if (audioSource != null) // RibbonCutter의 AudioSource가 있는지 확인
                {
                    audioSource.PlayOneShot(ribbonCutSound);
                    Debug.Log("RibbonCutter: 리본 자르는 사운드 재생 (SFX 활성화됨).");
                }
                else
                {
                    Debug.LogWarning("RibbonCutter Warning: 리본 사운드를 재생할 AudioSource가 없습니다 (RibbonCutter에).");
                }
            }
            else if (AudioManager.Instance != null && !AudioManager.Instance.IsSfxEnabled)
            {
                Debug.Log("RibbonCutter: 리본 자르는 사운드 재생 안 함 (SFX 비활성화됨).");
            }
            else if (AudioManager.Instance == null)
            {
                Debug.LogWarning("RibbonCutter Warning: AudioManager 인스턴스를 찾을 수 없어 리본 사운드 설정을 확인할 수 없습니다.");
                // AudioManager가 없는 경우, 그냥 재생하거나 재생하지 않는 정책을 정할 수 있습니다.
                // if (audioSource != null) audioSource.PlayOneShot(ribbonCutSound); // 예: AudioManager 없으면 그냥 재생
            }
        }
        else
        {
            Debug.LogWarning("RibbonCutter Warning: ribbonCutSound가 할당되지 않았습니다.");
        }

        if (cutSprite != null)
        {
            sr.sprite = cutSprite;
            Debug.Log("RibbonCutter: 스프라이트를 'cutSprite'으로 교체.");
            sr.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1f); // 알파값 원복
            Debug.Log("RibbonCutter: 잘린 리본 즉시 표시.");
        }
        else
        {
            Debug.LogWarning("RibbonCutter Warning: 'cutSprite'가 할당되지 않음.", this.gameObject);
        }

        if (delayAfterCut > 0)
        {
            yield return new WaitForSeconds(delayAfterCut);
        }

        LoadShopScene();
    }

    void LoadShopScene()
    {
        Debug.Log("RibbonCutter: 'ShopScene' 로드를 시도합니다.");
        SceneManager.LoadScene("ShopScene");
    }
}