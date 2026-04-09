using UnityEngine;

public class MovimientoJugador : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidad = 5f;
    public float sensibilidadRaton = 2f;
    public float gravedad = -9.81f;

    [Header("Salto")]
    public float fuerzaSalto = 4f;

    [Header("Ground Check")]
    [Tooltip("Radio de la esfera de deteccion de suelo (debe ser <= controller.radius)")]
    public float groundCheckRadius = 0.45f;
    [Tooltip("Capas que se consideran suelo. Asegurate de incluir la capa Default.")]
    public LayerMask groundMask = -1;

    [Header("Camera Bob (solo 1a persona)")]
    public float bobFrecuencia = 10f;
    public float bobAmplitud   = 0.05f;

    [Header("Perspectiva")]
    public KeyCode teclaCambiarVista = KeyCode.V;
    public float distanciaTercera = 3f;
    public float alturaTercera    = 1.5f;

    private CharacterController controller;
    private Camera camara;
    private Animator animador;
    private Renderer[] renderizadoresPersonaje;

    private Vector3 velocidadVertical;
    private float rotacionX = 0f;
    private float bobTimer  = 0f;
    private const float CAM_Y_PRIMERA = 1.6f;
    private bool primeraPersona = true;

    // Evita multiples saltos si el boton se mantiene pulsado
    private bool saltoPendiente = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        camara     = GetComponentInChildren<Camera>();
        animador   = GetComponentInChildren<Animator>();

        renderizadoresPersonaje = animador != null
            ? animador.GetComponentsInChildren<Renderer>()
            : new Renderer[0];

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        AplicarPerspectiva();
    }

    void Update()
    {
        // Capturar input de salto en Update (mas responsivo que FixedUpdate)
        if (Input.GetButtonDown("Jump"))
            saltoPendiente = true;

        ManejarMovimiento();
        ManejarSaltoYGravedad();
        ManejarAnimacion();
        ManejarCameraBob();
        ManejarRaton();
        ManejarCambioVista();
        ManejarCursor();
    }

    // -----------------------------------------------------------------------
    // GROUND CHECK — CheckSphere en la base de la capsula
    // Mas fiable que controller.isGrounded, funciona incluso en reposo.
    // -----------------------------------------------------------------------
    bool IsGrounded()
    {
        // Centro de la semiesfera inferior del CharacterController
        // = posicion del objeto + Vector3.up * radio (el pie de la capsula)
        Vector3 sphereCenter = transform.position
                               + Vector3.up * controller.radius;

        return Physics.CheckSphere(
            sphereCenter,
            groundCheckRadius,
            groundMask,
            QueryTriggerInteraction.Ignore);
    }

    // -----------------------------------------------------------------------
    // MOVIMIENTO HORIZONTAL
    // -----------------------------------------------------------------------
    void ManejarMovimiento()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        controller.Move(
            (transform.right * x + transform.forward * z) * velocidad * Time.deltaTime);
    }

    // -----------------------------------------------------------------------
    // SALTO Y GRAVEDAD
    // -----------------------------------------------------------------------
    void ManejarSaltoYGravedad()
    {
        bool grounded = IsGrounded();

        if (grounded)
        {
            // Fija una velocidad Y negativa pequeña para mantener contacto con el suelo
            if (velocidadVertical.y < 0f)
                velocidadVertical.y = -2f;

            // Aplicar salto si habia una pulsacion pendiente
            if (saltoPendiente)
                velocidadVertical.y = Mathf.Sqrt(fuerzaSalto * -2f * gravedad);
        }

        // Limpiar siempre la bandera de salto (evita acumulacion)
        saltoPendiente = false;

        // Gravedad acumulativa
        velocidadVertical.y += gravedad * Time.deltaTime;
        controller.Move(velocidadVertical * Time.deltaTime);
    }

    // -----------------------------------------------------------------------
    // ANIMACION
    // -----------------------------------------------------------------------
    void ManejarAnimacion()
    {
        float rawX     = Input.GetAxisRaw("Horizontal");
        float rawZ     = Input.GetAxisRaw("Vertical");
        float animSpeed = new Vector2(rawX, rawZ).magnitude;
        if (animador != null)
            animador.SetFloat("Velocidad", animSpeed, 0.1f, Time.deltaTime);
    }

    // -----------------------------------------------------------------------
    // CAMERA BOB (solo primera persona)
    // -----------------------------------------------------------------------
    void ManejarCameraBob()
    {
        if (!primeraPersona) return;

        float speed = new Vector2(
            Input.GetAxis("Horizontal"),
            Input.GetAxis("Vertical")).magnitude;

        float targetY;
        if (speed > 0.1f)
        {
            bobTimer += Time.deltaTime * bobFrecuencia;
            targetY   = CAM_Y_PRIMERA + Mathf.Sin(bobTimer) * bobAmplitud;
        }
        else
        {
            bobTimer = 0f;
            targetY  = CAM_Y_PRIMERA;
        }

        float newY = Mathf.Lerp(
            camara.transform.localPosition.y, targetY, Time.deltaTime * 10f);
        camara.transform.localPosition = new Vector3(0f, newY, 0f);
    }

    // -----------------------------------------------------------------------
    // CAMARA CON EL RATON
    // -----------------------------------------------------------------------
    void ManejarRaton()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensibilidadRaton;
        float mouseY = Input.GetAxis("Mouse Y") * sensibilidadRaton;

        rotacionX -= mouseY;
        rotacionX  = Mathf.Clamp(rotacionX, -80f, 80f);

        float clampedX = primeraPersona
            ? rotacionX
            : Mathf.Clamp(rotacionX, -30f, 60f);

        camara.transform.localRotation = Quaternion.Euler(clampedX, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    // -----------------------------------------------------------------------
    // CAMBIO DE PERSPECTIVA (tecla V)
    // -----------------------------------------------------------------------
    void ManejarCambioVista()
    {
        if (Input.GetKeyDown(teclaCambiarVista))
        {
            primeraPersona = !primeraPersona;
            AplicarPerspectiva();
        }
    }

    void AplicarPerspectiva()
    {
        if (primeraPersona)
        {
            camara.transform.localPosition = new Vector3(0f, CAM_Y_PRIMERA, 0f);
            foreach (var r in renderizadoresPersonaje) r.enabled = false;
        }
        else
        {
            camara.transform.localPosition =
                new Vector3(0f, alturaTercera, -distanciaTercera);
            foreach (var r in renderizadoresPersonaje) r.enabled = true;
        }
    }

    // -----------------------------------------------------------------------
    // CURSOR
    // -----------------------------------------------------------------------
    void ManejarCursor()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }
    }

#if UNITY_EDITOR
    // Dibuja el ground-check sphere en el Editor para facilitar el debug
    void OnDrawGizmosSelected()
    {
        if (controller == null) return;
        Gizmos.color = IsGrounded() ? Color.green : Color.red;
        Gizmos.DrawWireSphere(
            transform.position + Vector3.up * controller.radius,
            groundCheckRadius);
    }
#endif
}
