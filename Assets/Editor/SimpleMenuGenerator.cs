#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

// Editor utility to automatically generate the Main Menu hierarchy.
public static class SimpleMenuGenerator {

    public static void GenerarMenu() {
        if (Object.FindAnyObjectByType<Camera>() == null) {
            GameObject camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            Camera cam = camGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.08f, 0.12f, 1f);
            Undo.RegisterCreatedObjectUndo(camGO, "Crear Camara por Defecto");
            Debug.Log("No se encontró ninguna cámara en la escena. Se creó 'Main Camera' automáticamente.");
        }

        GameObject menuGO = new GameObject("Menu_Principal");

        UIDocument uiDoc = menuGO.AddComponent<UIDocument>();

        AssetDatabase.Refresh();

        PanelSettings settings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI Toolkit/PanelSettings.asset");

        if (settings == null) {
            string[] guids = AssetDatabase.FindAssets("t:PanelSettings");
            if (guids != null && guids.Length > 0) {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
            }
        }

        if (settings != null) {
            uiDoc.panelSettings = settings;

            if (settings.themeStyleSheet == null) {
                ThemeStyleSheet defaultTheme = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/UI Toolkit/MainMenuTheme.tss") as ThemeStyleSheet;

                if (defaultTheme != null) {
                    settings.themeStyleSheet = defaultTheme;
                    EditorUtility.SetDirty(settings);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"Corregido: Se asignó el tema por defecto '{defaultTheme.name}' al PanelSettings.");
                } else {
                    Debug.LogWarning("No se pudo cargar el archivo 'MainMenuTheme.tss' en 'Assets/UI Toolkit/'.");
                }
            }
            Debug.Log($"Asignado PanelSettings: {AssetDatabase.GetAssetPath(settings)}");
        } else {
            Debug.LogWarning("No se encontró ningún PanelSettings en el proyecto. Recuerda crear uno (Create > UI Toolkit > Panel Settings) y asignarlo al UIDocument.");
        }

        VisualTreeAsset uxmlAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI Toolkit/MainMenu.uxml");
        if (uxmlAsset != null) {
            uiDoc.visualTreeAsset = uxmlAsset;
            Debug.Log("Asignado 'MainMenu.uxml' automáticamente como Source Asset en el UIDocument.");
        } else {
            Debug.LogWarning("No se encontró el archivo UXML 'MainMenu.uxml' en la ruta 'Assets/UI Toolkit/MainMenu.uxml'.");
        }

        menuGO.AddComponent<MainMenuController>();

        Undo.RegisterCreatedObjectUndo(menuGO, "Generar Menú Simple");

        Selection.activeGameObject = menuGO;

        Debug.Log("¡Menú Principal generado con éxito!");
    }
}
#endif
