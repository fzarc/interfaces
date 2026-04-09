using UnityEngine;

public class CircuitR : MonoBehaviour
{
    [Header("Parametros del circuito")]
    public float voltage = 5f;     // Voltaje de la fuente
    public float resistance = 100f; // Resistencia actual
    public float current;          // Corriente calculada

    [Header("Componentes")]
    public ResistorVariable potentiometer;
    public MultimeterDisplay multimeter;
    public HapticMaterial haptic;

    [Header("Escala de fuerza")]
    public float k = 1f;

    void Update()
    {
        // 1 Obtener resistencia desde el potenciómetro
        if (potentiometer != null)
        {
            resistance = potentiometer.GetValue();
        }

        // 2 Evitar división por cero
        if (resistance < 0.01f)
        {
            resistance = 0.01f;
        }

        // 3 Ley de Ohm
        current = voltage / resistance;

        // 4 Actualizar multímetro
        if (multimeter != null)
        {
            multimeter.UpdateValue(current);
        }

      // 5 Calcular fuerza háptica (normalizada entre 0 y 1)
        float force = Mathf.Clamp01(k * current);

        // 6 Aplicar parámetros al material
        if (haptic != null)
        {
            // Asignar magnitud
            haptic.hConstForceMag = force;
            
            // Debes definir en qué dirección empuja el material. 
            // Esto asume un empuje hacia arriba (eje Y). Cámbialo según la física de tu escena.
            haptic.hConstForceDir = new Vector3(0f, 1f, 0f); 
        }
    }




     // Añade esto en cualquier lugar dentro de public class CircuitR
    public void SetVoltage(float newVoltage)
    {
        voltage = newVoltage;
    }



}
