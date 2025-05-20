using UnityEngine;
using System.Collections;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;

[System.Serializable]
public class DialogueEntry
{
    public enum Speaker { Kiki, Pupu }
    public Speaker speaker;
    [TextArea(3, 10)] public string line;
}

public class CustomerDialogueManager : MonoBehaviour
{
    [Header("씬 전환 설정")]
    public string fruitCatchingSceneName = "FruitCatchingGameScene"; // 과일 꽂기 씬 이름

    // private List<DialogueEntry> dialogueSequence = new List<DialogueEntry>(); // 이 부분은 이제 외부에서 받아옴
    private CustomerOrderData currentCustomerDataForDialogue; // 현재 대화할 손님의 전체 데이터

    private int currentDialogueIndex = 0;
    private bool isDialoguePlaying = false;
    public int customerIndexForThisDialogue = 0; // 이 대화가 어떤 손님을 위한 것인지 Inspector에서 설정 (0: 끼끼, 1: 뭉뭉 등)

    [Header("말풍선 설정")]
    public CanvasGroup kikiSpeechBubbleGroup;
    public TextMeshProUGUI kikiSpeechText;
    public Button kikiNextButton;

    public CanvasGroup pupuSpeechBubbleGroup;
    public TextMeshProUGUI pupuSpeechText;
    public Button pupuNextButton;

    [Header("오디오 설정")]
    public string bubbleOpenSoundName = "BubbleOpen"; // Inspector에서 연결할 사운드 이름
    public string textSoundName = "Text";         // Inspector에서 연결할 사운드 이름

    [Header("대화 순서 및 내용")]
    public List<DialogueEntry> dialogueSequence = new List<DialogueEntry>();
    private bool isTextTyping = false;
    private CanvasGroup currentBubbleGroup;
    private Button currentNextButton;

    void Start()
    {
        if (kikiNextButton != null)
        {
            kikiNextButton.onClick.AddListener(OnNextButtonClicked);
            kikiNextButton.gameObject.SetActive(false);
        }
        if (pupuNextButton != null)
        {
            pupuNextButton.onClick.AddListener(OnNextButtonClicked);
            pupuNextButton.gameObject.SetActive(false);
        }

        if (kikiSpeechBubbleGroup != null) kikiSpeechBubbleGroup.alpha = 0f;
        if (pupuSpeechBubbleGroup != null) pupuSpeechBubbleGroup.alpha = 0f;

        if (CustomerOrderManager.Instance != null && CustomerOrderManager.Instance.allCustomerOrders.Count > GameInfoHolder.CustomerIndexToLoad)
        {
            currentCustomerDataForDialogue = CustomerOrderManager.Instance.allCustomerOrders[GameInfoHolder.CustomerIndexToLoad];
            if (currentCustomerDataForDialogue.dialogueSequence != null && currentCustomerDataForDialogue.dialogueSequence.Count > 0)
            {
                StartDialogueForCustomer(currentCustomerDataForDialogue);
            }
            else
            {
                Debug.LogWarning(currentCustomerDataForDialogue.customerName + " 손님의 대화 내용이 없습니다. 바로 게임으로 넘어갑니다.");
                ProceedToGame(); // 대화 없이 바로 게임으로
            }
        }
        else
        {
            Debug.LogError("CustomerDialogueManager: 유효한 손님 정보를 찾을 수 없습니다. (CustomerOrderManager 또는 GameInfoHolder 확인)");
            // 오류 처리 또는 기본 게임 시작 등
            ProceedToGame(); // 예: 오류 시 바로 게임으로 (또는 메인 화면으로)
        }
    }

    public void StartDialogueForCustomer(CustomerOrderData customerData)
    {
        if (isDialoguePlaying || customerData == null || customerData.dialogueSequence == null || customerData.dialogueSequence.Count == 0)
        {
            if (customerData != null)
                Debug.LogWarning(customerData.customerName + "의 대화를 시작할 수 없거나 대화 내용이 없습니다.");
            else
                Debug.LogWarning("CustomerData가 null입니다.");
            ProceedToGame(); // 대화 시작 불가 시 게임으로
            return;
        }
        currentCustomerDataForDialogue = customerData;
        currentDialogueIndex = 0;
        StartCoroutine(ProceedDialogueInternal(customerData.dialogueSequence));
    }

