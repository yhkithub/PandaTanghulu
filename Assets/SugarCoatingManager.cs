// 파일: SugarCoatingManager.cs (수정 완료)
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class SugarCoatingManager : MonoBehaviour
{
    public static SugarCoatingManager Instance { get; private set; }

    [Header("게임 오브젝트 연결")]
    public GameObject fryingPanObject;
    public Transform skewerParent;
    public SpriteRenderer skewerRenderer;
    public SpriteRenderer skewerCoatedRenderer;
    public HoneyDipperController honeyDipper;
    public GameObject sparkleEffectObject1;
    public GameObject sparkleEffectObject2;
    public TextMeshProUGUI rubCountText;

    [Header("스프라이트 에셋")]
    public Sprite skewerBeforeCoatingSprite;
    public Sprite skewerAfterCoatingSprite;

    [Header("게임 설정")]
    public int rubsNeededForCoating = 10;
    public float coatingDuration = 2.0f;
    public float rubbingDistanceThreshold = 0.1f;
    public int rubsForFirstSparkle = 3;

    [Header("꼬치 시각적 설정")]
    public float targetVisualHeightInWorldUnits = 5.0f;


    private int currentRubs = 0;
    private bool isCoatingPhase = false;
    private bool coatingComplete = false;
    private Vector3 lastDipperPosition;
    private bool firstSparkleShown = false;
    private bool isDipperTouchingSkewer = false;
    private bool isEndlessModeActive = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        isEndlessModeActive = GameModeManager.IsEndlessMode;

        if (isEndlessModeActive)
        {
            if (skewerRenderer != null) skewerRenderer.gameObject.SetActive(false);
            if (skewerCoatedRenderer != null) skewerCoatedRenderer.gameObject.SetActive(false);
            
            // --- 꼬치 위치 문제 해결 ---
            // skewerParent와 skewerRenderer가 모두 할당되어 있을 때,
            // skewerParent의 위치를 skewerRenderer의 위치와 동일하게 맞춰줍니다.
            if (skewerParent != null && skewerRenderer != null)
            {
                skewerParent.position = skewerRenderer.transform.position;
            }
            
            Debug.Log("[SugarCoatingManager] 무한 모드로 설정되었습니다.");
        }
        else
        {
            if (CustomerOrderManager.Instance?.CurrentOrderData == null) return;
            skewerBeforeCoatingSprite = CustomerOrderManager.Instance.CurrentOrderData.completedSkewerSprite;
            skewerAfterCoatingSprite = CustomerOrderManager.Instance.CurrentOrderData.sugarCoatedSkewerSprite;
            if (skewerBeforeCoatingSprite == null) Debug.LogError("skewerBeforeCoatingSprite가 할당되지 않았습니다!");
        }

        if (rubCountText == null) Debug.LogWarning("rubCountText가 Inspector에 연결되지 않았습니다!");

        InitializeCoating();
    }

    void InitializeCoating()
    {
        currentRubs = 0;
        isCoatingPhase = false;
        coatingComplete = false;
        firstSparkleShown = false;

        if (!isEndlessModeActive)
        {
            if (skewerRenderer != null)
            {
                if (skewerBeforeCoatingSprite != null)
                {
                    skewerRenderer.sprite = skewerBeforeCoatingSprite;
                    AdjustSkewerSpriteSize(skewerRenderer, skewerBeforeCoatingSprite);
                }
                skewerRenderer.color = Color.white;
                skewerRenderer.gameObject.SetActive(true);
            }
            else Debug.LogError("주 꼬치 렌더러가 없습니다.");

            if (skewerCoatedRenderer != null)
            {
                if (skewerAfterCoatingSprite != null)
                {
                    skewerCoatedRenderer.sprite = skewerAfterCoatingSprite;
                    AdjustSkewerSpriteSize(skewerCoatedRenderer, skewerAfterCoatingSprite);
                }
                skewerCoatedRenderer.color = new Color(1, 1, 1, 0);
                skewerCoatedRenderer.gameObject.SetActive(true);
            }

            if (skewerRenderer != null && skewerBeforeCoatingSprite != null)
            {
                skewerRenderer.sprite = skewerBeforeCoatingSprite;
                skewerRenderer.color = Color.white;
                AdjustSkewerSpriteSize(skewerRenderer, skewerBeforeCoatingSprite);
                skewerRenderer.gameObject.SetActive(true);
            }
            else Debug.LogError("주 꼬치 렌더러 또는 설탕 코팅 전 스프라이트가 없습니다.");

            if (skewerCoatedRenderer != null && skewerAfterCoatingSprite != null)
            {
                skewerCoatedRenderer.sprite = skewerAfterCoatingSprite;
                skewerCoatedRenderer.color = new Color(1, 1, 1, 0);
                AdjustSkewerSpriteSize(skewerCoatedRenderer, skewerAfterCoatingSprite);
                skewerCoatedRenderer.gameObject.SetActive(true);
            }
            else if (skewerCoatedRenderer != null)
            {
                skewerCoatedRenderer.gameObject.SetActive(false);
            }
        }

        if (honeyDipper != null) honeyDipper.ResetDipper();
        else Debug.LogError("허니디퍼 컨트롤러가 연결되지 않았습니다!");

        if (sparkleEffectObject1 != null) sparkleEffectObject1.SetActive(false);
        if (sparkleEffectObject2 != null) sparkleEffectObject2.SetActive(false);

        UpdateRubCountText();
        Debug.Log("설탕 코팅 단계 시작!");
    }

    public void DipHoneyDipper()
    {
        if (!isCoatingPhase && !coatingComplete)
        {
            isCoatingPhase = true;
            Debug.Log("허니디퍼에 설탕물 묻힘!");
        }
    }

    public void RubSkewer(Vector3 currentDipperPos)
    {
        Debug.Log($"RubSkewer 호출! 현재 문지른 횟수: {currentRubs}");
        if (isCoatingPhase && !coatingComplete && honeyDipper.HasSugar())
        {
            float distance = Vector3.Distance(currentDipperPos, lastDipperPosition);
            if (distance < 0.01f) return;
            if (distance > rubbingDistanceThreshold)
            {
                currentRubs++;
                UpdateRubCountText();
                UpdateCoatingVisuals();

                if (!firstSparkleShown && currentRubs >= rubsForFirstSparkle)
                {
                    if (sparkleEffectObject1 != null)
                    {
                        sparkleEffectObject1.SetActive(true);
                    }
                    firstSparkleShown = true;
                }

                if (currentRubs >= rubsNeededForCoating)
                {
                    CompleteCoating();
                }
            }
            lastDipperPosition = currentDipperPos;
        }
    }
    
    public void OnDipperTouchSkewer(bool isTouching)
    {
        if (isTouching && !isDipperTouchingSkewer)
        {
            AudioManager.Instance?.PlayLoopSound("Coating");
        }
        else if (!isTouching && isDipperTouchingSkewer)
        {
            AudioManager.Instance?.StopLoopSound("Coating");
        }
        isDipperTouchingSkewer = isTouching;
    }

    void UpdateRubCountText()
    {
        if (rubCountText != null)
        {
            rubCountText.text = $"{currentRubs} / {rubsNeededForCoating}";
        }
    }

    void UpdateCoatingVisuals()
    {
        float coatingProgress = Mathf.Clamp01((float)currentRubs / rubsNeededForCoating);

        if (isEndlessModeActive)
        {
            // ✅ [수정] SkewerVisualizer의 새 함수를 호출하여 코팅 효과를 적용합니다.
            if (SkewerVisualizer.Instance != null)
            {
                SkewerVisualizer.Instance.ApplyMaskedSugarCoating(skewerParent, coatingProgress);
            }
        }
        else
        {
            // --- 스테이지 모드 로직 (기존과 동일) ---
            if (skewerRenderer == null || skewerCoatedRenderer == null) return;
            
            // 원본 꼬치는 점점 투명해지고,
            skewerRenderer.color = new Color(1, 1, 1, 1 - coatingProgress);
            
            // 코팅된 꼬치가 점점 나타납니다.
            if (skewerCoatedRenderer.sprite != null)
            {
                skewerCoatedRenderer.color = new Color(1, 1, 1, coatingProgress);
            }
        }
    }

    
    void CompleteCoating()
    {
        if (coatingComplete) return;

        coatingComplete = true;
        isCoatingPhase = false;
        currentRubs = rubsNeededForCoating;

        UpdateCoatingVisuals();
        
        Debug.Log("설탕 코팅 최종 완료!");


        if (sparkleEffectObject1 != null && sparkleEffectObject1.activeSelf)
        {
            sparkleEffectObject1.SetActive(false);
        }
        if (sparkleEffectObject2 != null)
        {
            sparkleEffectObject2.SetActive(true);
            Debug.Log("두 번째 (최종) 반짝이는 효과 표시.");
        }

        AudioManager.Instance?.PlayOneShotSound("Success");

        StartCoroutine(ProceedToNextStageAfterDelay(coatingDuration));
    }
    
    // AdjustSkewerSpriteSize, ProceedToNextStageAfterDelay, ResetCoatingGame 메서드는 기존과 동일
    
    void AdjustSkewerSpriteSize(SpriteRenderer renderer, Sprite spriteToScale)
    {
        if (renderer == null || spriteToScale == null)
        {
            Debug.LogWarning("AdjustSkewerSpriteSize: renderer 또는 spriteToScale이 null입니다.");
            return;
        }

        Transform objectTransform = renderer.transform;

        float spritePixelWidth = spriteToScale.texture.width;
        float spritePixelHeight = spriteToScale.texture.height;

        if (spritePixelWidth == 0 || spritePixelHeight == 0) {
            Debug.LogWarning($"Sprite '{spriteToScale.name}'의 텍스처 크기가 0입니다. texture.width/height: {spriteToScale.texture.width}x{spriteToScale.texture.height}, textureRect.width/height: {spriteToScale.textureRect.width}x{spriteToScale.textureRect.height}");
            return;
        }

        float ppu = spriteToScale.pixelsPerUnit;
        float spriteOriginalWorldHeight = spritePixelHeight / ppu;

        if (spriteOriginalWorldHeight == 0) {
                Debug.LogWarning($"Sprite '{spriteToScale.name}'의 PPU ({ppu}) 또는 픽셀 높이 ({spritePixelHeight})가 0이거나 유효하지 않아 월드 높이를 계산할 수 없습니다.");
                return;
        }

        float scaleMultiplier = targetVisualHeightInWorldUnits / spriteOriginalWorldHeight;

        Vector3 newLocalScale = new Vector3(
            scaleMultiplier * (spritePixelWidth / spriteOriginalWorldHeight),
            scaleMultiplier,
            1f
        );
        
        Vector3 parentWorldScale = Vector3.one;
        if (objectTransform.parent != null)
        {
            parentWorldScale = objectTransform.parent.lossyScale;
            if (Mathf.Approximately(parentWorldScale.x, 0f)) parentWorldScale.x = 1f;
            if (Mathf.Approximately(parentWorldScale.y, 0f)) parentWorldScale.y = 1f;
            if (Mathf.Approximately(parentWorldScale.z, 0f)) parentWorldScale.z = 1f;

            newLocalScale.x /= parentWorldScale.x;
            newLocalScale.y /= parentWorldScale.y;
            newLocalScale.z /= parentWorldScale.z; 
        }

        objectTransform.localScale = newLocalScale;
    }


    IEnumerator ProceedToNextStageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (CustomerOrderManager.Instance != null)
        {
            CustomerOrderManager.Instance.ProceedToNextMiniGameStep();
        }
        else
        {
            Debug.LogError("CustomerOrderManager 인스턴스가 없어 다음 단계로 진행할 수 없습니다.");
            SceneSwitcher.Instance?.LoadScene("TitleScene");
        }
    }

    public void ResetCoatingGame()
    {
        InitializeCoating();
    }
}