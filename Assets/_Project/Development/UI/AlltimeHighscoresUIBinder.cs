using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AlltimeHighscoresUIBinder : MonoBehaviour
{
    [SerializeField] private HighscoreUIField highscoreUIField;
    [SerializeField] private LayoutGroup highscoreGroup;

    [Serializable]
    private struct AlltimeHighscores
    {
        public PlayerScore[] highscores;

        [Serializable]
        public struct PlayerScore
        {
            public string name;
            public ushort highscore;
        }
    }

    private async void Start()
    {
        ((RectTransform)highscoreGroup.transform).sizeDelta = Vector2.zero;

        var content = await DatabaseHelper.GetContentAsync("https://studenthome.hku.nl/~casey.hofland/Database/CaveQuestGetHighscores.php");
        var alltimeHighscores = JsonUtility.FromJson<AlltimeHighscores>(content);

        foreach(var playerScore in alltimeHighscores.highscores)
        {
            SpawnHighscoreUIField(playerScore.name, playerScore.highscore);
        }
    }

    // A highscore UI field displays a name and highscore.
    public void SpawnHighscoreUIField(string name, ushort highscore)
    {
        var spawnedHighscoreField = Instantiate(highscoreUIField, highscoreGroup.transform);
        spawnedHighscoreField.SetName(name);
        spawnedHighscoreField.SetScore(highscore);

        var fieldHeight = ((RectTransform)spawnedHighscoreField.transform).sizeDelta.y;
        var highscoreGroupRectTransform = (RectTransform)highscoreGroup.transform;
        var newSize = highscoreGroupRectTransform.sizeDelta + new Vector2(0, fieldHeight);
        highscoreGroupRectTransform.sizeDelta = newSize;
    }

    // Method called by button for leaving the room. Returns to the joining area.
    public void LeaveRoom()
    {
        SceneManager.LoadScene("Joining Area");
    }
}
