using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Worm antagonist controller — state machine + dynamic tube mesh.
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class WormController : MonoBehaviour {

    public static WormController Instance { get; private set; }

    public enum WormState { Idle, Crawling, Corrupting, Retreating }

    [Header("Ajustes del Gusano")]
    [SerializeField] private float baseInterval = 5.2f;
    [SerializeField] private float minInterval = 2.0f;
    [SerializeField] private float wormSpeed = 4f;
    [SerializeField] private bool showTrail = true;
    [SerializeField] private bool enableWorm = true;

    private WormState state = WormState.Idle;
    private int currentLevel = 1;

    private Mesh _wormMesh;
    private Vector3[] _spine = new Vector3[6];
    private MeshFilter _mf;
    private MeshRenderer _mr;
    private CablePiece _targetTile;

    private Coroutine wormCycleCoroutine;

    private const float CORRUPT_DURATION = 0.6f;
    private const float RETREAT_DURATION = 0.8f;
    private const float WORM_RADIUS = 0.06f;

    public WormState State => state;
    public bool EnableWorm { get => enableWorm; set => enableWorm = value; }
    public float BaseInterval { get => baseInterval; set => baseInterval = value; }
    public float MinInterval { get => minInterval; set => minInterval = value; }
    public float WormSpeed { get => wormSpeed; set => wormSpeed = value; }
    public bool ShowTrail { get => showTrail; set => showTrail = value; }

    private void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
            return;
        }

        _mf = GetComponent<MeshFilter>();
        _mr = GetComponent<MeshRenderer>();

        Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (litShader == null) {
            litShader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        Material mat = new Material(litShader);
        mat.SetColor("_BaseColor", new Color(0.1f, 0.9f, 0.2f, 1f));
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(0.1f, 0.9f, 0.2f) * 3f);
        _mr.material = mat;
        _mr.enabled = false;
    }

    private void Update() {
        if (state == WormState.Idle) {
            return;
        }

        for (int i = 1; i < 6; i++) {
            _spine[i] = Vector3.Lerp(_spine[i], _spine[i - 1], Time.deltaTime * 8f);
        }

        Vector3[] animatedSpine = new Vector3[6];
        for (int i = 0; i < 6; i++) {
            Vector3 offset = new Vector3(
                Mathf.Sin(Time.time * 4f + i * 0.8f) * 0.04f,
                0f,
                Mathf.Cos(Time.time * 3f + i * 1.1f) * 0.04f
            );
            animatedSpine[i] = _spine[i] + offset;
        }

        float flicker = 1f + Mathf.Sin(Time.time * 20f) * 0.4f;
        if (_mr != null && _mr.material != null) {
            _mr.material.SetColor("_EmissionColor", new Color(0.1f, 0.9f, 0.2f) * 3f * flicker);
        }

        RebuildMesh(animatedSpine);
    }

    public void StartWorm() {
        if (!enableWorm) {
            return;
        }

        StopWorm();
        wormCycleCoroutine = StartCoroutine(WormCycle());
    }

    public void StopWorm() {
        if (wormCycleCoroutine != null) {
            StopCoroutine(wormCycleCoroutine);
            wormCycleCoroutine = null;
        }
        if (_mr != null) {
            _mr.enabled = false;
        }
        state = WormState.Idle;
    }

    public void SetLevel(int level) {
        currentLevel = level;
    }

    private IEnumerator WormCycle() {
        while (true) {
            if (_mr != null) {
                _mr.enabled = false;
            }
            state = WormState.Idle;
            float wait = Mathf.Max(minInterval, baseInterval - (currentLevel - 1) * 0.3f);
            yield return new WaitForSeconds(wait);

            if (GridManager.Instance == null || !enableWorm) {
                continue;
            }

            List<CablePiece> candidates = new List<CablePiece>();
            int size = GridManager.Instance.GridSize;
            int startZ = size / 2;

            for (int x = 0; x < size; x++) {
                for (int z = 0; z < size; z++) {
                    if (x == 0 && z == startZ) {
                        continue;
                    }
                    if (x == size - 1 && z == startZ) {
                        continue;
                    }

                    var cellData = GridManager.Instance.GetCellData(x, z);
                    if (cellData.piece != null && !cellData.piece.IsGlitching && !cellData.piece.IsLockedByWorm) {
                        candidates.Add(cellData.piece);
                    }
                }
            }

            if (candidates.Count == 0) {
                yield return new WaitForSeconds(1f);
                continue;
            }

            _targetTile = candidates[Random.Range(0, candidates.Count)];

            state = WormState.Crawling;
            if (_mr != null) {
                _mr.enabled = true;
            }

            Vector3 entryPos = GetRandomBorderPos();
            for (int i = 0; i < 6; i++) {
                _spine[i] = entryPos;
            }

            Vector3 targetPos = _targetTile.transform.position;

            while (Vector3.Distance(_spine[0], targetPos) > 0.05f) {
                _spine[0] = Vector3.MoveTowards(_spine[0], targetPos, wormSpeed * Time.deltaTime);

                if (showTrail) {
                    ParticleSpawner.EmitTrail(_spine[0]);
                }

                yield return null;
            }

            state = WormState.Corrupting;
            ParticleSpawner.EmitGlitch(_targetTile.transform.position);
            _targetTile.RotateByWorm();

            if (ScreenShakeController.Instance != null) {
                ScreenShakeController.Instance.GlitchShake();
            }

            PathSolver.Instance.Solve();

            yield return new WaitForSeconds(CORRUPT_DURATION);

            state = WormState.Retreating;
            Vector3 retreatPos = GetRandomBorderPos();

            while (Vector3.Distance(_spine[0], retreatPos) > 0.05f) {
                _spine[0] = Vector3.MoveTowards(_spine[0], retreatPos, wormSpeed * Time.deltaTime);

                if (showTrail) {
                    ParticleSpawner.EmitTrail(_spine[0]);
                }

                yield return null;
            }

            yield return new WaitForSeconds(RETREAT_DURATION);
        }
    }

    private Vector3 GetRandomBorderPos() {
        if (GridManager.Instance == null) {
            return Vector3.zero;
        }

        int size = GridManager.Instance.GridSize;
        int side = Random.Range(0, 4);

        float x = 0f, z = 0f;
        switch (side) {
            case 0:
                x = -2f;
                z = Random.Range(0f, size - 1f);
                break;
            case 1:
                x = size + 1f;
                z = Random.Range(0f, size - 1f);
                break;
            case 2:
                x = Random.Range(0f, size - 1f);
                z = -2f;
                break;
            case 3:
                x = Random.Range(0f, size - 1f);
                z = size + 1f;
                break;
        }

        return new Vector3(x, 0.025f, z);
    }

    private void RebuildMesh(Vector3[] spine) {
        if (_wormMesh == null) {
            _wormMesh = new Mesh();
            _wormMesh.name = "WormMesh";
            _wormMesh.MarkDynamic();
            if (_mf != null) {
                _mf.mesh = _wormMesh;
            }
        }

        Vector3[] vertices = new Vector3[36];
        List<int> triangles = new List<int>();

        for (int i = 0; i < 6; i++) {
            float radius = WORM_RADIUS;
            if (i == 0 || i == 5) {
                radius = 0f;
            }

            Vector3 T;
            if (i == 0) {
                T = (spine[1] - spine[0]).normalized;
            } else if (i == 5) {
                T = (spine[5] - spine[4]).normalized;
            } else {
                T = (spine[i + 1] - spine[i - 1]).normalized;
            }

            if (T.sqrMagnitude < 0.0001f) {
                T = Vector3.forward;
            }

            Vector3 N = Vector3.Cross(T, Vector3.up).normalized;
            if (N.sqrMagnitude < 0.0001f) {
                N = Vector3.right;
            }
            Vector3 B = Vector3.Cross(T, N).normalized;

            for (int j = 0; j < 6; j++) {
                float angle = j * 2f * Mathf.PI / 6f;
                Vector3 offset = radius * (Mathf.Cos(angle) * N + Mathf.Sin(angle) * B);
                vertices[i * 6 + j] = spine[i] + offset;
            }
        }

        for (int i = 0; i < 5; i++) {
            for (int j = 0; j < 6; j++) {
                int nextJ = (j + 1) % 6;
                int v0 = i * 6 + j;
                int v1 = i * 6 + nextJ;
                int v2 = (i + 1) * 6 + j;
                int v3 = (i + 1) * 6 + nextJ;

                triangles.Add(v0);
                triangles.Add(v2);
                triangles.Add(v1);

                triangles.Add(v1);
                triangles.Add(v2);
                triangles.Add(v3);
            }
        }

        _wormMesh.Clear();
        _wormMesh.vertices = vertices;
        _wormMesh.triangles = triangles.ToArray();
        _wormMesh.RecalculateNormals();
        _wormMesh.RecalculateBounds();
    }

    // [NEW - MathUnlock]
    public void PunishWrongAnswer() {
        if (GridManager.Instance == null) {
            return;
        }
        var candidates = GridManager.Instance.AllTiles
            .Where(t => t != null && !t.IsLocked && !t.IsRouterCell).ToList();
        if (candidates.Count == 0) {
            return;
        }
        var target = candidates[Random.Range(0, candidates.Count)];
        target.RotateByWorm();
    }
}


