// FruitCollision2D.cs
using UnityEngine;

public class FruitCollision2D : MonoBehaviour
{
    [Header("이 과일의 종류 설정")]
    public FruitType fruitType;

    private bool isAttached = false;
    private SkewerManager skewerManager;

    void Start()
    {
        GameObject skewerObject = GameObject.FindWithTag("Skewer"); // 꼬치는 "Skewer" 태그 사용
        if (skewerObject != null)
        {
            skewerManager = skewerObject.GetComponent<SkewerManager>();
            if (skewerManager == null)
            {
                Debug.LogError(gameObject.name + ": Skewer 오브젝트에서 SkewerManager 스크립트를 찾을 수 없습니다! 과일이 꽂히지 않습니다.");
            }
        }
        else
        {
            Debug.LogError(gameObject.name + ": 'Skewer' 태그를 가진 꼬치 오브젝트를 찾을 수 없습니다! 과일이 꽂히지 않습니다.");
        }

        if (fruitType == FruitType.None)
        {
            Debug.LogWarning(gameObject.name + "의 Fruit Type이 'None'으로 설정되어 있습니다. Inspector에서 올바른 과일 종류를 선택해주세요.");
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Skewer") && !isAttached && skewerManager != null)
        {
            isAttached = true;
            skewerManager.AddFruitToSkewer(fruitType, gameObject);
        }
        else if (other.CompareTag("TrashCan") && !isAttached)
        {
            Debug.Log(fruitType.ToString() + "을(를) 쓰레기통에 버렸습니다.");
            Destroy(gameObject);
        }
    }
}