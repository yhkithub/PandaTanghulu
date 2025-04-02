using UnityEngine;
using UnityEngine.SceneManagement; // 씬 전환을 위한 네임스페이스

public class TitleManager : MonoBehaviour  // MonoBehaviour 상속 확인
{
    public void StartNewGame()
    {
        SceneManager.LoadScene("MainScene");
    }

    public void ContinueGame()
    {
        SceneManager.LoadScene("SavedGameScene");
    }

    public void OpenAnimalBook()
    {
        SceneManager.LoadScene("AnimalBookScene");
    }
}