// Path solver singleton to detect connection state from Start to End.
public class PathSolver {

    public static PathSolver Instance { get; } = new PathSolver();

    private bool wasPathSolved = false;

    public void Solve() {
        bool isCurrentlySolved = CheckPath();
        if (wasPathSolved && !isCurrentlySolved) {
            if (UIOverlay.Instance != null) {
                UIOverlay.Instance.ShowWarning(1.0f);
            }
        }
        wasPathSolved = isCurrentlySolved;
    }

    public void Reset() {
        wasPathSolved = CheckPath();
    }

    private bool CheckPath() {
        if (GridManager.Instance == null) {
            return false;
        }

        int gridSize = GridManager.Instance.GridSize;
        int currentX = 0;
        int currentZ = gridSize / 2;
        ConnectionDirection entryDir = ConnectionDirection.West;

        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        while (true) {
            if (currentX == gridSize - 1 && currentZ == gridSize / 2) {
                return true;
            }

            Vector2Int pos = new Vector2Int(currentX, currentZ);
            if (visited.Contains(pos)) {
                return false;
            }
            visited.Add(pos);

            GridManager.CellData cell = GridManager.Instance.GetCellData(currentX, currentZ);
            if (!cell.occupied || cell.piece == null) {
                return false;
            }

            ConnectionDirection exitDir = cell.piece.GetExit(entryDir);
            if (exitDir == ConnectionDirection.None) {
                return false;
            }

            switch (exitDir) {
                case ConnectionDirection.North:
                    currentZ += 1;
                    entryDir = ConnectionDirection.South;
                    break;
                case ConnectionDirection.South:
                    currentZ -= 1;
                    entryDir = ConnectionDirection.North;
                    break;
                case ConnectionDirection.East:
                    currentX += 1;
                    entryDir = ConnectionDirection.West;
                    break;
                case ConnectionDirection.West:
                    currentX -= 1;
                    entryDir = ConnectionDirection.East;
                    break;
            }

            if (currentX < 0 || currentX >= gridSize || currentZ < 0 || currentZ >= gridSize) {
                return false;
            }
        }
    }
}
