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

    private int currentDialogueIndex = 0;
    private bool isDialoguePlaying = false;
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

            yield return WaitForNextButtonClick();
            currentDialogueIndex++;
        }

        isDialoguePlaying = false;
        Debug.Log("모든 대화 종료!");
        // 대화 종료 후 로직
    }

    IEnumerator ShowSpeechBubbleAndText(CanvasGroup bubbleGroup, TextMeshProUGUI textComponent, string message, Button nextBtn)
    {
        if (bubbleGroup != null)
        {
            bubbleGroup.gameObject.SetActive(true);
            yield return StartCoroutine(FadeIn(bubbleGroup));
            // AudioManager를 통해 사운드 재생
            AudioManager.Instance?.PlaySound(bubbleOpenSoundName);
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
        textComponent.text = "";
        foreach (char c in message)
        {
            textComponent.text += c;
            // AudioManager를 통해 사운드 재생
            AudioManager.Instance?.PlayOneShotSound(textSoundName);
            yield return new WaitForSeconds(0.02f);
        }
        isTextTyping = false;
    }

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
        if (!isTextTyping)
        {
            StartCoroutine(FadeOut(currentBubbleGroup));
            if (currentBubbleGroup == kikiSpeechBubbleGroup && gameObject.GetComponentInParent<CustomerSquishyBounce>() != null)
            {
                gameObject.GetComponentInParent<CustomerSquishyBounce>().gameObject.SetActive(false);
            }
            if (currentNextButton != null)
            {
                currentNextButton.gameObject.SetActive(false);
            }
        }
    }
}