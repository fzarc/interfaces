using UnityEngine;

public class ResistorVariable : MonoBehaviour
{
    public float minResistance = 10f;
    public float maxResistance = 1000f;

    [Range(0f,1f)]
    public float knobPosition = 0.5f;

    public float GetValue()
    {
        return Mathf.Lerp(minResistance, maxResistance, knobPosition);
    }

    // Para cambiar el potenciómetro desde UI o input
    public void SetKnob(float value)
    {
        knobPosition = Mathf.Clamp01(value);
    }
}