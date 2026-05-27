using System.Collections.Generic;
using UnityEngine;

// Procedural 3D tube mesh generator for cables.
public static class CableMeshBuilder {

    public static Mesh BuildCableMesh(Vector3 portA, Vector3 portB, float curveStrength, int segments, float radius) {
        Mesh mesh = new Mesh();
        mesh.name = "ProceduralCable";

        Vector3 tangentA = GetInwardTangent(portA);
        Vector3 tangentB = GetInwardTangent(portB);

        List<Vector3> points = SamplePath(portA, portB, tangentA, tangentB, curveStrength, segments);
        int numPoints = points.Count;

        Vector3[] vertices = new Vector3[numPoints * 8 + 2];
        Vector3[] normals = new Vector3[numPoints * 8 + 2];
        List<int> triangles = new List<int>();

        for (int i = 0; i < numPoints; i++) {
            Vector3 prevPoint = (i == 0) ? points[0] : points[i - 1];
            Vector3 nextPoint = (i == numPoints - 1) ? points[numPoints - 1] : points[i + 1];

            Vector3 tangent = (nextPoint - prevPoint).normalized;
            if (tangent == Vector3.zero) {
                tangent = Vector3.forward;
            }

            Vector3 normalVec = Vector3.Cross(tangent, Vector3.up).normalized;
            if (normalVec.sqrMagnitude < 0.001f) {
                normalVec = Vector3.right;
            }
            Vector3 binormal = Vector3.Cross(tangent, normalVec).normalized;

            for (int c = 0; c < 8; c++) {
                float angle = c * Mathf.PI * 2f / 8f;
                Vector3 localDirection = (normalVec * Mathf.Cos(angle) + binormal * Mathf.Sin(angle)).normalized;
                Vector3 offset = localDirection * radius;

                int vertexIndex = i * 8 + c;
                vertices[vertexIndex] = points[i] + offset;
                normals[vertexIndex] = localDirection;
            }
        }

        for (int i = 0; i < numPoints - 1; i++) {
            for (int c = 0; c < 8; c++) {
                int nextC = (c + 1) % 8;
                int v0 = i * 8 + c;
                int v1 = i * 8 + nextC;
                int v2 = (i + 1) * 8 + c;
                int v3 = (i + 1) * 8 + nextC;

                triangles.Add(v0);
                triangles.Add(v2);
                triangles.Add(v1);

                triangles.Add(v1);
                triangles.Add(v2);
                triangles.Add(v3);
            }
        }

        int vCenterStart = numPoints * 8;
        int vCenterEnd = numPoints * 8 + 1;

        vertices[vCenterStart] = points[0];
        normals[vCenterStart] = -GetInwardTangent(portA);

        vertices[vCenterEnd] = points[numPoints - 1];
        normals[vCenterEnd] = -GetInwardTangent(portB);

        for (int c = 0; c < 8; c++) {
            int nextC = (c + 1) % 8;
            triangles.Add(vCenterStart);
            triangles.Add(nextC);
            triangles.Add(c);
        }

        for (int c = 0; c < 8; c++) {
            int nextC = (c + 1) % 8;
            triangles.Add(vCenterEnd);
            triangles.Add(c);
            triangles.Add(nextC);
        }

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateBounds();

        return mesh;
    }

    public static List<Vector3> SamplePath(Vector3 portA, Vector3 portB, Vector3 tangentA, Vector3 tangentB, float curveStrength, int segments) {
        List<Vector3> points = new List<Vector3>();

        Vector3 B0 = portA + tangentA * 0.2f;
        Vector3 B3 = portB + tangentB * 0.2f;

        Vector3 B1 = B0 + tangentA * curveStrength;
        Vector3 B2 = B3 - tangentB * curveStrength;

        for (int i = 0; i < segments; i++) {
            float t = i / (float)(segments - 1);
            Vector3 point;

            if (t <= 0.2f) {
                point = Vector3.Lerp(portA, B0, t / 0.2f);
            } else if (t >= 0.8f) {
                point = Vector3.Lerp(B3, portB, (t - 0.8f) / 0.2f);
            } else {
                float u = (t - 0.2f) / 0.6f;
                float oneMinusU = 1f - u;

                point = oneMinusU * oneMinusU * oneMinusU * B0 +
                        3f * oneMinusU * oneMinusU * u * B1 +
                        3f * oneMinusU * u * u * B2 +
                        u * u * u * B3;
            }

            points.Add(point);
        }

        return points;
    }

    public static Vector3 GetInwardTangent(Vector3 localPort) {
        if (Mathf.Abs(localPort.x) > Mathf.Abs(localPort.z)) {
            return localPort.x > 0 ? new Vector3(-1f, 0f, 0f) : new Vector3(1f, 0f, 0f);
        } else {
            return localPort.z > 0 ? new Vector3(0f, 0f, -1f) : new Vector3(0f, 0f, 1f);
        }
    }
}
