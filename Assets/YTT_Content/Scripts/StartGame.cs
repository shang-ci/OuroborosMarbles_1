using UnityEngine;
using UnityEngine.SceneManagement;

public class StartGame : MonoBehaviour
{
    public void OnStartButtonClicked()
    {
        SceneManager.LoadScene("Ytt--Guidance");
    }
}