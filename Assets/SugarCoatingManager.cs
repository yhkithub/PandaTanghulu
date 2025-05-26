// SugarCoatingManager.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro; // TextMeshProUGUI 사용

public class SugarCoatingManager : MonoBehaviour
{
    public static SugarCoatingManager Instance { get; private set; }

    [Header("게임 오브젝트 연결")]
    public GameObject fryingPanObject;
    public SpriteRenderer skewerRenderer; // 설탕 코팅 전 이미지 표시용 (알파값 조절 대상)
    public SpriteRenderer skewerCoatedRenderer; // 설탕 코팅 후 이미지 표시용 (알파값 조절 대상)
    public HoneyDipperController honeyDipper;
    public GameObject sparkleEffectObject1;
    public GameObject sparkleEffectObject2;
    public TextMeshProUGUI rubCountText; // 문지른 횟수 표시용 Text

    [Header("스프라이트 에셋")]
    public Sprite skewerBeforeCoatingSprite; // CustomerOrderManager에서 로드됨
    public Sprite skewerAfterCoatingSprite;  // CustomerOrderManager에서 로드됨

    [Header("게임 설정")]
    public int rubsNeededForCoating = 10;
    public float coatingDuration = 2.0f; // 코팅 완료까지 걸리는 시간
    public float rubbingDistanceThreshold = 0.1f;
    public int rubsForFirstSparkle = 3;

    [Header("꼬치 시각적 설정")]
    public float targetVisualHeightInWorldUnits = 5.0f;

    private int currentRubs = 0;
    private bool isCoatingPhase = false;
    private bool coatingComplete = false;
    private Vector3 lastDipperPosition;
    private bool firstSparkleShown = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (CustomerOrderManager.Instance == null || CustomerOrderManager.Instance.CurrentOrderData == null)
        {
            Debug.LogError("SugarCoatingManager Error: CustomerOrderManager 또는 CurrentOrderData가 null입니다! TitleScene으로 이동합니다.");
            // SceneSwitcher.Instance?.LoadScene("TitleScene");
            return;
        }

        skewerBeforeCoatingSprite = CustomerOrderManager.Instance.CurrentOrderData.completedSkewerSprite;
        skewerAfterCoatingSprite = CustomerOrderManager.Instance.CurrentOrderData.sugarCoatedSkewerSprite;

        if (skewerBeforeCoatingSprite == null) Debug.LogError("skewerBeforeCoatingSprite가 할당되지 않았습니다!");
        if (skewerAfterCoatingSprite == null) Debug.LogWarning($"CustomerOrderData ({CustomerOrderManager.Instance.CurrentOrderData.name})에 sugarCoatedSkewerSprite가 할당되지 않았습니다!");
        if (skewerCoatedRenderer == null) Debug.LogError("skewerCoatedRenderer가 Inspector에 연결되지 않았습니다! 점진적 코팅 효과를 사용할 수 없습니다.");
        if (rubCountText == null) Debug.LogWarning("rubCountText가 Inspector에 연결되지 않았습니다! 문지른 횟수를 표시할 수 없습니다.");

