using System.Collections.Generic;
using UnityEngine;

namespace SimKit
{
    public enum ShapeType { Hexagon, Square };

    public struct Face
    {
        public List<Vector3> Vertices { get; private set; }
        public List<int> Triangles { get; private set; }
        public List<Vector2> UVs { get; private set; }

        public Face(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs)
        {
            Vertices = vertices;
            Triangles = triangles;
            UVs = uvs;
        }
    }

    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class TileRenderer : MonoBehaviour
    {
        [HideInInspector] public ShapeType shapeType;
        [HideInInspector] public bool isFlatTopped = false;
        [HideInInspector] public string[] tileTypes;
        [HideInInspector] public int currentTileIndex;

        private MeshRenderer meshRenderer;
        private float innerSize;
        private float outerSize;
        private float height;

        public void ChangeTileType(int index)
        {
            if (index == currentTileIndex) return;
            currentTileIndex = index;
            Tile tile = GetComponentInParent<GridLayout>().tileSO[currentTileIndex];
            SetValues(tile.Material, tile.InnerSize, tile.OuterSize, tile.Height);
            DrawMesh();
        }

        public void SetValues(Material material, float innerSize, float outerSize, float height)
        {
            meshRenderer.material = material;
            this.innerSize = innerSize;
            this.outerSize = outerSize;
            this.height = height;
        }

        private Mesh _mesh;
        private List<Face> _faces;

        private void Awake()
        {
            _mesh = new()
            {
                name = "Tile"
            };

            GetComponent<MeshFilter>().mesh = _mesh;
            meshRenderer = GetComponent<MeshRenderer>();
        }

        public void DrawMesh()
        {
            DrawFaces();
            CombineFaces();
        }

        private void DrawFaces()
        {
            _faces = new List<Face>();

            for (int i = 0; i < (shapeType == ShapeType.Hexagon ? 6 : 4); i++)
            {
                _faces.Add(CreateFace(innerSize, outerSize, height / 2f, height / 2f, i));
                _faces.Add(CreateFace(innerSize, outerSize, -height / 2f, -height / 2f, i, true));
                _faces.Add(CreateFace(outerSize, outerSize, height / 2f, -height / 2f, i, true));
                _faces.Add(CreateFace(innerSize, innerSize, height / 2f, -height / 2f, i));
            }
        }

        private Face CreateFace(float innerRad, float outerRad, float heightA, float heightB, int point, bool reverse = false)
        {
            Vector3 pointA = GetPoint(innerRad, heightB, point);
            Vector3 pointB = GetPoint(innerRad, heightB, (point < 5) ? point + 1 : 0);
            Vector3 pointC = GetPoint(outerRad, heightA, (point < 5) ? point + 1 : 0);
            Vector3 pointD = GetPoint(outerRad, heightA, point);

            List<Vector3> vertices = new() { pointA, pointB, pointC, pointD };
            List<int> triangles = new() { 0, 1, 2, 2, 3, 0 };
            List<Vector2> uvs = new() { new(0, 0), new(1, 0), new(1, 1), new(0, 1) };
            if (reverse) vertices.Reverse();

            return new Face(vertices, triangles, uvs);
        }

        private Vector3 GetPoint(float size, float height, int index)
        {
            float angleDeg = (shapeType == ShapeType.Hexagon ? 60 : 90) * index - (shapeType == ShapeType.Hexagon ? isFlatTopped ? 0 : 30 : isFlatTopped ? 45 : 0);
            float angleRad = Mathf.PI / 180f * angleDeg;
            return new(size * Mathf.Cos(angleRad), height, size * Mathf.Sin(angleRad));
        }

        private void CombineFaces()
        {
            List<Vector3> vertices = new();
            List<int> triangles = new();
            List<Vector2> uvs = new();

            for (int i = 0; i < _faces.Count; i++)
            {
                vertices.AddRange(_faces[i].Vertices);
                uvs.AddRange(_faces[i].UVs);

                int offset = i * 4;
                foreach (var triangle in _faces[i].Triangles)
                {
                    triangles.Add(triangle + offset);
                }
            }

            _mesh.vertices = vertices.ToArray();
            _mesh.triangles = triangles.ToArray();
            _mesh.uv = uvs.ToArray();
            _mesh.RecalculateNormals();
            _mesh.UploadMeshData(false);
        }
    }
}