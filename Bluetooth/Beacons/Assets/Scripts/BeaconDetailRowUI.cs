using TMPro;
using UnityEngine;

public class BeaconDetailRowUI : MonoBehaviour
{
    public TMP_Text fieldText;
    public TMP_Text valueText;

    public void Set(string field, string value)
    {
        if (fieldText != null) fieldText.text = field;
        if (valueText != null) valueText.text = value;
    }
}