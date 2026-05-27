using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Types of cable pieces.
public enum PieceType {
    STRAIGHT_H,
    STRAIGHT_V,
    BEND_NE,
    BEND_NW,
    BEND_SE,
    BEND_SW
}


// Port connection directions.
public enum ConnectionDirection {
    North,
    South,
    East,
    West,
    None
}


// Cable piece component representing a segment on the connection grid.
public class CablePiece : MonoBehaviour {

    [SerializeField] private PieceType type;
    [SerializeField] private ConnectionDirection portA;
    [SerializeField] private ConnectionDirection portB;

    [SerializeField] private bool hasBeenTraversed = false;
    [SerializeField] private Material cableMaterial;
    [SerializeField] private List<Vector3> localPathPoints = new List<Vector3>();

    private const float SQUASH_DURATION = 0.05f;
    private const float STRETCH_DURATION = 0.08f;
    private const float SPRING_DURATION = 0.20f;

    public PieceType Type => type;
    public ConnectionDirection PortA => portA;
    public ConnectionDirection PortB => portB;
    public bool HasBeenTraversed => hasBeenTraversed;

    public List<Vector3> WorldPathPoints {
        get {
            List<Vector3> pts = new List<Vector3>();
            if (localPathPoints != null) {
                foreach (var lp in localPathPoints) {
                    pts.Add(transform.TransformPoint(lp));
                }
            }
            return pts;
        }
    }

    public bool IsHovered { get; set; }
    private Vector3 baseScale;
    private Vector3 targetScale;
    private Color hoverGlowColor = new Color(0f, 0.4f, 0.8f, 1f);
    private Color currentEmission = Color.black;
    private float hoverOffset = 0f;

    private Coroutine rotateCoroutine;
    private Coroutine pulseCoroutine;

    private void Awake() {
        baseScale = transform.localScale;
        targetScale = baseScale;
        StartCoroutine(SquashStretchCoroutine());
    }

    public void Setup(PieceType type, Material material, List<Vector3> localPath) {
        this.type = type;
        this.cableMaterial = material;
        this.localPathPoints = localPath;

        ResetPorts();
    }

    private void ResetPorts() {
        switch (type) {
            case PieceType.STRAIGHT_H:
                portA = ConnectionDirection.West;
                portB = ConnectionDirection.East;
                break;
            case PieceType.STRAIGHT_V:
                portA = ConnectionDirection.North;
                portB = ConnectionDirection.South;
                break;
            case PieceType.BEND_NE:
                portA = ConnectionDirection.North;
                portB = ConnectionDirection.East;
                break;
            case PieceType.BEND_NW:
                portA = ConnectionDirection.North;
                portB = ConnectionDirection.West;
                break;
            case PieceType.BEND_SE:
                portA = ConnectionDirection.South;
                portB = ConnectionDirection.East;
                break;
            case PieceType.BEND_SW:
                portA = ConnectionDirection.South;
                portB = ConnectionDirection.West;
                break;
        }
    }

    private void Update() {
        float targetScaleMult = IsHovered ? 1.15f : 1.0f;
        targetScale = baseScale * targetScaleMult;
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * 15f);

        float targetHoverOffset = IsHovered ? 0.08f : 0f;
        hoverOffset = Mathf.Lerp(hoverOffset, targetHoverOffset, Time.deltaTime * 15f);
        Vector3 pos = transform.position;
        pos.y = 0.025f + hoverOffset;
        transform.position = pos;

