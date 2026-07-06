using UnityEngine;

public class HelicopterCamera : MonoBehaviour
{
    [Header("Objetivo de Seguimiento")]
    [Tooltip("El helicóptero que seguirá la cámara.")]
    [SerializeField] private Transform target;

    [Header("Ajustes de Distancia")]
    [Tooltip("Distancia detrás del helicóptero.")]
    [SerializeField] private float distance = 10.0f;
    [Tooltip("Altura sobre el helicóptero.")]
    [SerializeField] private float height = 4.0f;

    [Header("Suavizado")]
    [Tooltip("Suavizado de posición.")]
    [SerializeField] private float positionLag = 5.0f;
    [Tooltip("Suavizado de rotación.")]
    [SerializeField] private float rotationLag = 3.0f;

    [Header("Objetivo de Mirada")]
    [Tooltip("Desplazamiento vertical para mirar ligeramente por encima del helicóptero.")]
    [SerializeField] private float lookAtOffset = 1.0f;

    public Transform Target { get => target; set => target = value; }

    private void LateUpdate()
    {
        if (target == null) return;

        // 1. Calcular la posición objetivo basada en la rotación del helicóptero
        float targetAngle = target.eulerAngles.y;
        float targetHeight = target.position.y + height;

        float currentAngle = transform.eulerAngles.y;
        float currentHeight = transform.position.y;

        // Suavizar rotación y altura
        currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, rotationLag * Time.deltaTime);
        currentHeight = Mathf.Lerp(currentHeight, targetHeight, positionLag * Time.deltaTime);

        // Convertir el ángulo en una rotación
        Quaternion currentRotation = Quaternion.Euler(0f, currentAngle, 0f);

        // Calcular la posición deseada de la cámara detrás del target
        Vector3 targetPos = target.position;
        targetPos -= currentRotation * Vector3.forward * distance;
        targetPos.y = currentHeight;

        // Aplicar la posición
        transform.position = targetPos;

        // 2. Apuntar la cámara al objetivo
        Vector3 lookTarget = target.position + Vector3.up * lookAtOffset;
        transform.LookAt(lookTarget);
    }
}
