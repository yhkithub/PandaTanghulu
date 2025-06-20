using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;


public class CustomerDialogueManager : MonoBehaviour
{
    public static CustomerDialogueManager Instance { get; private set; }

    [Header("씬 전환 설정")]
    public string fruitCatchingSceneName = "FruitCatchingGameScene";

    [Header("손님 이미지 UI")]
    public Image customerImage;

    [Header("말풍선 UI 설정 (원래 변수명 유지)")]
    public CanvasGroup kikiSpeechBubbleGroup;
    public TextMeshProUGUI kikiSpeechText;
    public Button kikiNextButton;
    public CanvasGroup pupuSpeechBubbleGroup;
    public TextMeshProUGUI pupuDialogueText;
    public Button pupuNextButton;

    [Header("오디오 설정")]
    public string bubbleOpenSoundName = "BubbleOpen";
    public string textSoundName = "Text";
    public string buttonClickSoundName = "buttonclick";

    private CustomerOrderData currentCustomerData;
    private List<DialogueEntry> activeDialogueSequence;
    private int currentDialogueIndex = 0;
    private bool isDialoguePlaying = false;
    private bool isTextTyping = false;
    private Coroutine typingCoroutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        InitializeButtonsAndBubbles();

        currentCustomerData = CustomerOrderManager.Instance.CurrentOrderData;

        if (currentCustomerData != null)
        {
            if (customerImage != null && currentCustomerData.customerSprite != null)
            {
                customerImage.sprite = currentCustomerData.customerSprite;
                customerImage.gameObject.SetActive(true); // 애니메이션 시작점
            }
            activeDialogueSequence = currentCustomerData.dialogueSequence;
        }
        else
        {
            Debug.LogError("CustomerDialogueManager: CustomerOrderManager로부터 현재 손님 데이터를 가져올 수 없습니다!");
            ProceedToGame();
        }
    }

    void InitializeButtonsAndBubbles()
    {
        kikiNextButton.onClick.RemoveAllListeners();
        kikiNextButton.onClick.AddListener(OnNextButtonClicked);
        pupuNextButton.onClick.RemoveAllListeners();
        pupuNextButton.onClick.AddListener(OnNextButtonClicked);
        HideAllBubblesAndButtons();
    }

    public void StartDialogue()
    {
        if (isDialoguePlaying) return;

        if (activeDialogueSequence == null || activeDialogueSequence.Count == 0)
        {
            ProceedToGame();
            return;
        }

        isDialoguePlaying = true;
        currentDialogueIndex = 0;
        StartCoroutine(ProceedDialogueInternal());
    }

    IEnumerator ProceedDialogueInternal()
    {
        HideAllBubblesAndButtons();

        DialogueEntry entry = activeDialogueSequence[currentDialogueIndex];

        if (entry.speaker == DialogueEntry.Speaker.customer)
        {
            yield return ShowSpeechBubbleAndText(kikiSpeechBubbleGroup, kikiSpeechText, entry.line, kikiNextButton);
        }
        else
        {
            yield return ShowSpeechBubbleAndText(pupuSpeechBubbleGroup, pupuDialogueText, entry.line, pupuNextButton);
        }
    }

    IEnumerator ShowSpeechBubbleAndText(CanvasGroup bubbleGroup, TextMeshProUGUI textComponent, string message, Button nextBtn)
    {
        if (bubbleGroup == null || textComponent == null) yield break;

        bubbleGroup.gameObject.SetActive(true);
        AudioManager.Instance?.PlaySound(bubbleOpenSoundName);
        yield return StartCoroutine(FadeIn(bubbleGroup));

        textComponent.text = "";
        nextBtn.gameObject.SetActive(false);
        yield return StartCoroutine(TypeText(textComponent, message));

        nextBtn.gameObject.SetActive(true);
    }

    IEnumerator TypeText(TextMeshProUGUI textComponent, string message)
    {
        isTextTyping = true;
        textComponent.text = "";

        Sound s = AudioManager.Instance?.sounds.Find(sound => sound.name == textSoundName);
        if (s?.source != null) s.source.Play();

        while (isTextTyping && textComponent.text.Length < message.Length)
        {
            textComponent.text += message[textComponent.text.Length];
            yield return new WaitForSeconds(0.02f);
        }

        if (s?.source != null && s.source.isPlaying) s.source.Stop();

        textComponent.text = message;
        isTextTyping = false;
    }

    void OnNextButtonClicked()
    {
        AudioManager.Instance?.PlaySound(buttonClickSoundName);

        if (isTextTyping)
        {
            isTextTyping = false;
            return;
        }

        currentDialogueIndex++;
        if (currentDialogueIndex < activeDialogueSequence.Count)
        {
            StartCoroutine(ProceedDialogueInternal());
        }
        else
        {
            ProceedToGame();
        }
    }

    void ProceedToGame()
    {
        isDialoguePlaying = false;
        HideAllBubblesAndButtons();
        SceneManager.LoadScene(fruitCatchingSceneName);
    }

    void HideAllBubblesAndButtons()
    {
        if (kikiSpeechBubbleGroup != null) { kikiSpeechBubbleGroup.alpha = 0f; kikiSpeechBubbleGroup.gameObject.SetActive(false); }
        if (pupuSpeechBubbleGroup != null) { pupuSpeechBubbleGroup.alpha = 0f; pupuSpeechBubbleGroup.gameObject.SetActive(false); }
        if (kikiNextButton != null) kikiNextButton.gameObject.SetActive(false);
        if (pupuNextButton != null) pupuNextButton.gameObject.SetActive(false);
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

    public void OnSkipButtonClicked()
    {
        // 이미 구현된 "다음 씬으로 넘어가기" 함수를 바로 호출합니다.
        Debug.Log("대화를 스킵하고 게임 씬으로 바로 이동합니다.");
        ProceedToGame();
    }
}