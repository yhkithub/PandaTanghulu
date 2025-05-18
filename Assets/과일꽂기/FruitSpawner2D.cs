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

    [Header("여러 개 동시 생성 시 설정")]
    public float multipleSpawnXOffset = 0.8f; // 한 번에 여러 과일 생성 시 X축 기본 간격


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
        while (true)
        {
            float delay = Random.Range(minSpawnDelay, maxSpawnDelay);
            yield return new WaitForSeconds(delay);

            int countToSpawn = Random.Range(1, maxFruitsToSpawnAtOnce + 1);
            // 한 묶음의 전체 예상 너비 (대략적인 계산)
            float estimatedBundleWidth = (countToSpawn - 1) * multipleSpawnXOffset;
            // 생성될 묶음의 시작 X 위치 (화면 중앙에서 벗어나지 않도록)
            float bundleStartX = Random.Range(-(spawnAreaWidth / 2) + (estimatedBundleWidth / 2), (spawnAreaWidth / 2) - (estimatedBundleWidth / 2));

            if (countToSpawn == 1) // 하나만 생성할 경우엔 중앙에서 랜덤
            {
                bundleStartX = Random.Range(-spawnAreaWidth / 2, spawnAreaWidth / 2);
            }


            for (int i = 0; i < countToSpawn; i++)
            {
                // 각 과일의 최종 X 위치 계산
                // 여러 개일 경우: 시작 X 위치에서 (-(묶음너비/2) + i * 간격) 만큼 떨어진 위치
                // 하나일 경우: bundleStartX (이미 랜덤)
                float currentFruitX = bundleStartX;
                if (countToSpawn > 1)
                {
                    currentFruitX = bundleStartX - (estimatedBundleWidth / 2) + (i * multipleSpawnXOffset);
                }

                // Y축 위치는 동일하게 유지하거나 약간의 변화를 줄 수도 있음
                float currentFruitY = transform.position.y + spawnHeight;
                // float yOffsetRandom = Random.Range(-0.1f, 0.1f); // 아주 약간의 Y축 변화 (선택)
                // currentFruitY += yOffsetRandom;


                Vector3 spawnPos = new Vector3(transform.position.x + currentFruitX, currentFruitY, 0f);
                SpawnSpecificFruit(spawnPos); // 위치를 직접 전달하는 함수 호출

                // 아주 짧은 시간차를 두고 생성 (선택 사항, 좀 더 자연스러울 수 있음)
                if (countToSpawn > 1 && i < countToSpawn - 1) // 마지막 과일 제외
                {
                    yield return new WaitForSeconds(0.05f); // 예: 0.05초 간격
                }
            }
        }
    }

    void SpawnSpecificFruit(Vector3 positionToSpawn)
    {
        GameObject fruitToSpawnPrefab = GetRandomFruitPrefab(); // 어떤 과일을 생성할지 결정
        if (fruitToSpawnPrefab == null)
        {
            Debug.LogWarning("생성할 과일 프리팹을 선택하지 못했습니다.");
            return;
        }

        // 생성 범위 Clamp (필요하다면 X축만 또는 X,Y 모두)
        positionToSpawn.x = Mathf.Clamp(positionToSpawn.x, transform.position.x - spawnAreaWidth / 2, transform.position.x + spawnAreaWidth / 2);

        Instantiate(fruitToSpawnPrefab, positionToSpawn, Quaternion.identity);
        // Rigidbody2D 속도/중력 설정은 각 프리팹 또는 여기서 필요에 따라 추가
    }


    void SpawnRandomFruit(float specificX) // 또는 spawnXOffset 같은 이름으로
    {
        GameObject fruitToSpawn = GetRandomFruitPrefab();
        if (fruitToSpawn == null) return;

        // float randomX = Random.Range(-spawnAreaWidth / 2, spawnAreaWidth / 2); // 이 부분 대신 인자를 사용
        Vector3 spawnPosition = new Vector3(transform.position.x + specificX, transform.position.y + spawnHeight, 0f);

        // X축 생성 범위 초과하지 않도록 Clamp (선택 사항)
        spawnPosition.x = Mathf.Clamp(spawnPosition.x, transform.position.x - spawnAreaWidth / 2, transform.position.x + spawnAreaWidth / 2);

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