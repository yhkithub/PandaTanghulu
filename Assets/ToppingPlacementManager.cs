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
    public Transform skewerParent; 
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
        // 씬 시작 시 모드를 직접 확인하여 분기 처리
        if (GameModeManager.IsEndlessMode)
        {
            if (skewerParent != null && sugarCoatedSkewerRenderer != null)
            {
                skewerParent.position = sugarCoatedSkewerRenderer.transform.position;
            }

            if (sugarCoatedSkewerRenderer != null) sugarCoatedSkewerRenderer.gameObject.SetActive(false);
            if (finalSkewerRenderer != null) finalSkewerRenderer.gameObject.SetActive(false);
            if (resultImageDisplay != null) resultImageDisplay.gameObject.SetActive(false);
            
            // ✅ [핵심 추가] 토핑 씬에서 코팅 효과를 다시 그려줍니다.
            if (SkewerVisualizer.Instance != null && skewerParent != null)
            {
                var orderData = CustomerOrderManager.Instance.CurrentOrderData;
                SkewerVisualizer.Instance.DisplaySkewer(skewerParent, orderData.skewerOrder, targetVisualHeightInWorldUnits);
                
                // ✅ [수정] 이전 코팅 함수 호출 대신, 새로 만든 마스크 기반 함수를 호출합니다.
                // 코팅이 완료된 상태이므로 progress 값을 1.0f로 전달합니다.
                SkewerVisualizer.Instance.ApplyMaskedSugarCoating(skewerParent, 1.0f);
            }

            currentOrder = CustomerOrderManager.Instance.CurrentOrderData;
            requiredTopping = currentOrder.toppingItem;
            isToppingPlaced = false;
            SetupToppingChoices();
        }
        else
        {
            InitializeScene();
        }
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

        if (finalSkewerRenderer != null) finalSkewerRenderer.gameObject.SetActive(false);
        if (resultImageDisplay != null) resultImageDisplay.gameObject.SetActive(false);
        if (sparkleEffect != null) sparkleEffect.SetActive(false);
        SetupToppingChoices();
        isToppingPlaced = false;
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
        AudioManager.Instance?.PlayOneShotSound("Success");
        if (sparkleEffect != null)
        {
            sparkleEffect.SetActive(true);
        }

        // [수정] 무한모드와 스테이지 모드의 시각적 처리를 분리합니다.
        if (GameModeManager.IsEndlessMode)
        {
            // [무한모드]
            // 1. SkewerVisualizer를 통해 토핑을 먼저 시각적으로 추가합니다.
            if (SkewerVisualizer.Instance != null)
            {
                SkewerVisualizer.Instance.AddTopping(requiredTopping);
            }
            
            // 2. 꼬치가 가려지지 않도록 Clear 이미지는 비활성화합니다.
            if (resultImageDisplay != null)
            {
                resultImageDisplay.gameObject.SetActive(false);
            }
        }
        else
        {
            // [스테이지 모드] (기존 로직)
            if (resultImageDisplay != null && clearSprite != null)
            {
                resultImageDisplay.sprite = clearSprite;
                resultImageDisplay.gameObject.SetActive(true);
            }

            if (finalSkewerRenderer != null && currentOrder.skewerWithToppingSprite != null)
            {
                if (sugarCoatedSkewerRenderer != null) sugarCoatedSkewerRenderer.gameObject.SetActive(false);
                finalSkewerRenderer.sprite = currentOrder.skewerWithToppingSprite;
                finalSkewerRenderer.gameObject.SetActive(true);
            }
        }
        
        // [핵심] 모든 시각적 처리가 끝난 후, 공통적으로 다음 단계 진행 코루틴을 호출합니다.
        // 이렇게 하면 무한모드에서도 토핑이 추가된 모습을 2초간 본 후에 다음으로 넘어갑니다.
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

    public void ResetToppingChoices()
    {
        // 이전에 실패 처리용으로 만들었던 코루틴을 재활용할 수 있습니다.
        // 실패 시 하트 차감 로직 등은 없으므로, UI만 재설정합니다.
        isToppingPlaced = false; 
        if (resultImageDisplay != null) resultImageDisplay.gameObject.SetActive(false);
        SetupToppingChoices(); // 토핑 선택 UI 다시 구성
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