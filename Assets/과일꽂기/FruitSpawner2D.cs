// FruitSpawner2D.cs
using UnityEngine;
using System.Collections.Generic;
using System.Collections; // IEnumerator 사용

public class FruitSpawner2D : MonoBehaviour
{
    [Header("생성할 과일 프리팹")]
    public List<FruitPrefabChance> fruitPrefabsWithChance; // 각 과일 프리팹과 등장 확률

    [Header("생성 설정")]
    public float minSpawnDelay = 0.5f;  // 최소 생성 간격 (초)
    public float maxSpawnDelay = 1.5f;  // 최대 생성 간격 (초)
    public int maxFruitsToSpawnAtOnce = 1; // 한 번에 생성할 최대 과일 개수 (1로 두면 하나씩)
    public float fruitFallingSpeed = 5f; // 과일 기본 낙하 속도 (과일 프리팹 자체에 Rigidbody2D가 있고 Gravity Scale을 사용한다면 이 변수는 다르게 활용)

    [Header("생성 위치 설정")]
    public float spawnAreaWidth = 10f; // 과일이 생성될 X축 범위 (중앙 기준 좌우로 spawnAreaWidth / 2)
    public float spawnHeight = 7f;    // 과일이 생성될 Y축 높이

    // 특정 과일(특별 아이템)의 등장 확률을 낮추기 위한 설정
    [System.Serializable]
    public struct FruitPrefabChance
    {
        public GameObject prefab;
        public float chanceWeight; // 높을수록 자주 등장 (예: 일반과일 10, 특별과일 1)
        public bool isSpecialItem; // 이 아이템이 특별 아이템인지 여부
    }

    [Header("특별 아이템 등장 빈도 조절")]
    public float specialItemChanceModifier = 0.2f; // 특별 아이템의 기본 chanceWeight에 곱해질 값 (0.2면 20% 확률로 줄어듦)

    private List<GameObject> activeFallingFruits = new List<GameObject>(); // 현재 떨어지고 있는 과일들 (선택적 관리)

    void Start()
    {
        StartCoroutine(SpawnFruitsRoutine());
    }

    IEnumerator SpawnFruitsRoutine()
    {
        while (true) // 게임이 실행되는 동안 계속 반복
        {
            float delay = Random.Range(minSpawnDelay, maxSpawnDelay);
            yield return new WaitForSeconds(delay);

            int count = Random.Range(1, maxFruitsToSpawnAtOnce + 1);
            for (int i = 0; i < count; i++)
            {
                SpawnRandomFruit();
            }
        }
    }

    void SpawnRandomFruit()
    {
        GameObject fruitToSpawn = GetRandomFruitPrefab();
        if (fruitToSpawn == null)
        {
            Debug.LogWarning("생성할 과일 프리팹을 선택하지 못했습니다. fruitPrefabsWithChance 설정을 확인하세요.");
            return;
        }

        float randomX = Random.Range(-spawnAreaWidth / 2, spawnAreaWidth / 2);
        Vector3 spawnPosition = new Vector3(transform.position.x + randomX, transform.position.y + spawnHeight, 0f);

        GameObject spawnedFruit = Instantiate(fruitToSpawn, spawnPosition, Quaternion.identity);

        // 낙하 속도 설정 (방법1: Rigidbody2D가 없는 경우 직접 이동)
        // 이 방법을 사용하려면 과일 프리팹에 Rigidbody2D가 없거나 isKinematic=true여야 함.
        // 그리고 과일 자체에 아래로 이동하는 스크립트가 있어야 함.
        // 예: spawnedFruit.AddComponent<FallingFruit>().speed = fruitFallingSpeed;

        // 낙하 속도 설정 (방법2: Rigidbody2D의 중력 사용)
        // 과일 프리팹에 Rigidbody2D 컴포넌트가 있고, BodyType이 Dynamic이며, Use Gravity가 체크된 경우
        // Gravity Scale을 조절하거나, 여기서 초기 속도를 줄 수 있습니다.
        Rigidbody2D rb = spawnedFruit.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // rb.gravityScale = fruitFallingSpeed; // Gravity Scale을 속도처럼 사용 (값이 클수록 빨리 떨어짐)
            // 또는, 초기 하강 힘을 줄 수 있습니다.
            // rb.velocity = new Vector2(0, -fruitFallingSpeed);
            // 이 부분은 과일 프리팹의 Rigidbody2D 설정과 통일성 있게 관리해야 합니다.
            // 가장 간단한 방법은 과일 프리팹 자체의 Rigidbody2D에 Gravity Scale을 적절히 설정해두고,
            // 여기서는 생성만 하는 것입니다. 만약 여기서 개별 속도를 다르게 주고 싶다면 위와 같이 접근합니다.
        }
        else
        {
            Debug.LogWarning(spawnedFruit.name + "에 Rigidbody2D 컴포넌트가 없습니다. 낙하 로직을 확인하세요.");
        }


        // activeFallingFruits.Add(spawnedFruit); // 필요하다면 관리
    }

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
                currentWeight *= specialItemChanceModifier; // 특별 아이템이면 확률 보정
            }
            totalWeight += currentWeight;
            cumulativeWeights.Add(totalWeight);
        }

        float randomPoint = Random.Range(0, totalWeight);

        for (int i = 0; i < cumulativeWeights.Count; i++)
        {
            if (randomPoint < cumulativeWeights[i])
            {
                return fruitPrefabsWithChance[i].prefab;
            }
        }
        return fruitPrefabsWithChance[fruitPrefabsWithChance.Count - 1].prefab; // 만약의 경우 마지막 아이템
    }

    // 과일 프리팹의 Rigidbody2D 설정 (예시)
    // 각 과일 프리팹을 선택하고 Inspector에서 Rigidbody2D 컴포넌트를 확인/추가하세요.
    // - Body Type: Dynamic
    // - Material: None (or a physics material with low friction/bounciness)
    // - Simulated: True
    // - Use Auto Mass: True (or set mass manually)
    // - Linear Drag: 0 (or a small value if you want air resistance)
    // - Angular Drag: 0.05 (or as needed)
    // - Gravity Scale: 1 (기본 중력. 이 값을 Inspector에서 조절하여 전체적인 낙하 속도를 조절할 수 있습니다. FruitSpawner2D의 fruitFallingSpeed는 이 값을 덮어쓰거나 다른 용도로 사용될 수 있습니다.)
    // - Collision Detection: Discrete (or Continuous if fruits are very fast)
    // - Sleeping Mode: Start Awake
    // - Interpolate: None (or Interpolate for smoother movement if needed)
    // - Constraints: Freeze Rotation Z (보통 2D 과일은 Z축 회전 안 함)
}