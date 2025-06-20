// 파일: Assets/shoporder/CustomerSquishyBounce.cs

using UnityEngine;
using System.Collections;

// 이 컴포넌트는 반드시 RectTransform이 있는 오브젝트에 붙어있어야 합니다.
[RequireComponent(typeof(RectTransform))]
public class CustomerSquishyBounce : MonoBehaviour
{
    [Header("애니메이션 (UI 좌표 기준)")]
    [Tooltip("UI 애니메이션 시작 지점 역할을 할 UI 오브젝트 (RectTransform)")]
    public RectTransform startMarker; // Transform -> RectTransform으로 변경
    [Tooltip("UI 애니메이션 최종 도착 지점 역할을 할 UI 오브젝트 (RectTransform)")]
    public RectTransform endMarker;   // Transform -> RectTransform으로 변경

    [Header("애니메이션 디테일")]
    public float moveSpeed = 5f;
    public float stretchAmount = 1.3f;
    public Vector3 initialScale = Vector3.one;
    [Tooltip("UI 좌표 기준으로 얼마나 더 튀어 오를지에 대한 값")]
    public float bounceHeight = 100f; // 월드 단위(1f)가 아닌 UI 픽셀 단위로 변경

    [Header("사운드")]
    public string arrivalSoundName = "CustomerArrival";
    
    private RectTransform rectTransform; // 자신의 RectTransform을 저장할 변수

    void Start()
    {
        // 자신의 RectTransform 컴포넌트를 가져옵니다.
        rectTransform = GetComponent<RectTransform>();

        if (startMarker == null || endMarker == null)
        {
            Debug.LogError("시작 또는 끝 UI 마커(Marker)가 할당되지 않았습니다!", this.gameObject);
            return;
        }

        // 마커의 월드 좌표(position)가 아닌, UI 좌표(anchoredPosition)를 가져옵니다.
        Vector2 startPos = startMarker.anchoredPosition;
        Vector2 endPos = endMarker.anchoredPosition;

        rectTransform.localScale = initialScale;
        // 자신의 UI 위치를 시작 마커의 UI 위치로 설정합니다.
        rectTransform.anchoredPosition = startPos;
        
        AudioManager.Instance?.PlayOneShotSound(arrivalSoundName);

        StartCoroutine(AnimateAndStartDialogue(startPos, endPos));
    }
    
    // BounceWithSquash 함수의 인자를 Vector3에서 Vector2로 변경합니다.
    IEnumerator BounceWithSquash(Vector2 start, Vector2 end)
    {
        // 튀어 오르는 위치 계산도 UI 좌표계에 맞게 수정합니다.
        Vector2 peakPos = end + Vector2.up * bounceHeight; 
        float t = 0;
        Vector3 stretchedScale = new Vector3(initialScale.x, initialScale.y * stretchAmount, initialScale.z);
        
        while (t < 1f)
        {
            t += Time.deltaTime * moveSpeed;
            // transform.position 대신 rectTransform.anchoredPosition을 변경합니다.
            rectTransform.anchoredPosition = Vector2.Lerp(start, peakPos, t);
            transform.localScale = Vector3.Lerp(initialScale, stretchedScale, t);
            yield return null;
        }

        t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * moveSpeed;
            // transform.position 대신 rectTransform.anchoredPosition을 변경합니다.
            rectTransform.anchoredPosition = Vector2.Lerp(peakPos, end, t);
            transform.localScale = Vector3.Lerp(stretchedScale, initialScale, t);
            yield return null;
        }
        rectTransform.anchoredPosition = end;
        transform.localScale = initialScale;
    }

    // 이 함수의 인자도 Vector2로 변경합니다.
    IEnumerator AnimateAndStartDialogue(Vector2 start, Vector2 end)
    {
        yield return StartCoroutine(BounceWithSquash(start, end));

        if (CustomerDialogueManager.Instance != null)
        {
            CustomerDialogueManager.Instance.StartDialogue();
        }
    }
}