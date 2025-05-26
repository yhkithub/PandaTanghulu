// OrderDisplayUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshProUGUI 사용 시
using System.Collections.Generic;
using System.Linq;

public class OrderDisplayUI : MonoBehaviour
{
    [Header("이 씬의 주문서 UI 요소들")]
    public GameObject orderDisplayBackgroundPanel_Scene; // 이 씬의 주문서 배경 패널
    public Transform fruitsContainerForOrderUI_Scene;   // 이 씬의 과일 아이콘 컨테이너
    public Image skewerStickImagePrefab_SceneUI;       // 이 씬에서 사용할 꼬치 아이콘 프리팹
    public Image fruitImagePrefab_SceneUI;             // 이 씬에서 사용할 과일 아이콘 프리팹
    // 필요시 손님 이름 표시용 TextMeshProUGUI 등 추가

    void Start()
    {
        if (CustomerOrderManager.Instance != null)
        {
            CustomerOrderManager.Instance.OnOrderLoaded += UpdateOrderDisplay;
            // 초기 주문 정보가 이미 로드되었을 수 있으므로, Start에서 한번 호출
            if (CustomerOrderManager.Instance.CurrentOrderData != null)
            {
                UpdateOrderDisplay();
            }
            else
            {
                 if (orderDisplayBackgroundPanel_Scene != null) orderDisplayBackgroundPanel_Scene.SetActive(false);
            }
        }
        else
        {
            Debug.LogError("OrderDisplayUI: CustomerOrderManager.Instance를 찾을 수 없습니다.");
            if (orderDisplayBackgroundPanel_Scene != null) orderDisplayBackgroundPanel_Scene.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (CustomerOrderManager.Instance != null)
        {
            CustomerOrderManager.Instance.OnOrderLoaded -= UpdateOrderDisplay;
        }
    }

    void UpdateOrderDisplay()
    {
        if (CustomerOrderManager.Instance == null || CustomerOrderManager.Instance.CurrentOrderData == null)
        {
            if (orderDisplayBackgroundPanel_Scene != null) orderDisplayBackgroundPanel_Scene.SetActive(false);
            Debug.LogWarning("OrderDisplayUI: 현재 주문 정보가 없어 UI를 업데이트할 수 없습니다.");
            return;
        }

        if (fruitsContainerForOrderUI_Scene == null || fruitImagePrefab_SceneUI == null)
        {
            Debug.LogError("OrderDisplayUI: 주문서 표시에 필요한 UI 요소(fruitsContainer 또는 fruitImagePrefab)가 Inspector에 연결되지 않았습니다!");
            if (orderDisplayBackgroundPanel_Scene != null) orderDisplayBackgroundPanel_Scene.SetActive(true); // 패널은 보이되 내용은 없을 수 있음
            return;
        }

        if (orderDisplayBackgroundPanel_Scene != null) orderDisplayBackgroundPanel_Scene.SetActive(true);

        // 기존 과일 아이콘들 삭제
        foreach (Transform child in fruitsContainerForOrderUI_Scene)
        {
            Destroy(child.gameObject);
        }

        CustomerOrderData currentOrder = CustomerOrderManager.Instance.CurrentOrderData;

        // 꼬치 막대 이미지 추가 (선택 사항)
        if (skewerStickImagePrefab_SceneUI != null)
        {
            Image stickInstance = Instantiate(skewerStickImagePrefab_SceneUI, fruitsContainerForOrderUI_Scene);
            stickInstance.name = "SkewerStick_OrderDisplay";
            stickInstance.transform.SetAsFirstSibling(); // 꼬치가 맨 뒤에 오도록
        }

        // 현재 주문의 과일들 표시 (CurrentRequiredSkewerFruits 또는 CurrentOrderData.skewerOrder 사용)
        List<FruitType> fruitsToDisplay = CustomerOrderManager.Instance.CurrentRequiredSkewerFruits;
        // 만약 CurrentRequiredSkewerFruits가 비어있다면, CurrentOrderData.skewerOrder를 직접 참조할 수도 있습니다.
        if (fruitsToDisplay == null || fruitsToDisplay.Count == 0) {
            if (CustomerOrderManager.Instance.CurrentOrderData != null && CustomerOrderManager.Instance.CurrentOrderData.skewerOrder != null) {
                fruitsToDisplay = CustomerOrderManager.Instance.CurrentOrderData.skewerOrder.Select(item => item.fruit).ToList();
            } else {
                Debug.LogWarning("OrderDisplayUI: 표시할 과일 데이터가 없습니다.");
                return; // 과일 데이터 없으면 더 이상 진행 안 함
            }
        }

        Debug.Log($"OrderDisplayUI: 표시할 과일 개수: {fruitsToDisplay.Count}"); // 실제 개수 로깅

        foreach (FruitType fruit in fruitsToDisplay)
        {
            Sprite fruitSprite = CustomerOrderManager.Instance.GetSpriteForFruitUI(fruit);
            if (fruitSprite != null)
            {
                Image fruitIconInstance = Instantiate(fruitImagePrefab_SceneUI, fruitsContainerForOrderUI_Scene);
                fruitIconInstance.sprite = fruitSprite; // ★★★ 여기서 실제 과일 스프라이트 할당 ★★★
                fruitIconInstance.name = fruit.ToString() + "_OrderIcon_InScene";
                fruitIconInstance.color = Color.white; // 흰색으로 설정하여 스프라이트 원본 색상 유지 (필요시 알파 조절)
            }
            else
            {
                Debug.LogWarning($"OrderDisplayUI: {fruit}에 대한 스프라이트를 CustomerOrderManager에서 가져올 수 없습니다.");
            }
        }
        Debug.Log($"OrderDisplayUI: {currentOrder.customerName} 손님의 주문을 현재 씬의 UI에 표시했습니다.");
    }
}