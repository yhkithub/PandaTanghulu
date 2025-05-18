// CustomerOrderManager.cs
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI; // UI.Image 사용
using System.Linq;    // SequenceEqual 등 사용

public class CustomerOrderManager : MonoBehaviour
{
    [Header("주문서 UI 구성 요소")]
    public GameObject orderDisplayBackgroundPanel; // 주문서 배경 Panel (예: UI_Bill)
    public GameObject fruitsContainerForOrderUI; // 과일 이미지들이 담길 자식 Panel (VerticalLayoutGroup 적용 권장)
    public Image skewerStickImagePrefab_UI;   // 주문서에 표시될 꼬치 막대 UI Image 프리팹 (선택 사항)
    public Image fruitImagePrefab_UI;         // 주문서에 표시될 개별 과일 UI Image 프리팹

    public bool isTutorialActive = true; // 게임 시작 시 true로 설정


    // 과일 타입에 따른 스프라이트 매핑 (Inspector에서 설정)
    [System.Serializable]
    public struct FruitSpriteMapping
    {
        public FruitType fruitType;
        public Sprite sprite;
    }
    public List<FruitSpriteMapping> fruitSpritesForOrderUI;
    private Dictionary<FruitType, Sprite> fruitSpriteDic;

    [Header("손님 주문 데이터 목록")]
    public List<CustomerOrderData> allCustomerOrders; // 여기에 손님별 주문 데이터 ScriptableObject들을 연결

    public CustomerOrderData CurrentOrderData { get; private set; }
    public List<FruitType> CurrentRequiredFruits { get; private set; } = new List<FruitType>();

    private int currentCustomerIndex = 0;

