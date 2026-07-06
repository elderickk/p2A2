using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class HelicopterController : MonoBehaviour
{
    [Header("Motor y Sustentación (Up Arrow)")]
    [Tooltip("Fuerza ascendente aplicada al helicóptero cuando las palas superan el umbral.")]
    [SerializeField] private float liftForce = 18f;
    [Tooltip("Umbral de velocidad de rotación de las palas para empezar a elevarse (0.0 a 1.0).")]
    [SerializeField] private float liftThreshold = 0.75f;
    [Tooltip("Velocidad de aceleración del rotor.")]
    [SerializeField] private float rotorAcceleration = 0.5f;
    [Tooltip("Velocidad de desaceleración del rotor al soltar la tecla.")]
    [SerializeField] private float rotorDeceleration = 0.4f;

    [Header("Movimiento Horizontal (WASD)")]
    [Tooltip("Velocidad de avance y retroceso (W/S).")]
    [SerializeField] private float forwardSpeed = 10f;
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
    [SerializeField] private float mainRotorSpeed = 1200f;
    [Tooltip("Velocidad de rotación máxima del rotor de cola.")]
    [SerializeField] private float tailRotorSpeed = 1600f;

    private Rigidbody rb;
    private float currentForwardInput = 0f;
    private float currentTurnInput = 0f;
    
    // Velocidad actual del rotor (0 a 1)
    private float rotorRpmPercent = 0f;

    // Propiedades públicas para lectura desde la UI u otros scripts
    public float RotorRpmPercent => rotorRpmPercent;
    public float LiftThreshold => liftThreshold;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // Configuraciones del Rigidbody para vuelo físico estable
        rb.useGravity = true;
        rb.linearDamping = 0.5f;
        rb.angularDamping = 2.0f;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        
        // Empezar con el rotor apagado
        rotorRpmPercent = 0f;
    }

    private void Update()
    {
        // 1. Capturar Inputs
        // W/S para avanzar/retroceder
        currentForwardInput = Input.GetAxisRaw("Vertical"); // W = 1, S = -1
        
        // A/D para girar (Yaw)
        currentTurnInput = Input.GetAxis("Horizontal"); // A = -1, D = 1

        // Flecha Arriba (Up Arrow) aumenta la velocidad de las aspas progresivamente
        if (Input.GetKey(KeyCode.UpArrow))
        {
            rotorRpmPercent = Mathf.MoveTowards(rotorRpmPercent, 1f, Time.deltaTime * rotorAcceleration);
        }
        else
        {
            // Al soltar la tecla, disminuye progresivamente la velocidad
            rotorRpmPercent = Mathf.MoveTowards(rotorRpmPercent, 0f, Time.deltaTime * rotorDeceleration);
        }

        // 2. Animar Rotores basados en las RPM actuales
        RotateRotors();

        // 3. Inclinar Visualmente el Helicóptero
        ApplyVisualTilt();
    }

    private void FixedUpdate()
    {
        // 1. Lógica de despegue y elevación en Y
        // Si el rotor supera el umbral de velocidad, se eleva
        if (rotorRpmPercent > liftThreshold)
        {
            // Fuerza para contrarrestar la gravedad + fuerza extra de ascenso
            float hoverForce = rb.mass * Physics.gravity.magnitude;
            
            // La fuerza de elevación escala con las RPM por encima del umbral
            float excessPercent = (rotorRpmPercent - liftThreshold) / (1f - liftThreshold);
            float appliedLift = hoverForce + (liftForce * excessPercent);

            rb.AddForce(Vector3.up * appliedLift, ForceMode.Force);
        }
        else
        {
            // Si el rotor está por debajo del umbral, no se aplica sustentación activa (cae por gravedad)
        }

        // 2. Aplicar Movimiento de Avance/Retroceso (W/S) con WASD
        // Solo nos movemos horizontalmente si tenemos cierta sustentación del rotor
        if (rotorRpmPercent > 0.3f && Mathf.Abs(currentForwardInput) > 0.1f)
        {
            Vector3 forwardDir = transform.forward;
            forwardDir.y = 0f;
            forwardDir.Normalize();

            // La fuerza de empuje horizontal es proporcional a las RPM del rotor
            float forceMultiplier = rotorRpmPercent; 
            rb.AddForce(forwardDir * (currentForwardInput * forwardSpeed * forceMultiplier * rb.mass), ForceMode.Force);
        }
        else
        {
            // Amortiguar velocidad horizontal si no hay input
            Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
            localVel.z -= localVel.z * horizontalDamping * Time.fixedDeltaTime;
            rb.linearVelocity = transform.TransformDirection(localVel);
        }

        // 3. Aplicar Rotación / Giro (A/D)
        // Solo podemos girar si el rotor está girando (tiene potencia)
        if (rotorRpmPercent > 0.15f && Mathf.Abs(currentTurnInput) > 0.05f)
        {
            float yawForce = currentTurnInput * turnSpeed * rotorRpmPercent * Time.fixedDeltaTime;
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, yawForce, 0f));
        }
    }

    private void RotateRotors()
    {
        // Rotar rotor principal (alrededor de su eje Y local)
        if (mainRotor != null)
        {
            mainRotor.Rotate(Vector3.up * (mainRotorSpeed * rotorRpmPercent * Time.deltaTime), Space.Self);
        }

        // Rotar rotor de cola (alrededor de su eje X local)
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

        // Limitar inclinación si no estamos volando
        if (rotorRpmPercent < liftThreshold)
        {
            targetPitch *= (rotorRpmPercent / liftThreshold);
            targetRoll *= (rotorRpmPercent / liftThreshold);
        }

        // Interpolación suave del modelo visual
        Quaternion targetRotation = Quaternion.Euler(targetPitch, 0f, targetRoll);
        visualModel.localRotation = Quaternion.Slerp(visualModel.localRotation, targetRotation, Time.deltaTime * tiltSmoothSpeed);
    }
}
