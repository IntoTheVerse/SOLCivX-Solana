using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SimKit
{
    public class UILineRenderer : Graphic
    {
        public Vector2Int gridSize;
        public float thickness = 10f;

        private List<Vector2> points = new();
        private float width;
        private float height;
        private float unitWidth;
        private float unitHeight;
        private float angleAddition;

        public void DrawLine(Vector2 pointA, Vector2 pointB, float angleAddition)
        {
            points.Add(Vector2.zero);
            points.Add(pointA);
            points.Add(pointB);
            this.angleAddition = angleAddition;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            width = rectTransform.rect.width;
            height = rectTransform.rect.height;

            unitWidth = width / gridSize.x;
            unitHeight = height / gridSize.y;

            if (points.Count < 2) return;

            float angle = 0;
            for (int i = 0; i < points.Count; i++)
            {
                Vector2 point = points[i];
                if (i < points.Count - 1) angle = GetAngle(points[i], points[i + 1]) + angleAddition;
                DrawVerticesForPoint(point, vh, angle);
            }

            for (int i = 0; i < points.Count - 1; i++)
            {
                int index = 1 * 2;
                vh.AddTriangle(index + 0, index + 1, index + 3);
                vh.AddTriangle(index + 3, index + 2, index + 0);
            }
        }

        private void DrawVerticesForPoint(Vector2 point, VertexHelper vh, float angle)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;

            vertex.position = Quaternion.Euler(0, 0, angle) * new Vector3(-thickness / 2, 0);
            vertex.position += new Vector3(unitWidth * point.x, unitHeight * point.y);
            vh.AddVert(vertex);

            vertex.position = Quaternion.Euler(0, 0, angle) * new Vector3(thickness / 2, 0);
            vertex.position += new Vector3(unitWidth * point.x, unitHeight * point.y);
            vh.AddVert(vertex);
        }

        public float GetAngle(Vector2 me, Vector2 target)
        {
            return (float)(Mathf.Atan2(target.y - me.y, target.x - me.x) * (180 / Mathf.PI));
        }
    }
}