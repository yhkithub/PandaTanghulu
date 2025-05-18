// FruitSpawner2D.cs
using UnityEngine;
using System.Collections.Generic;
using System.Collections; // IEnumerator 사용

public class FruitSpawner2D : MonoBehaviour
{
    public static FruitSpawner2D Instance { get; private set; } // 싱글톤으로 만들기 (선택 사항)
    private bool isSpawningPaused = false;

    [Header("생성 위치 설정")]
    public float minSpawnX = -2f;  // 과일이 생성될 월드 X 좌표 최소값
    public float maxSpawnX = 5f;   // 과일이 생성될 월드 X 좌표 최대값
    public float spawnHeight = 7f;   // 과일이 생성될 Y축 높이 (스포너의 Y 위치 기준)

    [Header("생성할 과일 프리팹")]
    public List<FruitPrefabChance> fruitPrefabsWithChance;

    [Header("생성 설정")]
    public float minSpawnDelay = 0.5f;
    public float maxSpawnDelay = 1.5f;
    public int maxFruitsToSpawnAtOnce = 1;
    // public float fruitFallingSpeed = 5f; // 이 변수는 현재 코드에서 직접 사용되지 않고, Rigidbody2D의 Gravity Scale로 제어 권장

    [Header("여러 개 동시 생성 시 설정")]
    public float multipleSpawnXOffset = 0.8f; // 과일 사이의 X축 간격

    [System.Serializable]
    public struct FruitPrefabChance
    {
        public GameObject prefab;
        public float chanceWeight;
        public bool isSpecialItem;
    }

    [Header("특별 아이템 등장 빈도 조절")]
    public float specialItemChanceModifier = 0.2f;

    // private List<GameObject> activeFallingFruits = new List<GameObject>(); // 현재는 사용하지 않으므로 주석 처리 또는 삭제

    void Start()
    {
        StartCoroutine(SpawnFruitsRoutine());
    }
    void Awake() // 만약 싱글톤으로 만든다면
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    IEnumerator SpawnFruitsRoutine()
    {
        while (true)
        {
            if (isSpawningPaused) // 일시정지 상태이면 대기
            {
                yield return null; // 다음 프레임까지 대기하고 다시 체크
                continue;
            }

            float delay = Random.Range(minSpawnDelay, maxSpawnDelay);
            yield return new WaitForSeconds(delay);

            int countToSpawn = Random.Range(1, maxFruitsToSpawnAtOnce + 1);
            // 한 묶음의 전체 예상 너비
            float estimatedBundleWidth = (countToSpawn - 1) * multipleSpawnXOffset;

            // 생성될 묶음의 중앙 X 위치를 계산합니다.
            // minSpawnX와 maxSpawnX 사이에서, 묶음 전체가 범위 내에 들어올 수 있는 중앙 지점을 찾습니다.
            float validMinBundleCenterX = minSpawnX + (estimatedBundleWidth / 2);
            float validMaxBundleCenterX = maxSpawnX - (estimatedBundleWidth / 2);

            // 만약 묶음 너비가 너무 커서 유효 범위가 없다면 (예: min > max), 생성 위치를 minSpawnX나 maxSpawnX 근처로 고정하거나,
            // 묶음 개수를 줄이는 등의 처리가 필요할 수 있습니다. 여기서는 일단 minSpawnX로 설정합니다.
            float bundleCenterX;
            if (validMinBundleCenterX > validMaxBundleCenterX)
            {
                // 이 경우는 묶음이 너무 넓어서 전체 범위에 다 들어가지 못하는 상황입니다.
                // 단순하게 minSpawnX ~ maxSpawnX 사이에서 중앙점을 잡도록 수정하거나,
                // countToSpawn을 줄이는 로직을 고려해야 합니다.
                // 여기서는 가장 왼쪽 또는 오른쪽에 붙도록 하거나, 단일 생성처럼 처리합니다.
                // 또는, 항상 범위 내에 생성되도록 보장하는 Random.Range를 사용합니다.
                // bundleCenterX = Random.Range(minSpawnX, maxSpawnX); // 이렇게 하면 묶음이 잘릴 수 있음
                // 아래는 묶음의 중앙이 minSpawnX와 maxSpawnX 사이에 있도록 보장하는 방식
                bundleCenterX = Random.Range(Mathf.Max(minSpawnX, validMinBundleCenterX), Mathf.Min(maxSpawnX, validMaxBundleCenterX));
                if (countToSpawn == 1) // 하나만 생성할 경우, 전체 범위에서 랜덤
                {
                    bundleCenterX = Random.Range(minSpawnX, maxSpawnX);
                }
                else if (validMinBundleCenterX > validMaxBundleCenterX)
                {
                    // 묶음이 너무 넓어 minSpawnX ~ maxSpawnX 범위에 전체가 들어갈 수 없는 경우,
                    // 묶음의 일부가 잘리더라도 중앙을 minSpawnX ~ maxSpawnX 사이에 둠.
                    // 혹은 단일 과일처럼 생성되도록 처리할 수 있음.
                    // 여기서는 묶음의 중앙이 minSpawnX와 maxSpawnX 사이에 오도록 하되, 묶음이 잘릴 수 있음을 인지.
                    // 좀 더 나은 방법은 countToSpawn을 줄이거나, multipleSpawnXOffset을 줄이는 것.
                    // 간단히 처리하기 위해, 그냥 중앙값을 사용.
                    bundleCenterX = (minSpawnX + maxSpawnX) / 2;
                }

            }
            else
            {
                bundleCenterX = Random.Range(validMinBundleCenterX, validMaxBundleCenterX);
            }


            for (int i = 0; i < countToSpawn; i++)
            {
                float currentFruitX;
                if (countToSpawn > 1)
                {
                    // 묶음의 중앙을 기준으로 각 과일의 X 위치 계산
                    currentFruitX = bundleCenterX - (estimatedBundleWidth / 2) + (i * multipleSpawnXOffset);
                }
                else
                {
                    currentFruitX = bundleCenterX; // 하나일 때는 bundleCenterX (이미 minSpawnX, maxSpawnX 범위 내 랜덤)
                }

                // 스포너의 Y 위치를 기준으로 spawnHeight만큼 위에서 생성
                float currentFruitY = transform.position.y + spawnHeight;
                Vector3 spawnPos = new Vector3(currentFruitX, currentFruitY, 0f); // X 좌표는 월드 좌표 사용

                SpawnSpecificFruit(spawnPos);

                if (countToSpawn > 1 && i < countToSpawn - 1)
                {
                    yield return new WaitForSeconds(0.05f);
                }
            }
        }
    }

