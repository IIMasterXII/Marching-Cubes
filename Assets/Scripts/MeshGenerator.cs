using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class MeshGenerator : MonoBehaviour
{
    const int threadGroupSize = 8;

    [Header ("General Settings")]
    // public Renderer textureRender;
    // public MeshFilter meshFilter;
    // public MeshRenderer meshRenderer;
    public DensityGenerator densityGenerator;
    public bool fixedMapSize;
    [ConditionalHide (nameof (fixedMapSize), true)]
    public Vector3Int numChunks = Vector3Int.one;
    [ConditionalHide (nameof (fixedMapSize), false)]
    public Transform viewer;
    [ConditionalHide (nameof (fixedMapSize), false)]
    public float viewDistance = 30;
    public bool autoUpdateInGame = true;
    public bool autoUpdateInEditor = true;
    public Material mat;
    public bool generateColliders;

    [Header ("Voxel Settings")]
    public float isoLevel;
    public float boundsSize = 1;
    public Vector3 offset = Vector3.zero;

    [Range (2, 100)]
    public int pointsPerAxis = 30;

    [Header ("Gizmos")]
    public bool showGizmo = false;
    public Color gizmoColor = Color.white;

    GameObject chunkHolder;
    const string chunkHolderName = "Chunks Holder";
    List<Chunk> chunks;
    Dictionary<Vector3Int, Chunk> existingChunks;
    Queue<Chunk> recycleableChunks;

    bool settingsUpdated;

    void Awake() {
        if (Application.isPlaying && !fixedMapSize) {
            InitVariableChunkStructures ();
            
            var oldChunks = FindObjectsOfType<Chunk> ();
            for (int i = oldChunks.Length - 1; i >= 0; i--) {
                Destroy (oldChunks[i].gameObject);
            }
        }
    }

    void Update () {
         // Update endless terrain
        if ((Application.isPlaying && !fixedMapSize)) {
            Generate ();
        }

        if (settingsUpdated) {
            RequestMeshUpdate ();
            settingsUpdated = false;
        }
    }

    public void RequestMeshUpdate () {
        if ((Application.isPlaying && autoUpdateInGame) || (!Application.isPlaying && autoUpdateInEditor)) {
            Generate ();
        }
    }

    public void Generate(){
        if (fixedMapSize) {
            InitChunks ();
            UpdateAllChunks ();

        } else {
            if (Application.isPlaying) {
                InitVisibleChunks ();
            }
        }
    }

    void InitVariableChunkStructures () {
        recycleableChunks = new Queue<Chunk> ();
        chunks = new List<Chunk> ();
        existingChunks = new Dictionary<Vector3Int, Chunk> ();
    }

    void InitVisibleChunks () {
        if (chunks==null) {
            return;
        }
        CreateChunkHolder ();

        Vector3 p = viewer.position;
        Vector3 ps = p / boundsSize;
        Vector3Int viewerCoord = new Vector3Int (Mathf.RoundToInt (ps.x), Mathf.RoundToInt (ps.y), Mathf.RoundToInt (ps.z));

        int maxChunksInView = Mathf.CeilToInt (viewDistance / boundsSize);
        float sqrViewDistance = viewDistance * viewDistance;

        // Go through all existing chunks and flag for recyling if outside of max view dst
        for (int i = chunks.Count - 1; i >= 0; i--) {
            Chunk chunk = chunks[i];
            Vector3 centre = CenterFromCoord (chunk.coord);
            Vector3 viewerOffset = p - centre;
            Vector3 o = new Vector3 (Mathf.Abs (viewerOffset.x), Mathf.Abs (viewerOffset.y), Mathf.Abs (viewerOffset.z)) - Vector3.one * boundsSize / 2;
            float sqrDst = new Vector3 (Mathf.Max (o.x, 0), Mathf.Max (o.y, 0), Mathf.Max (o.z, 0)).sqrMagnitude;
            if (sqrDst > sqrViewDistance) {
                existingChunks.Remove (chunk.coord);
                recycleableChunks.Enqueue (chunk);
                chunks.RemoveAt (i);
            }
        }

        for (int x = -maxChunksInView; x <= maxChunksInView; x++) {
            for (int y = -maxChunksInView; y <= maxChunksInView; y++) {
                for (int z = -maxChunksInView; z <= maxChunksInView; z++) {
                    Vector3Int coord = new Vector3Int (x, y, z) + viewerCoord;
                    if (existingChunks.ContainsKey (coord)) {
                        continue;
                    }

                    Vector3 centre = CenterFromCoord (coord);
                    Vector3 viewerOffset = p - centre;
                    Vector3 o = new Vector3 (Mathf.Abs (viewerOffset.x), Mathf.Abs (viewerOffset.y), Mathf.Abs (viewerOffset.z)) - Vector3.one * boundsSize / 2;
                    float sqrDst = new Vector3 (Mathf.Max (o.x, 0), Mathf.Max (o.y, 0), Mathf.Max (o.z, 0)).sqrMagnitude;

                    // Chunk is within view distance and should be created (if it doesn't already exist)
                    if (sqrDst <= sqrViewDistance) {

                        Bounds bounds = new Bounds (CenterFromCoord (coord), Vector3.one * boundsSize);
                        if (IsVisibleFrom (bounds, Camera.main)) {
                            if (recycleableChunks.Count > 0) {
                                Chunk chunk = recycleableChunks.Dequeue ();
                                chunk.coord = coord;
                                existingChunks.Add (coord, chunk);
                                chunks.Add (chunk);
                                UpdateChunkMesh (chunk);
                            } else {
                                Chunk chunk = CreateChunk (coord);
                                chunk.coord = coord;
                                chunk.SetUp (mat, generateColliders);
                                existingChunks.Add (coord, chunk);
                                chunks.Add (chunk);
                                UpdateChunkMesh (chunk);
                            }
                        }
                    }

                }
            }
        }
    }

    public bool IsVisibleFrom (Bounds bounds, Camera camera) {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes (camera);
        return GeometryUtility.TestPlanesAABB (planes, bounds);
    }

    public void UpdateChunkMesh (Chunk chunk) {
        int voxelsPerAxis = pointsPerAxis - 1;
        int voxels = voxelsPerAxis * voxelsPerAxis * voxelsPerAxis;
        int maxTriangleCount = voxels * 5;
        float pointSpacing = boundsSize / (pointsPerAxis - 1);

        Vector3Int coord = chunk.coord;
        Vector3 center = CenterFromCoord (coord);

        Vector3 worldBounds = new Vector3 (numChunks.x, numChunks.y, numChunks.z) * boundsSize;

        Vector4[] points = densityGenerator.Generate(pointsPerAxis, boundsSize, worldBounds, center, offset, pointSpacing);
        List<Triangle> triangles = new List<Triangle>();


        for(int z = 0; z < pointsPerAxis-1; z++)
            for(int y = 0; y < pointsPerAxis-1; y++)
                for(int x = 0; x < pointsPerAxis-1; x++)
                    March(points, new Vector3Int(x,y,z), triangles);

        int numTris = triangles.Count;

        Mesh mesh = chunk.mesh;
        mesh.Clear ();

        var vertices = new Vector3[numTris * 3];
        var meshTriangles = new int[numTris * 3];

        for (int i = 0; i < numTris; i++) {
            for (int j = 0; j < 3; j++) {
                meshTriangles[i * 3 + j] = i * 3 + j;
                vertices[i * 3 + j] = triangles[i][2-j];
            }
        }
        mesh.vertices = vertices;
        mesh.triangles = meshTriangles;

        mesh.RecalculateNormals ();
    }

    // Create/get references to all chunks
    void InitChunks () {
        CreateChunkHolder ();
        chunks = new List<Chunk> ();
        List<Chunk> oldChunks = new List<Chunk> (FindObjectsOfType<Chunk> ());

        // Go through all coords and create a chunk there if one doesn't already exist
        for (int x = 0; x < numChunks.x; x++) {
            for (int y = 0; y < numChunks.y; y++) {
                for (int z = 0; z < numChunks.z; z++) {
                    Vector3Int coord = new Vector3Int (x, y, z);
                    bool chunkAlreadyExists = false;

                    // If chunk already exists, add it to the chunks list, and remove from the old list.
                    for (int i = 0; i < oldChunks.Count; i++) {
                        if (oldChunks[i].coord == coord) {
                            chunks.Add (oldChunks[i]);
                            oldChunks.RemoveAt (i);
                            chunkAlreadyExists = true;
                            break;
                        }
                    }

                    // Create new chunk
                    if (!chunkAlreadyExists) {
                        var newChunk = CreateChunk (coord);
                        chunks.Add (newChunk);
                    }

                    chunks[chunks.Count - 1].SetUp (mat, generateColliders);
                }
            }
        }

        // Delete all unused chunks
        for (int i = 0; i < oldChunks.Count; i++) {
            oldChunks[i].DestroyOrDisable ();
        }
    }

    void CreateChunkHolder () {
        // Create/find mesh holder object for organizing chunks under in the hierarchy
        if (chunkHolder == null) {
            if (GameObject.Find (chunkHolderName)) {
                chunkHolder = GameObject.Find (chunkHolderName);
            } else {
                chunkHolder = new GameObject (chunkHolderName);
            }
        }
    }

    void OnValidate() {
        settingsUpdated = true;
    }

    public void UpdateAllChunks () {
        // Create mesh for each chunk
        foreach (Chunk chunk in chunks) {
            UpdateChunkMesh (chunk);
        }

    }

    Vector3 CenterFromCoord (Vector3Int coord) {
        Vector3 totalBounds = (Vector3) numChunks * boundsSize;
        return -totalBounds / 2 + (Vector3) coord * boundsSize + Vector3.one * boundsSize / 2;
    }

    Vector3 interpolateVerts(Vector4 v1, Vector4 v2) {
        float t = (isoLevel - v1.w) / (v2.w - v1.w);
        return v1 + (t * (v2-v1));
    }

    int indexFromCoord(int x, int y, int z, int pointsPerAxis){
        return z * pointsPerAxis * pointsPerAxis + y * pointsPerAxis + x;
    }

    List<Triangle> March(Vector4[] points, Vector3Int point, List<Triangle> triangles){

        Vector4[] cubeCorners = {
            points[indexFromCoord(point.x, point.y, point.z, pointsPerAxis)],
            points[indexFromCoord(point.x + 1, point.y, point.z, pointsPerAxis)],
            points[indexFromCoord(point.x + 1, point.y, point.z + 1, pointsPerAxis)],
            points[indexFromCoord(point.x, point.y, point.z + 1, pointsPerAxis)],
            points[indexFromCoord(point.x, point.y + 1, point.z, pointsPerAxis)],
            points[indexFromCoord(point.x + 1, point.y + 1, point.z, pointsPerAxis)],
            points[indexFromCoord(point.x + 1, point.y + 1, point.z + 1, pointsPerAxis)],
            points[indexFromCoord(point.x, point.y + 1, point.z + 1, pointsPerAxis)]
        };

        int cubeIndex = 0;
        if (cubeCorners[0].w < isoLevel) cubeIndex |= 1;
        if (cubeCorners[1].w < isoLevel) cubeIndex |= 2;
        if (cubeCorners[2].w < isoLevel) cubeIndex |= 4;
        if (cubeCorners[3].w < isoLevel) cubeIndex |= 8;
        if (cubeCorners[4].w < isoLevel) cubeIndex |= 16;
        if (cubeCorners[5].w < isoLevel) cubeIndex |= 32;
        if (cubeCorners[6].w < isoLevel) cubeIndex |= 64;
        if (cubeCorners[7].w < isoLevel) cubeIndex |= 128;

            // Create triangles for current cube configuration
        for (int i = 0; MarchingCube.triTable[cubeIndex, i] != -1; i +=3) {
            // Get indices of corner points A and B for each of the three edges
            // of the cube that need to be joined to form the triangle.
            int a0 = MarchingCube.cornerIndexAFromEdge[MarchingCube.triTable[cubeIndex, i]];
            int b0 = MarchingCube.cornerIndexBFromEdge[MarchingCube.triTable[cubeIndex, i]];

            int a1 = MarchingCube.cornerIndexAFromEdge[MarchingCube.triTable[cubeIndex, i+1]];
            int b1 = MarchingCube.cornerIndexBFromEdge[MarchingCube.triTable[cubeIndex, i+1]];

            int a2 = MarchingCube.cornerIndexAFromEdge[MarchingCube.triTable[cubeIndex, i+2]];
            int b2 = MarchingCube.cornerIndexBFromEdge[MarchingCube.triTable[cubeIndex, i+2]];

            Triangle tri;
            tri.a = interpolateVerts(cubeCorners[a0], cubeCorners[b0]);
            tri.b = interpolateVerts(cubeCorners[a1], cubeCorners[b1]);
            tri.c = interpolateVerts(cubeCorners[a2], cubeCorners[b2]);
            triangles.Add(tri);
        }
        return triangles;
    }

    Chunk CreateChunk (Vector3Int coord) {
        GameObject chunk = new GameObject ($"Chunk ({coord.x}, {coord.y}, {coord.z})");
        chunk.transform.parent = chunkHolder.transform;
        Chunk newChunk = chunk.AddComponent<Chunk> ();
        newChunk.coord = coord;
        return newChunk;
    }

    struct Triangle {
#pragma warning disable 649 // disable unassigned variable warning
        public Vector3 a;
        public Vector3 b;
        public Vector3 c;

        public Vector3 this [int i] {
            get {
                switch (i) {
                    case 0:
                        return a;
                    case 1:
                        return b;
                    default:
                        return c;
                }
            }
        }
    }

    void OnDrawGizmos () {
        if (showGizmo) {
            Gizmos.color = gizmoColor;

            List<Chunk> chunks = (this.chunks == null) ? new List<Chunk> (FindObjectsOfType<Chunk> ()) : this.chunks;
            foreach (var chunk in chunks) {
                Bounds bounds = new Bounds (CenterFromCoord (chunk.coord), Vector3.one * boundsSize);
                Gizmos.color = gizmoColor;
                Gizmos.DrawWireCube (CenterFromCoord (chunk.coord), Vector3.one * boundsSize);
            }
        }
    }
}

