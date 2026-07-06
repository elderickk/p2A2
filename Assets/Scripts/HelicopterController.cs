using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class HelicopterController : MonoBehaviour
{
    [Header("Motor y Sustentación (Elevación)")]
    [Tooltip("Fuerza ascendente máxima aplicada al helicóptero.")]
    [SerializeField] private float maxLiftForce = 15f;
    [Tooltip("Velocidad a la que se asciende o desciende.")]
    [SerializeField] private float climbSpeed = 5f;
    [Tooltip("Amortiguación vertical para simular resistencia del aire y estabilizar el hover.")]
    [SerializeField] private float verticalDamping = 2f;

    [Header("Movimiento Horizontal (WASD)")]
    [Tooltip("Velocidad de avance y retroceso (W/S).")]
    [SerializeField] private float forwardSpeed = 8f;
    [Tooltip("Velocidad de giro (A/D) sobre el eje vertical.")]
    [SerializeField] private float turnSpeed = 90f;
    [Tooltip("Amortiguación de arrastre horizontal.")]
    [SerializeField] private float horizontalDamping = 1.5f;

    [Header("Efectos Visuales de Inclinación")]
    [Tooltip("El objeto visual del helicóptero que se inclinará (debe ser un hijo del Rigidbody principal).")]
    [SerializeField] private Transform visualModel;
    [Tooltip("Ángulo máximo de inclinación hacia adelante/atrás (Pitch).")]
    [SerializeField] private float maxVisualPitch = 15f;
    [Tooltip("Ángulo máximo de inclinación lateral (Roll) al girar.")]
    [SerializeField] private float maxVisualRoll = 10f;
    [Tooltip("Velocidad de inclinación visual.")]
    [SerializeField] private float tiltSmoothSpeed = 5f;

    [Header("Rotores")]
    [Tooltip("Objeto que representa las palas del rotor principal.")]
    [SerializeField] private Transform mainRotor;
    [Tooltip("Objeto que representa las palas del rotor de cola.")]
    [SerializeField] private Transform tailRotor;
    [Tooltip("Velocidad de rotación máxima del rotor principal.")]
    [SerializeField] private float mainRotorSpeed = 1000f;
    [Tooltip("Velocidad de rotación máxima del rotor de cola.")]
    [SerializeField] private float tailRotorSpeed = 1500f;

    private Rigidbody rb;
    private float currentThrustInput = 0f;
    private float currentForwardInput = 0f;
    private float currentTurnInput = 0f;
    
    // Velocidad actual del rotor (0 a 1) para un despegue/apagado suave
    private float rotorRpmPercent = 1f;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // Configuraciones del Rigidbody para vuelo físico estable
        rb.useGravity = true;
        rb.drag = 0.5f;
        rb.angularDrag = 2.0f;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    private void Update()
    {
        // 1. Capturar Inputs
        // W/S para avanzar/retroceder
        currentForwardInput = Input.GetAxisRaw("Vertical"); // W = 1, S = -1
        
        // A/D para girar (Yaw)
        currentTurnInput = Input.GetAxis("Horizontal"); // A = -1, D = 1

        // Espacio para subir, Shift izquierdo para bajar
        if (Input.GetKey(KeyCode.Space))
        {
            currentThrustInput = 1f;
        }
        else if (Input.GetKey(KeyCode.LeftShift))
        {
            currentThrustInput = -1f;
        }
        else
        {
            currentThrustInput = 0f;
        }

        // Q/E alternativos para subir/bajar
        if (Input.GetKey(KeyCode.E)) currentThrustInput = 1f;
        if (Input.GetKey(KeyCode.Q)) currentThrustInput = -1f;

        // 2. Animar Rotores
        RotateRotors();

        // 3. Inclinar Visualmente el Helicóptero
        ApplyVisualTilt();
    }

    private void FixedUpdate()
    {
        // 1. Aplicar Fuerza de Sustentación (Vertical)
        // Calculamos la fuerza necesaria para flotar (hover) contrarrestando la gravedad
        float hoverForce = rb.mass * Physics.gravity.magnitude;
        float verticalForce = hoverForce;

        if (currentThrustInput > 0.1f)
        {
            // Subir
            verticalForce += currentThrustInput * maxLiftForce;
        }
        else if (currentThrustInput < -0.1f)
        {
            // Bajar
            verticalForce += currentThrustInput * maxLiftForce * 0.7f;
        }
        else
        {
            // Hover estable: Amortiguar velocidad vertical para evitar rebotes
            float localVerticalVelocity = transform.InverseTransformDirection(rb.velocity).y;
            verticalForce -= localVerticalVelocity * verticalDamping * rb.mass;
        }

        // Aplicamos la sustentación en la dirección 'Up' local del helicóptero (para que se deslice si está inclinado)
        rb.AddForce(transform.up * verticalForce, ForceMode.Force);

        // 2. Aplicar Movimiento de Avance/Retroceso (W/S)
        Vector3 forwardDir = transform.forward;
        // Evitamos que empuje hacia abajo si el helicóptero está inclinado
        forwardDir.y = 0f;
        forwardDir.Normalize();

        if (Mathf.Abs(currentForwardInput) > 0.1f)
        {
            rb.AddForce(forwardDir * (currentForwardInput * forwardSpeed * rb.mass), ForceMode.Force);
        }
        else
        {
            // Amortiguar velocidad horizontal si no hay input para detenerse gradualmente
            Vector3 localVel = transform.InverseTransformDirection(rb.velocity);
            localVel.z -= localVel.z * horizontalDamping * Time.fixedDeltaTime;
            rb.velocity = transform.TransformDirection(localVel);
        }

        // 3. Aplicar Rotación / Giro (A/D)
        if (Mathf.Abs(currentTurnInput) > 0.05f)
        {
            float yawForce = currentTurnInput * turnSpeed * Time.fixedDeltaTime;
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, yawForce, 0f));
        }
    }

    private void RotateRotors()
    {
        // Rampa suave para simular encendido
        rotorRpmPercent = Mathf.MoveTowards(rotorRpmPercent, 1f, Time.deltaTime * 0.5f);

        // Rotar rotor principal (alrededor de su eje Y local)
        if (mainRotor != null)
        {
            mainRotor.Rotate(Vector3.up * (mainRotorSpeed * rotorRpmPercent * Time.deltaTime), Space.Self);
        }

        // Rotar rotor de cola (alrededor de su eje X o Z local)
        if (tailRotor != null)
        {
            tailRotor.Rotate(Vector3.right * (tailRotorSpeed * rotorRpmPercent * Time.deltaTime), Space.Self);
        }
    }

    private void ApplyVisualTilt()
    {
        if (visualModel == null) return;

        // Inclinación hacia adelante/atrás (Pitch) en base al movimiento longitudinal
        float targetPitch = -currentForwardInput * maxVisualPitch;

        // Inclinación lateral (Roll) en base al giro (Yaw)
        float targetRoll = -currentTurnInput * maxVisualRoll;

        // Interpolación suave del modelo visual (hijo) independiente del Rigidbody físico
        Quaternion targetRotation = Quaternion.Euler(targetPitch, 0f, targetRoll);
        visualModel.localRotation = Quaternion.Slerp(visualModel.localRotation, targetRotation, Time.deltaTime * tiltSmoothSpeed);
    }
}
