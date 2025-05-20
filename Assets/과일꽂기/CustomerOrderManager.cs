// CustomerOrderManager.cs
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;
using TMPro;

public class CustomerOrderManager : MonoBehaviour
{

    public GameState currentGameState = GameState.TutorialDisplay; // 초기 상태
    public bool isTutorialActive = false;

    public static CustomerOrderManager Instance { get; private set; }
    [Header("씬 전환 설정")]
    public string fruitCatchingSceneName = "FruitCatchingGameScene"; // 과일 꽂기 씬 이름

    private CustomerOrderData currentCustomerDataForDialogue; // 현재 대화할 손님의 전체 데이터


    [System.Serializable]
    public struct FruitSpriteMapping
    {
        public FruitType fruitType;
        public Sprite sprite;
    }

    [Header("UI 구성 요소")]
    public GameObject orderDisplayBackgroundPanel;
    public GameObject fruitsContainerForOrderUI;
    public Image skewerStickImagePrefab_UI;
    public Image fruitImagePrefab_UI;

    [Header("튜토리얼 UI 요소")]
    public GameObject tutorialPanel_UI;
    public TextMeshProUGUI tutorialMessageText_UI;
    public Button startGameButton_UI;

    [Header("데이터")]
    public List<FruitSpriteMapping> fruitSpritesForOrderUI;
    public List<CustomerOrderData> allCustomerOrders;

