using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Arrow3D : MonoBehaviour
{
    [SerializeField]
    MeshFilter meshFilter;
    [SerializeField]
    ArrowData arrowData;
    [SerializeField]
    bool generateArrow;

    Mesh mesh;

    [System.Serializable]
    public struct ArrowData
    {
        [SerializeField]
        public float tailLength;
        [SerializeField]
        public float tailWidth;
        [SerializeField]
        public float headLength;
        [SerializeField]
        public float headExtension;
        [SerializeField]
        public float width;
        [SerializeField]
        public Color color;
    }

    public ArrowData Data
    {
        get
        {
            return arrowData;
        }
        set
        {
            arrowData = value;
        }
    }

    // Update is called once per frame
    void OnValidate()
    {
        if (generateArrow) GenerateArrow();
    }
            
    public void GenerateArrow()
    {
        mesh = new Mesh();
        meshFilter.mesh = mesh;
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Color> colors = new List<Color>();

        /*
         *  AOB
         *  | |
         *  | |
         * EC DF
         *   G
         */
        Vector3 verticalOffset = Vector3.up * Data.width;
        Vector3 O = Vector3.zero;
        Vector3 A = O - Vector3.right * Data.tailWidth / 2, _A = A - verticalOffset;
        Vector3 B = O + Vector3.right * Data.tailWidth / 2, _B = B - verticalOffset;
        Vector3 C = A + Vector3.forward * Data.tailLength, _C = C - verticalOffset;
        Vector3 D = B + Vector3.forward * Data.tailLength, _D = D - verticalOffset;
        Vector3 E = C - Vector3.right * Data.headExtension, _E = E - verticalOffset;
        Vector3 F = D + Vector3.right * Data.headExtension, _F = F - verticalOffset;
        Vector3 G = O + Vector3.forward * (Data.tailLength + Data.headLength), _G = G - verticalOffset;
        #region Tail top
        vertices.AddRange(new Vector3[] { A, B, C, D });
        normals.AddRange(new Vector3[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up });
        colors.AddRange(new Color[] { Data.color, Data.color, Data.color, Data.color });

        int D1i = vertices.Count - 1, C1i = D1i - 1, B1i = C1i - 1, A1i = B1i - 1;

        triangles.AddRange(new int[] { C1i, B1i, A1i });
        triangles.AddRange(new int[] { C1i, D1i, B1i });
        #endregion
        #region Head top
        vertices.AddRange(new Vector3[] { E, C, D, F, G });
        normals.AddRange(new Vector3[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up, Vector3.up });
        colors.AddRange(new Color[] { Data.color, Data.color, Data.color, Data.color, Data.color });
        int G2i = vertices.Count - 1, F2i = G2i - 1, D2i = F2i - 1, C2i = D2i - 1, E2i = C2i - 1;
        triangles.AddRange(new int[] { E2i, G2i, C2i });
        triangles.AddRange(new int[] { D2i, G2i, F2i });
        triangles.AddRange(new int[] { C2i, G2i, D2i });
        #endregion
        #region Tail bottom
        vertices.AddRange(new Vector3[] { _A, _B, _C, _D });
        normals.AddRange(new Vector3[] { -Vector3.up, -Vector3.up, -Vector3.up, -Vector3.up });
        colors.AddRange(new Color[] { Data.color, Data.color, Data.color, Data.color });

        int D3i = vertices.Count - 1, C3i = D3i - 1, B3i = C3i - 1, A3i = B3i - 1;

        triangles.AddRange(new int[] { A3i, B3i, C3i });
        triangles.AddRange(new int[] { B3i, D3i, C3i });
        #endregion
        #region Head Bottom
        vertices.AddRange(new Vector3[] { _E, _C, _D, _F, _G });
        normals.AddRange(new Vector3[] { -Vector3.up, -Vector3.up, -Vector3.up, -Vector3.up, -Vector3.up });
        colors.AddRange(new Color[] { Data.color, Data.color, Data.color, Data.color, Data.color });
        int G4i = vertices.Count - 1, F4i = G4i - 1, D4i = F4i - 1, C4i = D4i - 1, E4i = C4i - 1;
        triangles.AddRange(new int[] { C4i, G4i, E4i });
        triangles.AddRange(new int[] { F4i, G4i, D4i });
        triangles.AddRange(new int[] { D4i, G4i, C4i });
        #endregion
        #region Side
        #region Tail AB side
        vertices.AddRange(new Vector3[] { A, B, _A, _B });
        normals.AddRange(new Vector3[] { -Vector3.forward, -Vector3.forward, -Vector3.forward, -Vector3.forward });
        colors.AddRange(new Color[] { Data.color, Data.color, Data.color, Data.color });
        int _B5i = vertices.Count - 1, _A5i = _B5i - 1, B5i = _A5i - 1, A5i = B5i - 1;
        triangles.AddRange(new int[] { _A5i, A5i, B5i });
        triangles.AddRange(new int[] { _A5i, B5i, _B5i });
        #endregion
        #region Tail AC side
        vertices.AddRange(new Vector3[] { A, C, _A, _C });
        normals.AddRange(new Vector3[] { -Vector3.right, -Vector3.right, -Vector3.right, -Vector3.right });
        colors.AddRange(new Color[] { Data.color, Data.color, Data.color, Data.color });
        int _C6i = vertices.Count - 1, _A6i = _C6i - 1, C6i = _A6i - 1, A6i = C6i - 1;
        triangles.AddRange(new int[] { C6i, A6i, _A6i });
        triangles.AddRange(new int[] { _C6i, C6i, _A6i });
        #endregion
        #region Tail BD side
        vertices.AddRange(new Vector3[] { B, D, _B, _D });
        normals.AddRange(new Vector3[] { Vector3.right, Vector3.right, Vector3.right, Vector3.right });
        colors.AddRange(new Color[] { Data.color, Data.color, Data.color, Data.color });
        int _D7i = vertices.Count - 1, _B7i = _D7i - 1, D7i = _B7i - 1, B7i = D7i - 1;
        triangles.AddRange(new int[] { _B7i, B7i, D7i });
        triangles.AddRange(new int[] { _B7i, D7i, _D7i });
        #endregion
        #region Head DF side
        vertices.AddRange(new Vector3[] { F, D, _F, _D });
        normals.AddRange(new Vector3[] { -Vector3.forward, -Vector3.forward, -Vector3.forward, -Vector3.forward });
        colors.AddRange(new Color[] { Data.color, Data.color, Data.color, Data.color });
        int _D8i = vertices.Count - 1, _F8i = _D8i - 1, D8i = _F8i - 1, F8i = D8i - 1;
        triangles.AddRange(new int[] { D8i, F8i, _F8i });
        triangles.AddRange(new int[] { _D8i, D8i, _F8i });
        #endregion
        #region Head CE side
        vertices.AddRange(new Vector3[] { E, C, _E, _C });
        normals.AddRange(new Vector3[] { -Vector3.forward, -Vector3.forward, -Vector3.forward, -Vector3.forward });
        colors.AddRange(new Color[] { Data.color, Data.color, Data.color, Data.color });
        int _C9i = vertices.Count - 1, _E9i = _C9i - 1, C9i = _E9i - 1, E9i = C9i - 1;
        triangles.AddRange(new int[] { _E9i, E9i, C9i });
        triangles.AddRange(new int[] { _E9i, C9i, _C9i });
        #endregion
        #region Head EG side
        vertices.AddRange(new Vector3[] { G, E, _G, _E });
        Vector3 EGDir = (Vector3.forward * Data.headLength - Vector3.right * (Data.headExtension + Data.tailWidth / 2)).normalized;
        normals.AddRange(new Vector3[] { EGDir, EGDir, EGDir, EGDir });
        colors.AddRange(new Color[] { Data.color, Data.color, Data.color, Data.color });
        int _E10i = vertices.Count - 1, _G10i = _E10i - 1, E10i = _G10i - 1, G10i = E10i - 1;
        triangles.AddRange(new int[] { _G10i, G10i, E10i });
        triangles.AddRange(new int[] { _G10i, E10i, _E10i });
        #endregion
        #region Head FG side
        vertices.AddRange(new Vector3[] { G, F, _G, _F });
        Vector3 FGDir = (Vector3.forward * Data.headLength + Vector3.right * (Data.headExtension + Data.tailWidth / 2)).normalized;
        normals.AddRange(new Vector3[] { FGDir, FGDir, FGDir, FGDir });
        colors.AddRange(new Color[] { Data.color, Data.color, Data.color, Data.color });
        int _F11i = vertices.Count - 1, _G11i = _F11i - 1, F11i = _G11i - 1, G11i = F11i - 1;
        triangles.AddRange(new int[] { F11i, G11i, _G11i });
        triangles.AddRange(new int[] { _F11i, F11i, _G11i });
        #endregion
        #endregion

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetNormals(normals);
        mesh.SetColors(colors);
    }
}