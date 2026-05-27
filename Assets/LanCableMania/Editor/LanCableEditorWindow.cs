using UnityEditor;
using UnityEngine;

// Custom editor control panel window to build/clear the scene and adjust settings.
public class LanCableEditorWindow : EditorWindow {

    private float signalSpeed = 0.7f;
    private float fuseDelay = 6.5f;
    private float shakeIntensity = 0.5f;
    private int sparkCount = 40;
    private float curveStrength = 0.5f;

    private bool enableRoundProgression = true;
    private int startingRound = 1;
    private float wrongPlacementShakeIntensity = 0.25f;
    private float fuseWarningThreshold = 3.0f;
    private float celebrationParticleMultiplier = 1.0f;

    private bool enableWorm = true;
    private float wormBaseInterval = 5.2f;
    private float wormMinInterval = 2.0f;
    private float wormSpeed = 4.0f;
    private bool wormShowTrail = true;

    [MenuItem("Tools/LAN Cable Mania/Control Panel")]
    public static void ShowWindow() {
        GetWindow<LanCableEditorWindow>("LCM Control Panel");
    }

    private void OnEnable() {
        LoadSettings();
    }

    private void LoadSettings() {
        signalSpeed = EditorPrefs.GetFloat("LCM_SignalSpeed", 0.7f);
        fuseDelay = EditorPrefs.GetFloat("LCM_FuseDelay", 6.5f);
        shakeIntensity = EditorPrefs.GetFloat("LCM_ScreenShakeIntensity", 0.5f);
        sparkCount = EditorPrefs.GetInt("LCM_SparkCount", 40);
        curveStrength = EditorPrefs.GetFloat("LCM_CableCurveStrength", 0.5f);

        enableRoundProgression = EditorPrefs.GetBool("LCM_EnableRoundProgression", true);
        startingRound = EditorPrefs.GetInt("LCM_StartingRound", 1);
        wrongPlacementShakeIntensity = EditorPrefs.GetFloat("LCM_WrongPlacementShakeIntensity", 0.25f);
        fuseWarningThreshold = EditorPrefs.GetFloat("LCM_FuseWarningThreshold", 3.0f);
        celebrationParticleMultiplier = EditorPrefs.GetFloat("LCM_CelebrationParticleMultiplier", 1.0f);

        enableWorm = EditorPrefs.GetBool("LCM_EnableWorm", true);
        wormBaseInterval = EditorPrefs.GetFloat("LCM_WormBaseInterval", 5.2f);
        wormMinInterval = EditorPrefs.GetFloat("LCM_WormMinInterval", 2.0f);
        wormSpeed = EditorPrefs.GetFloat("LCM_WormSpeed", 4.0f);
        wormShowTrail = EditorPrefs.GetBool("LCM_WormShowTrail", true);
    }

    private void SaveSettings() {
        EditorPrefs.SetFloat("LCM_SignalSpeed", signalSpeed);
        EditorPrefs.SetFloat("LCM_FuseDelay", fuseDelay);
        EditorPrefs.SetFloat("LCM_ScreenShakeIntensity", shakeIntensity);
        EditorPrefs.SetInt("LCM_SparkCount", sparkCount);
        EditorPrefs.SetFloat("LCM_CableCurveStrength", curveStrength);

        EditorPrefs.SetBool("LCM_EnableRoundProgression", enableRoundProgression);
        EditorPrefs.SetInt("LCM_StartingRound", startingRound);
        EditorPrefs.SetFloat("LCM_WrongPlacementShakeIntensity", wrongPlacementShakeIntensity);
        EditorPrefs.SetFloat("LCM_FuseWarningThreshold", fuseWarningThreshold);
        EditorPrefs.SetFloat("LCM_CelebrationParticleMultiplier", celebrationParticleMultiplier);

        EditorPrefs.SetBool("LCM_EnableWorm", enableWorm);
        EditorPrefs.SetFloat("LCM_WormBaseInterval", wormBaseInterval);
        EditorPrefs.SetFloat("LCM_WormMinInterval", wormMinInterval);
        EditorPrefs.SetFloat("LCM_WormSpeed", wormSpeed);
        EditorPrefs.SetBool("LCM_WormShowTrail", wormShowTrail);
    }

