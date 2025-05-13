using UnityEngine;

public class FruitSpawner : MonoBehaviour
{
    public GameObject fruitPrefab; // Inspector에서 과일 프리팹을 연결할 변수
    public float spawnRate = 1f;    // 과일 생성 간격 (초)
    public float spawnRangeX = 5f;  // X축 생성 범위

    void Start()
    {
        // 게임 시작 후 spawnRate 간격으로 SpawnFruit 함수를 반복 실행합니다.
        InvokeRepeating("SpawnFruit", 0f, spawnRate);
    }

    void SpawnFruit()
    {
        // 랜덤한 X 위치를 생성합니다.
        float randomX = Random.Range(-spawnRangeX, spawnRangeX);
        // 생성 위치를 설정합니다. Y축은 화면 위쪽으로 설정합니다.
        Vector3 spawnPosition = new Vector3(randomX, 10f, 0f);
        // 과일 프리팹을 생성합니다.
        Instantiate(fruitPrefab, spawnPosition, Quaternion.identity);
    }
}