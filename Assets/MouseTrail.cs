using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer), typeof(EdgeCollider2D))]
public class MouseTrail : MonoBehaviour
{
    public LineRenderer lineRenderer;
    public float trailMinDistance = 0.1f;

    private List<Vector3> points = new List<Vector3>();

    void Awake()
    {
        if(lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();
        // **월드 공간**에서 그리도록 설정
        lineRenderer.useWorldSpace = true;
    }

    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0f; // 2D이므로 Z=0

            if (points.Count == 0 || Vector3.Distance(points[points.Count - 1], mousePos) > trailMinDistance)
            {
                points.Add(mousePos);
                lineRenderer.positionCount = points.Count;
                lineRenderer.SetPositions(points.ToArray());

                // EdgeCollider2D 업데이트
                Vector2[] colliderPoints = new Vector2[points.Count];
                for(int i = 0; i < points.Count; i++)
                    colliderPoints[i] = new Vector2(points[i].x, points[i].y);
                GetComponent<EdgeCollider2D>().points = colliderPoints;
            }
        }
        else
        {
            points.Clear();
            lineRenderer.positionCount = 0;
            GetComponent<EdgeCollider2D>().points = new Vector2[0];
        }
    }
}
