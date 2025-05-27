using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class CustomerPresentationManager : MonoBehaviour
{
    public static CustomerPresentationManager Instance { get; private set; }

    [Header("UI Elements - Scene Specific")]
    public Image customerImage; // 기본 손님 이미지
    public Image smilingCustomerImage; // 웃는 손님 이미지
    public RectTransform tanghuluOnBoardRect;
    public Image finalTanghuluImageOnBoard;
    public Image draggableTanghuluImage;
    public Image tanghuluInCustomerHandImage; // 손님이 탕후루를 들고 있는 모습
    public RectTransform customerDropZoneRect;

    [Header("Dialogue UI")]
    public CanvasGroup kikiSpeechBubbleGroup;
    public TextMeshProUGUI kikiSpeechText;
    public Button kikiNextButton;
    public CanvasGroup pupuSpeechBubbleGroup;
    public TextMeshProUGUI pupuSpeechText;
    public Button pupuNextButton;

    [Header("Animation & Effects")]
    public float slideInDuration = 1.0f;
    public Vector2 tanghuluBoardHiddenPos = new Vector2(0, -1000f);
    public Vector2 tanghuluBoardTargetPos = new Vector2(0, -300f);
    public Image flashImage;
    public Image polaroidFrameImage;
    public Image smilingCustomerInPolaroidImage; // 폴라로이드 안의 웃는 손님
    public Image tanghuluInPolaroidImage;      // 폴라로이드 안의 탕후루
    public float polaroidDisplayDuration = 2.0f;

    [Header("Sound Effects (AudioManager에 등록된 이름)")]
    public string tanghuluSlideInSound = "TanghuluSlide";
    public string tanghuluDeliveredSound = "ItemCollect";
    public string cameraShutterSound = "CameraClick";
    public string polaroidAppearSound = "PolaroidAppear";
    public string dialogueTextSound = "Text";
    public string dialogueBubbleOpenSound = "BubbleOpen";

    private CustomerOrderData currentOrder;
    private bool tanghuluDelivered = false;
    private List<DialogueEntry> activeDialogueSequence;
    private int currentDialogueIndex = 0;
    private bool isDialoguePlaying = false;
    private bool isTextTyping = false;
    private CanvasGroup currentBubbleGroup;
    private Button currentNextButton;
    private TextMeshProUGUI currentSpeechText;
    private Coroutine typeTextCoroutine;
    private Coroutine playDialogueCoroutine;

    private Canvas m_Canvas;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }

        m_Canvas = GetComponentInParent<Canvas>();
        if (m_Canvas == null) m_Canvas = FindFirstObjectByType<Canvas>();
        if (m_Canvas == null) { Debug.LogError("CustomerPresentationManager Error: Canvas를 찾을 수 없습니다."); enabled = false; return; }
    }

    void Start()
    {
        if (CustomerOrderManager.Instance == null)
        {
            Debug.LogError("CustomerPresentationManager Error: CustomerOrderManager.Instance가 null입니다! TitleScene으로 이동합니다.");
            SceneSwitcher.Instance?.LoadScene("TitleScene");
            enabled = false;
            return;
        }
        StartCoroutine(InitializeAfterManagerReady());
    }

    IEnumerator InitializeAfterManagerReady()
    {
        float waitTime = 0f;
        float maxWaitTime = 5f;
        while (CustomerOrderManager.Instance.CurrentOrderData == null && waitTime < maxWaitTime)
        {
            yield return null; waitTime += Time.deltaTime;
        }

        currentOrder = CustomerOrderManager.Instance.CurrentOrderData;
        if (currentOrder == null)
        {
            Debug.LogError($"CustomerPresentationManager Error: CurrentOrderData 로드 실패. TitleScene으로 이동합니다.");
            SceneSwitcher.Instance?.LoadScene("TitleScene");
            enabled = false; yield break;
        }
        SetupInitialUI();
        StartCoroutine(SlideInTanghuluBoard());
    }

    void SetupInitialUI()
    {
        if (customerImage != null)
        {
            customerImage.sprite = currentOrder.customerSprite;
            customerImage.gameObject.SetActive(currentOrder.customerSprite != null);
        }
        if (smilingCustomerImage != null) smilingCustomerImage.gameObject.SetActive(false);

        if (finalTanghuluImageOnBoard != null)
        {
            finalTanghuluImageOnBoard.sprite = currentOrder.skewerWithToppingSprite;
            finalTanghuluImageOnBoard.gameObject.SetActive(currentOrder.skewerWithToppingSprite != null);
            finalTanghuluImageOnBoard.color = Color.white;
        }

        if (draggableTanghuluImage != null)
        {
            DraggableTanghuluItem itemScript = draggableTanghuluImage.GetComponent<DraggableTanghuluItem>();
            if (itemScript == null) Debug.LogError("draggableTanghuluImage GameObject에 DraggableTanghuluItem 스크립트가 없습니다! 추가해주세요.");
            draggableTanghuluImage.sprite = currentOrder.skewerWithToppingSprite;
            Image imgComp = draggableTanghuluImage.GetComponent<Image>();
            if (imgComp != null && !imgComp.raycastTarget) Debug.LogWarning("draggableTanghuluImage의 Image 컴포넌트에 Raycast Target이 꺼져있습니다. Inspector에서 활성화해주세요.");
            draggableTanghuluImage.gameObject.SetActive(false);
        }
        if (tanghuluInCustomerHandImage != null) tanghuluInCustomerHandImage.gameObject.SetActive(false);
        if (tanghuluOnBoardRect != null) tanghuluOnBoardRect.anchoredPosition = tanghuluBoardHiddenPos;

        InitializeDialogueUI();
        if (flashImage != null) flashImage.gameObject.SetActive(false);
        if (polaroidFrameImage != null) polaroidFrameImage.gameObject.SetActive(false);
        if (smilingCustomerInPolaroidImage != null) smilingCustomerInPolaroidImage.gameObject.SetActive(false);
        if (tanghuluInPolaroidImage != null) tanghuluInPolaroidImage.gameObject.SetActive(false);
    }

    void InitializeDialogueUI()
    {
        if (kikiSpeechBubbleGroup != null) { kikiSpeechBubbleGroup.alpha = 0; kikiSpeechBubbleGroup.gameObject.SetActive(false); }
        if (pupuSpeechBubbleGroup != null) { pupuSpeechBubbleGroup.alpha = 0; pupuSpeechBubbleGroup.gameObject.SetActive(false); }
        if (kikiNextButton != null) { kikiNextButton.gameObject.SetActive(false); kikiNextButton.onClick.RemoveAllListeners(); kikiNextButton.onClick.AddListener(OnDialogueNextButtonClicked); }
        if (pupuNextButton != null) { pupuNextButton.gameObject.SetActive(false); pupuNextButton.onClick.RemoveAllListeners(); pupuNextButton.onClick.AddListener(OnDialogueNextButtonClicked); }
    }

    IEnumerator SlideInTanghuluBoard()
    {
        if (tanghuluOnBoardRect == null) yield break;
        AudioManager.Instance?.PlayOneShotSound(tanghuluSlideInSound);
        float elapsedTime = 0f;
        Vector2 startPos = tanghuluOnBoardRect.anchoredPosition;
        while (elapsedTime < slideInDuration)
        {
            tanghuluOnBoardRect.anchoredPosition = Vector2.Lerp(startPos, tanghuluBoardTargetPos, elapsedTime / slideInDuration);
            elapsedTime += Time.deltaTime; yield return null;
        }
        tanghuluOnBoardRect.anchoredPosition = tanghuluBoardTargetPos;

        if (draggableTanghuluImage != null && draggableTanghuluImage.sprite != null && finalTanghuluImageOnBoard != null && finalTanghuluImageOnBoard.gameObject.activeSelf)
        {
            draggableTanghuluImage.gameObject.SetActive(true);
            draggableTanghuluImage.rectTransform.position = finalTanghuluImageOnBoard.rectTransform.position;
            draggableTanghuluImage.rectTransform.sizeDelta = finalTanghuluImageOnBoard.rectTransform.sizeDelta;
            finalTanghuluImageOnBoard.color = new Color(1, 1, 1, 0);
        }
    }

    public void HandleTanghuluDropped(DraggableTanghuluItem draggedItem, PointerEventData eventData)
    {
        if (tanghuluDelivered) return;
        Camera eventCamera = (m_Canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : m_Canvas.worldCamera;

        if (customerDropZoneRect != null && RectTransformUtility.RectangleContainsScreenPoint(customerDropZoneRect, eventData.position, eventCamera))
        {
            tanghuluDelivered = true;
            draggedItem.gameObject.SetActive(false);
            if (finalTanghuluImageOnBoard != null) finalTanghuluImageOnBoard.gameObject.SetActive(false);

            if (tanghuluInCustomerHandImage != null && currentOrder?.skewerWithToppingSprite != null)
            {
                tanghuluInCustomerHandImage.sprite = currentOrder.skewerWithToppingSprite;
                tanghuluInCustomerHandImage.gameObject.SetActive(true);
            }
            AudioManager.Instance?.PlayOneShotSound(tanghuluDeliveredSound);
            StartCustomerDialogue();
        }
        else
        {
            draggedItem.ResetToOriginalPosition();
        }
    }

    void StartCustomerDialogue()
    {
        if (currentOrder == null) { ProceedToTitleScene(); return; }

        activeDialogueSequence = (currentOrder.presentationDialogueSequence != null && currentOrder.presentationDialogueSequence.Count > 0)
                               ? currentOrder.presentationDialogueSequence
                               : GetDefaultThanksDialogues();

        if (activeDialogueSequence == null || activeDialogueSequence.Count == 0)
        {
            StartCoroutine(CaptureMoment());
            return;
        }

        currentDialogueIndex = 0;
        isDialoguePlaying = true;
        if (playDialogueCoroutine != null) StopCoroutine(playDialogueCoroutine);
        playDialogueCoroutine = StartCoroutine(PlayDialogueInternal());
    }

    List<DialogueEntry> GetDefaultThanksDialogues()
    {
        List<DialogueEntry> defaultDialogues = new List<DialogueEntry>();
        string customerName = currentOrder?.customerName ?? "손님";
        DialogueEntry.Speaker speaker = (customerName == "끼끼" || customerName == "키키") ? DialogueEntry.Speaker.Kiki : DialogueEntry.Speaker.Pupu;
        defaultDialogues.Add(new DialogueEntry { speaker = speaker, line = "정말 맛있어 보여! 고마워!" });
        return defaultDialogues;
    }

    IEnumerator PlayDialogueInternal()
    {
        HideAllDialogueBubbles();
        isDialoguePlaying = true;

        while (currentDialogueIndex < activeDialogueSequence.Count)
        {
            DialogueEntry entry = activeDialogueSequence[currentDialogueIndex];
            CanvasGroup targetBubbleGroup = null;
            TextMeshProUGUI targetTextComponent = null;
            Button targetNextButton = null;

            if (currentBubbleGroup != null && currentBubbleGroup.gameObject.activeSelf)
            {
                bool shouldHideOldBubble = (entry.speaker == DialogueEntry.Speaker.Kiki && currentBubbleGroup == pupuSpeechBubbleGroup) ||
                                           (entry.speaker == DialogueEntry.Speaker.Pupu && currentBubbleGroup == kikiSpeechBubbleGroup);
                if (shouldHideOldBubble) yield return StartCoroutine(FadeOutBubble(currentBubbleGroup));
            }

            if (entry.speaker == DialogueEntry.Speaker.Kiki)
            {
                targetBubbleGroup = kikiSpeechBubbleGroup; targetTextComponent = kikiSpeechText; targetNextButton = kikiNextButton;
            }
            else
            {
                targetBubbleGroup = pupuSpeechBubbleGroup; targetTextComponent = pupuSpeechText; targetNextButton = pupuNextButton;
            }
            currentSpeechText = targetTextComponent;
            currentBubbleGroup = targetBubbleGroup;
            currentNextButton = targetNextButton;

            if (targetBubbleGroup != null && targetTextComponent != null)
            {
                yield return ShowSingleDialogueBubble(targetBubbleGroup, targetTextComponent, entry.line, targetNextButton);
            }
            
            bool waitingForUserInteraction = true;
            while(waitingForUserInteraction)
            {
                if (isTextTyping) 
                {
                    yield return null;
                }
                else if (currentNextButton != null && currentNextButton.gameObject.activeSelf) 
                {
                    yield return null;
                }
                else 
                {
                    waitingForUserInteraction = false;
                }
            }
        }

        isDialoguePlaying = false;
        if (currentBubbleGroup != null && currentBubbleGroup.gameObject.activeSelf)
        {
            yield return StartCoroutine(FadeOutBubble(currentBubbleGroup));
        }
        Debug.Log("손님 감사 대화 종료. 사진 촬영 시작.");
        StartCoroutine(CaptureMoment());
    }

    void HideAllDialogueBubbles()
    {
        if (kikiSpeechBubbleGroup != null) { kikiSpeechBubbleGroup.alpha = 0; kikiSpeechBubbleGroup.gameObject.SetActive(false); }
        if (pupuSpeechBubbleGroup != null) { pupuSpeechBubbleGroup.alpha = 0; pupuSpeechBubbleGroup.gameObject.SetActive(false); }
        if (kikiNextButton != null) kikiNextButton.gameObject.SetActive(false);
        if (pupuNextButton != null) pupuNextButton.gameObject.SetActive(false);
    }

    IEnumerator ShowSingleDialogueBubble(CanvasGroup bubbleGroup, TextMeshProUGUI textComponent, string message, Button nextBtn)
    {
        if (bubbleGroup == null || textComponent == null) yield break;
        bubbleGroup.gameObject.SetActive(true);
        AudioManager.Instance?.PlayOneShotSound(dialogueBubbleOpenSound);
        yield return StartCoroutine(FadeInBubble(bubbleGroup));
        textComponent.text = "";

        if (typeTextCoroutine != null) StopCoroutine(typeTextCoroutine);
        typeTextCoroutine = StartCoroutine(TypeDialogueText(textComponent, message));
        yield return typeTextCoroutine;

        if (nextBtn != null)
        {
            nextBtn.gameObject.SetActive(true); // 마지막 대사 포함 모든 대사에서 다음 버튼 활성화
        }
    }

    IEnumerator TypeDialogueText(TextMeshProUGUI textComponent, string message)
    {
        isTextTyping = true;
        textComponent.text = "";
        AudioSource textSfxSource = null; // 사운드 준비 안됨
        Sound s = AudioManager.Instance?.sounds.Find(sound => sound.name == dialogueTextSound);
        if (s?.source != null)

        foreach (char c in message)
        {
            if (!isTextTyping) { textComponent.text = message; break; }
            textComponent.text += c;
            yield return new WaitForSeconds(0.03f);
        }
        if (textSfxSource != null && textSfxSource.isPlaying) { /* 사운드 중지 로직 */ }
        isTextTyping = false;
    }

    public void OnDialogueNextButtonClicked()
    {
        if (isTextTyping)
        {
            isTextTyping = false; 
            if (currentSpeechText != null && activeDialogueSequence != null && currentDialogueIndex < activeDialogueSequence.Count)
            {
                 currentSpeechText.text = activeDialogueSequence[currentDialogueIndex].line; 
            }
            AudioManager.Instance?.StopTextSound(); // 사운드 준비 안됨
        }
        else 
        {
            currentDialogueIndex++; 
            if (currentNextButton != null)
            {
                currentNextButton.gameObject.SetActive(false); 
            }
        }
    }

    IEnumerator FadeInBubble(CanvasGroup cg, float duration = 0.2f) { if (cg == null) yield break; float t = 0; cg.alpha = 0; while (t < duration) { t += Time.deltaTime; cg.alpha = Mathf.Lerp(0, 1, t / duration); yield return null; } cg.alpha = 1; }
    IEnumerator FadeOutBubble(CanvasGroup cg, float duration = 0.2f) { if (cg == null) yield break; float t = 0; float sa = cg.alpha; while (t < duration) { t += Time.deltaTime; cg.alpha = Mathf.Lerp(sa, 0, t / duration); yield return null; } cg.alpha = 0; cg.gameObject.SetActive(false); }

    IEnumerator CaptureMoment()
    {
        Debug.Log("CaptureMoment 코루틴 시작");
        // 사진 촬영 시에는 일반 손님 이미지는 비활성화하고, 웃는 손님 이미지와 손에 든 탕후루를 활성화합니다.
        if (customerImage != null)
        {
            customerImage.gameObject.SetActive(false);
        }

        if (smilingCustomerImage != null && currentOrder != null)
        {
            // CustomerOrderData에 smilingCustomerSprite 필드가 정의되어 있다고 가정합니다.
            // 없다면 customerSprite를 사용합니다.
            smilingCustomerImage.sprite = currentOrder.smilingCustomerSprite ?? currentOrder.customerSprite;
            smilingCustomerImage.gameObject.SetActive(smilingCustomerImage.sprite != null);
        }
        else if (customerImage != null) // 웃는 이미지가 없다면 기본 손님 이미지를 그대로 사용 (이미 활성화 되어있을 수 있음)
        {
            customerImage.gameObject.SetActive(true);
        }


        if (tanghuluInCustomerHandImage != null && currentOrder?.skewerWithToppingSprite != null)
        {
            tanghuluInCustomerHandImage.sprite = currentOrder.skewerWithToppingSprite;
            tanghuluInCustomerHandImage.gameObject.SetActive(true);
        }

        // 플래시 효과
        if (flashImage != null)
        {
            flashImage.gameObject.SetActive(true); flashImage.color = Color.white;
            AudioManager.Instance?.PlayOneShotSound(cameraShutterSound);
            yield return new WaitForSeconds(0.1f);
            float flashFadeDuration = 0.3f; float timer = 0;
            while (timer < flashFadeDuration) { flashImage.color = Color.Lerp(Color.white, Color.clear, timer / flashFadeDuration); timer += Time.deltaTime; yield return null; }
            flashImage.gameObject.SetActive(false);
        }
        else { AudioManager.Instance?.PlayOneShotSound(cameraShutterSound); yield return new WaitForSeconds(0.2f); }

        // 폴라로이드 효과
        if (polaroidFrameImage != null && smilingCustomerInPolaroidImage != null)
        {
            // 폴라로이드 안의 손님 이미지 설정
            if (smilingCustomerImage != null && smilingCustomerImage.sprite != null && smilingCustomerImage.gameObject.activeSelf)
                smilingCustomerInPolaroidImage.sprite = smilingCustomerImage.sprite;
            else if (customerImage != null && customerImage.sprite != null && customerImage.gameObject.activeSelf) // 웃는 이미지가 없으면 기본 손님 이미지
                smilingCustomerInPolaroidImage.sprite = customerImage.sprite;
            else if (currentOrder?.customerSprite != null) // 최후의 보루
                 smilingCustomerInPolaroidImage.sprite = currentOrder.customerSprite;


            // 폴라로이드 안의 탕후루 이미지 설정
            if (tanghuluInPolaroidImage != null && tanghuluInCustomerHandImage != null && tanghuluInCustomerHandImage.sprite != null && tanghuluInCustomerHandImage.gameObject.activeSelf)
            {
                tanghuluInPolaroidImage.sprite = tanghuluInCustomerHandImage.sprite;
                tanghuluInPolaroidImage.gameObject.SetActive(true);
            }

            polaroidFrameImage.gameObject.SetActive(true);
            if (smilingCustomerInPolaroidImage.sprite != null) smilingCustomerInPolaroidImage.gameObject.SetActive(true);
            AudioManager.Instance?.PlayOneShotSound(polaroidAppearSound);

            polaroidFrameImage.rectTransform.localScale = Vector3.zero;
            float appearDuration = 0.5f; float timer = 0;
            while (timer < appearDuration) { polaroidFrameImage.rectTransform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, timer / appearDuration); timer += Time.deltaTime; yield return null; }
            polaroidFrameImage.rectTransform.localScale = Vector3.one;

            yield return new WaitForSeconds(polaroidDisplayDuration);
            polaroidFrameImage.gameObject.SetActive(false);
            if (smilingCustomerInPolaroidImage != null) smilingCustomerInPolaroidImage.gameObject.SetActive(false);
            if (tanghuluInPolaroidImage != null) tanghuluInPolaroidImage.gameObject.SetActive(false);
        }
        else { yield return new WaitForSeconds(1.0f); }

        // 다음 씬으로 넘어가기 전에 현재 씬의 손님 관련 이미지들을 최종적으로 정리합니다.
        if (customerImage != null) customerImage.gameObject.SetActive(false);
        if (smilingCustomerImage != null) smilingCustomerImage.gameObject.SetActive(false);
        if (tanghuluInCustomerHandImage != null) tanghuluInCustomerHandImage.gameObject.SetActive(false);

        ProceedToTitleScene();
    }

    void ProceedToTitleScene()
    {
        GameInfoHolder.OpenStageSelectPanelOnLoad = true;
        string titleSceneName = CustomerOrderManager.Instance?.stageSelectSceneName ?? "TitleScene";
        Debug.Log(titleSceneName + " (스테이지 선택 화면)으로 돌아갑니다. StageSelectPanel 열기 요청됨.");
        SceneSwitcher.Instance?.LoadScene(titleSceneName);
    }
}
