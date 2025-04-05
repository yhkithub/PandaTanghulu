using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    public void StartNewGame()
    {
        SceneManager.LoadScene("StoryScene"); // 실제 게임 시작 씬 이름
    }

    public void ContinueGame()
    {
        SceneManager.LoadScene("SavedGameScene"); // 이어하기용 씬 이름
    }

    public void OpenAnimalBook()
    {
        SceneManager.LoadScene("AnimalBookScene"); // 도감 씬 이름
    }
}
