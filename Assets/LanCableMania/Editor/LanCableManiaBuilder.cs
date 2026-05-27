#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Editor script that builds the complete LAN Cable Mania scene.
public static class LanCableManiaBuilder {

    public static void BuildScene() {
        AddTagIfMissing("LCMDemo");
        AddTagIfMissing("LCMGrid");

        ClearExistingSceneObjects();

        GameObject cameraGO = new GameObject("Main Camera");
        cameraGO.tag = "MainCamera";
        cameraGO.transform.position = new Vector3(3.5f, 6f, 3.5f);
        cameraGO.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        Camera camera = cameraGO.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.02f, 0.02f, 0.05f);
        camera.fieldOfView = 60f;
        cameraGO.AddComponent<ScreenShakeController>();

        Undo.RegisterCreatedObjectUndo(cameraGO, "Create Main Camera");

        GameObject lightGO = new GameObject("Directional Light");
        Light light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.95f, 0.85f);
        light.intensity = 1.1f;
        lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        lightGO.tag = "LCMDemo";

        Undo.RegisterCreatedObjectUndo(lightGO, "Create Directional Light");

        GameObject groundGO = GameObject.CreatePrimitive(PrimitiveType.Plane);
        groundGO.name = "Ground";
        groundGO.transform.position = new Vector3(3.5f, -0.05f, 3.5f);
        groundGO.transform.localScale = new Vector3(5f, 1f, 5f);
        Material groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        groundMat.SetColor("_BaseColor", new Color(0.05f, 0.05f, 0.08f));
        groundGO.GetComponent<Renderer>().material = groundMat;
        groundGO.tag = "LCMDemo";

        Undo.RegisterCreatedObjectUndo(groundGO, "Create Ground Plane");

        GameObject gameManagerGO = new GameObject("GameManager");
        GameManager gameManager = gameManagerGO.AddComponent<GameManager>();
        WormController wormController = gameManagerGO.AddComponent<WormController>();
        gameManagerGO.tag = "LCMDemo";
        Undo.RegisterCreatedObjectUndo(gameManagerGO, "Create GameManager");

        GameObject gridManagerGO = new GameObject("GridManager");
        GridManager gridManager = gridManagerGO.AddComponent<GridManager>();
        gridManagerGO.tag = "LCMDemo";
        Undo.RegisterCreatedObjectUndo(gridManagerGO, "Create GridManager");

        GameObject signalControllerGO = new GameObject("SignalController");
        SignalController signalController = signalControllerGO.AddComponent<SignalController>();
        signalControllerGO.tag = "LCMDemo";
        Undo.RegisterCreatedObjectUndo(signalControllerGO, "Create SignalController");

        GameObject uiOverlayGO = new GameObject("UIOverlay");
        uiOverlayGO.AddComponent<UIOverlay>();
        uiOverlayGO.tag = "LCMDemo";
        Undo.RegisterCreatedObjectUndo(uiOverlayGO, "Create UIOverlay");

        SerializedObject soGM = new SerializedObject(gameManager);

        float signalSpeed = EditorPrefs.GetFloat("LCM_SignalSpeed", 1.0f);
        float fuseDelay = EditorPrefs.GetFloat("LCM_FuseDelay", 5.0f);
        float shakeIntensity = EditorPrefs.GetFloat("LCM_ScreenShakeIntensity", 0.5f);
        int sparkCount = EditorPrefs.GetInt("LCM_SparkCount", 40);
        float curveStrength = EditorPrefs.GetFloat("LCM_CableCurveStrength", 0.5f);

        bool enableRoundProgression = EditorPrefs.GetBool("LCM_EnableRoundProgression", true);
        int startingRound = EditorPrefs.GetInt("LCM_StartingRound", 1);
        float wrongPlacementShakeIntensity = EditorPrefs.GetFloat("LCM_WrongPlacementShakeIntensity", 0.25f);
        float fuseWarningThreshold = EditorPrefs.GetFloat("LCM_FuseWarningThreshold", 3.0f);
        float celebrationParticleMultiplier = EditorPrefs.GetFloat("LCM_CelebrationParticleMultiplier", 1.0f);

        soGM.FindProperty("signalSpeed").floatValue = signalSpeed;
        soGM.FindProperty("fuseDelay").floatValue = fuseDelay;
        soGM.FindProperty("shakeIntensity").floatValue = shakeIntensity;
        soGM.FindProperty("sparkCount").intValue = sparkCount;
        soGM.FindProperty("curveStrength").floatValue = curveStrength;

        soGM.FindProperty("enableRoundProgression").boolValue = enableRoundProgression;
        soGM.FindProperty("startingRound").intValue = startingRound;
        soGM.FindProperty("wrongPlacementShakeIntensity").floatValue = wrongPlacementShakeIntensity;
        soGM.FindProperty("fuseWarningThreshold").floatValue = fuseWarningThreshold;
        soGM.FindProperty("celebrationParticleMultiplier").floatValue = celebrationParticleMultiplier;

        soGM.ApplyModifiedProperties();

        SerializedObject soWorm = new SerializedObject(wormController);
        soWorm.FindProperty("enableWorm").boolValue = EditorPrefs.GetBool("LCM_EnableWorm", true);
        soWorm.FindProperty("baseInterval").floatValue = EditorPrefs.GetFloat("LCM_WormBaseInterval", 4.0f);
        soWorm.FindProperty("minInterval").floatValue = EditorPrefs.GetFloat("LCM_WormMinInterval", 1.5f);
        soWorm.FindProperty("wormSpeed").floatValue = EditorPrefs.GetFloat("LCM_WormSpeed", 4.0f);
        soWorm.FindProperty("showTrail").boolValue = EditorPrefs.GetBool("LCM_WormShowTrail", true);
        soWorm.ApplyModifiedProperties();

        SerializedObject soGrid = new SerializedObject(gridManager);
        soGrid.FindProperty("curveStrength").floatValue = curveStrength;
        soGrid.ApplyModifiedProperties();

        int gridSize = 8;
        if (startingRound == 1) {
            gridSize = 8;
        } else if (startingRound == 2) {
            gridSize = 9;
        } else if (startingRound == 3) {
            gridSize = 10;
        } else if (startingRound == 4) {
            gridSize = 11;
        } else if (startingRound >= 5) {
            gridSize = 12;
        }

        gridManager.RebuildGrid(gridSize);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("LAN Cable Mania scene successfully built and populated in hierarchy!");
    }

    public static void ClearExistingSceneObjects() {
        AddTagIfMissing("LCMDemo");
        AddTagIfMissing("LCMGrid");

        Camera existingCam = Camera.main;
        if (existingCam != null) {
            Undo.DestroyObjectImmediate(existingCam.gameObject);
        }

        GameObject[] demoObjects = GameObject.FindGameObjectsWithTag("LCMDemo");
        foreach (var obj in demoObjects) {
            Undo.DestroyObjectImmediate(obj);
        }

        GameObject[] gridObjects = GameObject.FindGameObjectsWithTag("LCMGrid");
        foreach (var obj in gridObjects) {
            Undo.DestroyObjectImmediate(obj);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Cleared existing LAN Cable Mania GameObjects from the scene.");
    }

    private static void AddTagIfMissing(string tag) {
        UnityEngine.Object[] tagManagerAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (tagManagerAsset == null || tagManagerAsset.Length == 0) {
            return;
        }

        SerializedObject tagManager = new SerializedObject(tagManagerAsset[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        bool found = false;
        for (int i = 0; i < tagsProp.arraySize; i++) {
            if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag) {
                found = true;
                break;
            }
        }

        if (!found) {
            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
            tagManager.ApplyModifiedProperties();
        }
    }
}
#endif
