using UnityEngine;

public class HelicopterLandingPad : MonoBehaviour
{
    private float stopTimer = 0f;
    private const float REQUIRE_STOP_DURATION = 1.5f;

    private void OnCollisionStay(Collision collision)
    {
        // Verificar si es el helicóptero
        HelicopterController player = collision.gameObject.GetComponentInParent<HelicopterController>();
        if (player != null)
        {
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Si la velocidad es muy baja (está quieto)
                if (rb.velocity.magnitude < 0.2f)
                {
                    stopTimer += Time.deltaTime;
                    if (stopTimer >= REQUIRE_STOP_DURATION)
                    {
                        // Intentar completar el juego
                        if (HelicopterGameManager.Instance != null)
                        {
                            HelicopterGameManager.Instance.CompleteGame();
                        }
                    }
                }
                else
                {
                    stopTimer = 0f;
                }
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        HelicopterController player = collision.gameObject.GetComponentInParent<HelicopterController>();
        if (player != null)
        {
            stopTimer = 0f;
        }
    }
}
