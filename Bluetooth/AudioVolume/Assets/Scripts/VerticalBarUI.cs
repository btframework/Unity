using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class VerticalBarUI : MonoBehaviour
{
    [Tooltip("Image с Type=Filled, FillMethod=Vertical, FillOrigin=Bottom")]
    public Image fill;

    public void SetValue01(float v)
    {
        if (fill != null) fill.fillAmount = Mathf.Clamp01(v);
    }
}