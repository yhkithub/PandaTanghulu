// SugarCoatingManager.cs
using UnityEngine;
using UnityEngine.UI; // 반짝이는 효과를 UI Image로 사용할 경우
using UnityEngine.SceneManagement; // 씬 전환용
using System.Collections;

public class SugarCoatingManager : MonoBehaviour
{
    public static SugarCoatingManager Instance { get; private set; }

    [Header("게임 오브젝트 연결")]
    public GameObject fryingPanObject; // 후라이팬 게임 오브젝트
    public SpriteRenderer skewerRenderer; // 설탕 묻히기 전/후 꼬치 이미지 표시용
    public HoneyDipperController honeyDipper; // 허니디퍼 컨트롤러 스크립트

    [Header("스프라이트 에셋")]
    public Sprite skewerBeforeCoatingSprite; // 설탕 묻히기 전 꼬치 (CustomerOrderManager에서도 가져올 수 있음)
    public Sprite skewerAfterCoatingSprite;  // 설탕 묻힌 후 꼬치
    public GameObject sparkleEffectObject1;  // 조금 문지를 때 나오는 반짝이
    public GameObject sparkleEffectObject2;  // 코팅 완료 후 나오는 반짝이

    [Header("꼬치 시각적 설정")]
    // public Vector2 desiredSkewerDisplaySize = new Vector2(1.0f, 5.0f); // Inspector에서 원하는 꼬치 크기 설정 (월드 단위)
    // public bool useSpriteOriginalSize = false; // true면 스프라이트 원본 크기를 사용하고, false면 desiredSkewerDisplaySize 사용

    [Header("게임 설정")]
    public int rubsForFirstSparkle = 5; // 첫 번째 반짝이 효과를 위한 문지르기 횟수 (rubsNeededForCoating 보다 작아야 함)
    public int rubsNeededForCoating = 10; // 설탕을 완전히 묻히기 위해 문지르는 횟수
    public float coatingDuration = 3f; // 설탕 코팅 성공 후 다음 씬으로 넘어가기 전 대기 시간 (반짝이 효과 표시 시간)
    private bool firstSparkleShown = false;

    public float rubbingDistanceThreshold = 0.1f; // 허니디퍼가 꼬치를 "문지른 것"으로 간주하기 위한 최소 이동 거리 (프레임 간)

    private int currentRubs = 0;
    private bool isCoatingPhase = false; // 허니디퍼에 설탕이 묻어 있고, 꼬치에 바르는 단계인지 여부
    private bool coatingComplete = false;
    private Vector3 lastDipperPosition; // 문지르기 감지를 위한 이전 프레임 허니디퍼 위치

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (CustomerOrderManager.Instance == null)
        {
            Debug.LogError("SugarCoatingManager Error: CustomerOrderManager.Instance가 null입니다! TitleScene으로 이동합니다.");
            // SceneManager.LoadScene("TitleScene");
            return;
        }

        if (CustomerOrderManager.Instance.CurrentOrderData == null)
        {
            Debug.LogError("SugarCoatingManager Error: CustomerOrderManager.Instance.CurrentOrderData가 null입니다! TitleScene으로 이동합니다.");
            // SceneManager.LoadScene("TitleScene");
            return;
        }

        // CustomerOrderData에서 스프라이트 로드
        skewerBeforeCoatingSprite = CustomerOrderManager.Instance.CurrentOrderData.completedSkewerSprite;
        skewerAfterCoatingSprite = CustomerOrderManager.Instance.CurrentOrderData.sugarCoatedSkewerSprite; // ★★★ 새로 추가된 필드에서 로드 ★★★

        // skewerAfterCoatingSprite가 할당되지 않았을 경우 경고
        if (skewerAfterCoatingSprite == null)
        {
            Debug.LogWarning($"SugarCoatingManager Warning: CustomerOrderData ({CustomerOrderManager.Instance.CurrentOrderData.name})에 sugarCoatedSkewerSprite가 할당되지 않았습니다!");
        }