    public static CustomerOrderManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        fruitSpriteDic = new Dictionary<FruitType, Sprite>();
        if (fruitSpritesForOrderUI != null)
        {
            foreach (var mapping in fruitSpritesForOrderUI)
            {
                if (!fruitSpriteDic.ContainsKey(mapping.fruitType))
                {
                    fruitSpriteDic.Add(mapping.fruitType, mapping.sprite);
                }
            }
        }
    }

    void Start()
    {
        if (allCustomerOrders == null || allCustomerOrders.Count == 0)
        {
            Debug.LogError("CustomerOrderManager: 손님 주문 데이터(allCustomerOrders)가 설정되지 않았습니다! Inspector를 확인해주세요.");
            if (orderDisplayBackgroundPanel != null) orderDisplayBackgroundPanel.SetActive(false);
            return;
        }
        LoadCustomerOrder(currentCustomerIndex);
    }

    public void LoadNextCustomerOrder()
    {
        currentCustomerIndex++;
        if (currentCustomerIndex >= allCustomerOrders.Count)
        {
            Debug.Log("모든 손님의 주문을 완료했습니다!");
            currentCustomerIndex = 0; // 예시: 처음으로 돌아감
        }
        LoadCustomerOrder(currentCustomerIndex);
    }

    void LoadCustomerOrder(int customerIndex)
    {
        if (customerIndex < 0 || customerIndex >= allCustomerOrders.Count)
        {
            Debug.LogError("CustomerOrderManager: 유효하지 않은 손님 인덱스입니다: " + customerIndex);
            return;
        }

        CurrentOrderData = allCustomerOrders[customerIndex];
        CurrentRequiredFruits.Clear();
        if (CurrentOrderData != null && CurrentOrderData.skewerOrder != null)
        {
            foreach (OrderItem item in CurrentOrderData.skewerOrder)
            {
                CurrentRequiredFruits.Add(item.fruit);
            }
        }
        Debug.Log(CurrentOrderData.customerName + " 손님의 주문 로드 완료. 주문: " + string.Join(", ", CurrentRequiredFruits.Select(f => f.ToString())));
        DisplayOrderOnUI();
    }

    void DisplayOrderOnUI()
    {
        if (fruitsContainerForOrderUI == null || fruitImagePrefab_UI == null || CurrentOrderData == null)
        {
            Debug.LogError("주문서 표시에 필요한 UI 요소가 없습니다! (fruitsContainerForOrderUI, fruitImagePrefab_UI, CurrentOrderData)");
            if (orderDisplayBackgroundPanel != null) orderDisplayBackgroundPanel.SetActive(false);
            return;
        }

        if (orderDisplayBackgroundPanel != null) orderDisplayBackgroundPanel.SetActive(true);

        foreach (Transform child in fruitsContainerForOrderUI.transform)
        {
            Destroy(child.gameObject);
        }

        // 꼬치 막대 이미지 생성 (fruitsContainerForOrderUI의 자식으로, 가장 먼저 또는 가장 나중에 추가하여 순서 조절)
        Image stickInstance = null; // stickInstance 변수 선언 위치 변경
        if (skewerStickImagePrefab_UI != null)
        {
            stickInstance = Instantiate(skewerStickImagePrefab_UI, fruitsContainerForOrderUI.transform);
            stickInstance.name = "SkewerStick_InOrderUI";
            // Layout Group을 사용한다면 막대의 순서(Sibling Index)가 중요합니다.
            // 예: 과일보다 먼저(뒤에) 그려지게 하려면 SetAsFirstSibling() 사용
            stickInstance.transform.SetAsFirstSibling();
        }

        if (CurrentOrderData.skewerOrder != null && CurrentOrderData.skewerOrder.Count > 0)
        {
            // 주문서 표시 순서 결정 (true: 주문 데이터 0번이 가장 위, false: 0번이 가장 아래)
            // 탕후루는 보통 아래에서 위로 꽂으므로, 주문서에서 0번 항목(첫번째 꽂는 과일)이
            // 아래에 표시되게 하려면 리스트를 뒤집거나 Layout Group의 Reverse Arrangement를 사용합니다.
            // 여기서는 Layout Group에서 처리한다고 가정하고, 주문 데이터 순서대로 생성합니다.
            // (Vertical Layout Group의 Child Alignment: Upper Center, Reverse Arrangement: false 라면 0번이 가장 위)
            // (Vertical Layout Group의 Child Alignment: Bottom Center, Reverse Arrangement: false 라면 0번이 가장 아래)

            List<OrderItem> orderItemsToDisplay = CurrentOrderData.skewerOrder;

            // 만약 Vertical Layout Group을 Upper Center로 설정하고,
            // 주문 데이터의 0번(첫번째 꽂는 과일)이 UI상 가장 아래에 보이길 원한다면 아래처럼 리스트를 뒤집습니다.
            // orderItemsToDisplay = new List<OrderItem>(CurrentOrderData.skewerOrder);
            // orderItemsToDisplay.Reverse();


            foreach (OrderItem item in orderItemsToDisplay)
            {
                if (fruitSpriteDic.TryGetValue(item.fruit, out Sprite fruitSpriteToShow))
                {
                    Image fruitUI = Instantiate(fruitImagePrefab_UI, fruitsContainerForOrderUI.transform);
                    fruitUI.sprite = fruitSpriteToShow;
                    fruitUI.name = item.fruit.ToString() + "_OrderUI";
                }
                else
                {
                    Debug.LogWarning("CustomerOrderManager: 주문서 UI에 표시할 " + item.fruit.ToString() + " 타입의 스프라이트를 fruitSpritesForOrderUI에서 찾을 수 없습니다.");
                }
            }
            Debug.Log(CurrentOrderData.customerName + " 손님의 주문을 UI에 동적으로 표시했습니다.");
        }
        else
        {
            Debug.LogWarning("CustomerOrderManager: " + CurrentOrderData.customerName + " 손님의 주문 내용(skewerOrder)이 비어있거나 없습니다.");
        }
    }

    public bool CheckOrder(List<FruitType> collectedPlayerFruits)
    {
        if (CurrentOrderData == null || CurrentRequiredFruits.Count == 0)
        {
            Debug.LogWarning("CustomerOrderManager: 현재 생성된 주문이 없어서 확인할 수 없습니다.");
            return false;
        }

        bool orderMatch = collectedPlayerFruits.SequenceEqual(CurrentRequiredFruits);

        if (orderMatch)
        {
            Debug.Log("주문 성공! (" + CurrentOrderData.customerName + ")");
            LoadNextCustomerOrder();
        }
        else
        {
            string playerOrderStr = string.Join(", ", collectedPlayerFruits.Select(f => f.ToString()));
            string correctOrderStr = string.Join(", ", CurrentRequiredFruits.Select(f => f.ToString()));
            Debug.Log("주문 실패! (" + CurrentOrderData.customerName + ")\n플레이어 제출: [" + playerOrderStr + "]\n정답: [" + correctOrderStr + "]");

            if (HeartManager.Instance != null)
            {
                HeartManager.Instance.LoseHeart();
            }
            else
            {
                Debug.LogError("CustomerOrderManager: HeartManager 인스턴스를 찾을 수 없어 하트를 차감할 수 없습니다.");
            }
        }
        return orderMatch;
    }
}