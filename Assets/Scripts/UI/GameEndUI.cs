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
        timeText.ShowTime(time);
        gameObject.SetActive(true);
    }
}
