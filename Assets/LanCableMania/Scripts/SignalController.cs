using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Manages the movement and visual updates of the active path signal.
public class SignalController : MonoBehaviour {

    public static SignalController Instance { get; private set; }

    private GameObject sphereGO;
    private Material signalMaterial;
    private TrailRenderer trailRenderer;
    private Coroutine travelCoroutine;
    private Coroutine trailUpdateCoroutine;

    private int currentCombo = 0;

    public Color CurrentSpeedColor {
        get {
            float speed = GameManager.Instance != null ? GameManager.Instance.SignalSpeed : 1.0f;
            return GetSpeedColor(speed);
        }
    }

    public int CurrentCombo => currentCombo;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
            return;
        }
    }

    private void Start() {
        ResetSignal();
    }

    private void Update() {
        if (sphereGO != null) {
            sphereGO.transform.localScale = Vector3.one * 0.3f + Vector3.one * (Mathf.Sin(Time.time * 8f) * 0.05f);

            if (signalMaterial != null) {
                float emissionIntensity = 3f + Mathf.Sin(Time.time * 8f) * 1f;
                signalMaterial.SetColor("_EmissionColor", CurrentSpeedColor * emissionIntensity);
            }
        }
    }

    public void ResetSignal() {
        if (travelCoroutine != null) {
            StopCoroutine(travelCoroutine);
            travelCoroutine = null;
        }
        if (trailUpdateCoroutine != null) {
            StopCoroutine(trailUpdateCoroutine);
            trailUpdateCoroutine = null;
        }

        if (sphereGO != null) {
            Destroy(sphereGO);
        }

        currentCombo = 0;

        int gridSize = GridManager.Instance != null ? GridManager.Instance.GridSize : 8;
        int startX = 0;
        int startZ = gridSize / 2;

        sphereGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphereGO.name = "SignalSphere";
        sphereGO.tag = "LCMGrid";
        sphereGO.transform.localScale = Vector3.one * 0.3f;

        Collider col = sphereGO.GetComponent<Collider>();
        if (col != null) {
            Destroy(col);
        }

        signalMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        signalMaterial.EnableKeyword("_EMISSION");
        signalMaterial.SetColor("_BaseColor", Color.white);
        sphereGO.GetComponent<Renderer>().material = signalMaterial;

        trailRenderer = sphereGO.AddComponent<TrailRenderer>();
        trailRenderer.startWidth = 0.08f;
        trailRenderer.endWidth = 0f;
        trailRenderer.time = 0.3f;

        Material trailMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        trailRenderer.material = trailMat;

        sphereGO.transform.position = new Vector3(startX, 0.026f, startZ);
    }

    public void StartSignalTraversal() {
        ResetSignal();
        trailUpdateCoroutine = StartCoroutine(UpdateTrailGradientCoroutine());
        travelCoroutine = StartCoroutine(SignalTravelCoroutine());
    }

    private IEnumerator SignalTravelCoroutine() {
        int gridSize = GridManager.Instance != null ? GridManager.Instance.GridSize : 8;
        int currentX = 0;
        int currentZ = gridSize / 2;
        ConnectionDirection entryDir = ConnectionDirection.West;

        float speed = GameManager.Instance != null ? GameManager.Instance.SignalSpeed : 1.0f;

        while (true) {
            if (currentX == gridSize - 1 && currentZ == gridSize / 2) {
                Vector3 startPos = sphereGO.transform.position;
                Vector3 endPos = new Vector3(currentX, 0.026f, currentZ);
                float slideElapsed = 0f;
                float slideDuration = 0.3f / speed;

                while (slideElapsed < slideDuration) {
                    sphereGO.transform.position = Vector3.Lerp(startPos, endPos, slideElapsed / slideDuration);
                    slideElapsed += Time.deltaTime;
                    yield return null;
                }
                sphereGO.transform.position = endPos;

                if (GameManager.Instance != null) {
                    GameManager.Instance.AdvanceRound();
                }
                yield break;
            }

            if (GridManager.Instance == null) {
                yield break;
            }
            GridManager.CellData cell = GridManager.Instance.GetCellData(currentX, currentZ);

            if (!cell.occupied || cell.piece == null) {
                TriggerGameOver();
                yield break;
            }

            ConnectionDirection exitDir = cell.piece.GetExit(entryDir);
            if (exitDir == ConnectionDirection.None) {
                if (ScreenShakeController.Instance != null) {
                    ScreenShakeController.Instance.WrongPlacementShake();
                }
                if (UIOverlay.Instance != null) {
                    UIOverlay.Instance.ShowWrongConnectionText();
                }
                TriggerGameOver();
                yield break;
            }

            currentCombo++;
            cell.piece.SetTraversed();
            if (GameManager.Instance != null) {
                GameManager.Instance.AddScore();
            }

            List<Vector3> travelPath = new List<Vector3>(cell.piece.WorldPathPoints);
            bool isReverse = (entryDir == cell.piece.PortB);
            if (isReverse) {
                travelPath.Reverse();
            }

            float duration = 1.0f / speed;
            float elapsed = 0f;

            while (elapsed < duration) {
                speed = GameManager.Instance != null ? GameManager.Instance.SignalSpeed : 1.0f;
                duration = 1.0f / speed;

                float t = elapsed / duration;
                float floatIdx = t * (travelPath.Count - 1);
                int idxA = Mathf.Clamp(Mathf.FloorToInt(floatIdx), 0, travelPath.Count - 1);
                int idxB = Mathf.Clamp(idxA + 1, 0, travelPath.Count - 1);
                float lerpT = floatIdx - idxA;

                if (sphereGO != null) {
                    sphereGO.transform.position = Vector3.Lerp(travelPath[idxA], travelPath[idxB], lerpT);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (sphereGO != null) {
                sphereGO.transform.position = travelPath[travelPath.Count - 1];
            }

            int nextX = currentX;
            int nextZ = currentZ;
            ConnectionDirection nextEntryDir = ConnectionDirection.None;

            switch (exitDir) {
                case ConnectionDirection.North:
                    nextZ++;
                    nextEntryDir = ConnectionDirection.South;
                    break;
                case ConnectionDirection.South:
                    nextZ--;
                    nextEntryDir = ConnectionDirection.North;
                    break;
                case ConnectionDirection.East:
                    nextX++;
                    nextEntryDir = ConnectionDirection.West;
                    break;
                case ConnectionDirection.West:
                    nextX--;
                    nextEntryDir = ConnectionDirection.East;
                    break;
            }

            if (nextX < 0 || nextX >= gridSize || nextZ < 0 || nextZ >= gridSize) {
                if (nextX == gridSize - 1 && nextZ == gridSize / 2) {
                    currentX = nextX;
                    currentZ = nextZ;
                    entryDir = nextEntryDir;
                    continue;
                }

                TriggerGameOver();
                yield break;
            }

            currentX = nextX;
            currentZ = nextZ;
            entryDir = nextEntryDir;
        }
    }

    private void TriggerGameOver() {
        if (GameManager.Instance != null) {
            GameManager.Instance.GameOver();
        }
    }

    private IEnumerator UpdateTrailGradientCoroutine() {
        while (true) {
            if (trailRenderer != null) {
                Gradient g = new Gradient();
                Color colorVal = CurrentSpeedColor;

                g.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(colorVal, 0f), new GradientColorKey(colorVal, 1f) },
                    new GradientAlphaKey[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) }
                );
                trailRenderer.colorGradient = g;
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    private Color GetSpeedColor(float speed) {
        if (speed <= 1.5f) {
            float t = Mathf.Clamp01((speed - 0.8f) / 0.7f);
            return Color.Lerp(Color.cyan, Color.yellow, t);
        } else {
            float t = Mathf.Clamp01((speed - 1.5f) / 1.0f);
            return Color.Lerp(Color.yellow, Color.red, t);
        }
    }
}
