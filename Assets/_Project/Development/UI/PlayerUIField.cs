using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUIField : MonoBehaviour
{
    [SerializeField] private Graphic colorField;
    [SerializeField] private TMP_Text textField;

    public void SetColor(Color color) => colorField.color = color;
    public void SetName(string name) => textField.text = string.IsNullOrWhiteSpace(name) ? "No Name" : name;
}
