// Assets/shoporder/CustomerSquishyBounce.cs
using UnityEngine;
using System.Collections;

public class CustomerSquishyBounce : MonoBehaviour
{
    [Header("애니메이션 (화면 비율 기준)")]
    [Tooltip("등장 시작 위치 (X:0~1, Y:0~1). Y를 0 미만으로 하면 화면 밖에서 시작합니다.")]
    public Vector2 viewportStartPos = new Vector2(0.5f, -0.2f);
    [Tooltip("최종 도착 위치 (X:0~1, Y:0~1).")]
    public Vector2 viewportEndPos = new Vector2(0.5f, 0.4f);

    [Header("애니메이션 디테일")]
    public float moveSpeed = 5f;
    public float stretchAmount = 1.3f;
    public Vector3 initialScale = Vector3.one;

    [Header("사운드")]
    public string arrivalSoundName = "CustomerArrival";

    // [수정] OnEnable 대신 Start를 사용하여 스크립트 생명주기 동안 단 한 번만 실행되도록 합니다.
    void Start()
    {
        // 뷰포트 좌표를 현재 카메라 기준의 월드 좌표로 변환
        Camera mainCamera = Camera.main;
        Vector3 worldStartPos = mainCamera.ViewportToWorldPoint(new Vector3(viewportStartPos.x, viewportStartPos.y, 10));
        Vector3 worldEndPos = mainCamera.ViewportToWorldPoint(new Vector3(viewportEndPos.x, viewportEndPos.y, 10));

        transform.localScale = initialScale;
        transform.position = worldStartPos;
        
        AudioManager.Instance?.PlayOneShotSound(arrivalSoundName);

        StartCoroutine(AnimateAndStartDialogue(worldStartPos, worldEndPos));
    }

    IEnumerator BounceWithSquash(Vector3 start, Vector3 end)
    {
        Vector3 peakPos = end + Vector3.up * 1f;
        float t = 0;
        Vector3 stretchedScale = new Vector3(initialScale.x, initialScale.y * stretchAmount, initialScale.z);
        
        while (t < 1f)
        {
            t += Time.deltaTime * moveSpeed;
            transform.position = Vector3.Lerp(start, peakPos, t);
            transform.localScale = Vector3.Lerp(initialScale, stretchedScale, t);
            yield return null;
        }

        t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * moveSpeed;
            transform.position = Vector3.Lerp(peakPos, end, t);
            transform.localScale = Vector3.Lerp(stretchedScale, initialScale, t);
            yield return null;
        }
        transform.position = end;
        transform.localScale = initialScale;
    }

    IEnumerator AnimateAndStartDialogue(Vector3 start, Vector3 end)
    {
        yield return StartCoroutine(BounceWithSquash(start, end));

        if (CustomerDialogueManager.Instance != null)
        {
            CustomerDialogueManager.Instance.StartDialogue();
        }
    }
}