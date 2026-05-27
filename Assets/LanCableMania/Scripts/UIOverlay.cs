using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

// Renders the HUD and wrong connection screen flashes using IMGUI.
public class UIOverlay : MonoBehaviour {

    public static UIOverlay Instance { get; private set; }

    public struct FloatingText {
        public string text;
        public Vector2 pos;
        public float alpha;
        public float timer;
        public Color color;
    }

    private List<FloatingText> floatingTexts = new List<FloatingText>();
    private float wrongConnectionTimer = 0f;
    private float transitionTimer = 0f;
    private Camera mainCamera;

    private Texture2D _panelTex;
    private Texture2D _leftTex;
    private Texture2D _overlayTex;
    private Texture2D _barBgTex;
    private Texture2D _speedBarTex;
    private Texture2D _warnTex;

    private GUIStyle _bigStyle;
    private GUIStyle _midStyle;
    private GUIStyle _smallStyle;
    private GUIStyle _floatStyle;
    private GUIStyle _warnStyle;
    private GUIStyle _dotStyle;

    private float _warnAlpha = 0f;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
            return;
        }
    }

    private void Start() {
        mainCamera = Camera.main;

        _panelTex = MakeTex(new Color(0f, 0.01f, 0.03f, 0.7f));
        _leftTex = MakeTex(new Color(0f, 0f, 0f, 0.55f));
        _overlayTex = MakeTex(new Color(0f, 0f, 0.02f, 1f));
        _barBgTex = MakeTex(new Color(0.05f, 0.05f, 0.08f, 0.8f));
        _speedBarTex = MakeTex(Color.cyan);
        _warnTex = MakeTex(new Color(0.8f, 0.05f, 0.05f, 1f));

        _bigStyle = new GUIStyle();
        _bigStyle.fontSize = 28;
        _bigStyle.fontStyle = FontStyle.Bold;
        _bigStyle.normal.textColor = Color.white;
        _bigStyle.alignment = TextAnchor.MiddleLeft;

        _midStyle = new GUIStyle();
        _midStyle.fontSize = 20;
        _midStyle.fontStyle = FontStyle.Bold;
        _midStyle.normal.textColor = Color.cyan;
        _midStyle.alignment = TextAnchor.MiddleCenter;

        _smallStyle = new GUIStyle();
        _smallStyle.fontSize = 16;
        _smallStyle.fontStyle = FontStyle.Normal;
        _smallStyle.normal.textColor = Color.gray;
        _smallStyle.alignment = TextAnchor.MiddleLeft;

        _floatStyle = new GUIStyle();
        _floatStyle.fontSize = 15;
        _floatStyle.fontStyle = FontStyle.Normal;
        _floatStyle.normal.textColor = Color.yellow;
        _floatStyle.alignment = TextAnchor.MiddleCenter;

        _warnStyle = new GUIStyle();
        _warnStyle.fontSize = 36;
        _warnStyle.fontStyle = FontStyle.Bold;
        _warnStyle.normal.textColor = Color.red;
        _warnStyle.alignment = TextAnchor.MiddleCenter;

        _dotStyle = new GUIStyle();
        _dotStyle.fontSize = 22;
        _dotStyle.fontStyle = FontStyle.Normal;
        _dotStyle.normal.textColor = Color.white;
        _dotStyle.alignment = TextAnchor.MiddleCenter;
    }

    private Texture2D MakeTex(Color c) {
        Texture2D t = new Texture2D(1, 1);
        t.SetPixel(0, 0, c);
        t.Apply();
        return t;
    }

    public void SpawnFloatingText(Vector3 worldPos) {
        if (mainCamera == null) {
            mainCamera = Camera.main;
        }
        if (mainCamera == null) {
            return;
        }

        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);
        if (screenPos.z < 0) {
            return;
        }

        Vector2 guiPos = new Vector2(screenPos.x, Screen.height - screenPos.y);

        int combo = SignalController.Instance != null ? SignalController.Instance.CurrentCombo : 1;
        Color col = Color.cyan;
        if (combo >= 5) {
            col = Color.red;
        } else if (combo >= 3) {
            col = Color.yellow;
        }

        FloatingText ft = new FloatingText {
            text = "+" + combo,
            pos = guiPos,
            alpha = 1f,
            timer = 0.5f,
            color = col
        };

        floatingTexts.Add(ft);
    }

    public void ShowWrongConnectionText() {
        wrongConnectionTimer = 0.8f;
    }

    private void Update() {
        if (wrongConnectionTimer > 0f) {
            wrongConnectionTimer -= Time.deltaTime;
            if (wrongConnectionTimer < 0f) {
                wrongConnectionTimer = 0f;
            }
        }

        if (GameManager.Instance != null && GameManager.Instance.IsRoundTransitioning) {
            transitionTimer += Time.deltaTime;
        } else {
            transitionTimer = 0f;
        }

        for (int i = floatingTexts.Count - 1; i >= 0; i--) {
            FloatingText ft = floatingTexts[i];
            ft.timer -= Time.deltaTime;
            if (ft.timer <= 0f) {
                floatingTexts.RemoveAt(i);
            } else {
                ft.pos.y -= 60f * Time.deltaTime;
                ft.alpha = Mathf.Clamp01(ft.timer / 0.5f);
                floatingTexts[i] = ft;
            }
        }
    }

    private void OnGUI() {
        if (GameManager.Instance == null) {
            return;
        }

        float sw = Screen.width;
        float sh = Screen.height;

        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(0, 0, sw, 80), _panelTex);

        _bigStyle.alignment = TextAnchor.MiddleLeft;
        GUI.Label(new Rect(20, 5, 200, 35), "PUNTOS: " + GameManager.Instance.RoundScore, _bigStyle);

        string stateStr = GameManager.Instance.State.ToString().ToUpper();
        if (GameManager.Instance.State == GameManager.GameState.PlacingCables) {
            stateStr = "ROTANDO CABLES";
        } else if (GameManager.Instance.State == GameManager.GameState.SignalRunning) {
            stateStr = "SEÑAL EN CURSO";
        } else if (GameManager.Instance.State == GameManager.GameState.WaitingToStart) {
            stateStr = "LISTO... [ESPACIO]";
        }
        _midStyle.alignment = TextAnchor.MiddleCenter;
        _midStyle.normal.textColor = Color.cyan;
        GUI.Label(new Rect(sw / 2 - 150, 5, 300, 35), stateStr, _midStyle);

        _midStyle.alignment = TextAnchor.MiddleRight;
        _midStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(sw - 180, 5, 150, 35), "RONDA " + GameManager.Instance.CurrentRound, _midStyle);

        _smallStyle.alignment = TextAnchor.MiddleRight;
        _smallStyle.normal.textColor = Color.gray;
        GUI.Label(new Rect(sw - 180, 42, 150, 28), "TOTAL: " + GameManager.Instance.TotalScore, _smallStyle);

        if (GameManager.Instance.State == GameManager.GameState.PlacingCables) {
            float timeLeft = GameManager.Instance.FuseTimer;
            float maxTime = GameManager.Instance.FuseDelay;
            GUIStyle fuseStyle = new GUIStyle(_midStyle);
            fuseStyle.alignment = TextAnchor.MiddleCenter;

            if (timeLeft < 3.0f) {
                float pulse = Mathf.Sin(Time.time * 6f) * 4f;
                fuseStyle.fontSize = Mathf.RoundToInt(24f + pulse);
                fuseStyle.normal.textColor = Color.Lerp(Color.white, Color.red, (3f - timeLeft) / 3f);
            } else {
                fuseStyle.fontSize = Mathf.RoundToInt(Mathf.Lerp(18f, 36f, 1f - timeLeft / maxTime));
                fuseStyle.normal.textColor = Color.white;
            }

            GUI.Label(new Rect(sw / 2 - 120, 90, 240, 50), "FUSIBLE: " + timeLeft.ToString("F1", CultureInfo.InvariantCulture) + "s", fuseStyle);
        }

        if (SignalController.Instance != null) {
            float minSpeed = 0.8f;
            float maxSpeed = 3.5f;
            float currentSpeed = GameManager.Instance.SignalSpeed;
            float fillRatio = Mathf.Clamp01((currentSpeed - minSpeed) / (maxSpeed - minSpeed));

            _smallStyle.alignment = TextAnchor.MiddleCenter;
            _smallStyle.normal.textColor = Color.white;
            _smallStyle.fontStyle = FontStyle.Bold;
            GUI.Label(new Rect(sw - 82, sh / 2 - 100, 70, 20), "VELOCIDAD", _smallStyle);

            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(sw - 62, sh / 2 - 80, 30, 160), _barBgTex);

            float barH = 160f * fillRatio;
            float barY = (sh / 2 - 80) + (160f - barH);

            _speedBarTex.SetPixel(0, 0, SignalController.Instance.CurrentSpeedColor);
            _speedBarTex.Apply();

            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(sw - 62, barY, 30, barH), _speedBarTex);

            _smallStyle.alignment = TextAnchor.MiddleCenter;
            _smallStyle.fontStyle = FontStyle.Normal;
            GUI.Label(new Rect(sw - 72, sh / 2 + 85, 50, 20), currentSpeed.ToString("F1", CultureInfo.InvariantCulture), _smallStyle);
        }

        _floatStyle.alignment = TextAnchor.MiddleCenter;
        foreach (var ft in floatingTexts) {
            GUI.color = new Color(1f, 1f, 1f, ft.alpha);
            _floatStyle.normal.textColor = ft.color;
            GUI.Label(new Rect(ft.pos.x - 25, ft.pos.y - 12, 50, 25), ft.text, _floatStyle);
        }
        GUI.color = Color.white;

        if (wrongConnectionTimer > 0f) {
            float alpha = wrongConnectionTimer / 0.8f;
            GUIStyle wrongStyle = new GUIStyle(_midStyle);
            wrongStyle.alignment = TextAnchor.MiddleCenter;
            wrongStyle.fontSize = 32;
            wrongStyle.normal.textColor = new Color(1f, 0f, 0f, alpha);
            GUI.Label(new Rect(sw / 2 - 300, sh / 2 - 50, 600, 50), "¡CONEXIÓN INCORRECTA!", wrongStyle);
        }

        if (GameManager.Instance.IsRoundTransitioning) {
            GUI.color = new Color(0f, 0f, 0.05f, GameManager.Instance.RoundTransitionAlpha);
            GUI.DrawTexture(new Rect(0, 0, sw, sh), _overlayTex);

            GUI.color = Color.white;
            float scaleT = Mathf.Clamp01(transitionTimer / 0.3f);
            GUIStyle completeStyle = new GUIStyle(_midStyle);
            completeStyle.alignment = TextAnchor.MiddleCenter;
            completeStyle.fontSize = Mathf.RoundToInt(Mathf.Lerp(10f, 48f, scaleT));
            completeStyle.fontStyle = FontStyle.Bold;
            completeStyle.normal.textColor = Color.white;

            GUI.Label(new Rect(sw / 2 - 400, sh / 2 - 40, 800, 60), "¡RONDA " + (GameManager.Instance.CurrentRound - 1) + " COMPLETADA!", completeStyle);

            GUIStyle pointsStyle = new GUIStyle(_midStyle);
            pointsStyle.alignment = TextAnchor.MiddleCenter;
            pointsStyle.fontSize = 24;
            pointsStyle.normal.textColor = Color.cyan;
            GUI.Label(new Rect(sw / 2 - 200, sh / 2 + 30, 400, 40), "+" + GameManager.Instance.RoundScore + " pts", pointsStyle);
        }

        if (GameManager.Instance.State == GameManager.GameState.GameOver) {
            GUI.color = new Color(0f, 0f, 0.02f, 0.75f);
            GUI.DrawTexture(new Rect(0, 0, sw, sh), _overlayTex);

            GUI.color = Color.white;
            GUIStyle goTitleStyle = new GUIStyle(_midStyle);
            goTitleStyle.alignment = TextAnchor.MiddleCenter;
            goTitleStyle.fontSize = 42;
            goTitleStyle.normal.textColor = Color.red;
            goTitleStyle.fontStyle = FontStyle.Bold;
            GUI.Label(new Rect(sw / 2 - 200, sh / 2 - 60, 400, 70), "SEÑAL PERDIDA", goTitleStyle);

            GUIStyle goScoreStyle = new GUIStyle(_midStyle);
            goScoreStyle.alignment = TextAnchor.MiddleCenter;
            goScoreStyle.fontSize = 20;
            goScoreStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(sw / 2 - 200, sh / 2 + 10, 400, 30), "PUNTOS: " + GameManager.Instance.RoundScore, goScoreStyle);
            GUI.Label(new Rect(sw / 2 - 200, sh / 2 + 40, 400, 30), "TOTAL: " + GameManager.Instance.TotalScore, goScoreStyle);

            GUIStyle goRestartStyle = new GUIStyle(_midStyle);
            goRestartStyle.alignment = TextAnchor.MiddleCenter;
            goRestartStyle.fontSize = 18;
            goRestartStyle.normal.textColor = Color.yellow;
            GUI.Label(new Rect(sw / 2 - 200, sh / 2 + 80, 400, 30), "[R] Reiniciar", goRestartStyle);
        }

        if (GameManager.Instance.State == GameManager.GameState.WaitingToStart || GameManager.Instance.State == GameManager.GameState.PlacingCables) {
            _smallStyle.alignment = TextAnchor.MiddleCenter;
            _smallStyle.normal.textColor = Color.gray;
            _smallStyle.fontStyle = FontStyle.Normal;
            GUI.Label(new Rect(0, sh - 35, sw, 28), "[CLICK IZQUIERDO] Rotar  |  [ESPACIO] Iniciar Señal  |  [R] Reiniciar", _smallStyle);
        }

        if (_warnAlpha > 0f) {
            GUI.color = new Color(1f, 1f, 1f, _warnAlpha);
            GUI.DrawTexture(new Rect(0, 0, sw, sh), _warnTex);
            GUI.Label(new Rect(sw / 2 - 220, sh / 2 - 35, 440, 70), "⚠  SIGNAL CORRUPTED!", _warnStyle);
            GUI.color = Color.white;
        }

        if (WormController.Instance != null && WormController.Instance.State != WormController.WormState.Idle) {
            float pulse = (Mathf.Sin(Time.time * 8f) + 1f) * 0.5f;
            GUI.color = new Color(0.1f, 0.9f, 0.2f, 0.5f + pulse * 0.5f);
            GUI.Label(new Rect(sw - 35, sh - 32, 28, 28), "●", _dotStyle);
            GUI.color = Color.white;
        }

        GUI.color = Color.white;
    }

    public void ShowWarning(float duration) {
        StartCoroutine(CorruptionFlash(duration));
    }

    private IEnumerator CorruptionFlash(float duration) {
        float t = 0f;
        while (t < duration * 0.3f) {
            _warnAlpha = Mathf.Lerp(0f, 0.55f, t / (duration * 0.3f));
            t += Time.deltaTime;
            yield return null;
        }
        t = 0f;
        while (t < duration * 0.7f) {
            _warnAlpha = Mathf.Lerp(0.55f, 0f, t / (duration * 0.7f));
            t += Time.deltaTime;
            yield return null;
        }
        _warnAlpha = 0f;
    }
}