        InitializeCoating();
    }

    void InitializeCoating()
    {
        currentRubs = 0;
        isCoatingPhase = false;
        coatingComplete = false;
        firstSparkleShown = false;

        if (skewerRenderer != null && skewerBeforeCoatingSprite != null)
        {
            skewerRenderer.sprite = skewerBeforeCoatingSprite;
            skewerRenderer.color = Color.white; // 완전 불투명
            AdjustSkewerSpriteSize(skewerRenderer, skewerBeforeCoatingSprite);
            skewerRenderer.gameObject.SetActive(true);
        }
        else Debug.LogError("주 꼬치 렌더러 또는 설탕 코팅 전 스프라이트가 없습니다.");

        if (skewerCoatedRenderer != null && skewerAfterCoatingSprite != null)
        {
            skewerCoatedRenderer.sprite = skewerAfterCoatingSprite;
            skewerCoatedRenderer.color = new Color(1, 1, 1, 0); // 시작 시 완전히 투명
            AdjustSkewerSpriteSize(skewerCoatedRenderer, skewerAfterCoatingSprite);
            skewerCoatedRenderer.gameObject.SetActive(true); // 항상 활성화 상태로 두고 알파로 제어
        }
        else if (skewerCoatedRenderer != null) // 스프라이트만 없는 경우
        {
             skewerCoatedRenderer.gameObject.SetActive(false); // 비활성화
        }


        if (honeyDipper != null) honeyDipper.ResetDipper();
        else Debug.LogError("허니디퍼 컨트롤러가 연결되지 않았습니다!");

        if (sparkleEffectObject1 != null) sparkleEffectObject1.SetActive(false);
        if (sparkleEffectObject2 != null) sparkleEffectObject2.SetActive(false);

        UpdateRubCountText(); // 문지른 횟수 초기화 및 UI 업데이트
        Debug.Log("설탕 코팅 단계 시작!");
    }

    public void DipHoneyDipper()
    {
        if (!isCoatingPhase && !coatingComplete)
        {
            isCoatingPhase = true;
            Debug.Log("허니디퍼에 설탕물 묻힘!");
            // 허니디퍼 스프라이트 변경은 HoneyDipperController에서 처리
        }
    }

    public void RubSkewer(Vector3 currentDipperPos)
    {
        if (isCoatingPhase && !coatingComplete && honeyDipper.HasSugar())
        {
            if (Vector3.Distance(currentDipperPos, lastDipperPosition) > rubbingDistanceThreshold)
            {
                currentRubs++;
                UpdateRubCountText(); // 문지를 때마다 횟수 UI 업데이트

                // 코팅 진행도에 따라 알파값 조절
                UpdateCoatingVisuals();

                if (!firstSparkleShown && currentRubs >= rubsForFirstSparkle)
                {
                    if (sparkleEffectObject1 != null)
                    {
                        sparkleEffectObject1.SetActive(true);
                        Debug.Log("첫 번째 반짝이는 효과 표시.");
                        // 필요하면 잠시 후 사라지게 하는 코루틴 호출
                        // StartCoroutine(HideSparkleAfterDelay(sparkleEffectObject1, 0.5f));
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

    void UpdateRubCountText()
    {
        if (rubCountText != null)
        {
            rubCountText.text = $"{currentRubs} / {rubsNeededForCoating}";
        }
    }

    void UpdateCoatingVisuals()
    {
        if (skewerRenderer == null || skewerCoatedRenderer == null || rubsNeededForCoating == 0) return;

        float coatingProgress = Mathf.Clamp01((float)currentRubs / rubsNeededForCoating);

        // skewerRenderer (코팅 전 이미지)는 점점 투명하게
        skewerRenderer.color = new Color(1, 1, 1, 1 - coatingProgress);

        // skewerCoatedRenderer (코팅 후 이미지)는 점점 불투명하게
        if (skewerCoatedRenderer.sprite != null) // 코팅 후 스프라이트가 할당되었을 때만
        {
             skewerCoatedRenderer.color = new Color(1, 1, 1, coatingProgress);
        }
    }

    void CompleteCoating()
    {
        if (coatingComplete) return;

        coatingComplete = true;
        isCoatingPhase = false; // 코팅 완료
        currentRubs = rubsNeededForCoating; // 확실하게 최대치로
        UpdateRubCountText();
        UpdateCoatingVisuals(); // 최종 알파값 적용 (코팅 전 이미지 완전 투명, 코팅 후 이미지 완전 불투명)
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

        AudioManager.Instance?.PlayOneShotSound("CoatingSuccessSound"); // 실제 사운드 이름으로 변경 필요

        StartCoroutine(ProceedToNextStageAfterDelay(coatingDuration));
    }

    void AdjustSkewerSpriteSize(SpriteRenderer renderer, Sprite spriteToScale)
    {
        if (renderer == null || spriteToScale == null)
        {
            Debug.LogWarning("AdjustSkewerSpriteSize: renderer 또는 spriteToScale이 null입니다.");
            return;
        }

        Transform objectTransform = renderer.transform;

        // 스프라이트의 원본 픽셀 크기 (PPU가 적용되지 않은 순수 픽셀)
        // textureRect 대신 texture의 width/height 사용
        float spritePixelWidth = spriteToScale.texture.width;
        float spritePixelHeight = spriteToScale.texture.height;

        if (spritePixelWidth == 0 || spritePixelHeight == 0) {
            Debug.LogWarning($"Sprite '{spriteToScale.name}'의 텍스처 크기가 0입니다. texture.width/height: {spriteToScale.texture.width}x{spriteToScale.texture.height}, textureRect.width/height: {spriteToScale.textureRect.width}x{spriteToScale.textureRect.height}");
            return;
        }

        // 스프라이트의 PPU (Pixels Per Unit)
        float ppu = spriteToScale.pixelsPerUnit;

        // PPU를 고려한 스프라이트의 원본 월드 유닛 크기
        float spriteOriginalWorldWidth = spritePixelWidth / ppu;
        float spriteOriginalWorldHeight = spritePixelHeight / ppu;

        if (spriteOriginalWorldHeight == 0) { // 0으로 나누기 방지
                Debug.LogWarning($"Sprite '{spriteToScale.name}'의 PPU ({ppu}) 또는 픽셀 높이 ({spritePixelHeight})가 0이거나 유효하지 않아 월드 높이를 계산할 수 없습니다.");
                return;
        }

        // 목표 시각적 높이(targetVisualHeightInWorldUnits)를 기준으로 스케일 계산
        float scaleMultiplier = targetVisualHeightInWorldUnits / spriteOriginalWorldHeight;

        Vector3 newLocalScale = new Vector3(
            scaleMultiplier * (spriteOriginalWorldWidth / spriteOriginalWorldHeight), // 원본 비율 유지하며 너비 계산
            scaleMultiplier, // 높이를 targetVisualHeightInWorldUnits에 맞춤
            1f // Z 스케일은 2D이므로 일반적으로 1
        );
        
        // 부모 스케일을 고려하여 최종 로컬 스케일 계산
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
        // Debug.Log($"{renderer.gameObject.name} ({spriteToScale.name}) 로컬 스케일: {newLocalScale}, 부모 스케일: {parentWorldScale}, 목표 월드 높이: {targetVisualHeightInWorldUnits}");
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