// CustomerDialogueManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;



public class CustomerDialogueManager : MonoBehaviour
{
    [Header("씬 전환 설정")]
    public string fruitCatchingSceneName = "FruitCatchingGameScene";

    [Header("대화 데이터 직접 연결 (중요!)")]
    public List<CustomerOrderData> allCustomerOrdersForDialogue;

    // private CustomerOrderData currentCustomerDataForDialogue; // Start에서 지역 변수로 사용하거나, 필요시 유지
    private List<DialogueEntry> activeDialogueSequence; // 현재 진행할 대화 목록

    private int currentDialogueIndex = 0;
    private bool isDialoguePlaying = false;

    [Header("말풍선 UI 설정")]
    public CanvasGroup kikiSpeechBubbleGroup;
    public TextMeshProUGUI kikiSpeechText;
    public Button kikiNextButton;

    public CanvasGroup pupuSpeechBubbleGroup;
    public TextMeshProUGUI pupuSpeechText;
    public Button pupuNextButton;

    [Header("오디오 설정")]
    public string bubbleOpenSoundName = "BubbleOpen";
    public string textSoundName = "Text";

    private bool isTextTyping = false;
    private CanvasGroup currentBubbleGroup;
    private Button currentNextButton;

    // dialogueSequence 리스트는 CustomerOrderData에서 가져오므로 여기서는 제거해도 됩니다.
    // [Header("대화 순서 및 내용")]
    // public List<DialogueEntry> dialogueSequence = new List<DialogueEntry>();


    void Start()
    {
        InitializeButtons();
        HideAllBubbles();

        int customerIndexToLoad = GameInfoHolder.CustomerIndexToLoad;
        Debug.Log("CustomerDialogueManager: 로드할 손님 인덱스 from GameInfoHolder: " + customerIndexToLoad);

        if (allCustomerOrdersForDialogue != null && allCustomerOrdersForDialogue.Count > customerIndexToLoad && allCustomerOrdersForDialogue[customerIndexToLoad] != null)
        {
            CustomerOrderData currentCustomerData = allCustomerOrdersForDialogue[customerIndexToLoad]; // 지역 변수로 변경
            activeDialogueSequence = currentCustomerData.dialogueSequence;

            if (activeDialogueSequence != null && activeDialogueSequence.Count > 0)
            {
                Debug.Log(currentCustomerData.customerName + " 손님의 대화 시작 준비.");
                StartDialogue();
            }
            else
            {
                Debug.LogWarning(currentCustomerData.customerName + " 손님의 대화 내용(dialogueSequence)이 없습니다. 바로 게임으로 넘어갑니다.");
                ProceedToGame();
            }
        }
        else
        {
            Debug.LogError("CustomerDialogueManager: 유효한 손님 주문 데이터(allCustomerOrdersForDialogue)를 찾을 수 없거나 인덱스가 잘못되었습니다. Inspector 연결 및 GameInfoHolder 값을 확인하세요. 로드 시도 인덱스: " + customerIndexToLoad);
            ProceedToGame();
        }
    }

    void InitializeButtons()
    {
        if (kikiNextButton != null)
        {
            kikiNextButton.onClick.RemoveAllListeners();
            kikiNextButton.onClick.AddListener(OnNextButtonClicked);
            kikiNextButton.gameObject.SetActive(false);
        }
        if (pupuNextButton != null)
        {
            pupuNextButton.onClick.RemoveAllListeners();
            pupuNextButton.onClick.AddListener(OnNextButtonClicked);
            pupuNextButton.gameObject.SetActive(false);
        }
    }

    void HideAllBubbles()
    {
        if (kikiSpeechBubbleGroup != null) { kikiSpeechBubbleGroup.alpha = 0f; kikiSpeechBubbleGroup.gameObject.SetActive(false); }
        if (pupuSpeechBubbleGroup != null) { pupuSpeechBubbleGroup.alpha = 0f; pupuSpeechBubbleGroup.gameObject.SetActive(false); }
    }

    public void StartDialogue()
    {
        if (isDialoguePlaying || activeDialogueSequence == null || activeDialogueSequence.Count == 0)
        {
            Debug.LogWarning("대화를 시작할 수 없거나 대화 내용이 없습니다.");
            if (activeDialogueSequence == null || activeDialogueSequence.Count == 0) ProceedToGame();
            return;
        }
        currentDialogueIndex = 0;
        StartCoroutine(ProceedDialogueInternal());
    }

