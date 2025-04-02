using UnityEngine;
using UnityEngine.SceneManagement; // �� ��ȯ�� ���� ���ӽ����̽�

public class TitleManager : MonoBehaviour  // MonoBehaviour ��� Ȯ��
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
