// FruitCatching_UIManager.cs (예시)
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic; // List 사용

public class FruitCatching_UIManager : MonoBehaviour
{
    [Header("=== 이 씬의 UI 요소 연결 ===")]
    [Header("튜토리얼 UI")]
    public GameObject tutorialPanel;
    public TextMeshProUGUI tutorialMessageText;
    public Button startGameButton; // 튜토리얼 진행 버튼

    [Header("주문서 UI")]
    public GameObject orderDisplayPanel;
    public Transform orderFruitsContainer; // 과일 아이콘들이 담길 부모 Transform
    public Image skewerStickIconPrefab;    // 주문서용 꼬치 아이콘 프리팹
    public Image fruitOrderIconPrefab;     // 주문서용 과일 아이콘 프리팹

    [Header("하트 UI")]
    public List<Image> heartImages; // 하트 Image UI 리스트
    public Sprite fullHeartSprite;
    public Sprite emptyHeartSprite;

    void Start()
    {
        // CustomerOrderManager 이벤트 구독
        if (CustomerOrderManager.Instance != null)
        {
            CustomerOrderManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            CustomerOrderManager.Instance.OnOrderLoaded += UpdateOrderDisplay; // 주문 로드 시 주문서 UI 업데이트

            // 초기 UI 상태 설정
            HandleGameStateChanged(CustomerOrderManager.Instance.currentGameState, CustomerOrderManager.Instance.isTutorialActive);
            if (CustomerOrderManager.Instance.CurrentOrderData != null)
            {
                UpdateOrderDisplay();
            } else {
                if(orderDisplayPanel != null) orderDisplayPanel.SetActive(false);
            }
        }
        else
        {
            Debug.LogError("FruitCatching_UIManager: CustomerOrderManager.Instance를 찾을 수 없습니다!");
            if (tutorialPanel != null) tutorialPanel.SetActive(false);
            if (orderDisplayPanel != null) orderDisplayPanel.SetActive(false);
        }

        // HeartManager 이벤트 구독
        if (HeartManager.Instance != null)
        {
            HeartManager.Instance.OnHeartsChanged += UpdateHeartDisplay;
            UpdateHeartDisplay(HeartManager.Instance.CurrentHearts); // 초기 하트 UI 업데이트
        }
        else
        {
            Debug.LogError("FruitCatching_UIManager: HeartManager.Instance를 찾을 수 없습니다!");
            foreach(var h in heartImages) h.gameObject.SetActive(false);
        }


        if (startGameButton != null)
        {
            startGameButton.onClick.RemoveAllListeners();
            startGameButton.onClick.AddListener(OnStartTutorialGameButtonClicked);
        }
    }

    void OnDestroy()
    {
        if (CustomerOrderManager.Instance != null)
        {
            CustomerOrderManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            CustomerOrderManager.Instance.OnOrderLoaded -= UpdateOrderDisplay;
        }
        if (HeartManager.Instance != null)
        {
            HeartManager.Instance.OnHeartsChanged -= UpdateHeartDisplay;
        }
    }

    void HandleGameStateChanged(GameState newState, bool tutorialActive)
    {
        bool shouldShowTutorial = tutorialActive && (newState == GameState.TutorialDisplay);
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(shouldShowTutorial);
        }

        if (shouldShowTutorial && tutorialMessageText != null)
        {
            if (CustomerOrderManager.Instance != null && CustomerOrderManager.Instance.currentCustomerIndex == 0)
            {
                tutorialMessageText.text = "어서오세요! 끼끼 손님이 첫 주문을 했어요.\n화면 위 주문서대로 과일을 꼬치에 드래그해서 꽂아주세요!\n다 꽂으면 오른쪽 아래 버튼을 눌러주세요.";
            }
        }

        // 게임 플레이 상태일 때 주문서가 보이도록 설정 (튜토리얼 중에도 주문서는 보일 수 있음)
        if (orderDisplayPanel != null && newState == GameState.Playing || newState == GameState.TutorialDisplay) // 튜토리얼 중에도 주문서는 보이게
        {
             // UpdateOrderDisplay()가 OnOrderLoaded 이벤트에 의해 호출되므로 여기서는 패널 활성화만
             if (CustomerOrderManager.Instance != null && CustomerOrderManager.Instance.CurrentOrderData != null) {
                orderDisplayPanel.SetActive(true);
             } else {
                orderDisplayPanel.SetActive(false);
             }
        }
        else if (orderDisplayPanel != null)
        {
            orderDisplayPanel.SetActive(false);
        }
    }

    void OnStartTutorialGameButtonClicked()
    {
        if (CustomerOrderManager.Instance != null)
        {
            CustomerOrderManager.Instance.EndTutorialAndStartGame();
        }
    }

    void UpdateOrderDisplay()
    {
        if (CustomerOrderManager.Instance == null || CustomerOrderManager.Instance.CurrentOrderData == null)
        {
            if (orderDisplayPanel != null) orderDisplayPanel.SetActive(false);
            return;
        }
        if (orderFruitsContainer == null || fruitOrderIconPrefab == null)
        {
            Debug.LogError("주문서 표시에 필요한 UI 요소(orderFruitsContainer 또는 fruitOrderIconPrefab)가 없습니다.");
            return;
        }

        if (orderDisplayPanel != null) orderDisplayPanel.SetActive(true);

        foreach (Transform child in orderFruitsContainer) Destroy(child.gameObject);

        if (skewerStickIconPrefab != null)
        {
            Instantiate(skewerStickIconPrefab, orderFruitsContainer).transform.SetAsFirstSibling();
        }

        List<FruitType> fruitsToDisplay = CustomerOrderManager.Instance.CurrentRequiredSkewerFruits;
        if (fruitsToDisplay != null && fruitsToDisplay.Count > 0)
        {
            foreach (FruitType fruit in fruitsToDisplay)
            {
                Sprite fruitSprite = CustomerOrderManager.Instance.GetSpriteForFruitUI(fruit);
                if (fruitSprite != null)
                {
                    Image fruitIcon = Instantiate(fruitOrderIconPrefab, orderFruitsContainer);
                    fruitIcon.sprite = fruitSprite;
                    fruitIcon.name = fruit.ToString() + "_OrderIcon_Scene";
                }
            }
        }
    }

    void UpdateHeartDisplay(int currentHeartCount)
    {
        if (heartImages == null || fullHeartSprite == null || emptyHeartSprite == null) return;

        for (int i = 0; i < heartImages.Count; i++)
        {
            if (i < HeartManager.Instance.maxHearts) // 최대 하트 개수 이내의 UI만 업데이트
            {
                heartImages[i].gameObject.SetActive(true);
                if (i < currentHeartCount)
                {
                    heartImages[i].sprite = fullHeartSprite;
                }
                else
                {
                    heartImages[i].sprite = emptyHeartSprite;
                }
            }
            else // 최대 하트 개수를 초과하는 UI는 비활성화
            {
                heartImages[i].gameObject.SetActive(false);
            }
        }
    }
}