using UnityEngine;

public class AspectRatioController : MonoBehaviour
{
    private float targetAspectRatio = 4.0f / 3.0f; // 640 / 480 = 4 / 3
    private int lastScreenWidth;
    private int lastScreenHeight;

    void Start()
    {
        // 초기 해상도 설정 (빌드 설정에서도 640x480으로 설정되어 있어야 함)
        Screen.SetResolution(640, 480, false); // false는 창 모드를 의미
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
    }

    void Update()
    {
        // 현재 창 크기가 이전 프레임과 다를 때만 비율 조정 로직 실행
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            // 현재 창 너비를 기준으로 높이 계산
            int newHeight = Mathf.RoundToInt(Screen.width / targetAspectRatio);

            // 현재 창 높이를 기준으로 너비 계산
            int newWidth = Mathf.RoundToInt(Screen.height * targetAspectRatio);

            // 너비를 기준으로 계산한 높이와 현재 높이 중 어느 것이 더 많이 변경되었는지 확인하여
            // 창 크기를 최소한으로 변경하면서 비율을 맞춤
            if (Mathf.Abs(Screen.height - newHeight) < Mathf.Abs(Screen.width - newWidth))
            {
                // 너비를 기준으로 높이 조절
                if (Screen.height != newHeight)
                {
                    Screen.SetResolution(Screen.width, newHeight, false);
                }
            }
            else
            {
                // 높이를 기준으로 너비 조절
                if (Screen.width != newWidth)
                {
                    Screen.SetResolution(newWidth, Screen.height, false);
                }
            }

            // 마지막 창 크기 업데이트
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
        }
    }
}