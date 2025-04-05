using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    public void StartNewGame()
    {
        SceneManager.LoadScene("StoryScene"); // ���� ���� ���� �� �̸�
    }

    public void ContinueGame()
    {
        SceneManager.LoadScene("SavedGameScene"); // �̾��ϱ�� �� �̸�
    }

    public void OpenAnimalBook()
    {
        SceneManager.LoadScene("AnimalBookScene"); // ���� �� �̸�
    }
}
