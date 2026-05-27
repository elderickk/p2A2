using System.Collections;
using UnityEngine;

// Controls camera shake, FOV punches, and red screen flash effects.
[RequireComponent(typeof(Camera))]
public class ScreenShakeController : MonoBehaviour {

    public static ScreenShakeController Instance { get; private set; }

    private Vector3 originalLocalPos;
    private Color originalBgColor;
    private float originalFOV;
    private Camera myCamera;
    private Coroutine shakeCoroutine;
    private Coroutine colorFlashCoroutine;
    private Coroutine fovPunchCoroutine;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
            return;
        }
        myCamera = GetComponent<Camera>();
    }

    private void Start() {
        originalLocalPos = transform.localPosition;
        originalBgColor = myCamera.backgroundColor;
        originalFOV = myCamera.fieldOfView;
    }

    public void Shake(float intensity, float duration) {
        if (shakeCoroutine != null) {
            StopCoroutine(shakeCoroutine);
        }
        shakeCoroutine = StartCoroutine(ShakeCoroutine(intensity, duration));
    }

    public void WrongPlacementShake() {
        Shake(0.25f, 0.15f);
        if (colorFlashCoroutine != null) {
            StopCoroutine(colorFlashCoroutine);
        }
        colorFlashCoroutine = StartCoroutine(WrongPlacementFlashCoroutine());
    }

    public void FuseWarningPulse() {
        Shake(0.04f, 0.06f);
    }

    public void CelebrationShake() {
        StartCoroutine(CelebrationShakeCoroutine());
    }

    public void FOVPunch(float targetFOV, float duration) {
        if (fovPunchCoroutine != null) {
            StopCoroutine(fovPunchCoroutine);
        }
        fovPunchCoroutine = StartCoroutine(FOVPunchCoroutine(targetFOV, duration));
    }

    public void UpdateOriginalPosition(Vector3 newPos) {
        originalLocalPos = newPos;
    }

    private IEnumerator ShakeCoroutine(float intensity, float duration) {
        float elapsed = 0f;
        while (elapsed < duration) {
            Vector3 offset = Random.insideUnitSphere * intensity * (1f - (elapsed / duration));
            transform.localPosition = originalLocalPos + offset;
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = originalLocalPos;
        shakeCoroutine = null;
    }

    private IEnumerator WrongPlacementFlashCoroutine() {
        float elapsed = 0f;
        float duration = 0.2f;
        Color flashColor = new Color(0.4f, 0.05f, 0.05f);

        while (elapsed < duration) {
            float t = elapsed / duration;
            if (t < 0.5f) {
                myCamera.backgroundColor = Color.Lerp(originalBgColor, flashColor, t * 2f);
            } else {
                myCamera.backgroundColor = Color.Lerp(flashColor, originalBgColor, (t - 0.5f) * 2f);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        myCamera.backgroundColor = originalBgColor;
        colorFlashCoroutine = null;
    }

    private IEnumerator CelebrationShakeCoroutine() {
        Shake(0.15f, 0.08f);
        yield return new WaitForSeconds(0.1f);

        Shake(0.25f, 0.10f);
        yield return new WaitForSeconds(0.1f);

        Shake(0.35f, 0.12f);
    }

    private IEnumerator FOVPunchCoroutine(float targetFOV, float duration) {
        float elapsed = 0f;
        float punchDuration = duration * 0.3f;
        float returnDuration = duration * 0.7f;

        while (elapsed < punchDuration) {
            myCamera.fieldOfView = Mathf.Lerp(originalFOV, targetFOV, elapsed / punchDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        myCamera.fieldOfView = targetFOV;

        elapsed = 0f;
        while (elapsed < returnDuration) {
            myCamera.fieldOfView = Mathf.Lerp(targetFOV, originalFOV, elapsed / returnDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        myCamera.fieldOfView = originalFOV;
        fovPunchCoroutine = null;
    }

    public void GlitchShake() {
        StartCoroutine(GlitchShakeCoroutine());
    }

    private IEnumerator GlitchShakeCoroutine() {
        Vector3 o = originalLocalPos;
        transform.localPosition = o + new Vector3(0.08f, 0f, 0.05f);
        yield return null;
        transform.localPosition = o + new Vector3(-0.06f, 0f, -0.09f);
        yield return null;
        transform.localPosition = o + new Vector3(0.04f, 0f, 0.07f);
        yield return null;
        transform.localPosition = o + new Vector3(-0.03f, 0f, -0.04f);
        yield return null;
        transform.localPosition = originalLocalPos;
    }
}
