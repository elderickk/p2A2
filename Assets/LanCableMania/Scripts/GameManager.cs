using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Controls overall game state transitions, rounds, and scoring.
public class GameManager : MonoBehaviour {

    public static GameManager Instance { get; private set; }

    public enum GameState {
        WaitingToStart,
        PlacingCables,
        SignalRunning,
        GameOver
    }

    [Header("Base Settings")]
    [SerializeField] private float signalSpeed = 1.0f;
    [SerializeField] private float fuseDelay = 5.0f;
    [SerializeField] private float shakeIntensity = 0.5f;
    [SerializeField] private int sparkCount = 40;
    [SerializeField] private float curveStrength = 0.5f;

    [Header("Patch v2 Polish Settings")]
    [SerializeField] private bool enableRoundProgression = true;
    [SerializeField] private int startingRound = 1;
    [SerializeField] private float wrongPlacementShakeIntensity = 0.25f;
    [SerializeField] private float fuseWarningThreshold = 3.0f;
    [SerializeField] private float celebrationParticleMultiplier = 1.0f;

    public struct RoundData {
        public int gridSize;
        public float fuseDelay;
        public float signalSpeed;

        public RoundData(int size, float delay, float speed) {
            gridSize = size;
            fuseDelay = delay;
            signalSpeed = speed;
        }
    }

    private static readonly RoundData[] RoundsTable = new RoundData[] {
        new RoundData(8, 26.0f, 0.56f),
        new RoundData(9, 23.0f, 0.7f),
        new RoundData(10, 20.0f, 0.9f),
        new RoundData(11, 16.0f, 1.1f),
        new RoundData(12, 13.0f, 1.4f)
    };

    private GameState state = GameState.WaitingToStart;
    private int currentRound = 1;
    private int gridSize = 8;
    private int roundScore = 0;
    private int totalScore = 0;
    private float fuseTimer = 0f;
    private bool roundTransitioning = false;
    private float roundTransitionAlpha = 0f;

    public GameState State => state;
    public float SignalSpeed { get => signalSpeed; set => signalSpeed = value; }
    public float FuseDelay { get => fuseDelay; set => fuseDelay = value; }
    public float ShakeIntensity { get => shakeIntensity; set => shakeIntensity = value; }
    public int SparkCount { get => sparkCount; set => sparkCount = value; }
    public float CurveStrength { get => curveStrength; set => curveStrength = value; }
    public bool EnableRoundProgression { get => enableRoundProgression; set => enableRoundProgression = value; }
    public int StartingRound { get => startingRound; set => startingRound = value; }
    public float WrongPlacementShakeIntensity { get => wrongPlacementShakeIntensity; set => wrongPlacementShakeIntensity = value; }
    public float FuseWarningThreshold { get => fuseWarningThreshold; set => fuseWarningThreshold = value; }
    public float CelebrationParticleMultiplier { get => celebrationParticleMultiplier; set => celebrationParticleMultiplier = value; }

    public int CurrentRound => currentRound;
    public int RoundScore => roundScore;
    public int TotalScore => totalScore;
    public float FuseTimer => fuseTimer;
    public int StartingGridSize => GetRoundData(currentRound).gridSize;
    public bool IsRoundTransitioning => roundTransitioning;
    public float RoundTransitionAlpha => roundTransitionAlpha;