    private void OnGUI() {
        GUILayout.Label("LAN Cable Mania — Control Panel", EditorStyles.boldLabel);

        if (GUILayout.Button("Build Scene")) {
            LanCableManiaBuilder.BuildScene();
        }

        if (GUILayout.Button("Clear Scene")) {
            LanCableManiaBuilder.ClearExistingSceneObjects();
        }

        GUILayout.Space(10);
        GUILayout.Label("── Base Game Settings ──", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();

        signalSpeed = EditorGUILayout.Slider("Signal Speed", signalSpeed, 0.5f, 3.0f);
        fuseDelay = EditorGUILayout.Slider("Fuse Delay", fuseDelay, 3.0f, 10.0f);
        shakeIntensity = EditorGUILayout.Slider("Screen Shake Intensity", shakeIntensity, 0.0f, 0.8f);
        sparkCount = EditorGUILayout.IntSlider("Spark Count", sparkCount, 20, 80);
        curveStrength = EditorGUILayout.Slider("Cable Curve Strength", curveStrength, 0.0f, 1.0f);

        GUILayout.Space(10);
        GUILayout.Label("── Round Settings ──", EditorStyles.boldLabel);

        enableRoundProgression = EditorGUILayout.Toggle("Enable Round Progression", enableRoundProgression);
        startingRound = EditorGUILayout.IntSlider("Starting Round", startingRound, 1, 5);

        GUILayout.Space(10);
        GUILayout.Label("── Feedback Effects ──", EditorStyles.boldLabel);

        wrongPlacementShakeIntensity = EditorGUILayout.Slider("Wrong Placement Shake Intensity", wrongPlacementShakeIntensity, 0.0f, 0.5f);
        fuseWarningThreshold = EditorGUILayout.Slider("Fuse Warning Threshold seconds", fuseWarningThreshold, 1.0f, 5.0f);
        celebrationParticleMultiplier = EditorGUILayout.Slider("Celebration Particle Multiplier", celebrationParticleMultiplier, 0.5f, 2.0f);

        GUILayout.Space(10);
        GUILayout.Label("── El Gusano ──", EditorStyles.boldLabel);
        enableWorm = EditorGUILayout.Toggle("Enable El Gusano", enableWorm);
        wormBaseInterval = EditorGUILayout.Slider("Base Interval", wormBaseInterval, 2.0f, 8.0f);
        wormMinInterval = EditorGUILayout.Slider("Min Interval", wormMinInterval, 1.0f, 3.0f);
        wormSpeed = EditorGUILayout.Slider("Worm Speed", wormSpeed, 2.0f, 6.0f);
        wormShowTrail = EditorGUILayout.Toggle("Show Trail", wormShowTrail);

        if (EditorGUI.EndChangeCheck()) {
            SaveSettings();
            SyncPlayMode();
        }

        GUILayout.Space(15);
        EditorGUILayout.HelpBox(
            "How the Game Works:\n" +
            "- Press play on an empty scene.\n" +
            "- Press SPACE to start placing cables (fuse timer starts countdown).\n" +
            "- Click on cells to place upcoming cable pieces from the queue.\n" +
            "- Extend the path before the signal catches up.\n" +
            "- Connect to the red END cell to complete the round!\n\n" +
            "Juiciness Principles:\n" +
            "- Cables stretch and squash when placed.\n" +
            "- Signal pulses and shoots colorful sparks dynamically based on current speed.\n" +
            "- Wrong connections flash red and trigger screen shake.\n" +
            "- Round completion triggers celebration fireworks and sequential screen shakes.\n\n" +
            "Antagonist 'El Gusano':\n" +
            "- Hostile worm that crawls from the grid borders and randomly rotates one cable.\n" +
            "- Warning red flashes and snapy glitch shakes are triggered during corruption.",
            MessageType.Info
        );
    }

    private void SyncPlayMode() {
        if (Application.isPlaying && GameManager.Instance != null) {
            GameManager.Instance.SignalSpeed = signalSpeed;
            GameManager.Instance.FuseDelay = fuseDelay;
            GameManager.Instance.ShakeIntensity = shakeIntensity;
            GameManager.Instance.SparkCount = sparkCount;
            GameManager.Instance.CurveStrength = curveStrength;

            GameManager.Instance.EnableRoundProgression = enableRoundProgression;
            GameManager.Instance.StartingRound = startingRound;
            GameManager.Instance.WrongPlacementShakeIntensity = wrongPlacementShakeIntensity;
            GameManager.Instance.FuseWarningThreshold = fuseWarningThreshold;
            GameManager.Instance.CelebrationParticleMultiplier = celebrationParticleMultiplier;

            if (WormController.Instance != null) {
                WormController.Instance.EnableWorm = enableWorm;
                WormController.Instance.BaseInterval = wormBaseInterval;
                WormController.Instance.MinInterval = wormMinInterval;
                WormController.Instance.WormSpeed = wormSpeed;
                WormController.Instance.ShowTrail = wormShowTrail;
            }
        }
    }
}
