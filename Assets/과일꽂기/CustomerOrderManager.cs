// CustomerOrderManager.cs
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI; // UI.Image 사용
using System.Linq;
using TMPro;    // SequenceEqual 등 사용

public enum GameState
{
    TutorialDisplay, // 튜토리얼 설명 보여주는 중
    Playing,         // 실제 게임 플레이 중
    Paused,          // 일시정지 등
    GameOver         // 게임 오버
}

public class CustomerOrderManager : MonoBehaviour
{

    [Header("주문서 UI 구성 요소")]
    public GameObject orderDisplayBackgroundPanel; // 주문서 배경 Panel (예: UI_Bill)
    public GameObject fruitsContainerForOrderUI; // 과일 이미지들이 담길 자식 Panel (VerticalLayoutGroup 적용 권장)
    public Image skewerStickImagePrefab_UI;   // 주문서에 표시될 꼬치 막대 UI Image 프리팹 (선택 사항)
    public Image fruitImagePrefab_UI;         // 주문서에 표시될 개별 과일 UI Image 프리팹

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

    public GameState currentGameState = GameState.Playing; // 현재 게임 상태, 초기값은 일반 플레이로 둘 수 있음
    public bool isTutorialActive = false; // 이 변수는 계속 사용 (튜토리얼 손님인지 여부)

    [Header("튜토리얼 UI 요소")]
    public GameObject tutorialPanel_UI;     // 튜토리얼 설명과 시작 버튼을 포함하는 Panel
    public TextMeshProUGUI tutorialMessageText_UI;   // 튜토리얼 설명을 보여줄 Text (tutorialPanel_UI의 자식)
    public Button startGameButton_UI;     // "게임 시작" 버튼 (tutorialPanel_UI의 자식)


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
            Debug.LogError("CustomerOrderManager: 손님 주문 데이터(allCustomerOrders)가 설정되지 않았습니다!");
            if (tutorialPanel_UI != null) tutorialPanel_UI.SetActive(false);
            return;
        }

        // "게임 시작" 버튼에 리스너 추가 (Inspector에서 직접 연결해도 됨)
        if (startGameButton_UI != null)
        {
            startGameButton_UI.onClick.AddListener(EndTutorialAndStartGame);
        }
        else if (isTutorialActive && tutorialPanel_UI != null) // 만약 첫 손님이 튜토리얼인데 버튼이 없다면 경고
        {
            Debug.LogWarning("CustomerOrderManager: startGameButton_UI가 연결되지 않았습니다. 튜토리얼 진행에 문제가 있을 수 있습니다.");
        }


        // 첫 번째 손님 주문 로드
        // LoadCustomerOrder(currentCustomerIndex); // 이 호출은 튜토리얼 상태 설정 후로 이동하거나 조정
        SetupInitialGameState();
    }

    void SetupInitialGameState()
    {
        // 첫 번째 손님인지 확인하여 튜토리얼 여부 결정
        if (allCustomerOrders.Count > 0 && currentCustomerIndex == 0) // 첫 번째 손님 (끼끼)
        {
            isTutorialActive = true;
            currentGameState = GameState.TutorialDisplay;
            Debug.Log("튜토리얼 모드 진입: 끼끼 손님. 게임 시작 대기 중.");

            // 튜토리얼 패널 활성화 및 설명 표시
            if (tutorialPanel_UI != null)
            {
                tutorialPanel_UI.SetActive(true);
                if (tutorialMessageText_UI != null)
                {
                    // 여기에 끼끼 손님을 위한 초기 튜토리얼 메시지 설정
                    tutorialMessageText_UI.text = "안녕하세요! 탕후루 가게에 오신 것을 환영합니다!\n첫 손님은 끼끼예요. 끼끼가 원하는 탕후루를 만들어주세요.\n화면 왼쪽 위에 보이는 주문서대로 과일을 꼬치에 꽂으면 됩니다.\n준비가 되면 아래 '게임 시작' 버튼을 눌러주세요!";
                }
            }

            // 튜토리얼 중에는 과일 스포너 등 게임 요소를 비활성화 할 수 있음
            if (FruitSpawner2D.Instance != null) // FruitSpawner2D가 싱글톤이라고 가정
            {
                FruitSpawner2D.Instance.PauseSpawning(true); // 스포너 일시정지 함수 필요
            }
            // 꼬치 움직임도 비활성화 할 수 있음 (Skewer2DController에 제어 함수 추가)
            // Skewer2DController.Instance.SetControllable(false);

        }
        else // 튜토리얼이 아닌 일반 손님
        {
            isTutorialActive = false;
            currentGameState = GameState.Playing;
            if (tutorialPanel_UI != null) tutorialPanel_UI.SetActive(false); // 튜토리얼 패널 숨김
            LoadCustomerOrder(currentCustomerIndex); // 바로 주문 로드 및 게임 시작
        }
    }


    // "게임 시작" 버튼을 눌렀을 때 호출될 함수
    public void EndTutorialAndStartGame()
    {
        if (currentGameState == GameState.TutorialDisplay)
        {
            Debug.Log("튜토리얼 설명 종료. 게임을 시작합니다!");
            currentGameState = GameState.Playing;

            if (tutorialPanel_UI != null)
            {
                tutorialPanel_UI.SetActive(false); // 튜토리얼 패널 숨기기
            }

            // 첫 손님(튜토리얼 손님) 주문 로드
            LoadCustomerOrder(currentCustomerIndex);

            // 게임 요소 활성화
            if (FruitSpawner2D.Instance != null)
            {
                FruitSpawner2D.Instance.PauseSpawning(false); // 스포너 다시 시작
            }
            // Skewer2DController.Instance.SetControllable(true); // 꼬치 움직임 활성화
        }
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

        if (customerIndex == 0) // 또는 CurrentOrderData.customerName == "끼끼" 등으로 확인
        {
            isTutorialActive = true;
            Debug.Log("튜토리얼 모드 시작: 끼끼 손님");
            // 여기에 튜토리얼 UI 설명(예: "화살표로 꼬치를 움직여 과일을 받으세요!")을 표시하는 로직 추가 가능
            ShowTutorialMessage("화살표 키 또는 마우스를 사용해 꼬치를 움직여 떨어지는 과일을 순서대로 받으세요!");
        }
        else
        {
            isTutorialActive = false;
        }

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

        CurrentOrderData = allCustomerOrders[currentCustomerIndex];
        CurrentRequiredFruits.Clear();

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

        Debug.Log(CurrentOrderData.customerName + " 손님의 주문 로드 완료.");
        DisplayOrderOnUI(); // 주문서 UI 표시
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
                isTutorialActive = false; // 튜토리얼 종료
                // ShowTutorialMessage("훌륭해요! 이제 다음 손님을 맞이해볼까요?");
                if (tutorialMessageText_UI != null) tutorialMessageText_UI.gameObject.SetActive(false); // 튜토리얼 메시지 숨김
            }
            LoadNextCustomerOrder();
        }
        // ... (실패 로직은 HeartManager에서 튜토리얼 여부 판단) ...
        return orderMatch;
    }

    public void ShowTutorialMessage(string message)
    {
        if (tutorialMessageText_UI != null && tutorialPanel_UI != null && tutorialPanel_UI.activeSelf) // 튜토리얼 패널이 활성화 되어있을 때만
        {
            tutorialMessageText_UI.text = message;
        }
    }
}