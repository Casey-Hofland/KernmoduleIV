using TMPro;
using UnityEngine;

public class HighscoreUIField : MonoBehaviour
{
    [SerializeField] private TMP_Text nameField;
    [SerializeField] private TMP_Text scoreField;

    public void SetName(string name) => nameField.text = name;
    public void SetScore(ushort score) => scoreField.text = score.ToString();
}