    IEnumerator ProceedDialogueInternal()
    {
        isDialoguePlaying = true;
        HideAllBubbles();

        while (currentDialogueIndex < activeDialogueSequence.Count)
        {
            DialogueEntry entry = activeDialogueSequence[currentDialogueIndex]; // 이제 DialogueEntry를 찾을 수 있어야 함
            CanvasGroup targetBubbleGroup = null;
            TextMeshProUGUI targetTextComponent = null;
            Button targetNextButton = null;

            if (currentBubbleGroup != null && currentBubbleGroup.gameObject.activeSelf &&
                ((entry.speaker == DialogueEntry.Speaker.Kiki && currentBubbleGroup == pupuSpeechBubbleGroup) ||
                 (entry.speaker == DialogueEntry.Speaker.Pupu && currentBubbleGroup == kikiSpeechBubbleGroup)))
            {
                yield return StartCoroutine(FadeOut(currentBubbleGroup));
            }

            if (entry.speaker == DialogueEntry.Speaker.Kiki)
            {
                targetBubbleGroup = kikiSpeechBubbleGroup;
                targetTextComponent = kikiSpeechText;
                targetNextButton = kikiNextButton;
            }
            else if (entry.speaker == DialogueEntry.Speaker.Pupu)
            {
                targetBubbleGroup = pupuSpeechBubbleGroup;
                targetTextComponent = pupuSpeechText;
                targetNextButton = pupuNextButton;
            }

            currentBubbleGroup = targetBubbleGroup;
            currentNextButton = targetNextButton;

            if (targetBubbleGroup != null && targetTextComponent != null)
            {
                yield return ShowSpeechBubbleAndText(targetBubbleGroup, targetTextComponent, entry.line, targetNextButton);
            }

            while (currentNextButton != null && currentNextButton.gameObject.activeSelf)
            {
                yield return null;
            }
            while(isTextTyping) yield return null;

            currentDialogueIndex++;
        }

        if(currentBubbleGroup != null && currentBubbleGroup.gameObject.activeSelf) // IsActiveSelf 대신 alpha로 체크해도 됨
        {
            yield return StartCoroutine(FadeOut(currentBubbleGroup));
        }

        isDialoguePlaying = false;
        // CustomerOrderData에서 손님 이름을 가져오려면 currentCustomerDataForDialogue를 사용해야 하지만,
        // Start에서만 설정되므로, 이 코루틴 시작 시점에 멤버 변수로 가지고 있는 것이 좋음.
        // 여기서는 일단 GameInfoHolder를 통해 가져온 인덱스로 다시 참조.
        string customerNameForLog = "Unknown Customer";
        if (allCustomerOrdersForDialogue != null && allCustomerOrdersForDialogue.Count > GameInfoHolder.CustomerIndexToLoad)
        {
            customerNameForLog = allCustomerOrdersForDialogue[GameInfoHolder.CustomerIndexToLoad].customerName;
        }
        Debug.Log("모든 대화 종료! 손님: " + customerNameForLog);
        ProceedToGame();
    }
    
    void ProceedToGame()
    {
        Debug.Log(fruitCatchingSceneName + " 씬으로 전환 준비. 로드할 손님 인덱스 (GameInfoHolder): " + GameInfoHolder.CustomerIndexToLoad);
        if (SceneSwitcher.Instance != null)
        {
            SceneSwitcher.Instance.LoadFruitCatchingScene(fruitCatchingSceneName);
        }
        else
        {
            Debug.LogError("SceneSwitcher 인스턴스를 찾을 수 없습니다! 직접 씬 로드 시도.");
            SceneManager.LoadScene(fruitCatchingSceneName);
        }
    }

    IEnumerator ShowSpeechBubbleAndText(CanvasGroup bubbleGroup, TextMeshProUGUI textComponent, string message, Button nextBtn)
    {
        if (bubbleGroup == null || textComponent == null) yield break;
        bubbleGroup.gameObject.SetActive(true);
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(bubbleOpenSoundName))
        {
            AudioManager.Instance.PlaySound(bubbleOpenSoundName);
        }
        yield return StartCoroutine(FadeIn(bubbleGroup));
        textComponent.text = "";
        yield return StartCoroutine(TypeText(textComponent, message));
        if (nextBtn != null)
        {
            nextBtn.gameObject.SetActive(true);
        }
    }

    IEnumerator TypeText(TextMeshProUGUI textComponent, string message)
    {
        isTextTyping = true;
        textComponent.text = "";
        Sound s = null;
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(textSoundName))
        {
             s = AudioManager.Instance.sounds.Find(sound => sound.name == textSoundName);
             if (s != null && s.source != null) s.source.Play(); else s = null;
        }

        foreach (char c in message)
        {
            if (!isTextTyping) { // 타이핑 중단 로직
                textComponent.text = message; // 전체 텍스트 즉시 표시
                break;
            }
            textComponent.text += c;
            yield return new WaitForSeconds(0.02f);
        }
        
        if (s != null && s.source != null && s.source.isPlaying) s.source.Stop(); // 재생 중일 때만 정지
        isTextTyping = false;
    }

    void OnNextButtonClicked()
    {
        if (isTextTyping)
        {
            isTextTyping = false; // TypeText 코루틴의 루프를 중단시킴 (전체 텍스트는 TypeText의 루프 종료 후 자동 표시됨)
                                  // 또는, 아래처럼 즉시 전체 텍스트를 표시하고 isTextTyping을 false로 설정
            // StopAllCoroutines(); // TypeText만 멈추고 싶다면 별도의 코루틴 참조 필요
            if (activeDialogueSequence != null && currentDialogueIndex < activeDialogueSequence.Count)
            {
                string fullLine = activeDialogueSequence[currentDialogueIndex].line;
                if (currentBubbleGroup == kikiSpeechBubbleGroup && kikiSpeechText != null) kikiSpeechText.text = fullLine;
                else if (currentBubbleGroup == pupuSpeechBubbleGroup && pupuSpeechText != null) pupuSpeechText.text = fullLine;
            }
            if (AudioManager.Instance != null) AudioManager.Instance.StopTextSound();
        }
        else
        {
            if (currentNextButton != null)
            {
                currentNextButton.gameObject.SetActive(false);
            }
        }
    }

    IEnumerator FadeIn(CanvasGroup canvasGroup, float duration = 0.2f)
    {
        float time = 0f;
        canvasGroup.alpha = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, time / duration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    IEnumerator FadeOut(CanvasGroup canvasGroup, float duration = 0.2f)
    {
        float time = 0f;
        float startAlpha = canvasGroup.alpha;
        while (time < duration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, time / duration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        canvasGroup.gameObject.SetActive(false); // 여기서 비활성화
        // 버튼 비활성화는 OnNextButtonClicked 또는 다음 대화 시작 시 처리
    }
}