        if (cableMaterial != null && !hasBeenTraversed) {
            Color targetGlow;
            if (IsLockedByWorm) {
                targetGlow = Color.red * 1.5f;
            } else {
                targetGlow = IsHovered ? hoverGlowColor * 2.0f : Color.black;
            }
            currentEmission = Color.Lerp(currentEmission, targetGlow, Time.deltaTime * 10f);
            cableMaterial.SetColor("_EmissionColor", currentEmission);
        }
    }

    public bool IsGlitching { get; private set; }
    public bool IsLockedByWorm { get; private set; }

    public void RotateCW() {
        if (hasBeenTraversed || IsLockedByWorm) {
            return;
        }

        portA = RotateDirCW(portA);
        portB = RotateDirCW(portB);

        if (rotateCoroutine != null) {
            StopCoroutine(rotateCoroutine);
        }
        rotateCoroutine = StartCoroutine(RotateCoroutine(90f));

        StartCoroutine(SquashStretchCoroutine());

        ParticleSpawner.SpawnRotationSparks(transform.position, hoverGlowColor);
    }

    public void RotateByWorm() {
        if (IsGlitching || IsLockedByWorm) {
            return;
        }
        IsGlitching = true;
        IsLockedByWorm = true;
        StartCoroutine(GlitchAnim());
    }

    private IEnumerator GlitchAnim() {
        const float FLICKER_T = 0.1f;
        const float SQUASH_T  = 0.15f;
        const float BOUNCE_T  = 0.15f;
        const float FADE_T    = 0.2f;

        Vector3 originalScale = transform.localScale;
        Color originalEmission = currentEmission;

        if (cableMaterial != null) {
            for (int i = 0; i < 2; i++) {
                cableMaterial.SetColor("_EmissionColor", Color.red * 4f);
                yield return new WaitForSeconds(FLICKER_T / 2f);
                cableMaterial.SetColor("_EmissionColor", new Color(0.1f, 0.9f, 0.2f) * 4f);
                yield return new WaitForSeconds(FLICKER_T / 2f);
            }
        }

        float t = 0f;
        while (t < SQUASH_T) {
            float s = Mathf.Lerp(1f, 0.3f, t / SQUASH_T);
            transform.localScale = new Vector3(originalScale.x, originalScale.y * s, originalScale.z);
            t += Time.deltaTime;
            yield return null;
        }

        portA = RotateDirCW(portA);
        portB = RotateDirCW(portB);
        transform.localRotation = transform.localRotation * Quaternion.Euler(0f, 90f, 0f);

        t = 0f;
        while (t < BOUNCE_T) {
            float s = Mathf.Lerp(0.3f, 1.15f, t / BOUNCE_T);
            transform.localScale = new Vector3(originalScale.x, originalScale.y * s, originalScale.z);
            t += Time.deltaTime;
            yield return null;
        }

        t = 0f;
        while (t < FADE_T) {
            float s = Mathf.Lerp(1.15f, 1f, t / FADE_T);
            transform.localScale = new Vector3(originalScale.x, originalScale.y * s, originalScale.z);
            if (cableMaterial != null) {
                cableMaterial.SetColor("_EmissionColor", Color.Lerp(new Color(0.1f, 0.9f, 0.2f) * 4f, originalEmission, t / FADE_T));
            }
            t += Time.deltaTime;
            yield return null;
        }

        transform.localScale = originalScale;
        IsGlitching = false;
    }

    public void RotateInstant(int rotations) {
        for (int i = 0; i < rotations; i++) {
            portA = RotateDirCW(portA);
            portB = RotateDirCW(portB);
        }
        transform.localRotation = Quaternion.Euler(0f, rotations * 90f, 0f);
    }

    private IEnumerator RotateCoroutine(float angle) {
        Quaternion startRot = transform.localRotation;
        Quaternion endRot = startRot * Quaternion.Euler(0f, angle, 0f);
        float elapsed = 0f;
        float duration = 0.12f;

        while (elapsed < duration) {
            transform.localRotation = Quaternion.Slerp(startRot, endRot, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localRotation = endRot;
    }

    public static ConnectionDirection RotateDirCW(ConnectionDirection dir) {
        switch (dir) {
            case ConnectionDirection.North: return ConnectionDirection.East;
            case ConnectionDirection.East: return ConnectionDirection.South;
            case ConnectionDirection.South: return ConnectionDirection.West;
            case ConnectionDirection.West: return ConnectionDirection.North;
            default: return ConnectionDirection.None;
        }
    }

    public ConnectionDirection GetExit(ConnectionDirection entry) {
        if (entry == portA) {
            return portB;
        }
        if (entry == portB) {
            return portA;
        }
        return ConnectionDirection.None;
    }

    public void SetTraversed() {
        hasBeenTraversed = true;

        Color speedColor = Color.cyan;
        if (SignalController.Instance != null) {
            speedColor = SignalController.Instance.CurrentSpeedColor;
        }

        if (pulseCoroutine != null) {
            StopCoroutine(pulseCoroutine);
        }
        pulseCoroutine = StartCoroutine(PulseEmissionCoroutine(speedColor));

        var pts = WorldPathPoints;
        if (pts != null && pts.Count > 0) {
            int sparkCount = 40;
            if (GameManager.Instance != null) {
                sparkCount = GameManager.Instance.SparkCount;
            }
            ParticleSpawner.SpawnAlongCurve(pts, speedColor, sparkCount);
        }

        if (UIOverlay.Instance != null) {
            UIOverlay.Instance.SpawnFloatingText(transform.position);
        }
    }

    public void ShakeOnGameOver() {
        StartCoroutine(GameOverShakeCoroutine());
    }

    private IEnumerator SquashStretchCoroutine() {
        float elapsed = 0f;
        Vector3 targetScaleSquash = new Vector3(baseScale.x * 1.3f, baseScale.y * 0.6f, baseScale.z * 1.3f);
        while (elapsed < SQUASH_DURATION) {
            transform.localScale = Vector3.Lerp(baseScale, targetScaleSquash, elapsed / SQUASH_DURATION);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = targetScaleSquash;

        elapsed = 0f;
        Vector3 startScale2 = transform.localScale;
        Vector3 targetScaleStretch = new Vector3(baseScale.x * 0.9f, baseScale.y * 1.2f, baseScale.z * 0.9f);
        while (elapsed < STRETCH_DURATION) {
            transform.localScale = Vector3.Lerp(startScale2, targetScaleStretch, elapsed / STRETCH_DURATION);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = targetScaleStretch;

        elapsed = 0f;
        Vector3 startScale3 = transform.localScale;
        Vector3 overshootScale = new Vector3(baseScale.x * 1.05f, baseScale.y * 0.95f, baseScale.z * 1.05f);
        while (elapsed < SPRING_DURATION) {
            float t = elapsed / SPRING_DURATION;
            if (t < 0.5f) {
                transform.localScale = Vector3.Lerp(startScale3, overshootScale, t * 2f);
            } else {
                transform.localScale = Vector3.Lerp(overshootScale, baseScale, (t - 0.5f) * 2f);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = baseScale;
    }

    private IEnumerator PulseEmissionCoroutine(Color pulseColor) {
        if (cableMaterial == null) {
            yield break;
        }

        float duration = 0.3f;
        float halfDuration = duration / 2f;
        float elapsed = 0f;

        while (elapsed < halfDuration) {
            float t = elapsed / halfDuration;
            cableMaterial.SetColor("_EmissionColor", Color.Lerp(Color.black, pulseColor * 3f, t));
            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < halfDuration) {
            float t = elapsed / halfDuration;
            cableMaterial.SetColor("_EmissionColor", Color.Lerp(pulseColor * 3f, Color.black, t));
            elapsed += Time.deltaTime;
            yield return null;
        }

        cableMaterial.SetColor("_EmissionColor", Color.black);
    }

    private IEnumerator GameOverShakeCoroutine() {
        float duration = 0.3f;
        float elapsed = 0f;
        Quaternion originalRotation = transform.localRotation;

        while (elapsed < duration) {
            Vector3 randomRotation = new Vector3(
                Random.Range(-8f, 8f),
                Random.Range(-8f, 8f),
                Random.Range(-8f, 8f)
            );
            transform.localRotation = originalRotation * Quaternion.Euler(randomRotation);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localRotation = originalRotation;
    }

    public static Vector3 GetPortLocalPos(ConnectionDirection dir) {
        switch (dir) {
            case ConnectionDirection.North: return new Vector3(0f, 0.026f, 0.5f);
            case ConnectionDirection.South: return new Vector3(0f, 0.026f, -0.5f);
            case ConnectionDirection.East: return new Vector3(0.5f, 0.026f, 0f);
            case ConnectionDirection.West: return new Vector3(-0.5f, 0.026f, 0f);
            default: return Vector3.zero;
        }
    }
}