    IEnumerator ProceedDialogueInternal(List<DialogueEntry> dialogueEntries)
    {
        isDialoguePlaying = true;
        // 푸푸와 손님 말풍선 초기화
        if (kikiSpeechBubbleGroup != null) { kikiSpeechBubbleGroup.alpha = 0f; kikiSpeechBubbleGroup.gameObject.SetActive(false); }
        if (pupuSpeechBubbleGroup != null) { pupuSpeechBubbleGroup.alpha = 0f; pupuSpeechBubbleGroup.gameObject.SetActive(false); }


        while (currentDialogueIndex < dialogueEntries.Count)
        {
            DialogueEntry entry = dialogueEntries[currentDialogueIndex];
            CanvasGroup targetBubbleGroup = null;
            TextMeshProUGUI targetTextComponent = null;
            Button targetNextButton = null;

            // 현재 대화 상대가 아닌 쪽 말풍선은 확실히 숨김
            if (entry.speaker == DialogueEntry.Speaker.Kiki) // 끼끼 또는 다른 동물 손님
            {
                targetBubbleGroup = kikiSpeechBubbleGroup;
                targetTextComponent = kikiSpeechText;
                targetNextButton = kikiNextButton;
                if (pupuSpeechBubbleGroup != null && pupuSpeechBubbleGroup.alpha > 0) yield return StartCoroutine(FadeOut(pupuSpeechBubbleGroup));
            }
            else if (entry.speaker == DialogueEntry.Speaker.Pupu) // 푸푸
            {
                targetBubbleGroup = pupuSpeechBubbleGroup;
                targetTextComponent = pupuSpeechText;
                targetNextButton = pupuNextButton;
                if (kikiSpeechBubbleGroup != null && kikiSpeechBubbleGroup.alpha > 0) yield return StartCoroutine(FadeOut(kikiSpeechBubbleGroup));
            }

            currentBubbleGroup = targetBubbleGroup; // OnNextButtonClicked에서 사용하기 위함
            currentNextButton = targetNextButton;   // OnNextButtonClicked에서 사용하기 위함

            if (targetBubbleGroup != null && targetTextComponent != null)
            {
                yield return ShowSpeechBubbleAndText(targetBubbleGroup, targetTextComponent, entry.line, targetNextButton);
            }

            // 다음 버튼 클릭 대기 (클릭되면 OnNextButtonClicked에서 currentNextButton이 비활성화됨)
            while (currentNextButton != null && currentNextButton.gameObject.activeSelf)
            {
                yield return null;
            }
            // isTextTyping이 false가 될 때까지 추가로 기다릴 수도 있음 (클릭 스킵 방지)
            while(isTextTyping) yield return null;


            currentDialogueIndex++;
        }

        // 모든 대화가 끝나면 현재 열려있는 말풍선도 FadeOut
        if(currentBubbleGroup != null && currentBubbleGroup.alpha > 0)
        {
            yield return StartCoroutine(FadeOut(currentBubbleGroup));
        }

        isDialoguePlaying = false;
        Debug.Log("모든 대화 종료!");
        ProceedToGame();
    }
    
    void ProceedToGame()
    {
        // GameInfoHolder.CustomerIndexToLoad는 이미 Start에서 설정되었거나,
        // 또는 이 DialogueManager가 특정 스테이지 전용이라면 해당 인덱스를 사용.
        // CustomerOrderManager가 다음 씬에서 이 값을 읽어 해당 손님의 주문을 로드.
        Debug.Log("과일 꽂기 게임 씬으로 전환 준비. 로드할 손님 인덱스: " + GameInfoHolder.CustomerIndexToLoad);

        if (SceneSwitcher.Instance != null)
        {
            SceneSwitcher.Instance.LoadFruitCatchingScene(fruitCatchingSceneName);
        }
        else
        {
            Debug.LogError("SceneSwitcher 인스턴스를 찾을 수 없습니다!");
            UnityEngine.SceneManagement.SceneManager.LoadScene(fruitCatchingSceneName); // 대체
        }
    }

    public void StartFirstDialogue()
    {
        if (isDialoguePlaying) return;
        StartCoroutine(ProceedDialogue());
    }

