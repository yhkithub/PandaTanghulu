// FruitSpawner2D.cs
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class FruitSpawner2D : MonoBehaviour
{
    public static FruitSpawner2D Instance { get; private set; }
    private bool isSpawningPaused = false;

    [Header("생성 위치 설정")]
    public float minSpawnX = -2f;
    public float maxSpawnX = 5f;
    public float spawnHeight = 7f;

    [Header("생성할 과일 프리팹")]
    public List<FruitPrefabChance> fruitPrefabsWithChance;

    [Header("생성 설정")]
    public float minSpawnDelay = 0.5f;
    public float maxSpawnDelay = 1.5f;
    public int maxFruitsToSpawnAtOnce = 1;
    public float fruitGravityScale = 1f; // ★★★ 새로 추가: 과일 낙하 속도(Gravity Scale) 조절용 ★★★

    [Header("여러 개 동시 생성 시 설정")]
    public float multipleSpawnXOffset = 0.8f;

    [System.Serializable]
    public struct FruitPrefabChance
    {
        public GameObject prefab;
        public float chanceWeight;
        public bool isSpecialItem;
    }

    [Header("특별 아이템 등장 빈도 조절")]
    public float specialItemChanceModifier = 0.2f;

    private Coroutine spawnCoroutine;

    void Awake() // 싱글톤 패턴은 Awake에 유지
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        // isSpawningPaused = true; // 필요하다면 여기서 true로 초기화하여 CustomerOrderManager가 명시적으로 StartSpawning() 호출하도록 유도
    }

    // Start에서는 CustomerOrderManager의 제어를 기다립니다.
    // void Start() { }


    public bool IsSpawningActive()
    {
        return spawnCoroutine != null && !isSpawningPaused;
    }

    public void StartSpawning()
    {
        if (spawnCoroutine == null)
        {
            isSpawningPaused = false;
            spawnCoroutine = StartCoroutine(SpawnFruitsRoutine());
            Debug.Log("FruitSpawner2D: 스폰 시작됨.");
        }
        else if (isSpawningPaused)
        {
             isSpawningPaused = false;
             Debug.Log("FruitSpawner2D: 스폰 재개됨 (이미 코루틴 존재).");
        }
    }

    public void StopSpawningCompletely()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
        isSpawningPaused = true; // isSpawningPaused도 true로 설정하여 확실히 멈춤
        Debug.Log("FruitSpawner2D: 스폰 완전 중지됨.");
    }

    public void PauseSpawning(bool pause)
    {
        isSpawningPaused = pause;
        if (pause) Debug.Log("FruitSpawner2D: 스폰 일시정지됨.");
        else
        {
            Debug.Log("FruitSpawner2D: 스폰 재개됨.");
            // CustomerOrderManager에서 게임 상태 확인 후 StartSpawning()을 호출하므로, 여기서는 isSpawningPaused만 관리
            // if (spawnCoroutine == null && CustomerOrderManager.Instance != null && CustomerOrderManager.Instance.currentGameState == GameState.Playing)
            // {
            //     StartSpawning();
            // }
        }
    }


    IEnumerator SpawnFruitsRoutine()
    {
        while (true)
        {
            if (isSpawningPaused)
            {
                yield return null;
                continue;
            }

            float delay = Random.Range(minSpawnDelay, maxSpawnDelay);
            yield return new WaitForSeconds(delay);

            int countToSpawn = Random.Range(1, maxFruitsToSpawnAtOnce + 1);
            float estimatedBundleWidth = (countToSpawn - 1) * multipleSpawnXOffset;
            float validMinBundleCenterX = minSpawnX + (estimatedBundleWidth / 2);
            float validMaxBundleCenterX = maxSpawnX - (estimatedBundleWidth / 2);
            float bundleCenterX;

            if (countToSpawn == 1 || validMinBundleCenterX >= validMaxBundleCenterX)
            {
                bundleCenterX = Random.Range(minSpawnX, maxSpawnX);
            }
            else
            {
                bundleCenterX = Random.Range(validMinBundleCenterX, validMaxBundleCenterX);
            }

            for (int i = 0; i < countToSpawn; i++)
            {
                if (isSpawningPaused) break; // 루프 중에도 멈출 수 있도록

                float currentFruitX;
                if (countToSpawn > 1)
                {
                    currentFruitX = bundleCenterX - (estimatedBundleWidth / 2) + (i * multipleSpawnXOffset);
                }
                else
                {
                    currentFruitX = bundleCenterX;
                }

                float currentFruitY = transform.position.y + spawnHeight;
                Vector3 spawnPos = new Vector3(currentFruitX, currentFruitY, 0f);

                SpawnSpecificFruit(spawnPos);

                if (countToSpawn > 1 && i < countToSpawn - 1)
                {
                     if (isSpawningPaused) break;
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

        positionToSpawn.x = Mathf.Clamp(positionToSpawn.x, minSpawnX, maxSpawnX);
        GameObject spawnedFruit = Instantiate(fruitToSpawnPrefab, positionToSpawn, Quaternion.identity);

        // ★★★ 생성된 과일의 Rigidbody2D를 가져와 Gravity Scale 설정 ★★★
        Rigidbody2D rb = spawnedFruit.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = fruitGravityScale;
        }
        else
        {
            Debug.LogWarning(spawnedFruit.name + " 프리팹에 Rigidbody2D 컴포넌트가 없습니다. Gravity Scale을 조절할 수 없습니다.");
        }
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
                currentWeight *= specialItemChanceModifier;
            }
            totalWeight += currentWeight;
            cumulativeWeights.Add(totalWeight);
        }

        if (totalWeight == 0)
        {
            Debug.LogWarning("모든 과일의 생성 확률 가중치가 0입니다.");
            if(fruitPrefabsWithChance.Count > 0) return fruitPrefabsWithChance[Random.Range(0, fruitPrefabsWithChance.Count)].prefab; // 가중치 없으면 랜덤하게 하나라도 반환
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
        return fruitPrefabsWithChance[fruitPrefabsWithChance.Count - 1].prefab; // 만약의 경우
    }
}