    void SpawnSpecificFruit(Vector3 positionToSpawn)
    {
        GameObject fruitToSpawnPrefab = GetRandomFruitPrefab();
        if (fruitToSpawnPrefab == null)
        {
            Debug.LogWarning("생성할 과일 프리팹을 선택하지 못했습니다.");
            return;
        }

        // 최종 생성 위치 X 좌표를 minSpawnX와 maxSpawnX 사이로 제한 (월드 좌표 기준)
        positionToSpawn.x = Mathf.Clamp(positionToSpawn.x, minSpawnX, maxSpawnX);

        Instantiate(fruitToSpawnPrefab, positionToSpawn, Quaternion.identity);
    }

    // GetRandomFruitPrefab() 함수는 이전과 동일
    GameObject GetRandomFruitPrefab()
    {
        if (fruitPrefabsWithChance == null || fruitPrefabsWithChance.Count == 0) return null;

        float totalWeight = 0f;
        List<float> cumulativeWeights = new List<float>();

        foreach (var fruitInfo in fruitPrefabsWithChance)
        {
            float currentWeight = fruitInfo.chanceWeight;
            if (fruitInfo.isSpecialItem)
            {
                currentWeight *= specialItemChanceModifier;
            }
            totalWeight += currentWeight;
            cumulativeWeights.Add(totalWeight);
        }

        if (totalWeight == 0) // 모든 가중치가 0이거나 리스트가 비었을 때 예외 처리
        {
            Debug.LogWarning("모든 과일의 생성 확률 가중치가 0입니다. GetRandomFruitPrefab()을 실행할 수 없습니다.");
            return null;
        }

        float randomPoint = Random.Range(0, totalWeight);

        for (int i = 0; i < cumulativeWeights.Count; i++)
        {
            if (randomPoint < cumulativeWeights[i])
            {
                return fruitPrefabsWithChance[i].prefab;
            }
        }
        // 만약의 경우 (부동소수점 오류 등으로 루프를 빠져나올 경우) 마지막 아이템 반환
        return fruitPrefabsWithChance[fruitPrefabsWithChance.Count - 1].prefab;
    }
    // 스포너 일시정지/재개 함수
    public void PauseSpawning(bool pause)
    {
        isSpawningPaused = pause;
        if (pause)
        {
            Debug.Log("FruitSpawner2D: 스폰 일시정지됨.");
        }
        else
        {
            Debug.Log("FruitSpawner2D: 스폰 재개됨.");
        }
    }
}