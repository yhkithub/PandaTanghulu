// ToppingPlacementManager.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using System.Collections;


public class ToppingPlacementManager : MonoBehaviour
{
    [Header("꼬치 시각적 설정")]
    public float targetVisualHeightInWorldUnits = 5.0f;
    public static ToppingPlacementManager Instance { get; private set; }

    public Vector2 targetColliderWorldSize = new Vector2(6f, 15f); // ★★★ 목표 콜라이더 월드 크기 (x6, y15)
    public Vector2 targetColliderWorldOffset = new Vector2(0.3f, 0f);

    [Header("씬 오브젝트 연결")]
    public SpriteRenderer sugarCoatedSkewerRenderer;
    public SpriteRenderer finalSkewerRenderer;
    public GameObject toppingSelectionArea;
    public Image resultImageDisplay;
    public GameObject sparkleEffect;

    [Header("에셋 연결")]
    public Sprite clearSprite;
    public Sprite failSprite;
    public GameObject toppingItemPrefab; // UI 프리팹
    public Transform skewerDropZoneTransform;

    // ★★★ 모든 사용 가능한 토핑 아이템 정보 리스트 (Inspector에서 설정) ★★★
    [System.Serializable]
    public struct ToppingInfo
    {
        public FruitType toppingType;
        public Sprite toppingSprite; // 각 토핑 UI에 표시될 스프라이트
    }
    public List<ToppingInfo> allAvailableToppings; // 여기에 게임에 등장할 모든 토핑 정보 추가

