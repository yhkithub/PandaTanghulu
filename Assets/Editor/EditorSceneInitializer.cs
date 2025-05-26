using UnityEngine;
using UnityEngine.SceneManagement; // SceneManager를 사용하기 위해 추가

#if UNITY_EDITOR // 에디터에서만 이 클래스가 컴파일되도록 합니다.
using UnityEditor;
#endif

public static class EditorSceneInitializer
{
    // 각 매니저 프리팹의 Resources 폴더 내 경로를 정의합니다.
    // 예: "Prefabs/Managers/CustomerOrderManager" (Assets/Resources/Prefabs/Managers/CustomerOrderManager.prefab)
    private const string CUSTOMER_ORDER_MANAGER_PREFAB_PATH = "Prefabs/Managers/CustomerOrderManager";
    private const string SCENE_SWITCHER_PREFAB_PATH = "Prefabs/Managers/SceneSwitcher";
    private const string AUDIO_MANAGER_PREFAB_PATH = "Prefabs/Managers/AudioManager";
    private const string HEART_MANAGER_PREFAB_PATH = "Prefabs/Managers/HeartManager";
    private const string STAGE_DATA_MANAGER_PREFAB_PATH = "Prefabs/Managers/StageDataManager";
    // 필요에 따라 다른 매니저 프리팹 경로 추가

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void InitializeManagersBeforeSceneLoad()
    {
        // 특정 씬(들)에서만 이 로직을 실행하고 싶다면 주석 해제 후 씬 이름 비교
        // string currentSceneName = SceneManager.GetActiveScene().name;
        // if (currentSceneName != "SugarBoilingScene" && currentSceneName != "FruitCatchingGameScene" /* && 다른 테스트 대상 씬들 */)
        // {
        //     return;
        // }

        Debug.Log("EditorSceneInitializer: Checking and initializing managers if needed...");

        EnsureManagerExists<CustomerOrderManager>(CUSTOMER_ORDER_MANAGER_PREFAB_PATH);
        EnsureManagerExists<SceneSwitcher>(SCENE_SWITCHER_PREFAB_PATH);
        EnsureManagerExists<AudioManager>(AUDIO_MANAGER_PREFAB_PATH);
        EnsureManagerExists<HeartManager>(HEART_MANAGER_PREFAB_PATH);
        EnsureManagerExists<StageDataManager>(STAGE_DATA_MANAGER_PREFAB_PATH);
        // 필요에 따라 다른 EnsureManagerExists 호출 추가
    }

    private static void EnsureManagerExists<T>(string prefabPath) where T : MonoBehaviour
    {
        // 제네릭 타입 T의 Instance가 있는지 확인 (싱글톤 패턴에 Instance 프로퍼티가 있다고 가정)
        // 각 매니저 클래스에 public static T Instance { get; private set; } 형태의 프로퍼티가 있어야 합니다.
        // 현재 CustomerOrderManager, SceneSwitcher, AudioManager, HeartManager, StageDataManager 모두 이 패턴을 따르고 있습니다.

        bool instanceExists = false;
        // 리플렉션을 사용하여 Instance 프로퍼티에 접근 (더 안전한 방법은 각 매니저가 공통 인터페이스를 갖거나, 직접 Instance를 확인하는 것)
        // 여기서는 각 매니저의 Instance 프로퍼티를 직접 호출하는 방식으로 변경합니다.
        if (typeof(T) == typeof(CustomerOrderManager)) instanceExists = CustomerOrderManager.Instance != null;
        else if (typeof(T) == typeof(SceneSwitcher)) instanceExists = SceneSwitcher.Instance != null;
        else if (typeof(T) == typeof(AudioManager)) instanceExists = AudioManager.Instance != null;
        else if (typeof(T) == typeof(HeartManager)) instanceExists = HeartManager.Instance != null;
        else if (typeof(T) == typeof(StageDataManager)) instanceExists = StageDataManager.Instance != null;
        // ... 다른 매니저 타입에 대한 확인 추가 ...

        if (!instanceExists)
        {
            GameObject prefab = Resources.Load<GameObject>(prefabPath);
            if (prefab != null)
            {
                GameObject instance = Object.Instantiate(prefab);
                instance.name = prefab.name; // 프리팹 이름 유지 (구분을 위해)
                // DontDestroyOnLoad는 각 매니저의 Awake()에서 처리되므로 여기서 호출할 필요 없음
                Debug.Log($"<color=yellow>EDITOR MODE: Instantiated [{instance.name}] from Resources for testing.</color>");
            }
            else
            {
                Debug.LogError($"<color=red>EDITOR MODE: Prefab for [{typeof(T).Name}] not found at Resources path: Assets/Resources/{prefabPath}.prefab. Please check the path.</color>");
            }
        }
        else
        {
             Debug.Log($"<color=green>EDITOR MODE: Instance of [{typeof(T).Name}] already exists.</color>");
        }
    }
#endif
}