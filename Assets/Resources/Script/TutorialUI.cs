// TutorialUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement; // 현재 씬 이름을 가져오기 위해 추가

public class TutorialUI : MonoBehaviour
{
    [Header("튜토리얼 UI 요소 (이 씬에 있는 것들)")]
    public GameObject tutorialPanelObject;
    // ▼▼▼ 이 TextMeshProUGUI의 Text 필드에 Inspector에서 씬별 튜토리얼 내용을 미리 입력해둘 수 있습니다. ▼▼▼
    public TextMeshProUGUI tutorialMessageTextObject;
    public Button startGameButtonObject; // 또는 tutorialContinueButton

    // ▼▼▼ (선택 사항) 코드에서 특정 씬의 텍스트를 덮어쓰고 싶을 때 사용 ▼▼▼
    [Header("코드에서 재정의할 튜토리얼 텍스트 (비워두면 Inspector 값 사용)")]
    [TextArea(3, 5)]
    public string overrideTutorialText; // 이 필드에 내용이 있으면 Inspector의 TextMeshPro 내용을 덮어씁니다.

    private CustomerOrderManager customerOrderManager;

    void Start()
    {
        customerOrderManager = CustomerOrderManager.Instance;

        if (customerOrderManager != null)
        {
            customerOrderManager.OnGameStateChanged += HandleGameStateChanged;
            // 초기 UI 상태 설정 (CustomerOrderManager의 현재 상태에 따라)
            HandleGameStateChanged(customerOrderManager.currentGameState, customerOrderManager.isTutorialActive);

            if (startGameButtonObject != null)
            {
                startGameButtonObject.onClick.RemoveAllListeners();
                startGameButtonObject.onClick.AddListener(OnTutorialButtonClicked);
            }
        }
        else
        {
            Debug.LogError("TutorialUI: CustomerOrderManager.Instance를 찾을 수 없습니다!");
            if (tutorialPanelObject != null) tutorialPanelObject.SetActive(false);
            enabled = false; // CustomerOrderManager 없이는 정상 작동 불가
        }
    }

    void OnDestroy()
    {
        if (customerOrderManager != null)
        {
            customerOrderManager.OnGameStateChanged -= HandleGameStateChanged;
        }
    }

    void HandleGameStateChanged(GameState newState, bool tutorialActive)
    {
        if (tutorialPanelObject == null || tutorialMessageTextObject == null)
        {
            Debug.LogError("TutorialUI: 튜토리얼 UI 요소(패널 또는 텍스트)가 Inspector에 연결되지 않았습니다.");
            return;
        }

        // 튜토리얼이 활성화 상태이고, 게임 상태가 TutorialDisplay일 때만 UI를 표시
        bool shouldShowTutorial = tutorialActive && (newState == GameState.TutorialDisplay);
        tutorialPanelObject.SetActive(shouldShowTutorial);

        if (shouldShowTutorial)
        {
            // 튜토리얼 UI가 활성화될 때 텍스트를 설정합니다.
            SetTutorialText();
        }
    }

    void SetTutorialText()
    {
        // 1. overrideTutorialText 필드에 내용이 있으면 그 텍스트를 사용
        if (!string.IsNullOrEmpty(overrideTutorialText))
        {
            tutorialMessageTextObject.text = overrideTutorialText;
        }
        // 2. overrideTutorialText 필드가 비어있으면,
        //    Inspector의 tutorialMessageTextObject에 이미 설정된 텍스트를 그대로 사용합니다.
        //    (즉, 이 경우에는 아래 로직을 실행할 필요가 없습니다.)
        //    만약 Inspector의 텍스트가 비어있거나 기본 안내 문구가 필요하다면 다음과 같이 처리:
        else if (string.IsNullOrEmpty(tutorialMessageTextObject.text) || tutorialMessageTextObject.text == "New Text" /* TextMeshPro 기본값 등 */)
        {
            // 현재 씬에 따라 다른 기본 텍스트를 코드에서 지정하고 싶다면 여기에 로직 추가 가능
            // string currentSceneName = SceneManager.GetActiveScene().name;
            // if (currentSceneName == "FruitCatchingGameScene") {
            //     tutorialMessageTextObject.text = "과일 잡기 튜토리얼 기본 메시지입니다.";
            // } else if (currentSceneName == "SugarBoilingScene") {
            //     tutorialMessageTextObject.text = "설탕 끓이기 튜토리얼 기본 메시지입니다.";
            // } else {
            //    tutorialMessageTextObject.text = "튜토리얼 진행 중입니다.";
            // }
            // 여기서는 단순히 경고만 남기고 Inspector 입력을 유도합니다.
            Debug.LogWarning($"TutorialUI ({gameObject.scene.name}): " +
                             "overrideTutorialText가 비어있고, tutorialMessageTextObject의 Inspector 값도 기본값이거나 비어있을 수 있습니다. " +
                             "Inspector에서 TextMeshProUGUI 컴포넌트의 Text 필드에 씬에 맞는 튜토리얼 내용을 직접 입력해주세요.");
            // 필요하다면, 여기서 기본 메시지를 설정할 수 있습니다.
            // tutorialMessageTextObject.text = "튜토리얼을 진행해주세요!";
        }
        // else : overrideTutorialText는 비어있지만, tutorialMessageTextObject.text에 이미 의미있는 내용이 Inspector에서 설정된 경우, 그 내용을 그대로 사용합니다.
    }

    void OnTutorialButtonClicked()
    {
        if (customerOrderManager != null)
        {
            customerOrderManager.EndTutorialAndStartGame();
        }
    }
}