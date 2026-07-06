using System.Collections.Generic;
using UnityEngine;

// Manages the placement, grid layout, mouse hovers, and rotation of cables.
public class GridManager : MonoBehaviour {

    public static GridManager Instance { get; private set; }

    [SerializeField] private float curveStrength = 0.5f;

    private int gridSize = 8;
    private CellData[,] cellGrid;
    private GameObject[,] cellObjects;
    private Material[,] cellMaterials;

    private Camera mainCamera;

    public int GridSize => gridSize;

    public struct CellData {
        public bool occupied;
        public CablePiece piece;
        public bool hasSignal;
    }

    private void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
            return;
        }
    }

    private void Start() {
        mainCamera = Camera.main;

        if (BindToExistingGrid()) {
            Debug.Log("Successfully bound to existing edit-time grid in hierarchy.");
            ReApplyGridVisuals();
            CenterCamera();
        } else {
            if (GameManager.Instance != null) {
                gridSize = GameManager.Instance.StartingGridSize;
            }
            RebuildGrid(gridSize);
        }
    }

    private void Update() {
        if (mainCamera == null) {
            mainCamera = Camera.main;
            if (mainCamera == null) {
                return;
            }
        }

        if (GameManager.Instance == null || GameManager.Instance.State == GameManager.GameState.GameOver) {
            return;
        }

        int hoverX = -1;
        int hoverZ = -1;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, Vector3.zero);
        if (plane.Raycast(ray, out float enter)) {
            Vector3 hitPoint = ray.GetPoint(enter);
            hoverX = Mathf.RoundToInt(hitPoint.x);
            hoverZ = Mathf.RoundToInt(hitPoint.z);
        }

        if (hoverX < 0 || hoverX >= gridSize || hoverZ < 0 || hoverZ >= gridSize) {
            hoverX = -1;
            hoverZ = -1;
        }

        for (int x = 0; x < gridSize; x++) {
            for (int z = 0; z < gridSize; z++) {
                GameObject cellGO = cellObjects[x, z];
                Material cellMat = cellMaterials[x, z];
                if (cellGO == null || cellMat == null) {
                    continue;
                }

                bool isHovered = (x == hoverX && z == hoverZ);
                float targetY = 0f;
                Color targetColor = new Color(0.15f, 0.15f, 0.2f);

                CablePiece piece = cellGrid[x, z].piece;
                if (piece != null) {
                    if (x == 0 && z == gridSize / 2) {
                        piece.IsHovered = false;
                    } else {
                        piece.IsHovered = isHovered && GameManager.Instance.State == GameManager.GameState.PlacingCables && !piece.IsLockedByWorm;
                    }
                }

                if (x == 0 && z == gridSize / 2) {
                    if (isHovered) {
                        targetColor = new Color(0.3f, 0.8f, 0.3f);
                        targetY = Mathf.Sin(Time.time * 4f) * 0.02f;
                    } else {
                        targetColor = new Color(0.2f, 0.6f, 0.2f);
                    }
                } else if (x == gridSize - 1 && z == gridSize / 2) {
                    if (isHovered) {
                        targetColor = new Color(0.8f, 0.3f, 0.3f);
                        targetY = Mathf.Sin(Time.time * 4f) * 0.02f;
                    } else {
                        targetColor = new Color(0.8f, 0.2f, 0.2f);
                    }
                } else {
                    if (piece != null && piece.IsLockedByWorm) {
                        targetColor = new Color(0.5f, 0.05f, 0.05f);
                    } else if (isHovered && GameManager.Instance.State == GameManager.GameState.PlacingCables) {
                        targetColor = new Color(0.35f, 0.35f, 0.45f);
                        targetY = Mathf.Sin(Time.time * 4f) * 0.02f;
                    }
                }

                Vector3 pos = cellGO.transform.position;
                pos.y = Mathf.Lerp(pos.y, targetY, Time.deltaTime * 10f);
                cellGO.transform.position = pos;

                Color curColor = cellMat.GetColor("_BaseColor");
                cellMat.SetColor("_BaseColor", Color.Lerp(curColor, targetColor, Time.deltaTime * 10f));
            }
        }

        if (Input.GetMouseButtonDown(0)) {
            Debug.Log($"[LCM-Click] Mouse click. Game State: {GameManager.Instance.State}");
            if (GameManager.Instance.State == GameManager.GameState.PlacingCables) {
                if (Physics.Raycast(ray, out RaycastHit hit)) {
                    Debug.Log($"[LCM-Click] Raycast hit GameObject: {hit.collider.gameObject.name}");
                    for (int x = 0; x < gridSize; x++) {
                        for (int z = 0; z < gridSize; z++) {
                            if (cellObjects[x, z] == hit.collider.gameObject) {
                                Debug.Log($"[LCM-Click] Match grid cell at ({x}, {z}). Rotating...");
                                RotateCableAt(x, z);
                                return;
                            }
                        }
                    }
                } else {
                    Debug.Log("[LCM-Click] Raycast hit nothing.");
                }
            }
        }
    }

    public CellData GetCellData(int x, int z) {
        if (x < 0 || x >= gridSize || z < 0 || z >= gridSize) {
            return new CellData();
        }
        return cellGrid[x, z];
    }

    private bool BindToExistingGrid() {
        GameObject[] gridObjs = GameObject.FindGameObjectsWithTag("LCMGrid");
        if (gridObjs.Length == 0) {
            return false;
        }

        int maxCoord = 0;
        foreach (var go in gridObjs) {
            if (go.name.StartsWith("GridCell_")) {
                string[] parts = go.name.Split('_');
                if (parts.Length >= 3) {
                    int x = int.Parse(parts[1]);
                    int z = int.Parse(parts[2]);
                    maxCoord = Mathf.Max(maxCoord, x, z);
                }
            }
        }

        if (maxCoord == 0) {
            return false;
        }

        gridSize = maxCoord + 1;
        cellGrid = new CellData[gridSize, gridSize];
        cellObjects = new GameObject[gridSize, gridSize];
        cellMaterials = new Material[gridSize, gridSize];

        foreach (var go in gridObjs) {
            if (go.name.StartsWith("GridCell_")) {
                string[] parts = go.name.Split('_');
                int x = int.Parse(parts[1]);
                int z = int.Parse(parts[2]);
                cellObjects[x, z] = go;

                Renderer r = go.GetComponent<Renderer>();
                if (r != null) {
                    cellMaterials[x, z] = r.material;
                }
            }
        }

        foreach (var go in gridObjs) {
            if (go.name.StartsWith("Cable_") || go.name == "StartCable") {
                int x = 0;
                int z = gridSize / 2;

                if (go.name.StartsWith("Cable_")) {
                    string[] parts = go.name.Split('_');
                    x = int.Parse(parts[1]);
                    z = int.Parse(parts[2]);
                }

                CablePiece piece = go.GetComponent<CablePiece>();
                if (piece != null) {
                    cellGrid[x, z].occupied = true;
                    cellGrid[x, z].piece = piece;
                }
            }
        }

        return true;
    }

    private void ReApplyGridVisuals() {
        int startZ = gridSize / 2;

        GameObject startCell = cellObjects[0, startZ];
        if (startCell != null) {
            Material mat = cellMaterials[0, startZ];
            if (mat != null) {
                mat.SetColor("_BaseColor", new Color(0.2f, 0.6f, 0.2f));
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.green * 2.0f);
            }

            Transform routerTrans = startCell.transform.Find("ProceduralRouter");
            GameObject routerGO = null;
            if (routerTrans != null) {
                routerGO = routerTrans.gameObject;
            } else {
                routerGO = RouterMeshBuilder.BuildRouter(Color.green);
                routerGO.name = "ProceduralRouter";
                routerGO.transform.SetParent(startCell.transform);

#if UNITY_EDITOR
                if (!Application.isPlaying) {
                    UnityEditor.Undo.RegisterCreatedObjectUndo(routerGO, "Create Start Router");
                }
#endif
            }

            routerGO.transform.localPosition = new Vector3(0f, 0.2f / startCell.transform.localScale.y, 0f);
            routerGO.transform.localScale = new Vector3(0.4f, 0.4f / startCell.transform.localScale.y, 0.4f);
        }

        GameObject endCell = cellObjects[gridSize - 1, startZ];
        if (endCell != null) {
            Material mat = cellMaterials[gridSize - 1, startZ];
            if (mat != null) {
                mat.SetColor("_BaseColor", new Color(0.8f, 0.2f, 0.2f));
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.red * 2.0f);
            }

            Transform routerTrans = endCell.transform.Find("ProceduralRouter");
            GameObject routerGO = null;
            if (routerTrans != null) {
                routerGO = routerTrans.gameObject;
            } else {
                routerGO = RouterMeshBuilder.BuildRouter(Color.red);
                routerGO.name = "ProceduralRouter";
                routerGO.transform.SetParent(endCell.transform);

#if UNITY_EDITOR
                if (!Application.isPlaying) {
                    UnityEditor.Undo.RegisterCreatedObjectUndo(routerGO, "Create End Router");
                }
#endif
            }

            routerGO.transform.localPosition = new Vector3(0f, 0.2f / endCell.transform.localScale.y, 0f);
            routerGO.transform.localScale = new Vector3(0.4f, 0.4f / endCell.transform.localScale.y, 0.4f);
        }
    }

    public void RebuildGrid(int newSize) {
        gridSize = newSize;

        GameObject[] toDestroy = GameObject.FindGameObjectsWithTag("LCMGrid");
        foreach (GameObject go in toDestroy) {
            if (Application.isPlaying) {
                Destroy(go);
            } else {
#if UNITY_EDITOR
                UnityEditor.Undo.DestroyObjectImmediate(go);
#else
                DestroyImmediate(go);
#endif
            }
        }

        cellGrid = new CellData[gridSize, gridSize];
        cellObjects = new GameObject[gridSize, gridSize];
        cellMaterials = new Material[gridSize, gridSize];

        for (int x = 0; x < gridSize; x++) {
            for (int z = 0; z < gridSize; z++) {
                GameObject cellGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cellGO.name = "GridCell_" + x + "_" + z;
                cellGO.tag = "LCMGrid";
                cellGO.transform.position = new Vector3(x * 1f, 0f, z * 1f);
                cellGO.transform.localScale = new Vector3(0.9f, 0.05f, 0.9f);
                cellGO.transform.SetParent(this.transform);

                Material cellMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                cellMat.SetColor("_BaseColor", new Color(0.15f, 0.15f, 0.2f));

                cellGO.GetComponent<Renderer>().material = cellMat;
                cellObjects[x, z] = cellGO;
                cellMaterials[x, z] = cellMat;

#if UNITY_EDITOR
                if (!Application.isPlaying) {
                    UnityEditor.Undo.RegisterCreatedObjectUndo(cellGO, "Create Cell");
                }
#endif
            }
        }

        SpawnStartCable(0, gridSize / 2);
        PrePopulateCables();
        ReApplyGridVisuals();
        CenterCamera();
    }

    private void CenterCamera() {
        if (mainCamera == null) {
            mainCamera = Camera.main;
        }
        if (mainCamera != null) {
            float gridCenter = (gridSize - 1) * 0.5f;
            float camHeight = gridSize * 0.7f + 1f;
            Vector3 newCamPos = new Vector3(gridCenter, camHeight, gridCenter);
            mainCamera.transform.position = newCamPos;
            mainCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            ScreenShakeController shake = mainCamera.GetComponent<ScreenShakeController>();
            if (shake != null) {
                shake.UpdateOriginalPosition(newCamPos);
            }
        }
    }

    private void PrePopulateCables() {
        for (int x = 0; x < gridSize; x++) {
            for (int z = 0; z < gridSize; z++) {
                if (x == 0 && z == gridSize / 2) {
                    continue;
                }
                if (x == gridSize - 1 && z == gridSize / 2) {
                    continue;
                }

                PieceType type = (PieceType)Random.Range(0, 6);

                GameObject cableGO = new GameObject("Cable_" + x + "_" + z);
                cableGO.transform.position = new Vector3(x, 0.025f, z);
                cableGO.tag = "LCMGrid";
                cableGO.transform.SetParent(this.transform);

                GetPortsForPiece(type, out Vector3 portALocal, out Vector3 portBLocal);

                Mesh mesh = CableMeshBuilder.BuildCableMesh(portALocal, portBLocal, curveStrength, 30, 0.06f);

                MeshFilter mf = cableGO.AddComponent<MeshFilter>();
                mf.mesh = mesh;

                MeshRenderer mr = cableGO.AddComponent<MeshRenderer>();
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.SetColor("_BaseColor", new Color(0.2f, 0.2f, 0.25f));
                mat.SetFloat("_Metallic", 0.6f);
                mat.SetFloat("_Smoothness", 0.4f);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.black);
                mr.material = mat;

                List<Vector3> localPoints = CableMeshBuilder.SamplePath(
                    portALocal,
                    portBLocal,
                    CableMeshBuilder.GetInwardTangent(portALocal),
                    CableMeshBuilder.GetInwardTangent(portBLocal),
                    curveStrength,
                    30
                );

                CablePiece piece = cableGO.AddComponent<CablePiece>();
                piece.Setup(type, mat, localPoints);

                if (x == gridSize - 2 && z == gridSize / 2) {
                    piece.IsRouterCell = true;
                }

                int rotations = Random.Range(0, 4);
                piece.RotateInstant(rotations);

                cellGrid[x, z].occupied = true;
                cellGrid[x, z].piece = piece;

#if UNITY_EDITOR
                if (!Application.isPlaying) {
                    UnityEditor.Undo.RegisterCreatedObjectUndo(cableGO, "Create Cable");
                }
#endif
            }
        }
    }

    private void RotateCableAt(int x, int z) {
        if (x == 0 && z == gridSize / 2) {
            return;
        }
        if (x == gridSize - 1 && z == gridSize / 2) {
            return;
        }

        CablePiece piece = cellGrid[x, z].piece;
        if (piece != null) {
            piece.RotateCW();

            if (ScreenShakeController.Instance != null) {
                ScreenShakeController.Instance.FOVPunch(61f, 0.15f);
            }
        }
    }

    private void SpawnStartCable(int x, int z) {
        GameObject cableGO = new GameObject("StartCable");
        cableGO.transform.position = new Vector3(x, 0.025f, z);
        cableGO.tag = "LCMGrid";
        cableGO.transform.SetParent(this.transform);

        GetPortsForPiece(PieceType.STRAIGHT_H, out Vector3 portALocal, out Vector3 portBLocal);
        Mesh mesh = CableMeshBuilder.BuildCableMesh(portALocal, portBLocal, curveStrength, 30, 0.06f);

        MeshFilter mf = cableGO.AddComponent<MeshFilter>();
        mf.mesh = mesh;

        MeshRenderer mr = cableGO.AddComponent<MeshRenderer>();
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetColor("_BaseColor", new Color(0.2f, 0.4f, 0.2f));
        mat.SetFloat("_Metallic", 0.6f);
        mat.SetFloat("_Smoothness", 0.4f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", Color.green * 1.5f);
        mr.material = mat;

        List<Vector3> localPoints = CableMeshBuilder.SamplePath(
            portALocal,
            portBLocal,
            CableMeshBuilder.GetInwardTangent(portALocal),
            CableMeshBuilder.GetInwardTangent(portBLocal),
            curveStrength,
            30
        );

        CablePiece piece = cableGO.AddComponent<CablePiece>();
        piece.Setup(PieceType.STRAIGHT_H, mat, localPoints);
        piece.IsRouterCell = true;

        cellGrid[x, z].occupied = true;
        cellGrid[x, z].piece = piece;

#if UNITY_EDITOR
        if (!Application.isPlaying) {
            UnityEditor.Undo.RegisterCreatedObjectUndo(cableGO, "Create Start Cable");
        }
#endif
    }

    private void GetPortsForPiece(PieceType type, out Vector3 localA, out Vector3 localB) {
        switch (type) {
            case PieceType.STRAIGHT_H:
                localA = new Vector3(-0.5f, 0f, 0f);
                localB = new Vector3(0.5f, 0f, 0f);
                break;
            case PieceType.STRAIGHT_V:
                localA = new Vector3(0f, 0f, 0.5f);
                localB = new Vector3(0f, 0f, -0.5f);
                break;
            case PieceType.BEND_NE:
                localA = new Vector3(0f, 0f, 0.5f);
                localB = new Vector3(0.5f, 0f, 0f);
                break;
            case PieceType.BEND_NW:
                localA = new Vector3(0f, 0f, 0.5f);
                localB = new Vector3(-0.5f, 0f, 0f);
                break;
            case PieceType.BEND_SE:
                localA = new Vector3(0f, 0f, -0.5f);
                localB = new Vector3(0.5f, 0f, 0f);
                break;
            case PieceType.BEND_SW:
                localA = new Vector3(0f, 0f, -0.5f);
                localB = new Vector3(-0.5f, 0f, 0f);
                break;
            default:
                localA = Vector3.zero;
                localB = Vector3.zero;
                break;
        }
    }

    public void ShakeAllCables() {
        for (int x = 0; x < gridSize; x++) {
            for (int z = 0; z < gridSize; z++) {
                if (cellGrid[x, z].occupied && cellGrid[x, z].piece != null) {
                    cellGrid[x, z].piece.ShakeOnGameOver();
                }
            }
        }
    }

    // [NEW - MathUnlock]
    public IEnumerable<CablePiece> AllTiles {
        get {
            for (int x = 0; x < gridSize; x++) {
                for (int z = 0; z < gridSize; z++) {
                    if (cellGrid[x, z].piece != null) {
                        yield return cellGrid[x, z].piece;
                    }
                }
            }
        }
    }

    public void SetGridSize(int size) => gridSize = size;
}