    private CustomerOrderData currentOrder;
    private FruitType requiredTopping;
    private bool isToppingPlaced = false;
    private DraggableTopping draggedTopping = null;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        InitializeScene();
    }

    void InitializeScene()
    {
        if (CustomerOrderManager.Instance == null || CustomerOrderManager.Instance.CurrentOrderData == null)
        {
            Debug.LogError("ToppingPlacementManager: CustomerOrderManager 또는 현재 주문 데이터를 가져올 수 없습니다.");
            if (SceneSwitcher.Instance != null) SceneSwitcher.Instance.LoadScene("TitleScene");
            return;
        }

        currentOrder = CustomerOrderManager.Instance.CurrentOrderData;
        requiredTopping = currentOrder.toppingItem;

        // 1. 설탕 코팅된 탕후루 이미지 설정 및 크기 조절
        if (sugarCoatedSkewerRenderer != null)
        {
            if (currentOrder.sugarCoatedSkewerSprite != null)
            {
                sugarCoatedSkewerRenderer.sprite = currentOrder.sugarCoatedSkewerSprite;
                AdjustSkewerSpriteSize(sugarCoatedSkewerRenderer, currentOrder.sugarCoatedSkewerSprite); // 이미지 크기 먼저 조절
                sugarCoatedSkewerRenderer.gameObject.SetActive(true);
                Debug.Log($"[InitializeScene] sugarCoatedSkewerRenderer 활성화 및 스프라이트 설정: {currentOrder.sugarCoatedSkewerSprite.name}");
            }
            else
            {
                Debug.LogError($"[InitializeScene] 현재 주문({currentOrder.customerName})에 'sugarCoatedSkewerSprite'가 할당되지 않았습니다!");
                sugarCoatedSkewerRenderer.gameObject.SetActive(false);
            }

            // --- 콜라이더 설정 ---
            // skewerDropZoneTransform이 sugarCoatedSkewerRenderer의 자식이라고 가정합니다.
            // 만약 아니라면, skewerDropZoneTransform의 부모 스케일을 기준으로 계산해야 합니다.
            if (skewerDropZoneTransform != null)
            {
                BoxCollider2D dropZoneCollider = skewerDropZoneTransform.GetComponent<BoxCollider2D>();
                if (dropZoneCollider != null)
                {
                    // 부모(sugarCoatedSkewerRenderer)의 최종 월드 스케일(lossyScale)을 가져옵니다.
                    Vector3 parentWorldScale = sugarCoatedSkewerRenderer.transform.lossyScale;

                    // 부모 스케일이 0이 되는 것을 방지
                    if (Mathf.Approximately(parentWorldScale.x, 0f)) parentWorldScale.x = 1f;
                    if (Mathf.Approximately(parentWorldScale.y, 0f)) parentWorldScale.y = 1f;

                    // 목표 월드 크기를 부모의 월드 스케일로 나누어 로컬 콜라이더 Size를 설정합니다.
                    dropZoneCollider.size = new Vector2(
                        targetColliderWorldSize.x / parentWorldScale.x,
                        targetColliderWorldSize.y / parentWorldScale.y
                    );

                    // 목표 월드 오프셋도 부모의 월드 스케일로 나누어 로컬 콜라이더 Offset을 설정합니다.
                    // (오프셋은 방향도 중요하므로, 부모의 회전 등도 고려해야 할 수 있으나, 여기서는 스케일만 고려)
                    dropZoneCollider.offset = new Vector2(
                        targetColliderWorldOffset.x / parentWorldScale.x,
                        targetColliderWorldOffset.y / parentWorldScale.y
                    );

                    Debug.Log($"[InitializeScene] DropZoneCollider - ParentWorldScale: {parentWorldScale}, Calculated Local Size: {dropZoneCollider.size}, Calculated Local Offset: {dropZoneCollider.offset}");
                    skewerDropZoneTransform.gameObject.tag = "TanghuluDropZone"; // 태그 설정 확인
                }
                else
                {
                    Debug.LogError("skewerDropZoneTransform에 BoxCollider2D가 없습니다!");
                }
            }
            else
            {
                Debug.LogWarning("skewerDropZoneTransform이 연결되지 않았습니다. 콜라이더 설정을 건너뜁니다.");
            }
        }
        else
        {
            Debug.LogError("[InitializeScene] sugarCoatedSkewerRenderer가 Inspector에 연결되지 않았습니다!");
        }

        // ... (나머지 InitializeScene 코드: finalSkewerRenderer, resultImageDisplay, sparkleEffect, SetupToppingChoices 등) ...
        if (finalSkewerRenderer != null) finalSkewerRenderer.gameObject.SetActive(false);
        if (resultImageDisplay != null) resultImageDisplay.gameObject.SetActive(false);
        if (sparkleEffect != null) sparkleEffect.SetActive(false);
        SetupToppingChoices();
        isToppingPlaced = false;
    }

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

        float spriteOriginalWorldWidth = spritePixelWidth / ppu;
        float spriteOriginalWorldHeight = spritePixelHeight / ppu;

        if (spriteOriginalWorldHeight == 0) {
                Debug.LogWarning($"Sprite '{spriteToScale.name}'의 PPU ({ppu}) 또는 픽셀 높이 ({spritePixelHeight})가 0이거나 유효하지 않아 월드 높이를 계산할 수 없습니다.");
                return;
        }

        float scaleMultiplier = targetVisualHeightInWorldUnits / spriteOriginalWorldHeight;

        Vector3 newLocalScale = new Vector3(
            scaleMultiplier * (spriteOriginalWorldWidth / spriteOriginalWorldHeight),
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
            // newLocalScale.z /= parentWorldScale.z; // 2D에서는 Z 스케일은 부모에 의해 영향받지 않도록 1로 유지하는 것이 일반적
        }

        objectTransform.localScale = newLocalScale;
        Debug.Log($"{renderer.gameObject.name} ({spriteToScale.name}) 로컬 스케일 조정됨: {newLocalScale}, 부모 스케일: {parentWorldScale}, 목표 월드 높이: {targetVisualHeightInWorldUnits}");
    }

    void SetupToppingChoices()
    {
        if (toppingSelectionArea == null || toppingItemPrefab == null)
        {
            Debug.LogError("토핑 선택 영역 또는 토핑 프리팹이 설정되지 않았습니다.");
            return;
        }

        foreach (Transform child in toppingSelectionArea.transform)
        {
            Destroy(child.gameObject);
        }

        List<ToppingInfo> toppingsToShow = new List<ToppingInfo>();

        if (CustomerOrderManager.Instance != null && CustomerOrderManager.Instance.isTutorialActive)
        {
            Debug.Log("튜토리얼 모드: 현재 손님의 토핑만 표시합니다.");
            if (requiredTopping != FruitType.None)
            {
                ToppingInfo requiredToppingInfo = allAvailableToppings.FirstOrDefault(t => t.toppingType == requiredTopping);
                if (requiredToppingInfo.toppingSprite != null && requiredToppingInfo.toppingType != FruitType.None) // toppingType도 None이 아닌지 확인
                {
                    toppingsToShow.Add(requiredToppingInfo);
                }
                else
                {
                    Debug.LogWarning($"튜토리얼: 필수 토핑 '{requiredTopping}'에 대한 정보가 'allAvailableToppings' 리스트에 없거나 유효하지 않은 스프라이트/타입입니다. Inspector를 확인해주세요.");
                    // 튜토리얼인데 필수 토핑을 못 찾으면, 비어있는 상태로 두거나, 에러 처리를 할 수 있습니다.
                    // 여기서는 일단 비워두어 아무 토핑도 표시되지 않게 합니다.
                }
            }
            else {
                Debug.LogWarning("튜토리얼 모드이지만, 현재 손님에게 requiredTopping이 FruitType.None으로 설정되어 있습니다.");
            }
        }
        else
        {
            Debug.Log("일반 모드: 모든 사용 가능한 토핑을 표시합니다.");
            toppingsToShow.AddRange(allAvailableToppings.Where(t => t.toppingSprite != null && t.toppingType != FruitType.None));
        }
        
        if(toppingsToShow.Count == 0) {
            Debug.LogWarning("화면에 표시할 토핑 아이템이 없습니다. 튜토리얼 상태 또는 'allAvailableToppings' 설정을 확인하세요.");
        }

        foreach (ToppingInfo toppingInfo in toppingsToShow)
        {
            GameObject toppingObj = Instantiate(toppingItemPrefab, toppingSelectionArea.transform);
            DraggableTopping draggable = toppingObj.GetComponent<DraggableTopping>();
            Image toppingImage = toppingObj.GetComponent<Image>();

            if (draggable != null)
            {
                draggable.toppingType = toppingInfo.toppingType;
            }

            if (toppingImage != null && toppingInfo.toppingSprite != null)
            {
                toppingImage.sprite = toppingInfo.toppingSprite;
            }
            else if (toppingImage != null && toppingInfo.toppingSprite == null)
            {
                Debug.LogWarning(toppingInfo.toppingType + " 토핑의 스프라이트가 'allAvailableToppings'에 설정되지 않았습니다.");
                // 필요하다면 기본 이미지 설정 또는 비활성화
                // toppingImage.gameObject.SetActive(false);
            }
            toppingObj.name = "Topping_" + toppingInfo.toppingType.ToString();
        }
    }

    public void StartDraggingTopping(DraggableTopping topping)
    {
        draggedTopping = topping;
    }

    public void PlaceToppingOnSkewer(FruitType placedToppingType)
    {
        Debug.Log($"PlaceToppingOnSkewer 호출됨. 놓인 토핑: {placedToppingType}, 필요한 토핑: {requiredTopping}");

        if (isToppingPlaced) // isToppingPlaced는 여기서 체크하는 것보다 DraggableTopping에서 먼저 체크하는 것이 나을 수 있습니다.
        {
            Debug.LogWarning("이미 토핑이 놓였습니다. 중복 처리 방지.");
            return;
        }
        
        // draggedTopping은 DraggableTopping.OnEndDrag에서 null 처리됨
        // if (draggedTopping == null)
        // {
        //     Debug.LogWarning("드래그 중인 토핑 정보가 없습니다.");
        //    return;
        // }

        isToppingPlaced = true; 

        if (placedToppingType == requiredTopping)
        {
            HandleSuccess();
        }
        else
        {
            HandleFailure();
        }
    }

    void HandleSuccess()
    {
        Debug.Log("토핑 꽂기 성공!");
        if (resultImageDisplay != null && clearSprite != null)
        {
            resultImageDisplay.sprite = clearSprite;
            resultImageDisplay.gameObject.SetActive(true);
            AudioManager.Instance?.PlayOneShotSound("Success");
        }
        if (sparkleEffect != null)
        {
            sparkleEffect.SetActive(true);
        }

        // ★★★ 최종 토핑된 탕후루 이미지로 변경 ★★★
        if (finalSkewerRenderer != null && currentOrder.skewerWithToppingSprite != null)
        {
            if (sugarCoatedSkewerRenderer != null)
            {
                sugarCoatedSkewerRenderer.gameObject.SetActive(false); // 코팅만 된 이미지 숨김
            }
            finalSkewerRenderer.sprite = currentOrder.skewerWithToppingSprite;
            AdjustSkewerSpriteSize(finalSkewerRenderer, currentOrder.skewerWithToppingSprite); // 크기 조절
            finalSkewerRenderer.gameObject.SetActive(true);
            Debug.Log($"최종 꼬치 이미지 ({currentOrder.skewerWithToppingSprite.name})로 변경 및 활성화됨.");
        }
        else
        {
            // finalSkewerRenderer나 skewerWithToppingSprite가 없으면,
            // sugarCoatedSkewerRenderer 위에 토핑 UI를 직접 배치하는 방식을 고려하거나,
            // 현재처럼 sugarCoatedSkewerRenderer를 그대로 보여줍니다.
            Debug.LogWarning("finalSkewerRenderer 또는 currentOrder.skewerWithToppingSprite가 설정되지 않아 최종 꼬치 이미지로 변경할 수 없습니다. sugarCoatedSkewerRenderer를 계속 사용합니다.");
            // 이 경우, 토핑은 시각적으로 꼬치 위에 '얹혀진' 형태로만 남게 됩니다 (별도의 GameObject로).
            // 토핑이 꽂힌 후의 '하나의 합쳐진 이미지'를 원한다면 skewerWithToppingSprite 사용이 필수입니다.
        }
        
        AudioManager.Instance?.PlayOneShotSound("ToppingSuccessSound");
        StartCoroutine(ProceedToNextCustomerAfterDelay(2.0f));
    }

    void HandleFailure()
    {
        Debug.Log("토핑 꽂기 실패!");
        if (resultImageDisplay != null && failSprite != null)
        {
            resultImageDisplay.sprite = failSprite;
            resultImageDisplay.gameObject.SetActive(true);
            AudioManager.Instance?.PlayOneShotSound("Fail");
        }

        AudioManager.Instance?.PlayOneShotSound("ToppingFailureSound");

        if (CustomerOrderManager.Instance != null && !CustomerOrderManager.Instance.isTutorialActive)
        {
            HeartManager.Instance?.LoseHeart();
        } else if (CustomerOrderManager.Instance != null && CustomerOrderManager.Instance.isTutorialActive)
        {
            Debug.Log("튜토리얼 중이므로 하트가 차감되지 않습니다.");
        }
        
        StartCoroutine(ResetForRetry(1.5f));
    }

    IEnumerator ResetForRetry(float delay)
    {
        yield return new WaitForSeconds(delay);
        isToppingPlaced = false; 
        if (resultImageDisplay != null) resultImageDisplay.gameObject.SetActive(false);
        SetupToppingChoices(); // 토핑 선택 UI 다시 구성
    }

    IEnumerator ProceedToNextCustomerAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if(CustomerOrderManager.Instance != null)
        {
            CustomerOrderManager.Instance.AllMiniGamesCompletedForCurrentCustomer();
        }
        else
        {
            Debug.LogError("CustomerOrderManager 인스턴스를 찾을 수 없어 다음 단계로 진행할 수 없습니다.");
            if(SceneSwitcher.Instance != null) SceneSwitcher.Instance.LoadScene("TitleScene");
        }
    }
}