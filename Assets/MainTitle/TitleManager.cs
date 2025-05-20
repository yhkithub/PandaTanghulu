// TitleManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class TitleManager : MonoBehaviour
{
    [Header("기본 UI 요소")]
    public GameObject newGameButton;
    public GameObject stageSelectButton;
    public GameObject animalBookButton;
    public GameObject settingsButton;
    public GameObject settingsPanel;
    public Image logoImage;
    public float logoFadeDuration = 1f;

    [Header("과일 롤 애니메이션 설정")]
    // public GameObject[] fruitPrefabs; // 이전 방식, 이제 사용 안 함
    public List<FruitPrefabChanceForRoll> fruitPrefabsWithChanceForRoll; // Inspector에서 설정!
    // public int totalFruitsInRollAnimation = 10; // 이제 targetYPositions.Length로 결정
    public Transform fruitRollStartPositionLeft;
    public Transform fruitRollStartPositionRight;
    public float fruitRollSpeed = 5f;
    public float fruitSpawnInterval = 0.3f; // 예전 코드의 0.3f 값으로 복원 시도
    // public float fruitRollDelay = 0.5f; // 명시적으로 사용되지 않음

    // ★★★ 화면을 채우기 위한 Y축 위치 배열 (예전 코드 방식) ★★★
    public float[] targetYPositions = new float[] { 4.5f, 3.5f, 2.5f, 1.5f, 0.5f, -0.5f, -1.5f, -2.5f, -3.5f, -4.5f };

    [Header("과일 롤 트레일 상세 설정")]
    public float trailTime = 8f; // 지속 시간 충분히 길게
    public float fixedTrailWidth = 1.0f; // ★★★ Inspector에서 트레일의 고정 Y축 너비 설정 (예: 1.0f) ★★★
        

    [Header("씬 이름 설정")]
    public string prologueSceneName = "StoryScene";
    public string dialogueSceneName = "DialogueScene";

    [Header("스테이지 선택 UI")]
    public GameObject stageSelectPanel_UI;
    public Button stageButtonPrefab_UI;
    public Transform stageButtonContainer_UI;
    public Sprite lockedStageSprite;

    [Header("오디오 설정 UI")]
    public Toggle bgmToggle;
    public Toggle sfxToggle;

    [Header("스테이지 정보 (CustomerOrderData 에셋들)")]
    public List<CustomerOrderData> customerOrderDataListForTitle;


    private Color initialLogoColor;
    private const string BGM_KEY = "BGMOn";
    private const string SFX_KEY = "SFXOn";

    [System.Serializable]
    public struct FruitPrefabChanceForRoll // 이 구조체는 minCount와 chanceWeight를 가집니다.
    {
        public GameObject prefab;
        public int minCount;     // 이 과일이 롤 애니메이션에 나올 최소 개수
        public float chanceWeight; // 최소 개수 충족 후, 나머지 슬롯을 채울 때의 등장 가중치
    }

    void Start()
    {
        if (logoImage != null)
        {
            logoImage.gameObject.SetActive(true);
            initialLogoColor = logoImage.color;
            logoImage.color = new Color(initialLogoColor.r, initialLogoColor.g, initialLogoColor.b, 1f);
        }
        else { Debug.LogError("TitleManager: 로고 이미지가 할당되지 않았습니다!"); }

        if (newGameButton != null) newGameButton.SetActive(true);
        if (stageSelectButton != null) stageSelectButton.SetActive(true);
        if (animalBookButton != null) animalBookButton.SetActive(true);
        if (settingsButton != null) settingsButton.SetActive(true);

        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (stageSelectPanel_UI != null) stageSelectPanel_UI.SetActive(false);

        LoadAudioSettings();
    }

    public void StartNewGame()
    {
        Debug.Log("StartNewGame 호출됨");
        if (StageDataManager.Instance != null)
        {
            StageDataManager.Instance.ResetAllStageProgress();
            Debug.Log("스테이지 진행 상황 초기화 완료.");
        }
        else { Debug.LogError("StageDataManager 인스턴스가 없어 새로하기 시 진행 상황 초기화 불가!"); }

        GameInfoHolder.CustomerIndexToLoad = 0;
        Debug.Log("GameInfoHolder.CustomerIndexToLoad를 0으로 설정.");

        if (newGameButton != null) newGameButton.SetActive(false);
        if (stageSelectButton != null) stageSelectButton.SetActive(false);
        if (animalBookButton != null) animalBookButton.SetActive(false);
        if (settingsButton != null) settingsButton.SetActive(false);
        Debug.Log("타이틀 버튼 비활성화됨.");

        StartCoroutine(TitleAnimationAndSceneLoad(prologueSceneName));
    }

    IEnumerator TitleAnimationAndSceneLoad(string sceneToLoadAfterAnimation)
    {
        Debug.Log("TitleAnimationAndSceneLoad 코루틴 시작. 로드할 씬: " + sceneToLoadAfterAnimation);

        // --- 1. 로고가 현재 위치에서 페이드 아웃 ---
        if (logoImage != null && logoImage.gameObject.activeSelf)
        {
            float currentAlpha = logoImage.color.a; // 현재 알파값 가져오기
            float timer = 0f;
            Debug.Log("로고 현재 위치에서 페이드 아웃 시작.");
            while (timer < logoFadeDuration) // logoFadeDuration은 Inspector에서 설정한 페이드 시간
            {
                // 현재 색상의 RGB는 유지하고 알파만 변경
                logoImage.color = new Color(logoImage.color.r, logoImage.color.g, logoImage.color.b, Mathf.Lerp(currentAlpha, 0f, timer / logoFadeDuration));
                timer += Time.deltaTime;
                yield return null;
            }
            logoImage.color = new Color(logoImage.color.r, logoImage.color.g, logoImage.color.b, 0f); // 확실하게 알파 0으로
            logoImage.gameObject.SetActive(false); // 일단 비활성화 (위치 변경 후 다시 활성화)
            Debug.Log("로고 현재 위치에서 페이드 아웃 완료.");
        }
        else
        {
            Debug.LogWarning("로고 이미지가 없거나 이미 비활성화되어 있어 페이드 아웃을 건너뜁니다.");
        }

        // --- 2. 로고를 Canvas 중앙으로 이동시키고 투명하게 설정 ---
        if (logoImage != null)
        {
            RectTransform logoRectTransform = logoImage.GetComponent<RectTransform>();
            if (logoRectTransform != null)
            {
                logoRectTransform.anchoredPosition = Vector2.zero; // Canvas 중앙 (앵커/피벗이 중앙으로 설정되어 있다고 가정)
                Debug.Log("로고를 Canvas 중앙으로 이동시킴.");
            }
            else
            {
                Debug.LogWarning("로고에 RectTransform이 없어 중앙으로 이동시킬 수 없습니다.");
            }
            // initialLogoColor는 Start()에서 이미 원본 색상을 저장해두었으므로, 알파만 0으로 설정
            logoImage.color = new Color(initialLogoColor.r, initialLogoColor.g, initialLogoColor.b, 0f);
            logoImage.gameObject.SetActive(true); // 페이드 인을 위해 다시 활성화
        }

        // --- 3. 로고가 Canvas 중앙에서 페이드 인 ---
        if (logoImage != null)
        {
            float timer = 0f;
            Debug.Log("로고 중앙에서 페이드 인 시작.");
            while (timer < logoFadeDuration)
            {
                // initialLogoColor의 RGB 값과 계산된 알파 값을 사용
                logoImage.color = new Color(initialLogoColor.r, initialLogoColor.g, initialLogoColor.b, Mathf.Lerp(0f, initialLogoColor.a, timer / logoFadeDuration));
                timer += Time.deltaTime;
                yield return null;
            }
            logoImage.color = initialLogoColor; // 원래 색상(알파 포함)으로 복원
            Debug.Log("로고 중앙에서 페이드 인 완료.");
        }

        yield return new WaitForSeconds(0.5f); // 로고가 잠시 중앙에 표시될 시간 (조절 가능)

        // --- 4. (선택 사항) 중앙 로고 다시 페이드 아웃 후 과일 롤 ---
        // 만약 중앙에 나타난 로고가 과일 롤 전에 다시 사라지길 원한다면 이 부분을 활성화합니다.
        /*
        if (logoImage != null && logoImage.gameObject.activeSelf)
        {
            float timer = 0f;
            Color currentColor = logoImage.color;
            Debug.Log("중앙 로고 페이드 아웃 시작.");
            while (timer < logoFadeDuration)
            {
                logoImage.color = new Color(currentColor.r, currentColor.g, currentColor.b, Mathf.Lerp(currentColor.a, 0f, timer / logoFadeDuration));
                timer += Time.deltaTime;
                yield return null;
            }
            logoImage.color = new Color(initialLogoColor.r, initialLogoColor.g, initialLogoColor.b, 0f);
            logoImage.gameObject.SetActive(false);
            Debug.Log("중앙 로고 페이드 아웃 완료.");
        }
        */

        // --- 5. 과일 롤 애니메이션 ---
        yield return StartCoroutine(RollFruitsAnimation());

        // --- 6. 씬 전환 ---
        Debug.Log(sceneToLoadAfterAnimation + " 씬으로 전환합니다.");
        SceneManager.LoadScene(sceneToLoadAfterAnimation);
    }

    IEnumerator RollFruitsAnimation()
    {
        if (fruitPrefabsWithChanceForRoll == null || fruitPrefabsWithChanceForRoll.Count == 0 ||
            fruitRollStartPositionLeft == null || fruitRollStartPositionRight == null || targetYPositions.Length == 0) // targetYPositions도 확인
        {
            Debug.LogError("과일 롤 애니메이션에 필요한 설정이 부족합니다! Inspector에서 다음을 확인하세요:\n" +
                           "- Fruit Prefabs With Chance For Roll 리스트 (하나 이상 항목 필요)\n" +
                           "- Target Y Positions 배열 (하나 이상 항목 필요)\n" +
                           "- Fruit Roll Start Position Left\n" +
                           "- Fruit Roll Start Position Right");
            yield return new WaitForSeconds(1f);
            yield break;
        }

        Debug.Log("과일 롤 애니메이션 시작.");
        // targetYPositions.Length를 기준으로 과일 목록 생성
        List<GameObject> fruitsToRoll = GenerateFruitListForRoll(targetYPositions.Length);

        if (fruitsToRoll.Count == 0) {
            Debug.LogWarning("롤 애니메이션에 생성할 과일이 없습니다. (GenerateFruitListForRoll 결과가 비어있음)");
            yield return new WaitForSeconds(1f);
            yield break;
        }
        if (fruitsToRoll.Count != targetYPositions.Length)
        {
             Debug.LogWarning("생성된 과일 수(" + fruitsToRoll.Count + ")와 targetYPositions 개수(" + targetYPositions.Length + ")가 다릅니다. Y 위치가 정확하지 않을 수 있습니다.");
        }


        List<GameObject> generatedRolledFruits = new List<GameObject>();

        for (int i = 0; i < targetYPositions.Length; i++)
        {
            if (i >= fruitsToRoll.Count) break;

            GameObject fruitPrefabToRoll = fruitsToRoll[i];
            Vector3 spawnPos;
            Vector2 rollDirection;
            float currentY = targetYPositions[i];// ★★★ 지정된 Y 위치 사용 ★★★

            if (i % 2 == 0)
            {
                spawnPos = new Vector3(fruitRollStartPositionLeft.position.x, currentY, fruitRollStartPositionLeft.position.z);
                rollDirection = Vector2.right;
            }
            else
            {
                spawnPos = new Vector3(fruitRollStartPositionRight.position.x, currentY, fruitRollStartPositionRight.position.z);
                rollDirection = Vector2.left;
            }

            GameObject rolledFruit = Instantiate(fruitPrefabToRoll, spawnPos, Quaternion.identity);
            Rigidbody2D rb = rolledFruit.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 0;
                rb.linearVelocity = rollDirection * fruitRollSpeed;
            }
            AddTrailToFruit(rolledFruit); // 트레일 추가
            generatedRolledFruits.Add(rolledFruit);
            yield return new WaitForSeconds(fruitSpawnInterval); // 이전 코드의 0.3f 또는 Inspector 값 사용
        }

        float screenWorldWidth = Camera.main.aspect * Camera.main.orthographicSize * 2;
        float longestRollTime = (screenWorldWidth / fruitRollSpeed) + 2f; // 화면 전체를 가로지르는 시간 + 여유
        if (fruitRollSpeed <= 0) longestRollTime = 5f; // 속도가 0이거나 음수일 때 기본 대기 시간

        
        yield return new WaitForSeconds(longestRollTime);

    }

    List<GameObject> GenerateFruitListForRoll(int totalFruitsToGenerate)
    {
        List<GameObject> fruitList = new List<GameObject>();
        if (fruitPrefabsWithChanceForRoll == null || fruitPrefabsWithChanceForRoll.Count == 0)
        {
            Debug.LogError("GenerateFruitListForRoll: fruitPrefabsWithChanceForRoll 리스트가 비어있습니다!");
            return fruitList;
        }

        Dictionary<GameObject, int> currentCounts = new Dictionary<GameObject, int>();

        // 1. 각 과일의 최소 개수만큼 먼저 추가
        foreach (var fruitInfo in fruitPrefabsWithChanceForRoll)
        {
            if (fruitInfo.prefab == null) continue;
            for (int i = 0; i < fruitInfo.minCount; i++)
            {
                if (fruitList.Count < totalFruitsToGenerate)
                {
                    fruitList.Add(fruitInfo.prefab);
                    if (currentCounts.ContainsKey(fruitInfo.prefab)) currentCounts[fruitInfo.prefab]++;
                    else currentCounts.Add(fruitInfo.prefab, 1);
                }
                else break;
            }
            if (fruitList.Count >= totalFruitsToGenerate) break;
        }

        // 2. 남은 슬롯을 확률에 따라 채우기
        while (fruitList.Count < totalFruitsToGenerate)
        {
            // ... (이전 답변의 확률 기반 채우기 로직은 동일하게 유지) ...
            float totalWeight = 0f;
            List<KeyValuePair<GameObject, float>> weightedList = new List<KeyValuePair<GameObject, float>>();

            foreach (var fruitInfo in fruitPrefabsWithChanceForRoll)
            {
                if (fruitInfo.prefab == null) continue;
                totalWeight += fruitInfo.chanceWeight;
                weightedList.Add(new KeyValuePair<GameObject, float>(fruitInfo.prefab, fruitInfo.chanceWeight));
            }

            if (totalWeight <= 0 || weightedList.Count == 0) {
                 Debug.LogWarning("더 이상 추가할 과일이 없거나 모든 과일의 가중치가 0입니다. (남은 슬롯 채우기 중단)");
                 // 남은 슬롯이 있다면, 가장 첫번째 유효한 프리팹으로 채우거나 다른 정책 사용
                 if (fruitList.Count < totalFruitsToGenerate && fruitPrefabsWithChanceForRoll.Any(f => f.prefab != null)) {
                     fruitList.Add(fruitPrefabsWithChanceForRoll.First(f => f.prefab != null).prefab);
                     continue;
                 }
                 break;
            }

            float randomPoint = Random.Range(0, totalWeight);
            float currentCumulativeWeight = 0f;
            GameObject selectedPrefab = null;

            foreach (var item in weightedList)
            {
                currentCumulativeWeight += item.Value;
                if (randomPoint < currentCumulativeWeight)
                {
                    selectedPrefab = item.Key;
                    break;
                }
            }
            
            if (selectedPrefab != null)
            {
                fruitList.Add(selectedPrefab);
            }
            else if (weightedList.Count > 0) // 만약 위 로직에서 선택이 안된 경우 (매우 드묾)
            {
                 fruitList.Add(weightedList[Random.Range(0, weightedList.Count)].Key);
            } else { // 추가할 후보가 아예 없으면 종료
                break;
            }
        }

        // 3. 리스트 섞기
        for (int i = 0; i < fruitList.Count; i++)
        {
            GameObject temp = fruitList[i];
            int randomIndex = Random.Range(i, fruitList.Count);
            fruitList[i] = fruitList[randomIndex];
            fruitList[randomIndex] = temp;
        }
        
        Debug.Log("생성될 과일 롤 목록 (" + fruitList.Count + "개)");
        // currentCounts 로깅은 필요시 다시 활성화

        return fruitList;
    }

    void AddTrailToFruit(GameObject fruitObject)
    {
        TrailRenderer trailRenderer = fruitObject.GetComponent<TrailRenderer>();
        if (trailRenderer == null)
        {
            trailRenderer = fruitObject.AddComponent<TrailRenderer>();
        }

        // --- 트레일 너비 결정 ---
        float actualTrailWidth = fixedTrailWidth; // Inspector에서 설정한 고정 너비를 사용

        trailRenderer.time = trailTime;           // Inspector에서 설정한 지속 시간 사용
        trailRenderer.startWidth = actualTrailWidth; // 계산되거나 설정된 너비 사용
        trailRenderer.endWidth = actualTrailWidth;   // 끝 너비도 동일하게 하여 직선 유지
        trailRenderer.minVertexDistance = 0.01f;  // 부드러운 직선을 위해 작은 값
        trailRenderer.alignment = LineAlignment.View; // 또는 TransformZ

        // 2. 머티리얼 설정: 불투명한 단색 표현을 위해
        // Sprites/Default 쉐이더는 기본적으로 알파 블렌딩을 사용하지 않음 (Cutout이면 가능)
        // 완전 불투명을 원하면 Unlit/Color 쉐이더가 더 적합할 수 있으나, Sprites/Default도 색상만 사용하면 됨.
        if (trailRenderer.material == null || !trailRenderer.material.shader.name.Equals("Sprites/Default"))
        {
            Shader shaderToUse = Shader.Find("Sprites/Default");
            if (shaderToUse == null) shaderToUse = Shader.Find("Unlit/Color"); // 대체 쉐이더
            
            if (shaderToUse != null)
            {
                trailRenderer.material = new Material(shaderToUse);
            }
            else
            {
                Debug.LogError(fruitObject.name + ": 트레일용 기본 쉐이더를 찾을 수 없습니다!");
                trailRenderer.emitting = false; // 쉐이더 없으면 트레일 끄기
                return;
            }
        }

        // 3. 색상 설정 (FruitColor 컴포넌트 사용, 완전 불투명)
        Color trailColorToUse = Color.white;
        FruitColor fruitColorComponent = fruitObject.GetComponent<FruitColor>();

        if (fruitColorComponent != null)
        {
            trailColorToUse = fruitColorComponent.trailColor;
        }
        else
        {
            Debug.LogWarning(fruitObject.name + "에 FruitColor 컴포넌트가 없어 트레일 색상을 흰색으로 설정합니다.");
        }

        trailColorToUse.a = 1f; // 완전 불투명
        trailRenderer.startColor = trailColorToUse;
        trailRenderer.endColor = trailColorToUse;

        SpriteRenderer fruitSpriteRenderer = fruitObject.GetComponent<SpriteRenderer>();
        if (fruitSpriteRenderer != null)
        {
            trailRenderer.sortingLayerID = fruitSpriteRenderer.sortingLayerID;
            trailRenderer.sortingOrder = fruitSpriteRenderer.sortingOrder - 1; // 과일보다 뒤
        }
        else
        {
            trailRenderer.sortingOrder = -1;
        }
        trailRenderer.emitting = true;
    }

    public void OpenStageSelectPanel()
    {
        if (stageSelectPanel_UI != null)
        {
            stageSelectPanel_UI.SetActive(true);
            PopulateStageButtons();
        }
        else { Debug.LogError("StageSelectPanel_UI가 연결되지 않았습니다!"); }
    }

    public void CloseStageSelectPanel()
    {
        if (stageSelectPanel_UI != null)
        {
            stageSelectPanel_UI.SetActive(false);
        }
        else { Debug.LogError("StageSelectPanel_UI가 연결되지 않아 닫을 수 없습니다!"); }
    }

    void PopulateStageButtons()
    {
        if (stageButtonPrefab_UI == null || stageButtonContainer_UI == null || StageDataManager.Instance == null)
        {
            Debug.LogError("스테이지 버튼 생성에 필요한 요소가 설정되지 않았습니다: stageButtonPrefab_UI, stageButtonContainer_UI, StageDataManager.Instance");
            return;
        }

        foreach (Transform child in stageButtonContainer_UI)
        {
            Destroy(child.gameObject);
        }

        // ★★★ customerOrderDataListForTitle 사용 ★★★
        int numberOfStages = (customerOrderDataListForTitle != null) ? customerOrderDataListForTitle.Count : StageDataManager.Instance.totalStages;

        if (numberOfStages <= 0)
        {
            Debug.LogWarning("생성할 스테이지가 없습니다. customerOrderDataListForTitle 또는 StageDataManager.totalStages를 확인하세요.");
            return;
        }

        for (int i = 0; i < numberOfStages; i++)
        {
            Button stageButtonInstance = Instantiate(stageButtonPrefab_UI, stageButtonContainer_UI);
            stageButtonInstance.name = "StageButton_" + (i + 1);

            TextMeshProUGUI buttonText = stageButtonInstance.GetComponentInChildren<TextMeshProUGUI>();
            Image buttonImage = stageButtonInstance.GetComponent<Image>();
            // 자식 Image (아이콘용)이 있다면 여기서 찾아 설정
            // Image iconImage = stageButtonInstance.transform.Find("IconImageName")?.GetComponent<Image>(); 

            int stageIndex = i;
            CustomerOrderData stageSpecificData = (customerOrderDataListForTitle != null && customerOrderDataListForTitle.Count > stageIndex) ? customerOrderDataListForTitle[stageIndex] : null;

            if (StageDataManager.Instance.IsStageUnlocked(stageIndex))
            {
                if (buttonText != null)
                {
                    buttonText.text = (stageSpecificData != null && !string.IsNullOrEmpty(stageSpecificData.customerName)) ? stageSpecificData.customerName : "스테이지 " + (stageIndex + 1);
                }
                if (buttonImage != null)
                {
                    buttonImage.color = StageDataManager.Instance.IsStageCleared(stageIndex) ? new Color(0.7f, 0.7f, 0.7f, 1f) : Color.white;
                    // if (iconImage != null && stageSpecificData != null && stageSpecificData.customerSprite != null)
                    // {
                    //     iconImage.sprite = stageSpecificData.customerSprite;
                    //     iconImage.gameObject.SetActive(true);
                    // }
                }
                stageButtonInstance.interactable = true;
                stageButtonInstance.onClick.AddListener(() => OnStageButtonClicked(stageIndex));
            }
            else
            {
                if (buttonText != null) buttonText.text = "???";
                if (buttonImage != null)
                {
                    if (lockedStageSprite != null) buttonImage.sprite = lockedStageSprite;
                    else buttonImage.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
                }
                // if (iconImage != null) iconImage.gameObject.SetActive(false);
                stageButtonInstance.interactable = false;
            }
        }
    }

    void OnStageButtonClicked(int stageIndex)
    {
        string stageName = (customerOrderDataListForTitle != null && customerOrderDataListForTitle.Count > stageIndex && customerOrderDataListForTitle[stageIndex] != null)
                            ? customerOrderDataListForTitle[stageIndex].customerName
                            : "스테이지 " + (stageIndex + 1);
        Debug.Log(stageName + " (인덱스: " + stageIndex + ") 선택됨");
        GameInfoHolder.CustomerIndexToLoad = stageIndex;

        if (SceneSwitcher.Instance != null)
        {
            SceneSwitcher.Instance.LoadDialogueScene(dialogueSceneName);
        }
        else
        {
            SceneManager.LoadScene(dialogueSceneName);
        }
    }

    public void OpenAnimalBook() { /* SceneManager.LoadScene("AnimalBookScene"); */ }
    public void ToggleSettingsPanel()
    {
        if (settingsPanel != null)
        {
            bool isActive = !settingsPanel.activeSelf;
            settingsPanel.SetActive(isActive);
            if (isActive) LoadAudioSettingsToUI();
        }
        else { Debug.LogError("Settings Panel이 할당되지 않았습니다!"); }
    }
    void LoadAudioSettings()
    {
        bool bgmOn = PlayerPrefs.GetInt(BGM_KEY, 1) == 1;
        if (bgmToggle != null)
        {
            bgmToggle.onValueChanged.RemoveAllListeners(); // 리스너 중복 방지
            bgmToggle.isOn = bgmOn;
            bgmToggle.onValueChanged.AddListener(OnBgmToggleChanged);
        }
        ApplyBGMSetting(bgmOn);

        bool sfxOn = PlayerPrefs.GetInt(SFX_KEY, 1) == 1;
        if (sfxToggle != null)
        {
            sfxToggle.onValueChanged.RemoveAllListeners(); // 리스너 중복 방지
            sfxToggle.isOn = sfxOn;
            sfxToggle.onValueChanged.AddListener(OnSfxToggleChanged);
        }
        ApplySFXSetting(sfxOn);
    }
    void LoadAudioSettingsToUI()
    {
        if (bgmToggle != null) bgmToggle.isOn = PlayerPrefs.GetInt(BGM_KEY, 1) == 1;
        if (sfxToggle != null) sfxToggle.isOn = PlayerPrefs.GetInt(SFX_KEY, 1) == 1;
    }
    public void OnBgmToggleChanged(bool isOn)
    {
        PlayerPrefs.SetInt(BGM_KEY, isOn ? 1 : 0);
        PlayerPrefs.Save();
        ApplyBGMSetting(isOn);
    }
    public void OnSfxToggleChanged(bool isOn)
    {
        PlayerPrefs.SetInt(SFX_KEY, isOn ? 1 : 0);
        PlayerPrefs.Save();
        ApplySFXSetting(isOn);
    }
    void ApplyBGMSetting(bool isOn)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SetBgmEnabled(isOn);
    }
    void ApplySFXSetting(bool isOn)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SetSfxEnabled(isOn);
    }
}