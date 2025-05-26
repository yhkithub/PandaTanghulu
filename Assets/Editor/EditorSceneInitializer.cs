// EditorSceneInitializer.cs
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class EditorSceneInitializer
{
    private const string CUSTOMER_ORDER_MANAGER_PREFAB_PATH = "Prefabs/Managers/CustomerOrderManager";
    private const string SCENE_SWITCHER_PREFAB_PATH = "Prefabs/Managers/SceneSwitcher";
    private const string AUDIO_MANAGER_PREFAB_PATH = "Prefabs/Managers/AudioManager";
    private const string HEART_MANAGER_PREFAB_PATH = "Prefabs/Managers/HeartManager";
    private const string STAGE_DATA_MANAGER_PREFAB_PATH = "Prefabs/Managers/StageDataManager";

    private const string TUTORIAL_COMPLETED_KEY = "TutorialCompleted";
    private const string EDITOR_PREFS_FORCE_TUTORIAL_KEY = "EditorSceneInitializer_ForceRunTutorial";

#if UNITY_EDITOR
    private const string FORCE_TUTORIAL_MENU_ITEM = "Tools/PandaTanghulu/Force Run Tutorial on Play";

    [MenuItem(FORCE_TUTORIAL_MENU_ITEM)]
    private static void ToggleForceRunTutorial()
    {
        bool currentValue = EditorPrefs.GetBool(EDITOR_PREFS_FORCE_TUTORIAL_KEY, false);
        EditorPrefs.SetBool(EDITOR_PREFS_FORCE_TUTORIAL_KEY, !currentValue);
        // EditorPrefs는 즉시 저장되므로 별도의 Save 호출은 필요 없습니다.
        Debug.Log("Toggled 'Force Run Tutorial on Play'. New state in EditorPrefs: " + !currentValue);
        // 메뉴 UI 업데이트를 위해 Validate 함수가 호출되도록 유도 (또는 에디터 창 포커스 변경 등)
    }

    // 메뉴 아이템의 체크 상태를 EditorPrefs 값 기준으로 설정하는 유효성 검사 함수
    [MenuItem(FORCE_TUTORIAL_MENU_ITEM, true)]
    private static bool ToggleForceRunTutorialValidate()
    {
        Menu.SetChecked(FORCE_TUTORIAL_MENU_ITEM, EditorPrefs.GetBool(EDITOR_PREFS_FORCE_TUTORIAL_KEY, false));
        return true; // 항상 메뉴를 활성화 상태로 둠
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void InitializeManagersBeforeSceneLoad()
    {
        // 플레이 모드 시작 시 EditorPrefs에서 값을 확실히 다시 읽어옴
        bool shouldForceTutorial = EditorPrefs.GetBool(EDITOR_PREFS_FORCE_TUTORIAL_KEY, false);
        Debug.Log($"EditorSceneInitializer: InitializeManagersBeforeSceneLoad called. Current 'Force Run Tutorial' (from EditorPrefs): {shouldForceTutorial}");

        if (shouldForceTutorial)
        {
            Debug.Log("<color=orange>EDITOR MODE: Force Run Tutorial is ON (from EditorPrefs). Deleting TutorialCompleted key.</color>");
            PlayerPrefs.DeleteKey(TUTORIAL_COMPLETED_KEY);
            PlayerPrefs.Save(); // PlayerPrefs 변경 사항은 명시적 저장이 좋음
            Debug.Log("<color=orange>EDITOR MODE: TutorialCompleted key deleted and saved. HasKey: " + PlayerPrefs.HasKey(TUTORIAL_COMPLETED_KEY) + "</color>");
        }
        else
        {
            Debug.Log("<color=cyan>EDITOR MODE: Force Run Tutorial is OFF (from EditorPrefs). Tutorial flag not changed by initializer.</color>");
        }

        EnsureManagerExists<CustomerOrderManager>(CUSTOMER_ORDER_MANAGER_PREFAB_PATH);
        EnsureManagerExists<SceneSwitcher>(SCENE_SWITCHER_PREFAB_PATH);
        EnsureManagerExists<AudioManager>(AUDIO_MANAGER_PREFAB_PATH);
        EnsureManagerExists<HeartManager>(HEART_MANAGER_PREFAB_PATH);
        EnsureManagerExists<StageDataManager>(STAGE_DATA_MANAGER_PREFAB_PATH);
    }

    private static void EnsureManagerExists<T>(string prefabPath) where T : MonoBehaviour
    {
        bool instanceExists = false;
        // 각 매니저의 Instance null 체크 (이전과 동일)
        if (typeof(T) == typeof(CustomerOrderManager)) instanceExists = CustomerOrderManager.Instance != null;
        else if (typeof(T) == typeof(SceneSwitcher)) instanceExists = SceneSwitcher.Instance != null;
        else if (typeof(T) == typeof(AudioManager)) instanceExists = AudioManager.Instance != null;
        else if (typeof(T) == typeof(HeartManager)) instanceExists = HeartManager.Instance != null;
        else if (typeof(T) == typeof(StageDataManager)) instanceExists = StageDataManager.Instance != null;


        if (!instanceExists)
        {
            GameObject prefab = Resources.Load<GameObject>(prefabPath);
            if (prefab != null)
            {
                GameObject instance = Object.Instantiate(prefab);
                instance.name = prefab.name; // 프리팹 이름 유지
                Debug.Log($"<color=yellow>EDITOR MODE: Instantiated [{instance.name}] from Resources for testing.</color>");
            }
            else
            {
                Debug.LogError($"<color=red>EDITOR MODE: Prefab for [{typeof(T).Name}] not found at Resources path: Assets/Resources/{prefabPath}.prefab</color>");
            }
        }
        // else
        // {
        //      Debug.Log($"<color=green>EDITOR MODE: Instance of [{typeof(T).Name}] already exists.</color>");
        // }
    }
#endif
}