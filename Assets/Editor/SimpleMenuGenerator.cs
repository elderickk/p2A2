#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Editor utility to automatically generate the Main Menu hierarchy and attach the code-only UI Toolkit controller.
/// </summary>
public static class SimpleMenuGenerator
{
    [MenuItem("Tools/Generar Menú Simple")]
    public static void GenerarMenu()
    {
        // 1. Crear cámara de seguridad si no hay ninguna en la escena (corrige el error 'No cameras rendering')
        if (Object.FindAnyObjectByType<Camera>() == null)
        {
            GameObject camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            Camera cam = camGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.08f, 0.12f, 1f); // Mismo fondo oscuro
            Undo.RegisterCreatedObjectUndo(camGO, "Crear Camara por Defecto");
            Debug.Log("No se encontró ninguna cámara en la escena. Se creó 'Main Camera' automáticamente.");
        }

        // 2. Crear el GameObject principal
        GameObject menuGO = new GameObject("Menu_Principal");

        // 3. Añadir y configurar el componente UIDocument
        UIDocument uiDoc = menuGO.AddComponent<UIDocument>();

        // Asegurar que el archivo TSS recién creado sea importado y reconocido por Unity
        AssetDatabase.Refresh();

        // Intentar cargar PanelSettings por ruta directa (más rápido y seguro que búsquedas indexadas)
        PanelSettings settings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI Toolkit/PanelSettings.asset");
        
        // Fallback si cambió de sitio
        if (settings == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:PanelSettings");
            if (guids != null && guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
            }
        }

        if (settings != null)
        {
            uiDoc.panelSettings = settings;

            // Corregir la falta de Theme Style Sheet (causa del aviso y del desfase de renderizado en Unity 6)
            if (settings.themeStyleSheet == null)
            {
                // Cargar el tema que acabamos de crear en Assets/UI Toolkit/
                ThemeStyleSheet defaultTheme = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/UI Toolkit/MainMenuTheme.tss") as ThemeStyleSheet;

                if (defaultTheme != null)
                {
                    settings.themeStyleSheet = defaultTheme;
                    EditorUtility.SetDirty(settings);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"Corregido: Se asignó el tema por defecto '{defaultTheme.name}' al PanelSettings.");
                }
                else
                {
                    Debug.LogWarning("No se pudo cargar el archivo 'MainMenuTheme.tss' en 'Assets/UI Toolkit/'.");
                }
            }
            Debug.Log($"Asignado PanelSettings: {AssetDatabase.GetAssetPath(settings)}");
        }
        else
        {
            Debug.LogWarning("No se encontró ningún PanelSettings en el proyecto. Recuerda crear uno (Create > UI Toolkit > Panel Settings) y asignarlo al UIDocument.");
        }

        // Cargar el archivo UXML como VisualTreeAsset y asignarlo al UIDocument (Source Asset)
        VisualTreeAsset uxmlAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI Toolkit/MainMenu.uxml");
        if (uxmlAsset != null)
        {
            uiDoc.visualTreeAsset = uxmlAsset;
            Debug.Log("Asignado 'MainMenu.uxml' automáticamente como Source Asset en el UIDocument.");
        }
        else
        {
            Debug.LogWarning("No se encontró el archivo UXML 'MainMenu.uxml' en la ruta 'Assets/UI Toolkit/MainMenu.uxml'.");
        }

        // 4. Añadir el script del controlador del menú
        menuGO.AddComponent<MainMenuController>();

        // 5. Registrar la acción de creación para soportar Undo (Ctrl + Z)
        Undo.RegisterCreatedObjectUndo(menuGO, "Generar Menú Simple");

        // 6. Seleccionar automáticamente el nuevo GameObject en la jerarquía
        Selection.activeGameObject = menuGO;

        Debug.Log("¡Menú Principal generado con éxito!");
    }
}
#endif
