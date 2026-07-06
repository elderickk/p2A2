using UnityEngine;

public class HelicopterRing : MonoBehaviour
{
    [Header("Ajustes del Anillo")]
    [Tooltip("Puntos que otorga este anillo al recolectarse.")]
    [SerializeField] private int scoreValue = 10;
    [Tooltip("Velocidad de rotación constante.")]
    [SerializeField] private float rotationSpeed = 50f;
    [Tooltip("Color de emisión del anillo.")]
    [SerializeField] private Color emissionColor = new Color(0.1f, 0.7f, 1f); // Celeste neón

    private bool collected = false;

    private void Start()
    {
        // Añadir iluminación/color neón al material del anillo si es posible
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null && renderer.material != null)
        {
            renderer.material.EnableKeyword("_EMISSION");
            renderer.material.SetColor("_BaseColor", emissionColor);
            renderer.material.SetColor("_EmissionColor", emissionColor * 2f);
        }
    }

    private void Update()
    {
        // Hacer girar el anillo continuamente para que se vea dinámico
        transform.Rotate(Vector3.up * (rotationSpeed * Time.deltaTime), Space.Self);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;

        // Comprobamos si el objeto que entra tiene el controlador de helicóptero
        HelicopterController player = other.GetComponentInParent<HelicopterController>();
        if (player != null)
        {
            collected = true;
            
            // Incrementar puntaje
            HelicopterGameManager gameManager = HelicopterGameManager.Instance;
            if (gameManager != null)
            {
                gameManager.AddScore(scoreValue);
            }
            else
            {
                Debug.Log($"¡Anillo recolectado! +{scoreValue} puntos.");
            }

            // Reproducir un sonido o efecto rápido
            PlayCollectionEffect();

            // Destruir el anillo
            Destroy(gameObject);
        }
    }

    private void PlayCollectionEffect()
    {
        // Crear un efecto visual simple usando partículas primitivas en tiempo de ejecución
        GameObject fx = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        fx.transform.position = transform.position;
        fx.transform.localScale = transform.localScale * 0.2f;
        
        // Destruir el colisionador para que no interfiera
        Destroy(fx.GetComponent<Collider>());

        Renderer renderer = fx.GetComponent<Renderer>();
        if (renderer != null && renderer.material != null)
        {
            renderer.material.color = emissionColor;
            renderer.material.EnableKeyword("_EMISSION");
            renderer.material.SetColor("_EmissionColor", emissionColor * 4f);
        }

        // Hacerlo crecer y desaparecer rápido
        Destroy(fx, 0.5f);
        fx.AddComponent<SimpleExpansionEffect>();
    }
}

// Script auxiliar para animar el efecto de recolección
public class SimpleExpansionEffect : MonoBehaviour
{
    private float timer = 0f;
    private void Update()
    {
        timer += Time.deltaTime;
        transform.localScale += Vector3.one * (10f * Time.deltaTime);
        Renderer r = GetComponent<Renderer>();
        if (r != null && r.material != null)
        {
            Color c = r.material.color;
            c.a = Mathf.Lerp(1f, 0f, timer / 0.5f);
            r.material.color = c;
        }
    }
}
