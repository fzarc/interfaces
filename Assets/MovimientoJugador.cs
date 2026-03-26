using UnityEngine;

public class MovimientoJugador : MonoBehaviour
{
    public float velocidad = 5f;
    public float sensibilidadRaton = 2f;
    public float gravedad = -9.81f;

    [Header("Camera Bob")]
    public float bobFrecuencia = 10f;
    public float bobAmplitud = 0.05f;

    private CharacterController controller;
    private Vector3 velocidadVertical;
    private float rotacionX = 0f;
    private Camera camara;
    private Animator animador;

    // Bob
    private float bobTimer = 0f;
    private float camYInicial;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        camara = GetComponentInChildren<Camera>();
        animador = GetComponentInChildren<Animator>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        camYInicial = 1.6f;
    }

    void Update()
    {
        // --- MOVIMIENTO WASD ---
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 movimiento = transform.right * x + transform.forward * z;
        controller.Move(movimiento * velocidad * Time.deltaTime);

        // --- GRAVEDAD ---
        if (controller.isGrounded && velocidadVertical.y < 0)
            velocidadVertical.y = -2f;
        velocidadVertical.y += gravedad * Time.deltaTime;
        controller.Move(velocidadVertical * Time.deltaTime);

        // --- ANIMACION ---
        float speed = new Vector2(x, z).magnitude;
        if (animador != null)
            animador.SetFloat("Velocidad", speed);

        // --- CAMERA BOB ---
        if (speed > 0.1f)
        {
            bobTimer += Time.deltaTime * bobFrecuencia;
            float bobOffset = Mathf.Sin(bobTimer) * bobAmplitud;
            camara.transform.localPosition = new Vector3(
                camara.transform.localPosition.x,
                camYInicial + bobOffset,
                camara.transform.localPosition.z
            );
        }
        else
        {
            bobTimer = 0f;
            camara.transform.localPosition = new Vector3(
                camara.transform.localPosition.x,
                Mathf.Lerp(camara.transform.localPosition.y, camYInicial, Time.deltaTime * 5f),
                camara.transform.localPosition.z
            );
        }

        // --- MIRAR CON EL RATON ---
        float mouseX = Input.GetAxis("Mouse X") * sensibilidadRaton;
        float mouseY = Input.GetAxis("Mouse Y") * sensibilidadRaton;
        rotacionX -= mouseY;
        rotacionX = Mathf.Clamp(rotacionX, -80f, 80f);
        camara.transform.localRotation = Quaternion.Euler(rotacionX, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);

        // --- DESBLOQUEAR CURSOR CON ESCAPE ---
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}