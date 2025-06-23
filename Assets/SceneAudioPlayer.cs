using UnityEngine;

public class SceneAudioPlayer : MonoBehaviour
{
    // 에디터에서 재생할 BGM의 '이름'을 입력
    public string bgmName;

    void Start()
    {
        // AudioManager가 준비되었는지 확인 후 이름으로 BGM 재생
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(bgmName))
        {
            AudioManager.Instance.PlayBgm(bgmName);
        }
    }
}