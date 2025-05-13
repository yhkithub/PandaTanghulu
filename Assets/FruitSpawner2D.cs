using UnityEngine;
using System.Collections.Generic;

public class FruitSpawner2D : MonoBehaviour
{
    public List<GameObject> fruitPrefabs; // Inspector에서 과일 프리팹들을 연결할 리스트
    public float spawnRate = 1f;
    public float spawnRangeX = 5f;

    void Start()
    {
        InvokeRepeating("SpawnRandomFruit", 0f, spawnRate);
    }

    void SpawnRandomFruit()
    {
        if (fruitPrefabs.Count > 0)
        {
            int randomIndex = Random.Range(0, fruitPrefabs.Count);
            float randomX = Random.Range(-spawnRangeX, spawnRangeX);
            Vector3 spawnPosition = new Vector3(randomX, 10f, 0f);
            Instantiate(fruitPrefabs[randomIndex], spawnPosition, Quaternion.identity);
        }
    }
}