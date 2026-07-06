#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HelicopterSceneSetup : EditorWindow
{
    [MenuItem("Tools/Helicopter Game/Setup Game Scene")]
    public static void SetupScene()
    {
        // 1. Crear una nueva escena vacía
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        newScene.name = "HelicopterPlayground";

        // Asegurar carpetas
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        // 2. Crear Materiales de prueba
        Material groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (groundMat.shader == null) groundMat.shader = Shader.Find("Standard");
        groundMat.color = new Color(0.18f, 0.48f, 0.20f); // Verde
        AssetDatabase.CreateAsset(groundMat, "Assets/Materials/Heli_GroundMat.mat");

        Material heliMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (heliMat.shader == null) heliMat.shader = Shader.Find("Standard");
        heliMat.color = new Color(0.15f, 0.15f, 0.15f); // Gris oscuro
        AssetDatabase.CreateAsset(heliMat, "Assets/Materials/Heli_BodyMat.mat");

        Material rotorMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (rotorMat.shader == null) rotorMat.shader = Shader.Find("Standard");
        rotorMat.color = new Color(0.85f, 0.85f, 0.85f); // Blanco/Plata
        AssetDatabase.CreateAsset(rotorMat, "Assets/Materials/Heli_RotorMat.mat");

        Material padMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (padMat.shader == null) padMat.shader = Shader.Find("Standard");
        padMat.color = new Color(0.85f, 0.35f, 0.1f); // Naranja brillante
        AssetDatabase.CreateAsset(padMat, "Assets/Materials/Heli_PadMat.mat");

        Material ringMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (ringMat.shader == null) ringMat.shader = Shader.Find("Standard");
        ringMat.color = new Color(0f, 0.8f, 1f); // Celeste neón
        AssetDatabase.CreateAsset(ringMat, "Assets/Materials/Heli_RingMat.mat");

        // 3. Crear el Terreno / Suelo
        GameObject groundObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        groundObj.name = "Ground";
        groundObj.transform.position = new Vector3(0f, -0.5f, 0f);
        groundObj.transform.localScale = new Vector3(300f, 1f, 300f);
        groundObj.GetComponent<Renderer>().material = groundMat;

        // 4. Crear el Helipuerto (Landing Pad)
        GameObject padObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        padObj.name = "LandingPad";
        padObj.transform.position = new Vector3(0f, 0.05f, 0f);
        padObj.transform.localScale = new Vector3(10f, 0.05f, 10f);
        padObj.GetComponent<Renderer>().material = padMat;
        padObj.AddComponent<HelicopterLandingPad>();

        // Crear una letra 'H' en 3D sobre el Helipuerto usando cubos pequeños
        GameObject hLetter = new GameObject("H_Letter");
        hLetter.transform.SetParent(padObj.transform, false);
        hLetter.transform.localPosition = new Vector3(0f, 1.1f, 0f);
        hLetter.transform.localScale = new Vector3(0.1f, 1f, 0.1f);

        GameObject line1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        line1.name = "Line1";
        line1.transform.SetParent(hLetter.transform, false);
        line1.transform.localPosition = new Vector3(-2f, 0f, 0f);
        line1.transform.localScale = new Vector3(1f, 1f, 6f);
        DestroyImmediate(line1.GetComponent<Collider>());

        GameObject line2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        line2.name = "Line2";
        line2.transform.SetParent(hLetter.transform, false);
        line2.transform.localPosition = new Vector3(2f, 0f, 0f);
        line2.transform.localScale = new Vector3(1f, 1f, 6f);
        DestroyImmediate(line2.GetComponent<Collider>());

        GameObject lineCross = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lineCross.name = "LineCross";
        lineCross.transform.SetParent(hLetter.transform, false);
        lineCross.transform.localPosition = new Vector3(0f, 0f, 0f);
        lineCross.transform.localScale = new Vector3(4f, 1f, 1.5f);
        DestroyImmediate(lineCross.GetComponent<Collider>());

        // 5. Crear el Helicóptero
        GameObject heliRoot = new GameObject("Helicopter");
        heliRoot.transform.position = new Vector3(0f, 1f, 0f);
        Rigidbody rb = heliRoot.AddComponent<Rigidbody>();
        rb.mass = 1.2f;
        rb.drag = 0.6f;
        rb.angularDrag = 2.5f;

        BoxCollider boxCol = heliRoot.AddComponent<BoxCollider>();
        boxCol.center = new Vector3(0f, 0.2f, 0f);
        boxCol.size = new Vector3(1.6f, 1.5f, 3.6f);

        // Modelo Visual (para inclinarse)
        GameObject visualObj = new GameObject("VisualModel");
        visualObj.transform.SetParent(heliRoot.transform, false);

        // Fuselaje (Cuerpo)
        GameObject bodyObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bodyObj.name = "Fuselage";
        bodyObj.transform.SetParent(visualObj.transform, false);
        bodyObj.transform.localScale = new Vector3(1.2f, 1.0f, 2.0f);
        bodyObj.GetComponent<Renderer>().material = heliMat;
        DestroyImmediate(bodyObj.GetComponent<Collider>());

        // Cabina (Esfera adelante)
        GameObject cabinObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        cabinObj.name = "Cabin";
        cabinObj.transform.SetParent(visualObj.transform, false);
        cabinObj.transform.localPosition = new Vector3(0f, 0.1f, 1.0f);
        cabinObj.transform.localScale = new Vector3(1.1f, 0.9f, 1.2f);
        cabinObj.GetComponent<Renderer>().material = rotorMat; // Color brillante para parabrisas
        DestroyImmediate(cabinObj.GetComponent<Collider>());

        // Cola (Tail Boom)
        GameObject tailObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tailObj.name = "TailBoom";
        tailObj.transform.SetParent(visualObj.transform, false);
        tailObj.transform.localPosition = new Vector3(0f, 0.2f, -1.8f);
        tailObj.transform.localScale = new Vector3(0.25f, 0.25f, 2.0f);
        tailObj.GetComponent<Renderer>().material = heliMat;
        DestroyImmediate(tailObj.GetComponent<Collider>());

        // Alerón de cola (Tail Fin)
        GameObject finObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        finObj.name = "TailFin";
        finObj.transform.SetParent(visualObj.transform, false);
        finObj.transform.localPosition = new Vector3(0f, 0.6f, -2.7f);
        finObj.transform.localScale = new Vector3(0.12f, 0.9f, 0.4f);
        finObj.GetComponent<Renderer>().material = heliMat;
        DestroyImmediate(finObj.GetComponent<Collider>());

        // Patines de aterrizaje (Skids)
        GameObject skidsLeft = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        skidsLeft.name = "SkidLeft";
        skidsLeft.transform.SetParent(visualObj.transform, false);
        skidsLeft.transform.localPosition = new Vector3(-0.6f, -0.6f, 0.1f);
        skidsLeft.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        skidsLeft.transform.localScale = new Vector3(0.12f, 1.3f, 0.12f);
        skidsLeft.GetComponent<Renderer>().material = heliMat;
        DestroyImmediate(skidsLeft.GetComponent<Collider>());

        GameObject skidsRight = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        skidsRight.name = "SkidRight";
        skidsRight.transform.SetParent(visualObj.transform, false);
        skidsRight.transform.localPosition = new Vector3(0.6f, -0.6f, 0.1f);
        skidsRight.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        skidsRight.transform.localScale = new Vector3(0.12f, 1.3f, 0.12f);
        skidsRight.GetComponent<Renderer>().material = heliMat;
        DestroyImmediate(skidsRight.GetComponent<Collider>());

        // Soportes de patines (Struts)
        GameObject strut1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        strut1.name = "Strut1";
        strut1.transform.SetParent(visualObj.transform, false);
        strut1.transform.localPosition = new Vector3(-0.4f, -0.35f, 0.3f);
        strut1.transform.localRotation = Quaternion.Euler(0f, 0f, 30f);
        strut1.transform.localScale = new Vector3(0.06f, 0.35f, 0.06f);
        DestroyImmediate(strut1.GetComponent<Collider>());

        GameObject strut2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        strut2.name = "Strut2";
        strut2.transform.SetParent(visualObj.transform, false);
        strut2.transform.localPosition = new Vector3(0.4f, -0.35f, 0.3f);
        strut2.transform.localRotation = Quaternion.Euler(0f, 0f, -30f);
        strut2.transform.localScale = new Vector3(0.06f, 0.35f, 0.06f);
        DestroyImmediate(strut2.GetComponent<Collider>());

        // Eje del rotor principal (Rotor Shaft)
        GameObject shaftObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shaftObj.name = "RotorShaft";
        shaftObj.transform.SetParent(visualObj.transform, false);
        shaftObj.transform.localPosition = new Vector3(0f, 0.65f, 0f);
        shaftObj.transform.localScale = new Vector3(0.12f, 0.3f, 0.12f);
        shaftObj.GetComponent<Renderer>().material = heliMat;
        DestroyImmediate(shaftObj.GetComponent<Collider>());

        // Pivote del rotor principal (Main Rotor Pivot)
        GameObject mainRotorPivot = new GameObject("MainRotorPivot");
        mainRotorPivot.transform.SetParent(visualObj.transform, false);
        mainRotorPivot.transform.localPosition = new Vector3(0f, 0.8f, 0f);

        // Aspa 1
        GameObject blade1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blade1.name = "Blade1";
        blade1.transform.SetParent(mainRotorPivot.transform, false);
        blade1.transform.localScale = new Vector3(0.15f, 0.02f, 4.2f);
        blade1.GetComponent<Renderer>().material = rotorMat;
        DestroyImmediate(blade1.GetComponent<Collider>());

        // Aspa 2 (perpendicular)
        GameObject blade2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blade2.name = "Blade2";
        blade2.transform.SetParent(mainRotorPivot.transform, false);
        blade2.transform.localScale = new Vector3(4.2f, 0.02f, 0.15f);
        blade2.GetComponent<Renderer>().material = rotorMat;
        DestroyImmediate(blade2.GetComponent<Collider>());

        // Pivote del rotor de cola
        GameObject tailRotorPivot = new GameObject("TailRotorPivot");
        tailRotorPivot.transform.SetParent(visualObj.transform, false);
        tailRotorPivot.transform.localPosition = new Vector3(0.2f, 0.5f, -2.7f);

        // Aspa de cola
        GameObject tailBlade = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tailBlade.name = "TailBlade";
        tailBlade.transform.SetParent(tailRotorPivot.transform, false);
        tailBlade.transform.localScale = new Vector3(0.02f, 0.9f, 0.08f);
        tailBlade.GetComponent<Renderer>().material = rotorMat;
        DestroyImmediate(tailBlade.GetComponent<Collider>());

        // Configurar Componente HelicopterController
        HelicopterController controller = heliRoot.AddComponent<HelicopterController>();
        // Usar reflexión o asignación directa de campos serializados
        var serializedController = new SerializedObject(controller);
        serializedController.FindProperty("visualModel").objectReferenceValue = visualObj.transform;
        serializedController.FindProperty("mainRotor").objectReferenceValue = mainRotorPivot.transform;
        serializedController.FindProperty("tailRotor").objectReferenceValue = tailRotorPivot.transform;
        serializedController.ApplyModifiedProperties();

        // 6. Configurar la Cámara
        GameObject cameraObj = GameObject.FindWithTag("MainCamera");
        if (cameraObj == null)
        {
            cameraObj = new GameObject("MainCamera", typeof(Camera), typeof(AudioListener));
            cameraObj.tag = "MainCamera";
        }
        cameraObj.transform.position = new Vector3(0f, 5f, -12f);
        cameraObj.transform.rotation = Quaternion.Euler(15f, 0f, 0f);

        HelicopterCamera heliCam = cameraObj.GetComponent<HelicopterCamera>();
        if (heliCam == null) heliCam = cameraObj.AddComponent<HelicopterCamera>();
        
        var serializedCam = new SerializedObject(heliCam);
        serializedCam.FindProperty("target").objectReferenceValue = heliRoot.transform;
        serializedCam.ApplyModifiedProperties();

        // 7. Crear los Anillos Flotantes (Recolectables)
        Vector3[] ringPositions = new Vector3[]
        {
            new Vector3(0f, 6f, 25f),
            new Vector3(20f, 10f, 45f),
            new Vector3(35f, 15f, 20f),
            new Vector3(15f, 18f, -15f),
            new Vector3(-20f, 12f, -40f),
            new Vector3(-35f, 8f, 0f),
            new Vector3(0f, 4f, -25f)
        };

        for (int i = 0; i < ringPositions.Length; i++)
        {
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = $"Ring_{i + 1}";
            ring.transform.position = ringPositions[i];
            ring.transform.localScale = new Vector3(4f, 0.2f, 4f);
            ring.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // Plano vertical
            ring.GetComponent<Renderer>().material = ringMat;

            // Eliminar colisionador físico y poner trigger esférico
            DestroyImmediate(ring.GetComponent<Collider>());
            SphereCollider sc = ring.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 1.2f;

            ring.AddComponent<HelicopterRing>();
        }

        // 8. Crear la Interfaz de Usuario (UI)
        GameObject canvasObj = new GameObject("HelicopterCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.AddComponent<GraphicRaycaster>();

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (defaultFont == null) defaultFont = Font.GetDefault();

        // Fondo oscuro superior para UI (Panel)
        GameObject headerPanel = new GameObject("HeaderPanel");
        headerPanel.transform.SetParent(canvasObj.transform, false);
        Image panelImage = headerPanel.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.6f);
        RectTransform panelRect = panelImage.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = new Vector3(0f, 0f, 0f);
        panelRect.sizeDelta = new Vector2(0f, 60f);

        // Texto de Puntaje
        GameObject scoreTextObj = new GameObject("ScoreText");
        scoreTextObj.transform.SetParent(headerPanel.transform, false);
        Text scoreText = scoreTextObj.AddComponent<Text>();
        scoreText.font = defaultFont;
        scoreText.fontSize = 20;
        scoreText.color = Color.white;
        scoreText.text = "Puntos: 0 | Anillos: 0/7";
        scoreText.alignment = TextAnchor.MiddleLeft;
        RectTransform scoreRect = scoreText.GetComponent<RectTransform>();
        scoreRect.anchorMin = new Vector2(0f, 0.5f);
        scoreRect.anchorMax = new Vector2(0.4f, 0.5f);
        scoreRect.pivot = new Vector2(0f, 0.5f);
        scoreRect.anchoredPosition = new Vector2(20f, 0f);
        scoreRect.sizeDelta = new Vector2(0f, 40f);

        // Texto de Altitud
        GameObject altitudeTextObj = new GameObject("AltitudeText");
        altitudeTextObj.transform.SetParent(headerPanel.transform, false);
        Text altitudeText = altitudeTextObj.AddComponent<Text>();
        altitudeText.font = defaultFont;
        altitudeText.fontSize = 18;
        altitudeText.color = new Color(0.7f, 0.9f, 1f);
        altitudeText.text = "Altitud: 0.0 m";
        altitudeText.alignment = TextAnchor.MiddleCenter;
        RectTransform altRect = altitudeText.GetComponent<RectTransform>();
        altRect.anchorMin = new Vector2(0.4f, 0.5f);
        altRect.anchorMax = new Vector2(0.7f, 0.5f);
        altRect.pivot = new Vector2(0.5f, 0.5f);
        altRect.anchoredPosition = new Vector2(0f, 0f);
        altRect.sizeDelta = new Vector2(0f, 40f);

        // Texto de Velocidad
        GameObject speedTextObj = new GameObject("SpeedText");
        speedTextObj.transform.SetParent(headerPanel.transform, false);
        Text speedText = speedTextObj.AddComponent<Text>();
        speedText.font = defaultFont;
        speedText.fontSize = 18;
        speedText.color = new Color(0.7f, 0.9f, 1f);
        speedText.text = "Velocidad: 0.0 km/h";
        speedText.alignment = TextAnchor.MiddleRight;
        RectTransform speedRect = speedText.GetComponent<RectTransform>();
        speedRect.anchorMin = new Vector2(0.7f, 0.5f);
        speedRect.anchorMax = new Vector2(1f, 0.5f);
        speedRect.pivot = new Vector2(1f, 0.5f);
        speedRect.anchoredPosition = new Vector2(-20f, 0f);
        speedRect.sizeDelta = new Vector2(0f, 40f);

        // Panel de Mensajes en el Centro
        GameObject msgObj = new GameObject("MessageText");
        msgObj.transform.SetParent(canvasObj.transform, false);
        Text msgText = msgObj.AddComponent<Text>();
        msgText.font = defaultFont;
        msgText.fontSize = 24;
        msgText.color = Color.yellow;
        msgText.text = "¡Recolecta todos los anillos y aterriza!";
        msgText.alignment = TextAnchor.MiddleCenter;
        RectTransform msgRect = msgText.GetComponent<RectTransform>();
        msgRect.anchorMin = new Vector2(0.1f, 0.4f);
        msgRect.anchorMax = new Vector2(0.9f, 0.6f);
        msgRect.pivot = new Vector2(0.5f, 0.5f);
        msgRect.anchoredPosition = new Vector2(0f, 0f);
        msgRect.sizeDelta = new Vector2(0f, 0f);

        // Panel de Controles en la esquina inferior izquierda
        GameObject controlsObj = new GameObject("ControlsText");
        controlsObj.transform.SetParent(canvasObj.transform, false);
        Text controlsText = controlsObj.AddComponent<Text>();
        controlsText.font = defaultFont;
        controlsText.fontSize = 14;
        controlsText.color = new Color(0.8f, 0.8f, 0.8f);
        controlsText.text = "Controles:\n• W / S: Cabeceo (Avanzar/Retroceder)\n• A / D: Giro (Guiñada)\n• Espacio: Subir\n• Left Shift: Bajar\n• R: Reiniciar";
        controlsText.alignment = TextAnchor.LowerLeft;
        RectTransform controlsRect = controlsText.GetComponent<RectTransform>();
        controlsRect.anchorMin = new Vector2(0f, 0f);
        controlsRect.anchorMax = new Vector2(0.4f, 0.3f);
        controlsRect.pivot = new Vector2(0f, 0f);
        controlsRect.anchoredPosition = new Vector2(20f, 20f);
        controlsRect.sizeDelta = new Vector2(0f, 0f);

        // 9. Crear el HelicopterGameManager
        GameObject gmObj = new GameObject("HelicopterGameManager");
        HelicopterGameManager gm = gmObj.AddComponent<HelicopterGameManager>();
        
        var serializedGM = new SerializedObject(gm);
        serializedGM.FindProperty("scoreText").objectReferenceValue = scoreText;
        serializedGM.FindProperty("altitudeText").objectReferenceValue = altitudeText;
        serializedGM.FindProperty("speedText").objectReferenceValue = speedText;
        serializedGM.FindProperty("messageText").objectReferenceValue = msgText;
        serializedGM.FindProperty("helicopter").objectReferenceValue = controller;
        serializedGM.ApplyModifiedProperties();

        // 10. Guardar la Escena
        EditorSceneManager.SaveScene(newScene, "Assets/Scenes/HelicopterPlayground.unity");
        Debug.Log("¡Escena 'HelicopterPlayground' creada y configurada con éxito en Assets/Scenes!");
    }
}
#endif
