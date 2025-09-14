using UnityEngine;
using UnityEngine.UI;

public static class TextExtenstion
{
    public static void ShowTime(this Text text, float sec)
    {
        int minutes = (int)(sec / 60);
        int seconds = (int)(sec % 60);
        text.text = $"{minutes:D2}:{seconds:D2}";
    }
}
