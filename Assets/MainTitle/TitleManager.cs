// TitleManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class TitleManager : MonoBehaviour
{
    public GameObject newGameButton;
    public GameObject stageSelectButton;
    public GameObject animalBookButton;
    public GameObject settingsButton; // 설정 버튼 GameObject
    public GameObject settingsPanel;  // 설정 UI 패널 GameObject (BGM on/off 등의 UI 포함)
    public Image logoImage;
    public float logoFadeInDuration = 2f;

    [Header("과일 롤 애니메이션 설정")]
    public GameObject[] fruitPrefabs; // ★★★ 이제 이 배열은 FruitPrefabChance와 역할이 통합됩니다. 아래 fruitPrefabsWithChanceForRoll 사용 ★★★
    public List<FruitPrefabChanceForRoll> fruitPrefabsWithChanceForRoll; // ★★★ 새로 추가: 롤 애니메이션용 과일 프리팹, 최소개수, 가중치
    public int totalFruitsInRollAnimation = 10; // 타이틀 롤 애니메이션에 나올 총 과일 개수
    public Transform fruitRollStartPositionLeft;
    public Transform fruitRollStartPositionRight;
    public float fruitRollSpeed = 5f;
    public float fruitSpawnInterval = 0.2f; // 과일 생성 간격
    public float fruitRollDelay = 0.5f;

    public string newGameSceneName = "StoryScene"; // 프롤로그 또는 첫 대화 씬
    public string dialogueSceneName = "ShopScene"; // 대화 씬 이름

    // --- 설정 패널 내부 UI 요소들 ---
    public UnityEngine.UI.Toggle bgmToggle; // BGM 켜고 끄는 토글 (UnityEngine.UI.Toggle로 명시)
    public UnityEngine.UI.Toggle sfxToggle; // 효과음 켜고 끄는 토글 (UnityEngine.UI.Toggle로 명시)
    // public Button closeSettingsButton; // 닫기 버튼은 기존 ToggleSettingsPanel 함수를 재활용하거나 아래에 새 함수를 만들 수 있습니다.

    private string savedSceneKey = "LastPlayedScene";
    private Color initialLogoColor;

    [Header("스테이지 선택 UI")]
    public GameObject stageSelectPanel_UI;        // 스테이지 선택 패널
    public Button stageButtonPrefab_UI;           // 각 스테이지를 나타낼 버튼 프리팹
    public Transform stageButtonContainer_UI;    // 스테이지 버튼들이 배치될 부모 Transform (GridLayoutGroup 권장)
    public Sprite lockedStageSprite;            // 아직 해금되지 않은 스테이지 표시용 스프라이트 (선택)

    // private string savedSceneKey = "LastPlayedScene"; // 이전 방식, 이제 StageDataManager 사용
    private const string BGM_KEY = "BGMOn";
    private const string SFX_KEY = "SFXOn";

        // ★★★ 과일 롤 애니메이션을 위한 새로운 구조체 ★★★
    [System.Serializable]
    public struct FruitPrefabChanceForRoll
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
            // 시작 시 로고 알파값을 1로 설정하여 즉시 보이도록 (애니메이션은 버튼 클릭 시)
            logoImage.color = new Color(initialLogoColor.r, initialLogoColor.g, initialLogoColor.b, 1f);
        }
        else
        {
            Debug.LogError("TitleManager Error: 로고 이미지가 할당되지 않았습니다!");
        }

        // 버튼 초기 활성화 상태
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
        // 1. 모든 진행 상황 초기화
        if (StageDataManager.Instance != null)
        {
            StageDataManager.Instance.ResetAllStageProgress();
            Debug.Log("스테이지 진행 상황 초기화 완료.");
        }
        else
        {
            Debug.LogError("StageDataManager 인스턴스가 없어 새로하기 시 진행 상황 초기화 불가!");
        }
        // PlayerPrefs.DeleteKey("CurrentHearts"); // 필요하다면 하트 초기화 (HeartManager에서 담당 가능)

        // 2. 첫 번째 손님(튜토리얼)으로 설정
        GameInfoHolder.CustomerIndexToLoad = 0;
        Debug.Log("GameInfoHolder.CustomerIndexToLoad를 0으로 설정.");

        // 3. 타이틀 화면 버튼들 비활성화
        if (newGameButton != null) newGameButton.SetActive(false);
        if (stageSelectButton != null) stageSelectButton.SetActive(false);
        if (animalBookButton != null) animalBookButton.SetActive(false);
        if (settingsButton != null) settingsButton.SetActive(false);
        Debug.Log("타이틀 버튼 비활성화됨.");

        // 4. 기존 애니메이션 코루틴 호출 (이 코루틴 마지막에 씬 전환)
        Debug.Log("StartNewGame 호출됨 - 애니메이션 시작");
        StartCoroutine(TitleAnimationAndSceneLoad(newGameSceneName));
    }

        // 기존 애니메이션 코루틴에 씬 로드 기능 통합
     IEnumerator TitleAnimationAndSceneLoad(string sceneToLoadAfterAnimation)
    {
        Debug.Log("TitleAnimationAndSceneLoad 코루틴 시작. 로드할 씬: " + sceneToLoadAfterAnimation);

        // 1. 버튼 비활성화 (이 코루틴 시작 시 바로 하는 것이 좋을 수 있음)
        if (newGameButton != null) newGameButton.SetActive(false);
        if (stageSelectButton != null) stageSelectButton.SetActive(false);
        if (animalBookButton != null) animalBookButton.SetActive(false);
        if (settingsButton != null) settingsButton.SetActive(false);

        // 2. 로고 페이드 아웃 (선택적, 현재 코드에서는 비활성화된 상태에서 시작)
        if (logoImage != null && logoImage.gameObject.activeInHierarchy)
        {
            float fadeOutDuration = logoFadeInDuration * 0.3f;
            Color startColor = logoImage.color;
            Color endFadeOutColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
            float timer = 0f;
            while (timer < fadeOutDuration)
            {
                logoImage.color = Color.Lerp(startColor, endFadeOutColor, timer / fadeOutDuration);
                timer += Time.deltaTime;
                yield return null;
            }
            logoImage.color = endFadeOutColor;
            logoImage.gameObject.SetActive(false);
        }

        // 3. 과일 롤 애니메이션
        yield return StartCoroutine(RollFruitsAnimation()); // 별도 코루틴으로 분리

        // 4. 모든 애니메이션 후 씬 전환
        Debug.Log(sceneToLoadAfterAnimation + " 씬으로 전환합니다.");
        SceneManager.LoadScene(sceneToLoadAfterAnimation);
    }

    IEnumerator RollFruitsAnimation()
    {
        if (fruitPrefabsWithChanceForRoll == null || fruitPrefabsWithChanceForRoll.Count == 0 || fruitRollStartPositionLeft == null || fruitRollStartPositionRight == null)
        {
            Debug.LogWarning("과일 롤 애니메이션에 필요한 설정이 부족하여 건너<0xEB><0xA9><0xB5>니다.");
            yield return new WaitForSeconds(1f); // 최소한의 딜레이
            yield break; // 코루틴 종료
        }

        Debug.Log("과일 롤 애니메이션 시작.");
        List<GameObject> fruitsToRoll = GenerateFruitListForRoll();
        if (fruitsToRoll.Count == 0) {
            Debug.LogWarning("롤 애니메이션에 생성할 과일이 없습니다.");
            yield break;
        }

        // Y축 위치는 기존처럼 고정된 배열 또는 다른 방식으로 결정 가능
        // 여기서는 totalFruitsInRollAnimation 개수만큼 Y 위치를 분배한다고 가정 (또는 고정된 Y 배열 사용)
        float yRange = 8f; // 예시: 과일이 나타날 전체 Y축 범위 (-4f ~ 4f)
        float yStep = yRange / Mathf.Max(1, fruitsToRoll.Count -1); // 과일이 하나일 경우 DivByZero 방지


        List<GameObject> generatedRolledFruits = new List<GameObject>();

        for (int i = 0; i < fruitsToRoll.Count; i++)
        {
            GameObject fruitPrefabToRoll = fruitsToRoll[i]; // 미리 준비된 리스트에서 가져옴
            Vector3 spawnPos;
            Vector2 rollDirection;
            float currentY = (yRange / 2f) - (i * yStep); // 위에서부터 아래로 균등 분배 (예시)

            if (i % 2 == 0) // 왼쪽에서 오른쪽으로
            {
                spawnPos = new Vector3(fruitRollStartPositionLeft.position.x, currentY, fruitRollStartPositionLeft.position.z);
                rollDirection = Vector2.right;
            }
            else // 오른쪽에서 왼쪽으로
            {
                spawnPos = new Vector3(fruitRollStartPositionRight.position.x, currentY, fruitRollStartPositionRight.position.z);
                rollDirection = Vector2.left;
            }

            GameObject rolledFruit = Instantiate(fruitPrefabToRoll, spawnPos, Quaternion.identity);
            Rigidbody2D rb = rolledFruit.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.gravityScale = 0;
                rb.linearVelocity = rollDirection * fruitRollSpeed;
            }
            AddTrailToFruit(rolledFruit);
            generatedRolledFruits.Add(rolledFruit);
            yield return new WaitForSeconds(fruitSpawnInterval);
        }

        foreach (GameObject fruit in generatedRolledFruits)
        {
            if (fruit != null) Destroy(fruit, 7f); // 애니메이션 시간보다 길게 설정
        }
        Debug.Log("과일 롤 애니메이션 완료.");
        yield return new WaitForSeconds(3f); // 과일들이 사라질 시간 충분히 확보
    }

    List<GameObject> GenerateFruitListForRoll()
    {
        List<GameObject> fruitList = new List<GameObject>();
        Dictionary<GameObject, int> currentCounts = new Dictionary<GameObject, int>();

        // 1. 각 과일의 최소 개수만큼 먼저 추가
        foreach (var fruitInfo in fruitPrefabsWithChanceForRoll)
        {
            for (int i = 0; i < fruitInfo.minCount; i++)
            {
                if (fruitList.Count < totalFruitsInRollAnimation)
                {
                    fruitList.Add(fruitInfo.prefab);
                    if (currentCounts.ContainsKey(fruitInfo.prefab))
                        currentCounts[fruitInfo.prefab]++;
                    else
                        currentCounts.Add(fruitInfo.prefab, 1);
                }
                else break; // 총 개수 초과 시 중단
            }
            if (fruitList.Count >= totalFruitsInRollAnimation) break;
        }

        // 2. 남은 슬롯을 확률에 따라 채우기
        while (fruitList.Count < totalFruitsInRollAnimation)
        {
            float totalWeight = 0f;
            List<KeyValuePair<GameObject, float>> weightedList = new List<KeyValuePair<GameObject, float>>();

            foreach (var fruitInfo in fruitPrefabsWithChanceForRoll)
            {
                // 이미 최소 개수를 채운 과일이라도 확률적으로 더 나올 수 있음
                totalWeight += fruitInfo.chanceWeight;
                weightedList.Add(new KeyValuePair<GameObject, float>(fruitInfo.prefab, fruitInfo.chanceWeight));
            }

            if (totalWeight == 0) break; // 더 이상 추가할 과일이 없거나 확률이 0이면 중단

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
                if (currentCounts.ContainsKey(selectedPrefab))
                    currentCounts[selectedPrefab]++;
                else
                    currentCounts.Add(selectedPrefab, 1);
            }
            else // 만약의 경우 (모든 가중치가 0이거나 할 때)
            {
                 if(fruitPrefabsWithChanceForRoll.Count > 0)
                    fruitList.Add(fruitPrefabsWithChanceForRoll[Random.Range(0, fruitPrefabsWithChanceForRoll.Count)].prefab);
                 else
                    break; // 프리팹 정보도 없으면 중단
            }
        }

        // 3. 리스트 섞기 (Fisher-Yates shuffle)
        for (int i = 0; i < fruitList.Count; i++)
        {
            GameObject temp = fruitList[i];
            int randomIndex = Random.Range(i, fruitList.Count);
            fruitList[i] = fruitList[randomIndex];
            fruitList[randomIndex] = temp;
        }
        
        Debug.Log("생성될 과일 롤 목록 (" + fruitList.Count + "개):");
        foreach(var fruitCount in currentCounts){
            Debug.Log("- " + fruitCount.Key.name + ": " + fruitCount.Value + "개");
        }

        return fruitList;
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

    // "스테이지 선택" 버튼 클릭 시 호출될 함수
    public void OpenStageSelectPanel()
    {
        if (stageSelectPanel_UI != null)
        {
            stageSelectPanel_UI.SetActive(true);
            PopulateStageButtons(); // 스테이지 버튼들 생성 및 업데이트
        }
        else
        {
            Debug.LogError("StageSelectPanel_UI가 연결되지 않았습니다!");
        }
    }

    public void CloseStageSelectPanel()
    {
        if (stageSelectPanel_UI != null)
        {
            stageSelectPanel_UI.SetActive(false); // 패널 비활성화
            Debug.Log("스테이지 선택 패널 닫힘.");
        }
        else
        {
            Debug.LogError("StageSelectPanel_UI가 연결되지 않아 닫을 수 없습니다!");
        }
    }

    void PopulateStageButtons()
    {
        // StageDataManager.Instance 와 CustomerOrderManager.Instance 의 null 체크 강화
        if (stageButtonPrefab_UI == null || stageButtonContainer_UI == null || StageDataManager.Instance == null)
        {
            Debug.LogError("스테이지 버튼 생성에 필요한 기본 요소(버튼 프리팹, 컨테이너, StageDataManager)가 설정되지 않았습니다.");
            return;
        }
        // CustomerOrderManager.Instance는 이 시점에 없을 수 있으므로, 스테이지 개수는 StageDataManager에서 가져옴
        if (CustomerOrderManager.Instance == null && StageDataManager.Instance.totalStages <= 0) {
            Debug.LogError("CustomerOrderManager 인스턴스가 없고, StageDataManager의 totalStages도 설정되지 않아 스테이지 수를 알 수 없습니다.");
            return;
        }


        foreach (Transform child in stageButtonContainer_UI)
        {
            Destroy(child.gameObject);
        }

        // 스테이지 개수는 StageDataManager 또는 CustomerOrderManager에서 가져옴
        int numberOfStages = (CustomerOrderManager.Instance != null && CustomerOrderManager.Instance.allCustomerOrders != null)
                        ? CustomerOrderManager.Instance.allCustomerOrders.Count
                        : StageDataManager.Instance.totalStages;

        if (numberOfStages <= 0)
        {
            Debug.LogWarning("생성할 스테이지가 없습니다 (스테이지 수 0).");
            return;
        }


        for (int i = 0; i < numberOfStages; i++)
        {
            Button stageButtonInstance = Instantiate(stageButtonPrefab_UI, stageButtonContainer_UI);
            stageButtonInstance.name = "StageButton_" + (i + 1);

            TextMeshProUGUI buttonText = stageButtonInstance.GetComponentInChildren<TextMeshProUGUI>();
            Image buttonImage = stageButtonInstance.GetComponent<Image>(); // 버튼 자체의 Image
            // Image_Icon 이라는 이름의 자식 Image가 있다면:
            // Image iconImage = stageButtonInstance.transform.Find("Image_Icon")?.GetComponent<Image>();


            int stageIndex = i;

            // CustomerOrderData를 직접 참조하여 손님 이름과 스프라이트를 가져오려면,
            // CustomerOrderManager가 타이틀 씬에 있거나, 해당 데이터를 다른 방식으로 로드해야 함.
            // 여기서는 StageDataManager의 해금/클리어 여부만 사용하고, 버튼 표시는 단순화.
            CustomerOrderData stageSpecificData = null;
            if (CustomerOrderManager.Instance != null && CustomerOrderManager.Instance.allCustomerOrders.Count > stageIndex)
            {
                stageSpecificData = CustomerOrderManager.Instance.allCustomerOrders[stageIndex];
            }


            if (StageDataManager.Instance.IsStageUnlocked(stageIndex))
            {
                if (buttonText != null)
                {
                    buttonText.text = (stageSpecificData != null) ? stageSpecificData.customerName : "스테이지 " + (stageIndex + 1);
                }
                if (buttonImage != null) // 버튼 배경 이미지
                {
                    if (stageSpecificData != null && stageSpecificData.customerSprite != null) {
                        // buttonImage.sprite = stageSpecificData.customerSprite; // 버튼 자체에 손님 이미지를 넣을 경우
                    }
                    // 클리어 여부에 따라 버튼 모양 변경 가능
                    buttonImage.color = StageDataManager.Instance.IsStageCleared(stageIndex) ? Color.gray : Color.white;
                }
                // if (iconImage != null && stageSpecificData != null && stageSpecificData.customerSprite != null){ // 자식 아이콘 이미지에 설정
                //     iconImage.sprite = stageSpecificData.customerSprite;
                //     iconImage.gameObject.SetActive(true);
                // } else if (iconImage != null){
                //     iconImage.gameObject.SetActive(false);
                // }


                stageButtonInstance.interactable = true;
                stageButtonInstance.onClick.AddListener(() => OnStageButtonClicked(stageIndex));
            }
            else
            {
                if (buttonText != null) buttonText.text = "???";
                if (buttonImage != null) {
                    if (lockedStageSprite != null) buttonImage.sprite = lockedStageSprite;
                    else buttonImage.color = new Color(0.5f, 0.5f, 0.5f, 0.5f); // 잠김 표시 (흐리게)
                }
                // if (iconImage != null) iconImage.gameObject.SetActive(false);
                stageButtonInstance.interactable = false;
            }
        }
    }

        void OnStageButtonClicked(int stageIndex)
    {
        Debug.Log("스테이지 " + stageIndex + " (" + CustomerOrderManager.Instance.allCustomerOrders[stageIndex].customerName + ") 선택됨");
        GameInfoHolder.CustomerIndexToLoad = stageIndex;

        // 대화 씬으로 이동 (또는 바로 게임 씬으로 이동할 수도 있음, 게임 플로우에 따라)
        if (SceneSwitcher.Instance != null)
        {
            // 대화가 있는 캐릭터라면 대화 씬으로, 없다면 바로 게임 씬으로 갈 수도 있음.
            // 여기서는 모든 스테이지가 대화 후 게임으로 이어진다고 가정.
            SceneSwitcher.Instance.LoadDialogueScene(dialogueSceneName);
        }
        else
        {
            SceneManager.LoadScene(dialogueSceneName);
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
        bool bgmOn = PlayerPrefs.GetInt(BGM_KEY, 1) == 1; // 기본값 1 (ON)
        if (bgmToggle != null)
        {
            // 리스너를 임시로 제거하여 OnBgmToggleChanged가 불필요하게 호출되는 것을 방지
            bgmToggle.onValueChanged.RemoveListener(OnBgmToggleChanged);
            bgmToggle.isOn = bgmOn;
            // 리스너를 다시 추가
            bgmToggle.onValueChanged.AddListener(OnBgmToggleChanged);
        }
        ApplyBGMSetting(bgmOn);

        bool sfxOn = PlayerPrefs.GetInt(SFX_KEY, 1) == 1; // 기본값 1 (ON)
        if (sfxToggle != null)
        {
            sfxToggle.onValueChanged.RemoveListener(OnSfxToggleChanged);
            sfxToggle.isOn = sfxOn;
            sfxToggle.onValueChanged.AddListener(OnSfxToggleChanged);
        }
        ApplySFXSetting(sfxOn);
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
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetBgmEnabled(isOn); // AudioManager의 함수 호출
        }
        // Debug.Log("TitleManager -> ApplyBGMSetting 호출됨: " + isOn); // 로그는 AudioManager에서 찍히므로 중복 필요 X
    }

    // 실제 SFX 설정을 오디오 시스템에 적용하는 부분 (예시)
    void ApplySFXSetting(bool isOn)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSfxEnabled(isOn); // AudioManager의 함수 호출
        }
        // Debug.Log("TitleManager -> ApplySFXSetting 호출됨: " + isOn);
    }

}