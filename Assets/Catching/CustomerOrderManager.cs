// CustomerOrderManager.cs
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using UnityEngine.SceneManagement;

public class CustomerOrderManager : MonoBehaviour
{

    public GameState currentGameState = GameState.TutorialDisplay; // 초기 상태
    public bool isTutorialActive = false;

    public static CustomerOrderManager Instance { get; private set; }

    [Header("씬 전환 설정")]
    // public string fruitCatchingSceneName = "FruitCatchingGameScene"; // 현재 씬이므로 여기선 불필요
    public string sugarBoilingSceneName = "SugarBoilingScene";       // 다음 단계: 설탕 끓이기 씬
    public string sugarCoatingSceneName = "SugarCoatingScene";     // 그 다음 단계: 설탕 묻히기 씬
    public string toppingPlacementSceneName = "ToppingPlacementScene"; // 마지막 단계: 토핑 꽂기 씬
    public string stageSelectSceneName = "TitleScene"; // 모든 단계 완료 후 돌아갈 스테이지 선택 씬


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
            DontDestroyOnLoad(gameObject); // CustomerOrderManager가 씬마다 새로 로드된다면 필요 없음
        }
        else if (Instance != this) // 이미 다른 인스턴스가 있다면 현재 것을 파괴
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
        // Awake에서 Instance가 이미 설정되었으므로 Start에서는 Instance 관련 로직 제거

        if (allCustomerOrders == null || allCustomerOrders.Count == 0)
        {
            Debug.LogError("CustomerOrderManager: 손님 주문 데이터(allCustomerOrders)가 설정되지 않았습니다!");
            if (tutorialPanel_UI != null) tutorialPanel_UI.SetActive(false);
            currentGameState = GameState.Playing;
            return;
        }

        if (startGameButton_UI != null)
        {
            startGameButton_UI.onClick.AddListener(EndTutorialAndStartGame);
        }

        // GameInfoHolder에서 로드할 손님 인덱스를 먼저 가져옵니다.
        currentCustomerIndex = GameInfoHolder.CustomerIndexToLoad;
        Debug.Log("CustomerOrderManager (FruitCatchingGameScene): 로드할 손님 인덱스: " + currentCustomerIndex);

        // 그 다음에 SetupInitialGameState()를 한 번만 호출합니다.
        SetupInitialGameState();
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
        CurrentRequiredFruits.Clear(); // 과일 꽂기 단계에서 필요한 과일 목록

        if (CurrentOrderData != null && CurrentOrderData.skewerOrder != null)
        {
            // ★★★ 이제 skewerOrder (기본 과일)만 CurrentRequiredFruits에 추가 ★★★
            foreach (OrderItem item in CurrentOrderData.skewerOrder)
            {
                CurrentRequiredFruits.Add(item.fruit);
            }
        }
        // CurrentOrderData.toppingItem은 "토핑 아이템 선택" 단계에서 사용됩니다.

        Debug.Log(CurrentOrderData.customerName + " 손님의 (기본 과일) 주문 로드 완료. 주문: " + string.Join(", ", CurrentRequiredFruits.Select(f => f.ToString())) + (isTutorialActive ? " (튜토리얼 진행중)" : ""));
        DisplayOrderOnUI(); // 주문서 UI에는 전체 완성본 또는 기본 과일 부분만 표시할지 결정 필요
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

        if (CurrentOrderData != null && CurrentOrderData.skewerOrder != null && CurrentOrderData.skewerOrder.Count > 0)
        {
            // 이 부분은 기본 과일만 표시하도록 하거나, completedSkewerSprite를 사용하도록 변경해야 할 수 있습니다.
            // 현재는 기본 과일만 표시하는 것으로 가정.
            List<OrderItem> orderItemsToDisplay = CurrentOrderData.skewerOrder;
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

        // CurrentRequiredFruits는 이제 '기본 과일' 목록임 (CustomerOrderData.skewerOrder에서 옴)
        bool orderMatch = collectedPlayerFruits.SequenceEqual(CurrentRequiredFruits);

        if (orderMatch)
        {
            Debug.Log("과일 꽂기 미니게임 성공! (" + CurrentOrderData.customerName + ")");

            // 과일 스포너 중지 (다음 미니게임으로 넘어가므로)
            if (FruitSpawner2D.Instance != null)
            {
                FruitSpawner2D.Instance.StopSpawningCompletely();
            }

            // ★★★ 다음 단계: 설탕 끓이기 씬으로 전환 ★★★
            // GameInfoHolder.CustomerIndexToLoad는 현재 손님 인덱스를 유지.
            // SkewerManager의 꼬치 상태는 DontDestroyOnLoad로 유지되어야 함.
            Debug.Log(CurrentOrderData.customerName + " 손님의 [" + sugarBoilingSceneName + "] 단계로 넘어갑니다.");
            if (SceneSwitcher.Instance != null)
            {
                SceneSwitcher.Instance.LoadScene(sugarBoilingSceneName);
            }
            else
            {
                Debug.LogError("SceneSwitcher 인스턴스를 찾을 수 없습니다! 직접 씬 로드 시도.");
                SceneManager.LoadScene(sugarBoilingSceneName);
            }
        }
        else // 과일 꽂기 실패
        {
            Debug.Log("과일 꽂기 미니게임 실패! (" + CurrentOrderData.customerName + ")");
            if (HeartManager.Instance != null)
            {
                HeartManager.Instance.LoseHeart(); // HeartManager에서 튜토리얼 여부 등 판단
            }
            // 실패 시 현재 꼬치 비우기
            if (SkewerManager.Instance != null) // ★★★ SkewerManager.Instance로 접근 ★★★
            {
                SkewerManager.Instance.ClearSkewer();
            }
            else
            {
                Debug.LogError("SkewerManager 인스턴스를 찾을 수 없어 꼬치를 비울 수 없습니다.");
            }
            // 실패 메시지 등 UI 처리
            if (isTutorialActive && tutorialMessageText_UI != null && tutorialPanel_UI != null && tutorialPanel_UI.activeSelf)
            {
                tutorialMessageText_UI.text = "이런! 주문과 조금 다른 것 같아요. 다시 한번 만들어 볼까요?";
            }
        }
        return orderMatch;
    }
    
    public void AllMiniGamesCompletedForCurrentCustomer()
    {
        if (CurrentOrderData == null)
        {
            Debug.LogError("AllMiniGamesCompletedForCurrentCustomer: CurrentOrderData가 null입니다.");
            // 오류 처리 후 타이틀 씬으로 보낼 수 있음
            if (SceneSwitcher.Instance != null) SceneSwitcher.Instance.LoadScene(stageSelectSceneName);
            else SceneManager.LoadScene(stageSelectSceneName);
            return;
        }

        Debug.Log(CurrentOrderData.customerName + " 손님의 모든 탕후루 제작 단계 완료! 도감 등록 및 스테이지 선택 화면으로.");

        // 도감 등록 로직 (구현 필요)
        // 예: AnimalBookManager.Instance.UnlockEntry(currentCustomerIndex, 완성된탕후루이미지);

        // 현재 손님 스테이지 클리어 처리
        if (StageDataManager.Instance != null)
        {
            StageDataManager.Instance.SetStageCleared(currentCustomerIndex);
        }

        if (isTutorialActive) // 만약 현재 손님이 튜토리얼이었다면
        {
            isTutorialActive = false; // 튜토리얼 상태 종료
            // 튜토리얼 완료 관련 특별한 처리가 있다면 여기에 추가
        }

        // ★★★ 다음 손님을 바로 로드하는 대신, 스테이지 선택 화면으로 돌아감 ★★★
        Debug.Log(stageSelectSceneName + " (스테이지 선택 화면)으로 돌아갑니다.");
        if (SceneSwitcher.Instance != null)
        {
            SceneSwitcher.Instance.LoadScene(stageSelectSceneName);
        }
        else
        {
            SceneManager.LoadScene(stageSelectSceneName);
        }
    }
}