    IEnumerator ProceedDialogue()
    {
        isDialoguePlaying = true;

        while (currentDialogueIndex < dialogueSequence.Count)
        {
            DialogueEntry entry = dialogueSequence[currentDialogueIndex];

            if (entry.speaker == DialogueEntry.Speaker.Kiki)
            {
                currentBubbleGroup = kikiSpeechBubbleGroup;
                currentNextButton = kikiNextButton;
                yield return ShowSpeechBubbleAndText(kikiSpeechBubbleGroup, kikiSpeechText, entry.line, kikiNextButton);
            }
            else if (entry.speaker == DialogueEntry.Speaker.Pupu)
            {
                currentBubbleGroup = pupuSpeechBubbleGroup;
                currentNextButton = pupuNextButton;
                yield return ShowSpeechBubbleAndText(pupuSpeechBubbleGroup, pupuSpeechText, entry.line, pupuNextButton);
            }

            yield return WaitForNextButtonClick(); // 버튼 클릭 대기
            // 현재 말풍선 숨기기 (OnNextButtonClicked에서 이미 처리하고 있다면 이 부분은 필요 없을 수 있음)
            if(currentBubbleGroup != null && currentBubbleGroup.alpha > 0)
            {
                StartCoroutine(FadeOut(currentBubbleGroup));
            }
            currentDialogueIndex++;
        }

        isDialoguePlaying = false;
        Debug.Log("모든 대화 종료! (" + (dialogueSequence.Count > 0 ? dialogueSequence[0].speaker.ToString() : "Unknown") + " 손님)");

        // 대화 종료 후 다음 씬으로 전환하고, 어떤 손님의 주문을 로드할지 정보 전달
        GameInfoHolder.CustomerIndexToLoad = customerIndexForThisDialogue; // ★★★ 정보 저장
        Debug.Log("다음 로드할 손님 인덱스 저장: " + GameInfoHolder.CustomerIndexToLoad);

        // SceneSwitcher를 통해 씬 로드
        if (SceneSwitcher.Instance != null)
        {
            SceneSwitcher.Instance.LoadFruitCatchingScene(fruitCatchingSceneName);
        }
        else
        {
            Debug.LogError("SceneSwitcher 인스턴스를 찾을 수 없습니다!");
            // 대체 방법: 직접 씬 로드 (하지만 SceneSwitcher 사용 권장)
            // UnityEngine.SceneManagement.SceneManager.LoadScene(fruitCatchingSceneName);
        }

        // 대화 종료 후 로직
    }

    IEnumerator ShowSpeechBubbleAndText(CanvasGroup bubbleGroup, TextMeshProUGUI textComponent, string message, Button nextBtn)
    {
        if (bubbleGroup != null)
        {
            bubbleGroup.gameObject.SetActive(true);
            // 말풍선 등장 사운드를 FadeIn 시작 전에 재생
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
    }

    IEnumerator TypeText(TextMeshProUGUI textComponent, string message)
    {
        isTextTyping = true;
        textComponent.text = ""; // 텍스트 초기화
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(textSoundName))
        {
            // PlayOneShot 대신 Play를 사용하고, StopTextSound로 제어
            Sound s = AudioManager.Instance.sounds.Find(sound => sound.name == textSoundName);
            if (s != null)
            {
                s.source.Play();
            }
        }
        foreach (char c in message)
        {
            textComponent.text += c;
            yield return new WaitForSeconds(0.02f); // 타이핑 속도 약간 빠르게 조정
        }
        isTextTyping = false;
        // 타이핑이 끝나면 텍스트 사운드 정지
        AudioManager.Instance?.StopTextSound();
    }


    //IEnumerator TypeText(TextMeshProUGUI textComponent, string message)
    //{
    //    isTextTyping = true;
    //    textComponent.text = "";
    //    foreach (char c in message)
    //    {
    //        textComponent.text += c;
    //        // AudioManager를 통해 사운드 재생
    //        AudioManager.Instance?.PlayOneShotSound(textSoundName);
    //        yield return new WaitForSeconds(0.02f);
    //    }
    //    isTextTyping = false;
    //}

    IEnumerator FadeIn(CanvasGroup canvasGroup, float duration = 0.2f)
    {
        float time = 0f;
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
        while (time < duration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, time / duration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        if (canvasGroup == kikiSpeechBubbleGroup && kikiNextButton != null)
        {
            kikiNextButton.gameObject.SetActive(false);
        }
        else if (canvasGroup == pupuSpeechBubbleGroup && pupuNextButton != null)
        {
            pupuNextButton.gameObject.SetActive(false);
        }
    }

    IEnumerator WaitForNextButtonClick()
    {
        while (currentNextButton != null && currentNextButton.gameObject.activeSelf)
        {
            yield return null;
        }
    }

    void OnNextButtonClicked()
    {
        if (isTextTyping) // 타이핑 중이면 텍스트 전체 표시
        {
            StopAllCoroutines(); // 현재 진행중인 TypeText 코루틴 강제 종료
            if (currentBubbleGroup == kikiSpeechBubbleGroup) kikiSpeechText.text = dialogueSequence[currentDialogueIndex].line;
            else if (currentBubbleGroup == pupuSpeechBubbleGroup) pupuSpeechText.text = dialogueSequence[currentDialogueIndex].line;
            isTextTyping = false;
             AudioManager.Instance?.StopTextSound();
            // nextButton은 계속 활성화 상태 유지
        }
        else // 타이핑 완료 후 클릭
        {
            if (currentNextButton != null)
            {
                currentNextButton.gameObject.SetActive(false); // 버튼 비활성화하여 WaitForNextButtonClick 코루틴을 진행시킴
            }
             // FadeOut은 ProceedDialogueInternal 루프에서 다음 대사 시작 전에 처리하거나, 여기서 명시적으로 해도 됨.
             // StartCoroutine(FadeOut(currentBubbleGroup)); // 여기서 바로 숨기면 다음 대사 FadeIn과 겹칠 수 있음
        }
    }
}