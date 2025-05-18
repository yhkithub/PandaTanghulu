// TitleManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class TitleManager : MonoBehaviour
{
    public GameObject newGameButton;
    public GameObject continueGameButton;
    public GameObject animalBookButton;
    public GameObject settingsButton; // 설정 버튼 GameObject
    public GameObject settingsPanel;  // 설정 UI 패널 GameObject (BGM on/off 등의 UI 포함)
    public Image logoImage;
    public float logoFadeInDuration = 2f;
    public float fruitRollDelay = 0.5f;
    public GameObject[] fruitPrefabs; // 여러 종류의 과일 프리팹 배열
    public int[] minFruitCounts; // 각 과일별 최소 생성 개수
    public Transform fruitRollStartPositionLeft;
    public Transform fruitRollStartPositionRight;
    public float fruitRollSpeed = 5f;
    public float minYSpawn = -3f; // 최소 Y 생성 좌표
    public float maxYSpawn = 3f;  // 최대 Y 생성 좌표
    public string newGameSceneName = "StoryScene";
    public string gameSceneToLoad = "";

    // --- 설정 패널 내부 UI 요소들 ---
    public UnityEngine.UI.Toggle bgmToggle; // BGM 켜고 끄는 토글 (UnityEngine.UI.Toggle로 명시)
    public UnityEngine.UI.Toggle sfxToggle; // 효과음 켜고 끄는 토글 (UnityEngine.UI.Toggle로 명시)
    // public Button closeSettingsButton; // 닫기 버튼은 기존 ToggleSettingsPanel 함수를 재활용하거나 아래에 새 함수를 만들 수 있습니다.

    private string savedSceneKey = "LastPlayedScene";
    private Color initialLogoColor;

    // 오디오 설정을 저장할 때 사용할 키 값들
    private const string BGM_KEY = "BGMOn";
    private const string SFX_KEY = "SFXOn";

    void Start()
    {
        if (logoImage != null)
        {
            logoImage.gameObject.SetActive(true);
            initialLogoColor = logoImage.color;
            logoImage.color = new Color(initialLogoColor.r, initialLogoColor.g, initialLogoColor.b, 1f);
        }
        else
        {
            Debug.LogError("TitleManager Error: 로고 이미지가 할당되지 않았습니다!");
        }

        gameSceneToLoad = PlayerPrefs.GetString(savedSceneKey, "");
        if (continueGameButton != null) continueGameButton.SetActive(!string.IsNullOrEmpty(gameSceneToLoad)); // null 체크 추가

        if (newGameButton != null) newGameButton.SetActive(true);
        // continueGameButton은 위에서 이미 처리
        if (animalBookButton != null) animalBookButton.SetActive(true);
        if (settingsButton != null) settingsButton.SetActive(true);

        if (settingsPanel != null) settingsPanel.SetActive(false);

        // 게임 시작 시 저장된 오디오 설정 불러오기
        LoadAudioSettings();
    }

    public void StartNewGame()
    {
        if (newGameButton != null) newGameButton.SetActive(false);
        if (continueGameButton != null) continueGameButton.SetActive(false);
        if (animalBookButton != null) animalBookButton.SetActive(false);
        if (settingsButton != null) settingsButton.SetActive(false);
        StartCoroutine(FadeOutLogoThenFadeInAndRollFruits());
    }

    IEnumerator FadeOutLogoThenFadeInAndRollFruits()
    {
        // ... (기존 로고 및 과일 애니메이션 코드는 동일) ...
        if (logoImage != null)
        {
            float fadeOutDuration = logoFadeInDuration / 2f;
            float fadeInDuration = logoFadeInDuration / 2f;
            Color startColor = logoImage.color;
            Color endFadeOut = new Color(startColor.r, startColor.g, startColor.b, 0f);
            RectTransform logoRectTransform = logoImage.GetComponent<RectTransform>();
            Vector2 originalPosition = logoRectTransform.anchoredPosition;

            float timer = 0f;
            while (timer < fadeOutDuration)
            {
                logoImage.color = Color.Lerp(startColor, endFadeOut, timer / fadeOutDuration);
                timer += Time.deltaTime;
                yield return null;
            }
            logoImage.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.2f);

            if (logoRectTransform != null && GetComponentInParent<Canvas>() != null)
            {
                logoRectTransform.anchoredPosition = Vector2.zero;
            }
            else
            {
                Debug.LogWarning("TitleManager Warning: 로고 RectTransform 또는 Canvas가 없습니다.");
                Vector3 viewportCenter = new Vector3(0.5f, 0.5f, 0f);
                logoImage.transform.position = Camera.main.ViewportToWorldPoint(viewportCenter);
            }
            logoImage.gameObject.SetActive(true);

            timer = 0f;
            Color startFadeIn = new Color(initialLogoColor.r, initialLogoColor.g, initialLogoColor.b, 0f);
            Color endFadeIn = initialLogoColor;
            while (timer < fadeInDuration)
            {
                logoImage.color = Color.Lerp(startFadeIn, endFadeIn, timer / fadeInDuration);
                timer += Time.deltaTime;
                yield return null;
            }
            logoImage.color = endFadeIn;
        }

        if (fruitPrefabs.Length > 0 && fruitRollStartPositionLeft != null && fruitRollStartPositionRight != null)
        {
            float[] targetYPositions = new float[] { 4.5f, 3.5f, 2.5f, 1.5f, 0.5f, -0.5f, -1.5f, -2.5f, -3.5f, -4.5f };
            List<GameObject> generatedFruits = new List<GameObject>();

            for (int i = 0; i < targetYPositions.Length; i++)
            {
                if (i % 2 == 0)
                {
                    Vector3 leftSpawnPosition = new Vector3(fruitRollStartPositionLeft.position.x, targetYPositions[i], fruitRollStartPositionLeft.position.z);
                    GameObject leftFruit = Instantiate(fruitPrefabs[Random.Range(0, fruitPrefabs.Length)], leftSpawnPosition, Quaternion.identity);
                    Rigidbody2D leftRb = leftFruit.GetComponent<Rigidbody2D>();
                    if (leftRb != null) leftRb.linearVelocity = Vector2.right * fruitRollSpeed;
                    AddTrailToFruit(leftFruit);
                    generatedFruits.Add(leftFruit);
                    yield return new WaitForSeconds(0.3f);
                }
                else
                {
                    Vector3 rightSpawnPosition = new Vector3(fruitRollStartPositionRight.position.x, targetYPositions[i], fruitRollStartPositionRight.position.z);
                    GameObject rightFruit = Instantiate(fruitPrefabs[Random.Range(0, fruitPrefabs.Length)], rightSpawnPosition, Quaternion.identity);
                    Rigidbody2D rightRb = rightFruit.GetComponent<Rigidbody2D>();
                    if (rightRb != null) rightRb.linearVelocity = Vector2.left * fruitRollSpeed;
                    AddTrailToFruit(rightFruit);
                    generatedFruits.Add(rightFruit);
                    yield return new WaitForSeconds(0.3f);
                }
            }

            foreach (GameObject fruit in generatedFruits)
            {
                Destroy(fruit, 20f);
            }
        }

        yield return new WaitForSeconds(5f);
        SceneManager.LoadScene(newGameSceneName);
    }


    void AddTrailToFruit(GameObject fruitObject)
    {
        // ... (기존 AddTrailToFruit 코드는 동일) ...
        TrailRenderer trailRenderer = fruitObject.GetComponent<TrailRenderer>();
        FruitColor fruitColorComponent = fruitObject.GetComponent<FruitColor>();

        if (trailRenderer == null)
        {
            trailRenderer = fruitObject.AddComponent<TrailRenderer>();
            trailRenderer.time = 10f;
            trailRenderer.startWidth = 0.1f;
            trailRenderer.endWidth = 0f;
            trailRenderer.material = new Material(Shader.Find("Sprites/Default"));

            if (fruitColorComponent != null)
            {
                trailRenderer.startColor = fruitColorComponent.trailColor;
                trailRenderer.endColor = fruitColorComponent.trailColor;
            }
            else
            {
                trailRenderer.startColor = Color.white;
                trailRenderer.endColor = Color.white;
                Debug.LogWarning("FruitColor 컴포넌트가 없어 흰색 트레일을 사용합니다.", fruitObject);
            }
        }
        else
        {
            if (fruitColorComponent != null)
            {
                trailRenderer.startColor = fruitColorComponent.trailColor;
                trailRenderer.endColor = fruitColorComponent.trailColor;
            }
            trailRenderer.time = 10f;
        }
    }

    public void ContinueGame()
    {
        if (!string.IsNullOrEmpty(gameSceneToLoad))
        {
            SceneManager.LoadScene(gameSceneToLoad);
        }
        else
        {
            Debug.LogWarning("저장된 게임이 없습니다.");
        }
    }

    public void OpenAnimalBook()
    {
        SceneManager.LoadScene("AnimalBookScene");
    }

    // 설정 패널 열기/닫기 (기존 함수)
    public void ToggleSettingsPanel()
    {
        if (settingsPanel != null)
        {
            bool isActive = !settingsPanel.activeSelf;
            settingsPanel.SetActive(isActive);

            // 패널이 열릴 때마다 현재 저장된 설정 값으로 토글 UI 업데이트
            if (isActive)
            {
                LoadAudioSettingsToUI();
            }
        }
        else
        {
            Debug.LogError("Settings Panel이 할당되지 않았습니다!");
        }
    }

    // --- 오디오 설정 관련 함수들 ---
    void LoadAudioSettings()
    {
        // BGM 설정 불러오기 (기본값은 true, 즉 켜짐)
        bool bgmOn = PlayerPrefs.GetInt(BGM_KEY, 1) == 1;
        if (bgmToggle != null) bgmToggle.isOn = bgmOn;
        ApplyBGMSetting(bgmOn); // 실제 오디오 매니저에 적용하는 로직 (아래에 예시)

        // SFX 설정 불러오기 (기본값은 true, 즉 켜짐)
        bool sfxOn = PlayerPrefs.GetInt(SFX_KEY, 1) == 1;
        if (sfxToggle != null) sfxToggle.isOn = sfxOn;
        ApplySFXSetting(sfxOn); // 실제 오디오 매니저에 적용하는 로직 (아래에 예시)
    }

    void LoadAudioSettingsToUI()
    {
        if (bgmToggle != null)
        {
            bgmToggle.isOn = PlayerPrefs.GetInt(BGM_KEY, 1) == 1;
        }
        if (sfxToggle != null)
        {
            sfxToggle.isOn = PlayerPrefs.GetInt(SFX_KEY, 1) == 1;
        }
    }

    // BGM 토글 값 변경 시 호출될 함수
    public void OnBgmToggleChanged(bool isOn)
    {
        PlayerPrefs.SetInt(BGM_KEY, isOn ? 1 : 0);
        PlayerPrefs.Save(); // 변경사항 즉시 저장
        ApplyBGMSetting(isOn);
        Debug.Log("BGM 설정 변경: " + (isOn ? "ON" : "OFF"));
    }

    // SFX 토글 값 변경 시 호출될 함수
    public void OnSfxToggleChanged(bool isOn)
    {
        PlayerPrefs.SetInt(SFX_KEY, isOn ? 1 : 0);
        PlayerPrefs.Save(); // 변경사항 즉시 저장
        ApplySFXSetting(isOn);
        Debug.Log("SFX 설정 변경: " + (isOn ? "ON" : "OFF"));
    }

    // 실제 BGM 설정을 오디오 시스템에 적용하는 부분 (예시)
    void ApplyBGMSetting(bool isOn)
    {
        // 여기에 실제 BGM을 켜고 끄는 코드를 작성합니다.
        // 예: FindObjectOfType<AudioManager>()?.SetBGM(isOn);
        // 지금은 AudioManager가 없으므로, 콘솔에 로그만 남깁니다.
        Debug.Log("ApplyBGMSetting 호출됨: " + isOn);
    }

    // 실제 SFX 설정을 오디오 시스템에 적용하는 부분 (예시)
    void ApplySFXSetting(bool isOn)
    {
        // 여기에 실제 효과음을 켜고 끄는 코드를 작성합니다.
        // 예: FindObjectOfType<AudioManager>()?.SetSFX(isOn);
        // 지금은 AudioManager가 없으므로, 콘솔에 로그만 남깁니다.
        Debug.Log("ApplySFXSetting 호출됨: " + isOn);
    }


    public void SaveLastPlayedScene(string sceneName)
    {
        PlayerPrefs.SetString(savedSceneKey, sceneName);
        PlayerPrefs.Save();
        if (continueGameButton != null) continueGameButton.SetActive(true);
    }
}