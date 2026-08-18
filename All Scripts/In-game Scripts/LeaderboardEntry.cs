//DOCUMENTED CODE 
using UnityEngine;
using TMPro;
public class LeaderboardEntry : MonoBehaviour
{

    public TextMeshProUGUI usernameText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI killsText;
    public TextMeshProUGUI pingText;


    public void SetData(PlayerStats player)
    {
        usernameText.text = player.username;
        scoreText.text = $"{player.score}";
        killsText.text =$"{player.kills}";
        pingText.text = $"{player.ping} ms";

    }
}
