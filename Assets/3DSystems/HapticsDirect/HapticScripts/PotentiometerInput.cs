using UnityEngine;

public class PotentiometerInput : MonoBehaviour
{
    public ResistorVariable resistor;
    public float speed = 0.5f;

    void Update()
    {
        float input = Input.GetAxis("Horizontal");

        if (resistor != null)
        {
            resistor.knobPosition += input * speed * Time.deltaTime;
            resistor.knobPosition = Mathf.Clamp01(resistor.knobPosition);
        }
    }
}