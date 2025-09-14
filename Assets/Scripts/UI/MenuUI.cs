using HideAndSeek.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuUI : MonoBehaviour
{
    public void OnStartClick()
    {
        AudioEffectManaager.Instance.PlayUIClickEffect();
        SceneManager.LoadScene(1);
    }

    public void OnExitClick()
    {
        AudioEffectManaager.Instance.PlayUIClickEffect();
        Application.Quit();
    }
}