        InitializeCoating();
    }


    void InitializeCoating()
    {
        currentRubs = 0;
        isCoatingPhase = false;
        coatingComplete = false;
        firstSparkleShown = false;

        if (skewerRenderer != null)
        {
            if (skewerBeforeCoatingSprite != null)
            {
                skewerRenderer.sprite = skewerBeforeCoatingSprite;
                AdjustSkewerSpriteSize(); // 스프라이트 할당 후 크기 조절 함수 호출
            }
            else
            {
                Debug.LogError("설탕 묻히기 전 꼬치 스프라이트(skewerBeforeCoatingSprite)가 설정되지 않았습니다!");
            }
        }
        else
        {
            Debug.LogError("꼬치 렌더러(skewerRenderer)가 연결되지 않았습니다!");
        }

        if (skewerRenderer != null && skewerBeforeCoatingSprite != null)
        {
            skewerRenderer.sprite = skewerBeforeCoatingSprite;
        }
        else
        {
            Debug.LogError("꼬치 렌더러 또는 설탕 묻히기 전 꼬치 스프라이트가 설정되지 않았습니다!");
        }

        if (honeyDipper != null)
        {
            honeyDipper.ResetDipper(); // 허니디퍼 초기 상태로 (설탕 안 묻은 상태)
        }
        else
        {
            Debug.LogError("허니디퍼 컨트롤러가 연결되지 않았습니다!");
        }
        firstSparkleShown = false; // 초기화 시 첫 번째 반짝이 표시 여부 초기화

        if (sparkleEffectObject1 != null) sparkleEffectObject1.SetActive(false);
        if (sparkleEffectObject2 != null) sparkleEffectObject2.SetActive(false);

        Debug.Log("설탕 코팅 단계 시작!");
    }

    // 허니디퍼가 후라이팬의 설탕물에 닿았을 때 HoneyDipperController에서 호출
    public void DipHoneyDipper()
    {
        if (!isCoatingPhase && !coatingComplete) // 아직 설탕을 묻히지 않았고, 코팅이 완료되지 않았다면
        {
            isCoatingPhase = true;
            Debug.Log("허니디퍼에 설탕물 묻힘!");
            // 허니디퍼 스프라이트 변경은 HoneyDipperController에서 처리
        }
    }

    // 허니디퍼가 꼬치에 문질러질 때 HoneyDipperController에서 호출
    public void RubSkewer(Vector3 currentDipperPos)
    {
        if (isCoatingPhase && !coatingComplete && honeyDipper.HasSugar())
        {
            if (Vector3.Distance(currentDipperPos, lastDipperPosition) > rubbingDistanceThreshold)
            {
                currentRubs++;
                Debug.Log("탕후루 문지르는 중... 횟수: " + currentRubs + " / " + rubsNeededForCoating);

                // 첫 번째 반짝이 효과 (아직 표시 안 됐고, 특정 횟수 도달 시)
                if (!firstSparkleShown && currentRubs >= rubsForFirstSparkle)
                {
                    if (sparkleEffectObject1 != null)
                    {
                        sparkleEffectObject1.SetActive(true);
                        Debug.Log("첫 번째 반짝이는 효과 표시.");
                        // 필요하다면 여기서 sparkleEffectObject1을 잠시 후 자동으로 사라지게 하는 코루틴 실행
                        // StartCoroutine(HideSparkleAfterDelay(sparkleEffectObject1, 1.0f));
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

    void CompleteCoating()
    {
        coatingComplete = true;
        isCoatingPhase = false;
        Debug.Log("설탕 코팅 완료!");

        if (skewerRenderer != null)
        {
            if (skewerAfterCoatingSprite != null)
            {
                skewerRenderer.sprite = skewerAfterCoatingSprite;
                AdjustSkewerSpriteSize(); // 스프라이트 변경 후 크기 조절 함수 호출
                Debug.Log("꼬치 이미지를 '설탕 묻은 후' 상태로 변경 및 크기 조절 완료.");
            }
            else
            {
                Debug.LogWarning("설탕 묻은 후 꼬치 스프라이트(skewerAfterCoatingSprite)가 할당되지 않았습니다!");
            }
        }

        if (skewerRenderer != null && skewerAfterCoatingSprite != null)
        {
            skewerRenderer.sprite = skewerAfterCoatingSprite;
        }

        // 첫 번째 반짝이가 보이고 있다면 숨김 (두 번째 반짝이와 겹치지 않도록)
        if (sparkleEffectObject1 != null && sparkleEffectObject1.activeSelf)
        {
            sparkleEffectObject1.SetActive(false);
        }

        if (sparkleEffectObject2 != null) // 두 번째 반짝이 효과 표시
        {
            sparkleEffectObject2.SetActive(true);
            Debug.Log("두 번째 (최종) 반짝이는 효과 표시.");
        }

        // ... (성공 효과음, 다음 단계 진행 로직은 동일)
        StartCoroutine(ProceedToNextStageAfterDelay(coatingDuration));
    }

    IEnumerator ProceedToNextStageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (CustomerOrderManager.Instance != null)
        {
            // CustomerOrderManager의 ProceedToNextMiniGameStep을 호출하여 다음 단계(토핑 또는 완료)로 진행
            CustomerOrderManager.Instance.ProceedToNextMiniGameStep();
        }
        else
        {
            Debug.LogError("CustomerOrderManager 인스턴스가 없어 다음 단계로 진행할 수 없습니다.");
            // 예외 처리: 타이틀 씬 등으로 이동
            SceneManager.LoadScene("TitleScene");
        }
    }

    // 게임 재시작 또는 실패 시 호출될 수 있음
    public void ResetCoatingGame()
    {
        InitializeCoating();
    }
    
    // 꼬치 스프라이트 크기 조절 함수
    void AdjustSkewerSpriteSize()
    {
        if (skewerRenderer == null || skewerRenderer.sprite == null)
        {
            Debug.LogWarning("AdjustSkewerSpriteSize: skewerRenderer 또는 sprite가 null입니다.");
            return;
        }

        // skewerRenderer의 GameObject가 실제로 화면에 표시될 최종 크기 (월드 유닛 기준)
        // 이 GameObject의 Transform Scale이 (6,6,1)로 설정되어 있다고 가정합니다.
        // 이 값이 SpriteRenderer의 '영역' 크기가 됩니다.
        float targetWorldWidth = skewerRenderer.transform.localScale.x;
        float targetWorldHeight = skewerRenderer.transform.localScale.y;

        if (targetWorldWidth <= 0 || targetWorldHeight <= 0)
        {
            Debug.LogWarning("AdjustSkewerSpriteSize: skewerRenderer의 transform.localScale 값이 유효하지 않습니다 (0 이하). 크기를 조절할 수 없습니다.");
            // 기본 크기로라도 설정하거나, 오류 처리를 할 수 있습니다.
            // skewerRenderer.transform.localScale = Vector3.one; // 예시: 안전하게 기본 스케일로
            // skewerRenderer.size = skewerRenderer.sprite.bounds.size; // SpriteRenderer의 size를 원본으로 (DrawMode Simple시엔 크게 의미 없을 수 있음)
            return;
        }

        // 스프라이트 원본 크기 (월드 유닛 기준, 현재 PixelsPerUnit과 텍스처 크기에 따라 결정됨)
        float spriteOriginalWidth = skewerRenderer.sprite.bounds.size.x;
        float spriteOriginalHeight = skewerRenderer.sprite.bounds.size.y;

        if (spriteOriginalWidth <= 0 || spriteOriginalHeight <= 0)
        {
            Debug.LogWarning("AdjustSkewerSpriteSize: 스프라이트의 원본 bounds 크기가 0입니다. 크기를 조절할 수 없습니다.");
            return;
        }

        // SpriteRenderer의 Draw Mode가 "Simple"일 때, 스프라이트가 targetWorldWidth, targetWorldHeight에 맞도록
        // 해당 GameObject의 새로운 localScale을 계산합니다.
        // 스프라이트를 SpriteRenderer에 할당하면, 해당 SpriteRenderer가 붙은 GameObject의 localScale이 (1,1,1)일 때
        // 스프라이트는 sprite.bounds.size 만큼의 월드 유닛 크기를 가집니다.
        // 따라서, 원하는 최종 월드 크기(targetWorldWidth, targetWorldHeight)가 되려면,
        // GameObject의 localScale은 (targetWorldWidth / spriteOriginalWidth, targetWorldHeight / spriteOriginalHeight, 1)이 되어야 합니다.

        // 하지만, 이미 skewerRenderer의 GameObject의 스케일이 (6,6)으로 설정되어 있고,
        // 그 "영역에 맞추고 싶다"는 것은, SpriteRenderer 자체의 스케일은 (1,1,1)로 두고,
        // SpriteRenderer의 `size` 프로퍼티를 조절하거나 (Draw Mode가 Sliced/Tiled일 때 유효),
        // 또는 Sprite의 `Pixels Per Unit`을 조절하여 렌더링 크기를 맞춰야 합니다.

        // 현재 상황(SpriteRenderer의 Transform Scale (6,6)이 목표 크기)에 대한 가장 직접적인 해결책:
        // 1. skewerRenderer의 GameObject의 Scale을 (1,1,1)로 초기화합니다.
        // 2. 이 GameObject를 원하는 최종 크기(예: 월드 유닛으로 너비 6, 높이 6)를 가진 부모 GameObject의 자식으로 넣습니다.
        //    또는, 이 GameObject의 RectTransform (UI인 경우)의 Width/Height를 6,6으로 설정합니다.
        // 3. 그런 다음 SpriteRenderer의 Draw Mode를 "Sliced"로 하고, Sprite Border를 설정하여 이미지가 늘어나지 않게 합니다.
        //    또는 Draw Mode를 "Simple"로 두고 아래처럼 스프라이트의 PPU를 고려하여 스케일링합니다.

        // "SpriteRenderer의 transform 영역에 맞추고 싶다"는 말씀은
        // skewerRenderer GameObject의 현재 world scale이 (6,6)이고,
        // 그 안에 sprite가 꽉 차게 나오되, 비율은 유지되거나, 아니면 꽉 채우는 것을 의미할 수 있습니다.

        // 여기서는 **스프라이트의 비율을 유지하면서, skewerRenderer의 Transform 영역(가로 6, 세로 6) 중
        // 더 작은 쪽에 완전히 맞춰지고, 다른 쪽은 비율에 따라 레터박스/필러박스가 생기도록 하는 방식**을 제안합니다.
        // 또는, **영역을 완전히 채우되 비율이 깨질 수 있는 방식**도 있습니다.

        // **옵션 A: 비율 유지하며 영역 안에 최대한 크게 (레터박스/필러박스 가능)**
        float worldScaleRatio = targetWorldWidth / targetWorldHeight;
        float spriteRatio = spriteOriginalWidth / spriteOriginalHeight;
        Vector3 finalLocalScale = Vector3.one;

        if (worldScaleRatio > spriteRatio) // 월드 영역이 스프라이트보다 가로로 더 길면 (스프라이트 높이에 맞춤)
        {
            finalLocalScale.y = targetWorldHeight / spriteOriginalHeight;
            finalLocalScale.x = finalLocalScale.y * spriteRatio; // 비율에 맞춰 너비 조절
        }
        else // 월드 영역이 스프라이트보다 세로로 더 길거나 같으면 (스프라이트 너비에 맞춤)
        {
            finalLocalScale.x = targetWorldWidth / spriteOriginalWidth;
            finalLocalScale.y = finalLocalScale.x / spriteRatio; // 비율에 맞춰 높이 조절
        }
        // Z 스케일은 보통 1로 둡니다.
        finalLocalScale.z = skewerRenderer.transform.localScale.z; // 기존 Z 스케일 유지 또는 1

        skewerRenderer.transform.localScale = finalLocalScale;
        Debug.Log($"꼬치 크기 조절 (비율 유지, 영역 내 맞춤): New LocalScale = {finalLocalScale}");


        // **옵션 B: 영역을 완전히 채움 (비율 깨질 수 있음) - SpriteRenderer Draw Mode: Simple**
        // 이 경우, skewerRenderer의 GameObject의 Scale이 (6,6,1)이라면,
        // SpriteRenderer 자체의 localScale은 다음과 같이 설정하여 Sprite가 부모의 Scale에 의해 최종 크기가 결정되도록 합니다.
        // skewerRenderer.transform.localScale = new Vector3(
        //     1.0f / spriteOriginalWidth,
        //     1.0f / spriteOriginalHeight,
        //     1.0f
        // );
        // Debug.Log($"꼬치 크기 조절 (영역 채움): SpriteRenderer LocalScale = {skewerRenderer.transform.localScale} (부모 스케일 ({targetWorldWidth},{targetWorldHeight})에 의해 최종 크기 결정)");
        // 이 방법은 부모의 Scale이 (6,6)이고, SpriteRenderer의 localScale을 위처럼 설정하면,
        // 최종적으로 Sprite는 (6/spriteOriginalWidth, 6/spriteOriginalHeight) 배율로 렌더링 됩니다.
        // 만약 스프라이트의 원본 월드 크기가 (예: 15, 15) 라면, (6/15, 6/15) = (0.4, 0.4) 가 되어 원하는 결과가 나올 수 있습니다.

        // **정보를 바탕으로 한 가장 적합한 해결책**:
        // "꼬치 이미지를 직접 camera에 띄웠을 때는 scale이 0.4 0.4여야 둘이 크기가 똑같아" 라는 정보는
        // 꼬치 스프라이트의 원본 월드 크기(PPU 등 고려)가 (X: 6 / 0.4 = 15, Y: 6 / 0.4 = 15) 임을 의미합니다. (대략적으로)
        // 즉, SpriteRenderer의 GameObject의 스케일이 (6,6,1)일 때,
        // SpriteRenderer 자체의 localScale은 (0.4, 0.4, 1)이 되어야 최종적으로 원하는 크기가 됩니다.

        // 따라서 AdjustSkewerSpriteSize 함수를 아래와 같이 단순화할 수 있습니다.
        // 단, 이 방식은 꼬치 스프라이트의 가로세로 비율이 1:1이 아닐 경우 이미지가 왜곡될 수 있습니다.
        // skewerRenderer.transform.localScale = new Vector3(0.4f, 0.4f, skewerRenderer.transform.localScale.z);

        // **비율을 유지하면서, (6,6) 영역에 맞추는 더 정확한 방법:**
        // (위의 옵션 A와 유사하지만, 목표 스케일 0.4를 기준으로 합니다.)
        /*
        float referenceScale = 0.4f;
        float originalAspect = spriteOriginalWidth / spriteOriginalHeight;

        float targetHeightBasedOnWidth = (targetWorldWidth / originalAspect) / spriteOriginalHeight * referenceScale;
        float targetWidthBasedOnHeight = (targetWorldHeight * originalAspect) / spriteOriginalWidth * referenceScale;

        if (targetWorldWidth / originalAspect <= targetWorldHeight) { // 너비에 맞추기 (세로가 레터박스)
            skewerRenderer.transform.localScale = new Vector3(referenceScale, targetHeightBasedOnWidth, skewerRenderer.transform.localScale.z);
        } else { // 높이에 맞추기 (가로가 레터박스)
            skewerRenderer.transform.localScale = new Vector3(targetWidthBasedOnHeight, referenceScale, skewerRenderer.transform.localScale.z);
        }
        Debug.Log($"꼬치 크기 재조정됨: {skewerRenderer.transform.localScale}");
        */
    }
}