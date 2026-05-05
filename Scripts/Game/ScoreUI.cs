using Mirror.BouncyCastle.Ocsp;
using UnityEngine;
using UnityEngine.UI;

public class ScoreUI : MonoBehaviour
{
    public static ScoreUI Instance;

    public Text redScoreText;
    public Text blueScoreText;

    void Awake()
    {
        Instance = this;
    }

    public void UpdateScore(int red, int blue)
    {
        redScoreText.text = "Red Team Score: " + red.ToString();
        blueScoreText.text = "Blue Team Score: " + blue.ToString();
    }
}