    private WormController _worm;
    private MathChallengeController _math;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
            return;
        }
    }

    private void Start() {
        _worm = GetComponent<WormController>();
        if (_worm == null) {
            _worm = FindAnyObjectByType<WormController>();
        }

        _math = GetComponent<MathChallengeController>();
        if (_math == null) {
            _math = FindAnyObjectByType<MathChallengeController>();
        }

        if (enableRoundProgression) {
            currentRound = startingRound;
            ApplyRoundData();
        }

        fuseTimer = fuseDelay;
    }

    private void Update() {
        switch (state) {
            case GameState.WaitingToStart:
                if (Input.GetKeyDown(KeyCode.Space)) {
                    state = GameState.PlacingCables;
                    fuseTimer = fuseDelay;
                    StartCoroutine(FuseWarningCoroutine());
                    if (_worm != null) {
                        _worm.SetLevel(currentRound);
                        _worm.StartWorm();
                    }
                }
                break;

            case GameState.PlacingCables:
                if (Input.GetKeyDown(KeyCode.Space)) {
                    StartSignal();
                } else {
                    fuseTimer -= Time.deltaTime;
                    if (fuseTimer <= 0f) {
                        fuseTimer = 0f;
                        StartSignal();
                    }
                }
                break;

            case GameState.SignalRunning:
                break;

            case GameState.GameOver:
                if (Input.GetKeyDown(KeyCode.R)) {
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                }
                break;
        }

        if (Input.GetKeyDown(KeyCode.R)) {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void StartSignal() {
        state = GameState.SignalRunning;
        if (_worm != null) {
            _worm.StopWorm();
        }
        if (SignalController.Instance != null) {
            SignalController.Instance.StartSignalTraversal();
        }
    }

    public void AddScore() {
        roundScore++;
    }

    public void AdvanceRound() {
        totalScore += roundScore;
        currentRound++;

        StartCoroutine(RoundClearSequence());
    }

    public void GameOver() {
        state = GameState.GameOver;
        if (_worm != null) {
            _worm.StopWorm();
        }

        if (ScreenShakeController.Instance != null) {
            ScreenShakeController.Instance.Shake(shakeIntensity * 1.2f, 0.6f);
            ScreenShakeController.Instance.FOVPunch(75f, 0.4f);
        }

        CablePiece[] pieces = FindObjectsByType<CablePiece>(FindObjectsSortMode.None);
        foreach (var piece in pieces) {
            piece.ShakeOnGameOver();
        }

        StartCoroutine(GameOverExplosionsCoroutine());
    }

    private IEnumerator GameOverExplosionsCoroutine() {
        yield return new WaitForSeconds(0.1f);

        CablePiece[] pieces = FindObjectsByType<CablePiece>(FindObjectsSortMode.None);
        foreach (var piece in pieces) {
            ParticleSpawner.SpawnExplosion(piece.transform.position, Color.red, 30);
            yield return new WaitForSeconds(0.05f);
        }
    }

    private IEnumerator FuseWarningCoroutine() {
        while (state == GameState.PlacingCables) {
            if (fuseTimer < fuseWarningThreshold) {
                if (ScreenShakeController.Instance != null) {
                    ScreenShakeController.Instance.FuseWarningPulse();
                }
                yield return new WaitForSeconds(0.5f);
            } else {
                yield return null;
            }
        }
    }

    private IEnumerator RoundClearSequence() {
        roundTransitioning = true;

        if (ScreenShakeController.Instance != null) {
            ScreenShakeController.Instance.CelebrationShake();
            ScreenShakeController.Instance.FOVPunch(72f, 0.3f);
        }

        int size = GridManager.Instance != null ? GridManager.Instance.GridSize : 8;
        ParticleSpawner.EmitCelebration(new Vector3(size / 2f, 0f, size / 2f), currentRound, size, celebrationParticleMultiplier);

        float elapsed = 0f;
        while (elapsed < 0.5f) {
            roundTransitionAlpha = Mathf.Lerp(0f, 0.7f, elapsed / 0.5f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        roundTransitionAlpha = 0.7f;

        yield return new WaitForSeconds(1.0f);

        if (enableRoundProgression) {
            ApplyRoundData();
        }

        if (GridManager.Instance != null) {
            GridManager.Instance.RebuildGrid(gridSize);
        }

        PathSolver.Instance.Reset();

        if (SignalController.Instance != null) {
            SignalController.Instance.ResetSignal();
        }

        roundScore = 0;
        fuseTimer = fuseDelay;
        state = GameState.PlacingCables;
        if (_worm != null) {
            _worm.SetLevel(currentRound);
            _worm.StartWorm();
        }
        StartCoroutine(FuseWarningCoroutine());

        elapsed = 0f;
        while (elapsed < 0.5f) {
            roundTransitionAlpha = Mathf.Lerp(0.7f, 0f, elapsed / 0.5f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        roundTransitionAlpha = 0f;

        roundTransitioning = false;
    }

    private void ApplyRoundData() {
        RoundData data = GetRoundData(currentRound);
        gridSize = data.gridSize;
        fuseDelay = data.fuseDelay;
        signalSpeed = data.signalSpeed;
    }

    private RoundData GetRoundData(int round) {
        if (round <= 5) {
            return RoundsTable[round - 1];
        } else {
            float extraSpeed = (round - 5) * 0.14f;
            return new RoundData(12, 4.0f, 1.4f + extraSpeed);
        }
    }

    private void OnGUI() {
        if (_worm != null && (_worm.State == WormController.WormState.Crawling || _worm.State == WormController.WormState.Corrupting)) {
            GUIStyle wormLabelStyle = new GUIStyle();
            wormLabelStyle.fontSize = 11;
            wormLabelStyle.fontStyle = FontStyle.Bold;
            float pulse = (Mathf.Sin(Time.time * 15f) + 1f) * 0.5f;
            wormLabelStyle.normal.textColor = new Color(0.1f, 0.9f, 0.2f, pulse);

            GUI.Label(new Rect(15, Screen.height - 65, 100, 20), "EL GUSANO", wormLabelStyle);
        }
    }
}
