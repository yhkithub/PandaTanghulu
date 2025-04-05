using UnityEngine;
using UnityEngine.SceneManagement;

public class RibbonCutter : MonoBehaviour
{
    private bool isCut = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isCut) return;

        // MouseTrail 태그를 가진 오브젝트와 충돌 시
        if (other.CompareTag("MouseTrail"))
        {
            isCut = true;
            Debug.Log("🎀 리본이 잘렸습니다!");
            Destroy(gameObject);
            SceneManager.LoadScene("ShopScene");
        }
    }
}
