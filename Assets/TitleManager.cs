using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class TitleManager : MonoBehaviour
{
    public GameObject newGameButton;
    public GameObject continueGameButton;
    public GameObject animalBookButton;
    public Image logoImage;
    public float logoFadeInDuration = 2f;
    public float fruitRollDelay = 0.5f;
    public GameObject[] fruitPrefabs; // 여러 종류의 과일 프리팹 배열
    public Transform fruitRollStartPositionLeft;
    public Transform fruitRollStartPositionRight;
    public float fruitRollSpeed = 5f;
    public float minYSpawn = -3f; // 최소 Y 생성 좌표
    public float maxYSpawn = 3f;  // 최대 Y 생성 좌표
    public string newGameSceneName = "StoryScene";
    public string gameSceneToLoad = "";

    private string savedSceneKey = "LastPlayedScene";
    private Color initialLogoColor; // 초기 로고 색상 저장

    void Start()
    {
        // 초기 로고 표시
        if (logoImage != null)
        {
            logoImage.gameObject.SetActive(true);
            initialLogoColor = logoImage.color;
            logoImage.color = new Color(initialLogoColor.r, initialLogoColor.g, initialLogoColor.b, 1f); // 처음부터 보이도록 설정
        }
        else
        {
            Debug.LogError("TitleManager Error: 로고 이미지가 할당되지 않았습니다!");
        }

        gameSceneToLoad = PlayerPrefs.GetString(savedSceneKey, "");
        continueGameButton.SetActive(!string.IsNullOrEmpty(gameSceneToLoad));

        if (newGameButton != null) newGameButton.SetActive(true);
        if (continueGameButton != null) continueGameButton.SetActive(true);
        if (animalBookButton != null) animalBookButton.SetActive(true);
    }

    public void StartNewGame()
    {
        // 버튼들 비활성화
        if (newGameButton != null) newGameButton.SetActive(false);
        if (continueGameButton != null) continueGameButton.SetActive(false);
        if (animalBookButton != null) animalBookButton.SetActive(false);

        // 로고 페이드 아웃 후 중앙에서 페이드 인 및 과일 생성
        StartCoroutine(FadeOutLogoThenFadeInAndRollFruits());
    }

    IEnumerator FadeOutLogoThenFadeInAndRollFruits()
    {
        if (logoImage != null)
        {
            float fadeOutDuration = logoFadeInDuration / 2f;
            float fadeInDuration = logoFadeInDuration / 2f;
            Color startColor = logoImage.color;
            Color endFadeOut = new Color(startColor.r, startColor.g, startColor.b, 0f);
            RectTransform logoRectTransform = logoImage.GetComponent<RectTransform>();
            Vector2 originalPosition = logoRectTransform.anchoredPosition; // 원래 위치 저장

            float timer = 0f;
            while (timer < fadeOutDuration)
            {
                logoImage.color = Color.Lerp(startColor, endFadeOut, timer / fadeOutDuration);
                timer += Time.deltaTime;
                yield return null;
            }
            logoImage.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.2f);

            // 로고 위치를 캔버스 중앙으로 설정 (anchoredPosition 사용)
            if (logoRectTransform != null && GetComponentInParent<Canvas>() != null)
            {
                logoRectTransform.anchoredPosition = Vector2.zero; // 캔버스 중앙 (앵커 기준)
            }
            else
            {
                Debug.LogWarning("TitleManager Warning: 로고 RectTransform 또는 Canvas가 없습니다.");
                // fallback: 월드 좌표 중앙 (정확하지 않을 수 있음)
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

        // 과일 단면 생성 및 굴러가는 효과
        if (fruitPrefabs.Length > 0 && fruitRollStartPositionLeft != null && fruitRollStartPositionRight != null)
        {
            int numberOfFruits = 10;
            float totalYRange = maxYSpawn - minYSpawn;

            for (int i = 0; i < numberOfFruits / 2; i++)
            {
                // 왼쪽 과일 생성
                float leftSpawnY = Mathf.Lerp(minYSpawn, maxYSpawn, (float)i / (numberOfFruits / 2 - 1)) + Random.Range(-0.3f, 0.3f); // Y 오프셋 추가
                Vector3 leftSpawnPosition = new Vector3(fruitRollStartPositionLeft.position.x, leftSpawnY, fruitRollStartPositionLeft.position.z);
                GameObject leftFruit = Instantiate(fruitPrefabs[Random.Range(0, fruitPrefabs.Length)], leftSpawnPosition, Quaternion.identity);
                Rigidbody2D leftRb = leftFruit.GetComponent<Rigidbody2D>();
                if (leftRb != null)
                {
                    leftRb.velocity = Vector2.right * fruitRollSpeed;
                }
                AddTrailToFruit(leftFruit);
                Destroy(leftFruit, 20f);
                yield return new WaitForSeconds(0.4f);

                // 오른쪽 과일 생성
                float rightSpawnY = Mathf.Lerp(minYSpawn, maxYSpawn, (float)i / (numberOfFruits / 2 - 1)) + Random.Range(-0.3f, 0.3f); // Y 오프셋 추가
                Vector3 rightSpawnPosition = new Vector3(fruitRollStartPositionRight.position.x, rightSpawnY, rightSpawnPosition.z);
                GameObject rightFruit = Instantiate(fruitPrefabs[Random.Range(0, fruitPrefabs.Length)], rightSpawnPosition, Quaternion.identity);
                Rigidbody2D rightRb = rightFruit.GetComponent<Rigidbody2D>();
                if (rightRb != null)
                {
                    rightRb.velocity = Vector2.left * fruitRollSpeed;
                }
                AddTrailToFruit(rightFruit);
                Destroy(rightFruit, 20f);
                yield return new WaitForSeconds(0.4f);
            }
        }
        else
        {
            Debug.LogWarning("TitleManager Warning: 과일 프리팹 배열이 비어 있거나 시작 위치가 할당되지 않았습니다.");
        }

        yield return new WaitForSeconds(5f); // 전환 대기 시간 (조정 필요)
        SceneManager.LoadScene(newGameSceneName);
    }

    void AddTrailToFruit(GameObject fruitObject)
    {
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
                trailRenderer.endColor = fruitColorComponent.trailColor; // End Color도 불투명하게
            }
            else
            {
                trailRenderer.startColor = Color.white;
                trailRenderer.endColor = Color.white; // End Color도 불투명하게
                Debug.LogWarning("FruitColor 컴포넌트가 없어 흰색 트레일을 사용합니다.", fruitObject);
            }
        }
        else
        {
            if (fruitColorComponent != null)
            {
                trailRenderer.startColor = fruitColorComponent.trailColor;
                trailRenderer.endColor = fruitColorComponent.trailColor; // End Color도 불투명하게
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

    public void SaveLastPlayedScene(string sceneName)
    {
        PlayerPrefs.SetString(savedSceneKey, sceneName);
        PlayerPrefs.Save();
        continueGameButton.SetActive(true);
    }
}