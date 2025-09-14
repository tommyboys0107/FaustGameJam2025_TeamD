using HideAndSeek.Core;
using UnityEngine;

public class MenuUI : MonoBehaviour
{
    public void OnStartClick()
    {
        GameManager.Instance.StartGame();
    }

    public void OnExitClick()
    {
        Application.Quit();
    }
}
