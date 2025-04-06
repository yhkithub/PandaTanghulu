using UnityEngine;

// SpriteRenderer 컴포넌트가 반드시 필요함을 명시
[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundScaler : MonoBehaviour
{
    private SpriteRenderer sr; // SpriteRenderer 참조 캐싱

    void Awake() // Start 대신 Awake에서 참조를 얻는 것이 더 안전합니다.
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            Debug.LogError("BackgroundScaler Error: 이 오브젝트에 SpriteRenderer 컴포넌트가 없습니다!", this.gameObject);
            enabled = false; // 오류 발생 시 스크립트 비활성화
        }
    }

    void Start()
    {
        // 게임 시작 시 첫 배경 스케일 조절
        ScaleBackground();
        Debug.Log("BackgroundScaler: 초기 배경 스케일 조절 완료.", this.gameObject);
    }

    // 배경 스케일을 조절하는 public 함수 (StoryManager에서 호출하기 위함)
    public void ScaleBackground()
    {
        // SpriteRenderer나 Sprite가 할당되지 않았으면 함수 종료
        if (sr == null || sr.sprite == null)
        {
            Debug.LogWarning("BackgroundScaler Warning: SpriteRenderer 또는 Sprite가 null이므로 스케일 조절 불가.", this.gameObject);
            return;
        }

        // 메인 카메라가 없으면 함수 종료
        if (Camera.main == null)
        {
             Debug.LogError("BackgroundScaler Error: 씬에 Main Camera가 없거나 'MainCamera' 태그가 지정되지 않았습니다.");
             return;
        }

        // 카메라가 Orthographic 타입인지 확인 (Perspective면 로직 변경 필요)
        if (!Camera.main.orthographic)
        {
            Debug.LogWarning("BackgroundScaler Warning: 메인 카메라가 Orthographic 모드가 아닙니다. 스케일링이 정확하지 않을 수 있습니다.", Camera.main.gameObject);
            // 필요시 Perspective 카메라용 스케일링 로직 추가
        }

        // 카메라의 Orthographic 크기를 기반으로 월드 높이/너비 계산
        float cameraHeight = Camera.main.orthographicSize * 2f;
        float cameraWidth = cameraHeight * Camera.main.aspect; // 카메라 종횡비 고려

        // 현재 스프라이트의 원본 크기 (테두리 포함)
        Vector2 spriteSize = sr.sprite.bounds.size;

        // 스프라이트 크기가 0이면 오류 방지 (간혹 임포트 문제로 발생)
        if (spriteSize.x == 0 || spriteSize.y == 0)
        {
            Debug.LogError($"BackgroundScaler Error: Sprite '{sr.sprite.name}'의 bounds 크기가 0입니다. 스케일 조절 불가.", this.gameObject);
            return;
        }

        // 카메라 크기에 맞추기 위한 스케일 계산
        Vector3 newScale = transform.localScale; // Z 스케일은 유지하기 위해 기존 값 사용
        newScale.x = cameraWidth / spriteSize.x;
        newScale.y = cameraHeight / spriteSize.y;

        // 계산된 스케일 적용
        transform.localScale = newScale;

        Debug.Log($"BackgroundScaler: 배경 '{sr.sprite.name}' 스케일 조절됨. New Scale: {newScale}", this.gameObject);
    }
}