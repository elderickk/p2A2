using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

/// <summary>
/// Controller for the Main Menu. Loads elements from a UXML layout (Visual Tree Asset)
/// and handles scene loading, allowing you to edit the design visually in the UI Builder.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class MainMenuController : MonoBehaviour
{
    [Header("Configuración de Escena")]
    [Tooltip("Nombre de la escena que se cargará al pulsar 'New Game'.")]
    [SerializeField] private string targetSceneName = "SampleScene";

    [Header("Identificadores de UI (UI Builder)")]
    [Tooltip("El nombre (Name / ID) que le diste al botón de jugar en el UI Builder.")]
    [SerializeField] private string newGameButtonName = "NewGameButton";

    private UIDocument uiDocument;

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
    }

    private void Start()
    {
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }

        if (uiDocument != null)
        {
            SetupMenuEvents();
        }
        else
        {
            Debug.LogError("No se encontró el componente UIDocument en este GameObject.");
        }
    }

    /// <summary>
    /// Queries the visual tree from the assigned UXML asset and binds interaction events.
    /// </summary>
    private void SetupMenuEvents()
    {
        VisualElement root = uiDocument.rootVisualElement;
        if (root == null)
        {
            Debug.LogError("No se pudo obtener el rootVisualElement. Asegúrate de tener asignado un Source Asset (UXML) y Panel Settings en el UIDocument.");
            return;
        }

        // Buscar el botón por el nombre definido en el UI Builder (name="NewGameButton")
        Button newGameButton = root.Q<Button>(newGameButtonName);
        
        if (newGameButton != null)
        {
            // Registrar evento de click
            newGameButton.clicked += OnNewGameClicked;

            // Añadir efectos interactivos de escala (hover bounce) por código
            newGameButton.RegisterCallback<MouseOverEvent>(evt =>
            {
                newGameButton.style.scale = new StyleScale(new Scale(new Vector3(1.05f, 1.05f, 1f)));
            });
            newGameButton.RegisterCallback<MouseOutEvent>(evt =>
            {
                newGameButton.style.scale = new StyleScale(new Scale(new Vector3(1f, 1f, 1f)));
            });
        }
        else
        {
            Debug.LogWarning($"Advertencia: No se encontró ningún botón llamado '{newGameButtonName}' en el archivo UXML. Por favor, revisa el nombre del botón en el UI Builder.");
        }
    }

    private void OnNewGameClicked()
    {
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            if (targetSceneName == SceneManager.GetActiveScene().name)
            {
                Debug.Log("La escena destino es la escena activa. Desactivando el menú 'Menu_Principal' para revelar el juego e iniciar.");
                gameObject.SetActive(false);
            }
            else
            {
                Debug.Log($"Cargando la escena del juego: {targetSceneName}");
                SceneManager.LoadScene(targetSceneName);
            }
        }
        else
        {
            Debug.LogError("Error: El nombre de la escena de destino está vacío. Configúralo en el inspector del MainMenuController.");
        }
    }
}
