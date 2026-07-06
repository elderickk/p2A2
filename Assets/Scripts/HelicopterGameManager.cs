using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HelicopterGameManager : MonoBehaviour
{
    public static HelicopterGameManager Instance { get; private set; }

    [Header("UI Text References")]
    [Tooltip("Componente de Texto de la UI para mostrar el puntaje.")]
    [SerializeField] private Text scoreText;
    [Tooltip("Componente de Texto de la UI para mostrar la altitud.")]
    [SerializeField] private Text altitudeText;
    [Tooltip("Componente de Texto de la UI para mostrar la velocidad.")]
    [SerializeField] private Text speedText;
    [Tooltip("Componente de Texto de la UI para mostrar mensajes del juego (Ganaste/Perdiste).")]
    [SerializeField] private Text messageText;

    [Header("Helicóptero")]
    [Tooltip("El helicóptero en juego.")]
    [SerializeField] private HelicopterController helicopter;

    private int score = 0;
    private int totalRings = 0;
    private int ringsCollected = 0;
    private bool gameFinished = false;

    public Text ScoreText { get => scoreText; set => scoreText = value; }
    public Text AltitudeText { get => altitudeText; set => altitudeText = value; }
    public Text SpeedText { get => speedText; set => speedText = value; }
    public Text MessageText { get => messageText; set => messageText = value; }
    public HelicopterController Helicopter { get => helicopter; set => helicopter = value; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Contar el total de anillos en la escena
        totalRings = FindObjectsByType<HelicopterRing>(FindObjectsSortMode.None).Length;
        UpdateUI();
        
        if (messageText != null)
        {
            messageText.text = "¡Recolecta todos los anillos y aterriza!";
            Invoke(nameof(ClearStartMessage), 4f);
        }
    }

    private void Update()
    {
        // Actualizar datos en tiempo real
        if (helicopter != null && !gameFinished)
        {
            Rigidbody rb = helicopter.GetComponent<Rigidbody>();
            if (rb != null)
            {
                float speed = rb.linearVelocity.magnitude * 3.6f; // m/s a km/h
                float altitude = helicopter.transform.position.y;
                float rpmPercent = helicopter.RotorRpmPercent * 100f;
                float thresholdPercent = helicopter.LiftThreshold * 100f;
                string status = rpmPercent >= thresholdPercent ? "DESPEGUE/ASCENSO" : "INSUFICIENTE (PRESIONA ARROW UP)";

                if (speedText != null) speedText.text = $"Velocidad: {speed:F1} km/h";
                if (altitudeText != null) altitudeText.text = $"Altitud: {altitude:F1} m";
                
                if (scoreText != null)
                {
                    scoreText.text = $"Puntos: {score} | Anillos: {ringsCollected}/{totalRings}\nAspas: {rpmPercent:F0}% (Umbral: {thresholdPercent:F0}%) [{status}]";
                }
            }
        }

        // Reiniciar escena con R si es necesario
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    public void AddScore(int amount)
    {
        score += amount;
        ringsCollected++;
        UpdateUI();

        if (ringsCollected >= totalRings)
        {
            ShowMessage("¡Todos los anillos recolectados! Aterriza en la plataforma H.");
        }
    }

    public void CompleteGame()
    {
        if (ringsCollected >= totalRings)
        {
            gameFinished = true;
            ShowMessage("¡FELICIDADES! ¡Misión de Vuelo Completada con Éxito!\nPresiona [R] para reiniciar.");
        }
        else
        {
            ShowMessage($"Faltan anillos: {ringsCollected}/{totalRings} recolectados.");
            Invoke(nameof(ClearMessage), 3f);
        }
    }

    private void UpdateUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Puntos: {score} | Anillos: {ringsCollected}/{totalRings}";
        }
    }

    private void ClearStartMessage()
    {
        if (messageText != null && messageText.text == "¡Recolecta todos los anillos y aterriza!")
        {
            messageText.text = "";
        }
    }

    private void ClearMessage()
    {
        if (messageText != null && !gameFinished)
        {
            messageText.text = "";
        }
    }

    public void ShowMessage(string message)
    {
        if (messageText != null)
        {
            messageText.text = message;
        }
    }
}
