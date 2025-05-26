using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class CustomerPresentationManager : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [Header("UI Elements - Scene Specific")]
    public Image customerImage;
    public Image smilingCustomerImage; // 손님 웃는 표정 이미지 (인스펙터에서 할당, 또는 CustomerOrderData에 추가)
    public RectTransform tanghuluOnBoardRect;
    public Image finalTanghuluImageOnBoard; // 도마 위에 '고정된' 최종 탕후루 이미지
    public Image draggableTanghuluImage;    // 플레이어가 드래그할 탕후루 이미지
    public RectTransform customerDropZoneRect;

    [Header("Dialogue UI (CustomerDialogueManager와 유사하게)")]
    public CanvasGroup kikiSpeechBubbleGroup;
    public TextMeshProUGUI kikiSpeechText;
    public Button kikiNextButton;
    public CanvasGroup pupuSpeechBubbleGroup;
    public TextMeshProUGUI pupuSpeechText;
    public Button pupuNextButton;

    [Header("Animation & Effects")]
    public float slideInDuration = 1.0f;
    public Vector2 tanghuluBoardHiddenPos = new Vector2(0, -1000f); // 화면 아래 숨겨진 위치 (anchoredPosition 기준)
    public Vector2 tanghuluBoardTargetPos = new Vector2(0, -300f);  // 화면에 나타날 최종 위치 (anchoredPosition 기준)
    public Image flashImage;
    public Image polaroidFrameImage;
    public Image smilingCustomerInPolaroidImage;
    public float polaroidDisplayDuration = 2.0f;

    [Header("Sound Effects (AudioManager에 등록된 이름)")]
    public string tanghuluSlideInSound = "TanghuluSlide";
    public string tanghuluDeliveredSound = "ItemCollect";
    public string cameraShutterSound = "CameraClick";
    public string polaroidAppearSound = "PolaroidAppear";
    public string dialogueTextSound = "Text"; // 대화 타이핑 소리
    public string dialogueBubbleOpenSound = "BubbleOpen"; // 말풍선 나타나는 소리

    private CustomerOrderData currentOrder;
    private Vector3 draggableTanghuluOriginalScreenPos; // 드래그 시작 전 스크린 좌표
    private bool isDraggingTanghulu = false;
    private bool tanghuluDelivered = false;
    private List<DialogueEntry> activeDialogueSequence;
    private int currentDialogueIndex = 0;
    private bool isDialoguePlaying = false;
    private bool isTextTyping = false;
    private CanvasGroup currentBubbleGroup;
    private Button currentNextButton;
    private TextMeshProUGUI currentSpeechText;

    private Canvas m_Canvas; // UI 이벤트 처리를 위한 Canvas 참조

    void Awake()
    {
        m_Canvas = GetComponentInParent<Canvas>();
        if (m_Canvas == null)
        {
            m_Canvas = FindFirstObjectByType<Canvas>();
            if (m_Canvas == null)
            {
                Debug.LogError("CustomerPresentationManager Error: 씬에서 Canvas를 찾을 수 없습니다. UI 기능이 정상적으로 동작하지 않을 수 있습니다. Canvas가 씬에 존재하는지 확인해주세요.");
                enabled = false;
                return;
            }
            else
            {
                Debug.LogWarning("CustomerPresentationManager Warning: 부모에서 Canvas를 찾지 못해 씬 전체에서 Canvas를 찾았습니다. UI GameObject가 Canvas 하위에 있는지 확인하는 것이 좋습니다.");
            }
        }
    }

    void Start()
    {
        Debug.Log("CustomerPresentationManager: Start() 호출됨");

        if (CustomerOrderManager.Instance == null)
        {
            Debug.LogError("CustomerPresentationManager Error: CustomerOrderManager.Instance가 null입니다! EditorSceneInitializer가 매니저를 생성하지 못했거나, 실행 순서에 문제가 있을 수 있습니다. TitleScene으로 이동합니다.");
            SceneSwitcher.Instance?.LoadScene("TitleScene");
            enabled = false;
            return;
        }
        Debug.Log("CustomerPresentationManager: CustomerOrderManager.Instance 확인 완료.");

        StartCoroutine(InitializeAfterManagerReady());
    }

    IEnumerator InitializeAfterManagerReady()
    {
        float waitTime = 0f;
        float maxWaitTime = 5f; // 최대 5초간 대기

        // CustomerOrderManager.Instance.CurrentOrderData가 null이 아닐 때까지 대기
        while (CustomerOrderManager.Instance.CurrentOrderData == null && waitTime < maxWaitTime)
        {
            Debug.Log($"CustomerPresentationManager: CurrentOrderData를 기다리는 중... (경과 시간: {waitTime:F1}s / {maxWaitTime}s)");
            yield return null; // 한 프레임 대기
            waitTime += Time.deltaTime;
        }

        currentOrder = CustomerOrderManager.Instance.CurrentOrderData;
        if (currentOrder == null)
        {
            Debug.LogError($"CustomerPresentationManager Error: 대기 시간({maxWaitTime}s) 초과 후에도 CustomerOrderManager.Instance.CurrentOrderData가 null입니다! GameInfoHolder.CustomerIndexToLoad ({GameInfoHolder.CustomerIndexToLoad})가 유효한지, CustomerOrderManager의 allCustomerOrders 리스트 및 초기화 로직을 확인하세요. TitleScene으로 이동합니다.");
            SceneSwitcher.Instance?.LoadScene("TitleScene");
            enabled = false; // 주문 데이터 없으면 진행 불가
            yield break; // 코루틴 종료
        }
        Debug.Log($"CustomerPresentationManager: 현재 손님 주문 데이터 '{currentOrder.customerName}' 로드 완료.");

        // 손님 이미지 설정
        if (customerImage != null && currentOrder.customerSprite != null)
        {
            customerImage.sprite = currentOrder.customerSprite;
            customerImage.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("CustomerPresentationManager: 손님 이미지 또는 주문 데이터의 손님 스프라이트가 없습니다.");
            if(customerImage != null) customerImage.gameObject.SetActive(false);
        }
        if (smilingCustomerImage != null) smilingCustomerImage.gameObject.SetActive(false);

        // 도마 위의 최종 탕후루 이미지 설정
        if (finalTanghuluImageOnBoard != null && currentOrder.skewerWithToppingSprite != null)
        {
            finalTanghuluImageOnBoard.sprite = currentOrder.skewerWithToppingSprite;
            finalTanghuluImageOnBoard.gameObject.SetActive(true);
            finalTanghuluImageOnBoard.color = Color.white; // 슬라이드 인 시 보이도록
        }
        else
        {
            Debug.LogWarning("CustomerPresentationManager: 도마 위 최종 탕후루 이미지(UI) 또는 주문 데이터의 최종 탕후루 스프라이트가 없습니다.");
            if (finalTanghuluImageOnBoard != null) finalTanghuluImageOnBoard.gameObject.SetActive(false);
        }

        // 드래그 가능한 탕후루 이미지 설정 (처음엔 숨김)
        if (draggableTanghuluImage != null)
        {
            if (currentOrder.skewerWithToppingSprite != null) {
                draggableTanghuluImage.sprite = currentOrder.skewerWithToppingSprite;
                Image imgComponent = draggableTanghuluImage.GetComponent<Image>();
                if (imgComponent != null) {
                    imgComponent.raycastTarget = true; // 드래그를 위해 Raycast Target 명시적 활성화
                    Debug.Log("DraggableTanghuluImage의 Raycast Target을 true로 설정했습니다.");
                } else {
                    Debug.LogError("DraggableTanghuluImage에 Image 컴포넌트가 없습니다!");
                }
            } else {
                 Debug.LogWarning("CustomerPresentationManager: 드래그할 탕후루 이미지가 주문 데이터에 없습니다.");
            }
            draggableTanghuluImage.gameObject.SetActive(false); // 처음엔 숨김
        } else {
            Debug.LogError("CustomerPresentationManager: DraggableTanghuluImage가 Inspector에 연결되지 않았습니다.");
        }


        if (tanghuluOnBoardRect != null)
        {
            tanghuluOnBoardRect.anchoredPosition = tanghuluBoardHiddenPos;
        }

        InitializeDialogueUI();

        if (flashImage != null) flashImage.gameObject.SetActive(false);
        if (polaroidFrameImage != null) polaroidFrameImage.gameObject.SetActive(false);
        if (smilingCustomerInPolaroidImage != null) smilingCustomerInPolaroidImage.gameObject.SetActive(false);

        StartCoroutine(SlideInTanghuluBoard());
    }

    void InitializeDialogueUI()
    {
        if (kikiSpeechBubbleGroup != null) { kikiSpeechBubbleGroup.alpha = 0; kikiSpeechBubbleGroup.gameObject.SetActive(false); }
        if (pupuSpeechBubbleGroup != null) { pupuSpeechBubbleGroup.alpha = 0; pupuSpeechBubbleGroup.gameObject.SetActive(false); }
        if (kikiNextButton != null) kikiNextButton.gameObject.SetActive(false);
        if (pupuNextButton != null) pupuNextButton.gameObject.SetActive(false);

        kikiNextButton?.onClick.RemoveAllListeners();
        kikiNextButton?.onClick.AddListener(OnDialogueNextButtonClicked);
        pupuNextButton?.onClick.RemoveAllListeners();
        pupuNextButton?.onClick.AddListener(OnDialogueNextButtonClicked);
    }

    IEnumerator SlideInTanghuluBoard()
    {
        if (tanghuluOnBoardRect == null)
        {
            Debug.LogError("SlideInTanghuluBoard: tanghuluOnBoardRect가 null입니다.");
            yield break;
        }

        AudioManager.Instance?.PlayOneShotSound(tanghuluSlideInSound);

        float elapsedTime = 0f;
        Vector2 startPos = tanghuluOnBoardRect.anchoredPosition;

        while (elapsedTime < slideInDuration)
        {
            tanghuluOnBoardRect.anchoredPosition = Vector2.Lerp(startPos, tanghuluBoardTargetPos, elapsedTime / slideInDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        tanghuluOnBoardRect.anchoredPosition = tanghuluBoardTargetPos;

        if (draggableTanghuluImage != null && draggableTanghuluImage.sprite != null && finalTanghuluImageOnBoard != null && finalTanghuluImageOnBoard.gameObject.activeSelf)
        {
            draggableTanghuluImage.gameObject.SetActive(true);
            draggableTanghuluImage.rectTransform.position = finalTanghuluImageOnBoard.rectTransform.position;
            draggableTanghuluImage.rectTransform.sizeDelta = finalTanghuluImageOnBoard.rectTransform.sizeDelta;
            draggableTanghuluOriginalScreenPos = draggableTanghuluImage.rectTransform.position;

            Image imgComp = draggableTanghuluImage.GetComponent<Image>();
            if (imgComp != null)
            {
                if (!imgComp.raycastTarget)
                {
                    Debug.LogWarning("DraggableTanghuluImage의 Raycast Target이 꺼져있어 드래그가 안 될 수 있습니다. Inspector에서 확인하거나 코드로 활성화합니다.");
                    imgComp.raycastTarget = true; // 명시적으로 활성화
                }
                Debug.Log($"DraggableTanghuluImage Raycast Target 상태 (SlideIn 후): {imgComp.raycastTarget}");
            }

            finalTanghuluImageOnBoard.color = new Color(1, 1, 1, 0);
            Debug.Log("도마 위 탕후루 숨김, 드래그용 탕후루 활성화 및 위치/크기 동기화 완료.");
        }
        else
        {
            Debug.LogWarning("드래그용 탕후루를 준비할 수 없거나, 도마 위 탕후루가 활성화되지 않았습니다. draggableTanghuluImage.sprite: " + (draggableTanghuluImage?.sprite != null) + ", finalTanghuluImageOnBoard: " + (finalTanghuluImageOnBoard != null) + ", finalTanghuluImageOnBoard.activeSelf: " + (finalTanghuluImageOnBoard?.gameObject.activeSelf));
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (draggableTanghuluImage == null) { Debug.LogWarning("OnBeginDrag: draggableTanghuluImage is null. 드래그 불가."); return; }
        if (eventData.pointerDrag == null) { Debug.LogWarning("OnBeginDrag: eventData.pointerDrag is null. 드래그 불가."); return; }

        // 드래그 대상이 draggableTanghuluImage인지, 활성화되어 있는지, 아직 전달되지 않았는지 확인
        if (eventData.pointerDrag == draggableTanghuluImage.gameObject && draggableTanghuluImage.gameObject.activeSelf && !tanghuluDelivered)
        {
            Image imgComp = draggableTanghuluImage.GetComponent<Image>();
            if (imgComp != null && !imgComp.raycastTarget)
            {
                Debug.LogError("OnBeginDrag: draggableTanghuluImage의 Raycast Target이 꺼져있어 드래그를 시작할 수 없습니다! Inspector에서 Image 컴포넌트의 Raycast Target을 활성화해주세요.");
                return;
            }

            isDraggingTanghulu = true;
            draggableTanghuluImage.rectTransform.SetAsLastSibling(); // 드래그 중인 이미지를 가장 앞으로 가져옴
            Debug.Log("탕후루 드래그 시작: " + draggableTanghuluImage.name);
        } else {
            // 드래그가 시작되지 않는 이유를 더 자세히 로그로 남깁니다.
            if (eventData.pointerDrag != draggableTanghuluImage.gameObject) Debug.LogWarning($"OnBeginDrag: 드래그된 오브젝트({eventData.pointerDrag.name})가 draggableTanghuluImage가 아닙니다. 실제 드래그 대상: {draggableTanghuluImage.name}");
            if (draggableTanghuluImage.gameObject.activeSelf == false) Debug.LogWarning("OnBeginDrag: draggableTanghuluImage가 비활성화되어 있습니다.");
            if (tanghuluDelivered) Debug.LogWarning("OnBeginDrag: 탕후루가 이미 전달되었습니다.");
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isDraggingTanghulu && draggableTanghuluImage != null)
        {
            if (m_Canvas != null && m_Canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                draggableTanghuluImage.rectTransform.position = eventData.position;
            }
            else if (m_Canvas != null) // ScreenSpaceCamera or WorldSpace
            {
                Vector2 localPointerPosition;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(m_Canvas.transform as RectTransform, eventData.position, m_Canvas.worldCamera, out localPointerPosition))
                {
                    draggableTanghuluImage.rectTransform.localPosition = localPointerPosition;
                }
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDraggingTanghulu || draggableTanghuluImage == null) return;

        isDraggingTanghulu = false;
        Camera eventCamera = (m_Canvas != null && m_Canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : m_Canvas.worldCamera;

        if (customerDropZoneRect != null &&
            RectTransformUtility.RectangleContainsScreenPoint(customerDropZoneRect, eventData.position, eventCamera))
        {
            Debug.Log("탕후루가 손님에게 전달되었습니다!");
            tanghuluDelivered = true;
            draggableTanghuluImage.gameObject.SetActive(false);

            if(finalTanghuluImageOnBoard != null) finalTanghuluImageOnBoard.gameObject.SetActive(false);

            AudioManager.Instance?.PlayOneShotSound(tanghuluDeliveredSound);
            StartCustomerDialogue();
        }
        else
        {
            Debug.Log("탕후루가 손님 영역 바깥에 놓였습니다. 원위치합니다.");
            draggableTanghuluImage.rectTransform.position = draggableTanghuluOriginalScreenPos;
        }
    }

    void StartCustomerDialogue()
    {
        if (currentOrder == null)
        {
            Debug.LogError("StartCustomerDialogue: currentOrder가 null입니다. 사진 촬영으로 넘어갈 수 없습니다.");
            ProceedToTitleScene();
            return;
        }

        var presentationDialogueField = currentOrder.GetType().GetField("presentationDialogueSequence");
        if (presentationDialogueField != null)
        {
             activeDialogueSequence = presentationDialogueField.GetValue(currentOrder) as List<DialogueEntry>;
             if (activeDialogueSequence == null || activeDialogueSequence.Count == 0) {
                Debug.Log("presentationDialogueSequence가 비어있거나 CustomerOrderData에 정의되지 않아 기본 감사 대사를 사용합니다.");
                activeDialogueSequence = GetDefaultThanksDialogues();
             } else {
                Debug.Log(currentOrder.customerName + " 손님의 전달 후 대사를 사용합니다.");
             }
        }
        else
        {
            Debug.Log("presentationDialogueSequence 필드가 CustomerOrderData에 정의되지 않았습니다. 기본 감사 대사를 사용합니다.");
            activeDialogueSequence = GetDefaultThanksDialogues();
        }

        if (activeDialogueSequence == null || activeDialogueSequence.Count == 0) {
             Debug.Log("표시할 감사 대사가 없습니다. 바로 사진 촬영으로 넘어갑니다.");
            StartCoroutine(CaptureMoment());
            return;
        }

        currentDialogueIndex = 0;
        isDialoguePlaying = true;
        StartCoroutine(PlayDialogueInternal());
    }

    List<DialogueEntry> GetDefaultThanksDialogues()
    {
        List<DialogueEntry> defaultDialogues = new List<DialogueEntry>();
        string customerName = (currentOrder != null) ? currentOrder.customerName : "손님";

        if (customerName == "끼끼") {
            defaultDialogues.Add(new DialogueEntry { speaker = DialogueEntry.Speaker.Kiki, line = "우와! 정말 맛있어 보이는 탕후루야! 고마워 푸푸!" });
        } else if (customerName == "푸푸") {
            defaultDialogues.Add(new DialogueEntry { speaker = DialogueEntry.Speaker.Pupu, line = "내가 주문한 탕후루다! 정말 고마워!" });
        }
        else {
            defaultDialogues.Add(new DialogueEntry { speaker = DialogueEntry.Speaker.Pupu, line = $"정말 고마워요, {customerName}님! 맛있게 먹을게요!" });
        }
        return defaultDialogues;
    }

    IEnumerator PlayDialogueInternal() {
        HideAllDialogueBubbles();

        while (currentDialogueIndex < activeDialogueSequence.Count)
        {
            DialogueEntry entry = activeDialogueSequence[currentDialogueIndex];
            CanvasGroup targetBubbleGroup = null;
            TextMeshProUGUI targetTextComponent = null;
            Button targetNextButton = null;

            if (currentBubbleGroup != null && currentBubbleGroup.gameObject.activeSelf)
            {
                 bool shouldHideOldBubble = false;
                 if (entry.speaker == DialogueEntry.Speaker.Kiki && currentBubbleGroup == pupuSpeechBubbleGroup) shouldHideOldBubble = true;
                 if (entry.speaker == DialogueEntry.Speaker.Pupu && currentBubbleGroup == kikiSpeechBubbleGroup) shouldHideOldBubble = true;
                 if (shouldHideOldBubble) yield return StartCoroutine(FadeOutBubble(currentBubbleGroup));
            }

            if (entry.speaker == DialogueEntry.Speaker.Kiki)
            {
                targetBubbleGroup = kikiSpeechBubbleGroup;
                targetTextComponent = kikiSpeechText;
                targetNextButton = kikiNextButton;
                currentSpeechText = kikiSpeechText;
            }
            else
            {
                targetBubbleGroup = pupuSpeechBubbleGroup;
                targetTextComponent = pupuSpeechText;
                targetNextButton = pupuNextButton;
                currentSpeechText = pupuSpeechText;
            }

            currentBubbleGroup = targetBubbleGroup;
            currentNextButton = targetNextButton;

            if (targetBubbleGroup != null && targetTextComponent != null)
            {
                yield return ShowSingleDialogueBubble(targetBubbleGroup, targetTextComponent, entry.line, targetNextButton);
            }

            while (isTextTyping || (currentNextButton != null && currentNextButton.gameObject.activeSelf))
            {
                yield return null;
            }
        }

        isDialoguePlaying = false;
        if (currentBubbleGroup != null && currentBubbleGroup.gameObject.activeSelf) {
            yield return StartCoroutine(FadeOutBubble(currentBubbleGroup));
        }
        Debug.Log("손님 감사 대화 종료.");
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
        if (bubbleGroup == null || textComponent == null)
        {
            Debug.LogError("ShowSingleDialogueBubble: bubbleGroup 또는 textComponent가 null입니다.");
            yield break;
        }
        bubbleGroup.gameObject.SetActive(true);
        AudioManager.Instance?.PlayOneShotSound(dialogueBubbleOpenSound);
        yield return StartCoroutine(FadeInBubble(bubbleGroup));
        textComponent.text = "";
        yield return StartCoroutine(TypeDialogueText(textComponent, message));

        if (nextBtn != null)
        {
            if (currentDialogueIndex < activeDialogueSequence.Count - 1)
            {
                nextBtn.gameObject.SetActive(true);
            }
            else
            {
                nextBtn.gameObject.SetActive(false);
            }
        }
    }

    IEnumerator TypeDialogueText(TextMeshProUGUI textComponent, string message)
    {
        isTextTyping = true;
        textComponent.text = "";
        AudioSource textSfxSource = null;

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(dialogueTextSound))
        {
            Sound s = AudioManager.Instance.sounds.Find(sound => sound.name == dialogueTextSound);
            if (s != null && s.source != null)
            {
                textSfxSource = s.source;
                textSfxSource.loop = true;
                if (AudioManager.Instance.IsSfxEnabled) textSfxSource.Play();
            }
        }

        foreach (char c in message)
        {
            if (!isTextTyping) {
                textComponent.text = message;
                break;
            }
            textComponent.text += c;
            yield return new WaitForSeconds(0.03f);
        }

        if (textSfxSource != null && textSfxSource.isPlaying)
        {
            textSfxSource.Stop();
            textSfxSource.loop = false;
        }
        isTextTyping = false;
    }

    public void OnDialogueNextButtonClicked() {
        if (isTextTyping) {
            isTextTyping = false;
        } else {
            if (currentNextButton != null) {
                currentNextButton.gameObject.SetActive(false);
            }
            currentDialogueIndex++;
        }
    }

    IEnumerator FadeInBubble(CanvasGroup canvasGroup, float duration = 0.2f)
    {
        if (canvasGroup == null) yield break;
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

    IEnumerator FadeOutBubble(CanvasGroup canvasGroup, float duration = 0.2f)
    {
        if (canvasGroup == null) yield break;
        float time = 0f;
        float startAlpha = canvasGroup.alpha;
        while (time < duration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, time / duration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        canvasGroup.gameObject.SetActive(false);
    }

    IEnumerator CaptureMoment()
    {
        if (customerImage != null) customerImage.gameObject.SetActive(false);
        if (smilingCustomerImage != null && currentOrder != null)
        {
            Sprite smileSprite = null;
            var smilingSpriteField = currentOrder.GetType().GetField("smilingCustomerSprite");
            if (smilingSpriteField != null)
            {
                smileSprite = smilingSpriteField.GetValue(currentOrder) as Sprite;
            }

            if (smileSprite != null)
            {
                smilingCustomerImage.sprite = smileSprite;
            }
            else if (currentOrder.customerSprite != null)
            {
                smilingCustomerImage.sprite = currentOrder.customerSprite;
                Debug.LogWarning($"{currentOrder.customerName}의 웃는 스프라이트(smilingCustomerSprite)가 없어 기본 스프라이트를 사용합니다.");
            }
            else
            {
                Debug.LogError($"{currentOrder.customerName}의 기본 스프라이트 및 웃는 스프라이트 모두 없습니다.");
            }

            if (smilingCustomerImage.sprite != null) smilingCustomerImage.gameObject.SetActive(true);
            else smilingCustomerImage.gameObject.SetActive(false);
        }

        if (flashImage != null)
        {
            flashImage.gameObject.SetActive(true);
            flashImage.color = Color.white;
            AudioManager.Instance?.PlayOneShotSound(cameraShutterSound);
            yield return new WaitForSeconds(0.1f);
            float flashFadeDuration = 0.3f;
            float timer = 0;
            while(timer < flashFadeDuration)
            {
                flashImage.color = Color.Lerp(Color.white, Color.clear, timer / flashFadeDuration);
                timer += Time.deltaTime;
                yield return null;
            }
            flashImage.gameObject.SetActive(false);
        } else {
            AudioManager.Instance?.PlayOneShotSound(cameraShutterSound);
            yield return new WaitForSeconds(0.2f);
        }

        if (polaroidFrameImage != null && smilingCustomerInPolaroidImage != null)
        {
            if (smilingCustomerImage != null && smilingCustomerImage.sprite != null && smilingCustomerImage.gameObject.activeSelf) {
                 smilingCustomerInPolaroidImage.sprite = smilingCustomerImage.sprite;
            } else if (currentOrder != null && currentOrder.customerSprite != null) {
                 smilingCustomerInPolaroidImage.sprite = currentOrder.customerSprite;
            }


            polaroidFrameImage.gameObject.SetActive(true);
             if (smilingCustomerInPolaroidImage.sprite != null) {
                smilingCustomerInPolaroidImage.gameObject.SetActive(true);
            }
            AudioManager.Instance?.PlayOneShotSound(polaroidAppearSound);

            polaroidFrameImage.rectTransform.localScale = Vector3.zero;
            float appearDuration = 0.5f;
            float timer = 0;
            while(timer < appearDuration)
            {
                polaroidFrameImage.rectTransform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, timer / appearDuration);
                timer += Time.deltaTime;
                yield return null;
            }
            polaroidFrameImage.rectTransform.localScale = Vector3.one;

            yield return new WaitForSeconds(polaroidDisplayDuration);
            polaroidFrameImage.gameObject.SetActive(false);
             if (smilingCustomerInPolaroidImage != null) smilingCustomerInPolaroidImage.gameObject.SetActive(false);

        }
        else
        {
            yield return new WaitForSeconds(1.0f);
        }

        ProceedToTitleScene();
    }

    void ProceedToTitleScene()
    {
        string titleSceneName = "TitleScene"; // 기본값
        if (CustomerOrderManager.Instance != null && !string.IsNullOrEmpty(CustomerOrderManager.Instance.stageSelectSceneName))
        {
            titleSceneName = CustomerOrderManager.Instance.stageSelectSceneName;
        }

        Debug.Log(titleSceneName + " (스테이지 선택 화면)으로 돌아갑니다.");
        if (SceneSwitcher.Instance != null)
        {
            SceneSwitcher.Instance.LoadScene(titleSceneName);
        }
        else
        {
            Debug.LogWarning("SceneSwitcher.Instance가 null입니다. SceneManager를 사용하여 직접 로드합니다.");
            SceneManager.LoadScene(titleSceneName);
        }
    }
}
