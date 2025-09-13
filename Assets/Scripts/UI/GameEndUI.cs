using UnityEngine;
using UnityEngine.UI;

public class GameEndUI : MonoBehaviour
{
    [SerializeField] private Text killCountText;
    [SerializeField] private Text scoreText;
    [SerializeField] private Text timeText;

    public void Show(int killCount, int score, float time)
    {
        killCountText.text = $"Kill: {killCount}";
        scoreText.text = $"Score: {score}";
        int minutes = (int)(time / 60);
        int seconds = (int)(time % 60);
        timeText.text = $"{minutes:D2}:{seconds:D2}";
        gameObject.SetActive(true);
    }
}
