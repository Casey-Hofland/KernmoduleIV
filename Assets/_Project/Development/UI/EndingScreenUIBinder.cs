using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndingScreenUIBinder : MonoBehaviour
{
    [SerializeField] private ScoreUIField highScoreUIField;
    [SerializeField] private LayoutGroup highScoreGroup;

    // A highscore UI field displays a players ID and highscore.
    public void SpawnHighScoreUIField(int playerID, ushort highScore)
    {
        var spawnedHighScoreField = Instantiate(highScoreUIField, highScoreGroup.transform);
        spawnedHighScoreField.SetID(playerID);
        spawnedHighScoreField.SetScore(highScore);
    }

    // Method called by button for leaving the room. Destroys drivers and returns to the joining area.
    public void LeaveRoom()
    {
        foreach(var server in FindObjectsOfType<Server>())
        {
            Destroy(server.gameObject);
        }
        foreach(var client in FindObjectsOfType<Client>())
        {
            Destroy(client.gameObject);
        }

        SceneManager.LoadScene("Joining Area");
    }
}
