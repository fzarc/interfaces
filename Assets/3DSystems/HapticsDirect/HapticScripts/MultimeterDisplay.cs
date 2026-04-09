using UnityEngine;
using UnityEngine.UI;

public class MultimeterDisplay : MonoBehaviour
{
    public Text display;

    public void UpdateValue(float current)
    {
        if (display != null)
        {
            display.text = current.ToString("F3") + " A";
        }
    }
}