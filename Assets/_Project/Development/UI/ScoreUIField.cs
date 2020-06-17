using TMPro;
using UnityEngine;

public class ScoreUIField : MonoBehaviour
{
    [SerializeField] private TMP_Text idField;
    [SerializeField] private TMP_Text scoreField;

    public void SetID(int playerID) => idField.text = playerID.ToString();
    public void SetScore(ushort score) => scoreField.text = score.ToString();
}
