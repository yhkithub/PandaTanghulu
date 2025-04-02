using UnityEngine;
using System.Collections;
using TMPro;

public class CustomerSquishyBounce : MonoBehaviour
{
    public Vector3 startPos = new Vector3(-6f, -10f, 0f);
    public Vector3 endPos = new Vector3(-6f, -1f, 0f);
    public float moveSpeed = 5f;
    public float stretchAmount = 1.3f;
    public float stretchSpeed = 10f;

    public CanvasGroup speechBubbleGroup;
    public TextMeshProUGUI speechText;

    private MonkeySpeech monkeySpeech;

    void Start()
    {
        transform.position = startPos;
        speechBubbleGroup.alpha = 0;
        speechBubbleGroup.gameObject.SetActive(false);

        // 👉 MonkeySpeech 스크립트 가져오기
        monkeySpeech = GetComponent<MonkeySpeech>();

        StartCoroutine(BounceWithSquash());
    }

    IEnumerator BounceWithSquash()
    {
        Vector3 peakPos = endPos + Vector3.up * 1f;
        float t = 0;
        Vector3 stretchedScale = new Vector3(1f, stretchAmount, 1f);
        Vector3 originalScale = Vector3.one;

        while (t < 1f)
        {
            t += Time.deltaTime * moveSpeed;
            transform.position = Vector3.Lerp(startPos, peakPos, t);
            transform.localScale = Vector3.Lerp(originalScale, stretchedScale, t);
            yield return null;
        }

        t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * moveSpeed;
            transform.position = Vector3.Lerp(peakPos, endPos, t);
            transform.localScale = Vector3.Lerp(stretchedScale, originalScale, t);
            yield return null;
        }

        // 도착 후 말풍선 등장
        StartCoroutine(ShowSpeechBubble());
    }

    IEnumerator ShowSpeechBubble()
    {
        speechBubbleGroup.gameObject.SetActive(true); // 말풍선 보이기
        speechBubbleGroup.alpha = 0f; // 처음에 투명

        // 말풍선 페이드 인
        float fadeTime = 0f;
        while (fadeTime < 1f)
        {
            fadeTime += Time.deltaTime * 2f;
            speechBubbleGroup.alpha = Mathf.Lerp(0f, 1f, fadeTime);
            yield return null;
        }

        // 👉 대사 비우기
        speechText.text = "";

        // 👉 랜덤 대사 받아오기
        string randomMessage = monkeySpeech.GetRandomSpeech();
        yield return StartCoroutine(TypeText(randomMessage)); // 타이핑 효과 시작
    }

    IEnumerator TypeText(string msg)
    {
        // 타이핑 효과
        foreach (char c in msg)
        {
            speechText.text += c; // 한 글자씩 추가
            yield return new WaitForSeconds(0.05f); // 타이핑 속도
        }
    }
}