    private Dictionary<FruitType, Sprite> fruitSpriteDic;
    public CustomerOrderData CurrentOrderData { get; private set; }
    public List<FruitType> CurrentRequiredFruits { get; private set; } = new List<FruitType>();
    private int currentCustomerIndex = 0;

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
                if (mapping.sprite != null && !fruitSpriteDic.ContainsKey(mapping.fruitType))
                {
                    fruitSpriteDic.Add(mapping.fruitType, mapping.sprite);
                }
                else if (mapping.sprite == null)
                {
                    Debug.LogWarning("FruitSpriteMapping: " + mapping.fruitType + "에 대한 Sprite가 할당되지 않았습니다.");
                }
            }
        }
    }

    void Start()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

         if (allCustomerOrders == null || allCustomerOrders.Count == 0)
        {
            Debug.LogError("CustomerOrderManager: 손님 주문 데이터(allCustomerOrders)가 설정되지 않았습니다!");
            if (tutorialPanel_UI != null) tutorialPanel_UI.SetActive(false);
            currentGameState = GameState.Playing; // 데이터 없으면 바로 플레이 (또는 오류 처리)
            return;
        }

        if (startGameButton_UI != null)
        {
            startGameButton_UI.onClick.AddListener(EndTutorialAndStartGame);
        }

        SetupInitialGameState(); // 이 함수가 currentCustomerIndex를 사용하여 튜토리얼 또는 일반 게임 시작
        
        // ★★★ GameInfoHolder에서 로드할 손님 인덱스 가져오기 ★★★
        currentCustomerIndex = GameInfoHolder.CustomerIndexToLoad;
        Debug.Log("과일 꽂기 씬 시작 - 로드할 손님 인덱스: " + currentCustomerIndex);

        SetupInitialGameState(); // 이 함수가 currentCustomerIndex를 사용하여 튜토리얼 또는 일반 게임 시작
    }
    
    void SetupInitialGameState()
    {
        // 첫 번째 손님(currentCustomerIndex == 0)일 때 튜토리얼 시작
        if (currentCustomerIndex == 0)
        {
            isTutorialActive = true;
            currentGameState = GameState.TutorialDisplay;
            Debug.Log("튜토리얼 모드 진입: 끼끼 손님. 게임 시작 대기 중.");

            if (tutorialPanel_UI != null)
            {
                tutorialPanel_UI.SetActive(true);
                if (tutorialMessageText_UI != null)
                {
                    tutorialMessageText_UI.text = " ";
                }
            }
            else
            {
                Debug.LogWarning("CustomerOrderManager: TutorialPanel_UI가 연결되지 않았습니다.");
            }

            if (FruitSpawner2D.Instance != null)
            {
                FruitSpawner2D.Instance.StopSpawningCompletely(); // 과일 스폰 완전 중지
            }
        }
        else // 튜토리얼이 아닌 일반 손님
        {
            isTutorialActive = false;
            currentGameState = GameState.Playing;
            if (tutorialPanel_UI != null) tutorialPanel_UI.SetActive(false);
            LoadOrderForCurrentCustomer(); // 함수 이름 변경 및 호출
        }
    }

    public void EndTutorialAndStartGame()
    {
        if (currentGameState == GameState.TutorialDisplay)
        {
            Debug.Log("튜토리얼 설명 종료. 게임을 시작합니다!");
            currentGameState = GameState.Playing; // 게임 상태 변경

            if (tutorialPanel_UI != null)
            {
                tutorialPanel_UI.SetActive(false);
            }
            LoadOrderForCurrentCustomer(); // 주문 로드 및 UI 표시, 스포너 시작
        }
    }
    
    // LoadCustomerOrder에서 스포너 시작 로직 분리하여 재사용성 높임
    void LoadOrderForCurrentCustomer()
    {
        LoadCustomerOrder(currentCustomerIndex);
        if (FruitSpawner2D.Instance != null)
        {
            FruitSpawner2D.Instance.StartSpawning();
        }
    }

    public void LoadNextCustomerOrder()
    {
        currentCustomerIndex++;
        if (currentCustomerIndex >= allCustomerOrders.Count)
        {
            Debug.Log("모든 손님의 주문을 완료했습니다!");
            currentCustomerIndex = 0; 
            SetupInitialGameState(); // 게임 사이클 다시 시작 (튜토리얼 포함)
            return;
        }
        isTutorialActive = false; 
        currentGameState = GameState.Playing; 
        if (tutorialPanel_UI != null) tutorialPanel_UI.SetActive(false); 

        LoadOrderForCurrentCustomer();
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
        Debug.Log(CurrentOrderData.customerName + " 손님의 주문 로드 완료. 주문: " + string.Join(", ", CurrentRequiredFruits.Select(f => f.ToString())) + (isTutorialActive ? " (튜토리얼 진행중)" : ""));
        DisplayOrderOnUI();
    }

    void DisplayOrderOnUI()
    {
        if (fruitsContainerForOrderUI == null || fruitImagePrefab_UI == null || CurrentOrderData == null)
        {
            Debug.LogError("주문서 표시에 필요한 UI 요소가 없습니다!");
            if (orderDisplayBackgroundPanel != null) orderDisplayBackgroundPanel.SetActive(false);
            return;
        }

        if (orderDisplayBackgroundPanel != null) orderDisplayBackgroundPanel.SetActive(true);

        foreach (Transform child in fruitsContainerForOrderUI.transform)
        {
            Destroy(child.gameObject);
        }

        Image stickInstance = null;
        if (skewerStickImagePrefab_UI != null)
        {
            stickInstance = Instantiate(skewerStickImagePrefab_UI, fruitsContainerForOrderUI.transform);
            stickInstance.name = "SkewerStick_InOrderUI";
            stickInstance.transform.SetAsFirstSibling();
        }

        if (CurrentOrderData.skewerOrder != null && CurrentOrderData.skewerOrder.Count > 0)
        {
            List<OrderItem> orderItemsToDisplay = CurrentOrderData.skewerOrder;
            // 주문서 UI 표시 순서 로직 (필요시 orderItemsToDisplay.Reverse(); 등 사용)

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
                    Debug.LogWarning("CustomerOrderManager: 주문서 UI에 표시할 " + item.fruit.ToString() + " 타입의 스프라이트를 찾을 수 없습니다.");
                }
            }
            //Debug.Log(CurrentOrderData.customerName + " 손님의 주문을 UI에 동적으로 표시했습니다."); // 한번만 로깅되도록 위치 이동
        }
        else
        {
            // Debug.LogWarning("CustomerOrderManager: " + CurrentOrderData.customerName + " 손님의 주문 내용(skewerOrder)이 비어있거나 없습니다.");
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
            if (isTutorialActive)
            {
                Debug.Log("튜토리얼 모드 종료.");
                isTutorialActive = false;
                if (tutorialPanel_UI != null && tutorialPanel_UI.activeSelf)
                {
                     // tutorialMessageText_UI.text = "훌륭해요! 첫 주문을 완벽하게 만들었어요!";
                     // Invoke("HideTutorialPanel", 2f);
                }
                 if (tutorialPanel_UI != null) tutorialPanel_UI.SetActive(false);
            }
            LoadNextCustomerOrder();
        }
        else
        {
            if (HeartManager.Instance != null)
            {
                HeartManager.Instance.LoseHeart();
            }
            else
            {
                Debug.LogError("CustomerOrderManager: HeartManager 인스턴스를 찾을 수 없어 하트를 차감할 수 없습니다.");
            }

            string playerOrderStr = string.Join(", ", collectedPlayerFruits.Select(f => f.ToString()));
            string correctOrderStr = string.Join(", ", CurrentRequiredFruits.Select(f => f.ToString()));
            Debug.Log("주문 실패! (" + CurrentOrderData.customerName + ")\n플레이어 제출: [" + playerOrderStr + "]\n정답: [" + correctOrderStr + "]");

            if (isTutorialActive && tutorialMessageText_UI != null && tutorialPanel_UI.activeSelf)
            {
                tutorialMessageText_UI.text = "이런! 주문과 조금 다른 것 같아요. 다시 한번 만들어 볼까요?";
            }
        }
        return orderMatch;
    }
}