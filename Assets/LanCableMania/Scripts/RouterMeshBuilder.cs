using System.Collections.Generic;
using UnityEngine;

// Procedurally builds a 3D router model using basic primitives.
public static class RouterMeshBuilder {

    public static Mesh GenerateCylinder(float radius, float height, int sides) {
        Mesh mesh = new Mesh();
        mesh.name = "ProceduralCylinder";

        List<Vector3> verts = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<int> tris = new List<int>();

        Vector3 bottomCenter = new Vector3(0, -height / 2f, 0);
        verts.Add(bottomCenter);
        normals.Add(Vector3.down);
        int bottomCenterIdx = 0;

        for (int i = 0; i < sides; i++) {
            float angle = i * Mathf.PI * 2f / sides;
            verts.Add(new Vector3(Mathf.Cos(angle) * radius, -height / 2f, Mathf.Sin(angle) * radius));
            normals.Add(Vector3.down);
        }

        for (int i = 0; i < sides; i++) {
            tris.Add(bottomCenterIdx);
            tris.Add(bottomCenterIdx + 1 + (i + 1) % sides);
            tris.Add(bottomCenterIdx + 1 + i);
        }

        int topCenterIdx = verts.Count;
        Vector3 topCenter = new Vector3(0, height / 2f, 0);
        verts.Add(topCenter);
        normals.Add(Vector3.up);

        for (int i = 0; i < sides; i++) {
            float angle = i * Mathf.PI * 2f / sides;
            verts.Add(new Vector3(Mathf.Cos(angle) * radius, height / 2f, Mathf.Sin(angle) * radius));
            normals.Add(Vector3.up);
        }

        for (int i = 0; i < sides; i++) {
            tris.Add(topCenterIdx);
            tris.Add(topCenterIdx + 1 + i);
            tris.Add(topCenterIdx + 1 + (i + 1) % sides);
        }

        int sideStartIdx = verts.Count;
        for (int i = 0; i < sides; i++) {
            float angle = i * Mathf.PI * 2f / sides;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            verts.Add(new Vector3(cos * radius, -height / 2f, sin * radius));
            normals.Add(new Vector3(cos, 0, sin));

            verts.Add(new Vector3(cos * radius, height / 2f, sin * radius));
            normals.Add(new Vector3(cos, 0, sin));
        }

        for (int i = 0; i < sides; i++) {
            int next = (i + 1) % sides;
            int b0 = sideStartIdx + i * 2;
            int t0 = sideStartIdx + i * 2 + 1;
            int b1 = sideStartIdx + next * 2;
            int t1 = sideStartIdx + next * 2 + 1;

            tris.Add(b0);
            tris.Add(t0);
            tris.Add(b1);

            tris.Add(b1);
            tris.Add(t0);
            tris.Add(t1);
        }

        mesh.vertices = verts.ToArray();
        mesh.normals = normals.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.RecalculateBounds();

        return mesh;
    }

    public static GameObject BuildRouter(Color accentColor) {
        GameObject routerGO = new GameObject("ProceduralRouter");
        routerGO.tag = "LCMGrid";

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.tag = "LCMGrid";
        body.transform.SetParent(routerGO.transform);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale = new Vector3(1.8f, 0.35f, 1.1f);

        Collider bodyCol = body.GetComponent<Collider>();
        if (bodyCol != null) {
            Object.DestroyImmediate(bodyCol);
        }

        Material bodyMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        bodyMat.SetColor("_BaseColor", new Color(0.18f, 0.18f, 0.22f));
        bodyMat.SetFloat("_Metallic", 0.5f);
        bodyMat.SetFloat("_Smoothness", 0.6f);
        body.GetComponent<Renderer>().material = bodyMat;

        GameObject ant1 = new GameObject("Antenna_Left");
        ant1.tag = "LCMGrid";
        ant1.transform.SetParent(routerGO.transform);
        ant1.transform.localPosition = new Vector3(-0.75f, 0.3f, -0.45f);
        ant1.transform.localRotation = Quaternion.Euler(0f, 0f, 15f);
        ant1.AddComponent<MeshFilter>().mesh = GenerateCylinder(0.04f, 0.55f, 8);

        GameObject ant2 = new GameObject("Antenna_Right");
        ant2.tag = "LCMGrid";
        ant2.transform.SetParent(routerGO.transform);
        ant2.transform.localPosition = new Vector3(0.75f, 0.3f, -0.45f);
        ant2.transform.localRotation = Quaternion.Euler(0f, 0f, -15f);
        ant2.AddComponent<MeshFilter>().mesh = GenerateCylinder(0.04f, 0.55f, 8);

        Material antMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        antMat.SetColor("_BaseColor", new Color(0.12f, 0.12f, 0.15f));
        ant1.AddComponent<MeshRenderer>().material = antMat;
        ant2.AddComponent<MeshRenderer>().material = antMat;

        Color[] ledColors = new Color[] { Color.green, Color.green, new Color(1.0f, 0.6f, 0.0f), accentColor };
        for (int i = 0; i < 4; i++) {
            GameObject led = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            led.name = "LED_" + i;
            led.tag = "LCMGrid";
            led.transform.SetParent(routerGO.transform);
            led.transform.localPosition = new Vector3(-0.6f + i * 0.15f, 0.08f, 0.56f);
            led.transform.localScale = Vector3.one * 0.07f;

            Collider ledCol = led.GetComponent<Collider>();
            if (ledCol != null) {
                Object.DestroyImmediate(ledCol);
            }

            Material ledMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            ledMat.SetColor("_BaseColor", ledColors[i]);
            ledMat.EnableKeyword("_EMISSION");
            ledMat.SetColor("_EmissionColor", ledColors[i] * 3.0f);
            led.GetComponent<Renderer>().material = ledMat;
        }

        for (int i = 0; i < 4; i++) {
            float posX = -0.45f + i * 0.3f;

            GameObject portOuter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            portOuter.name = "EthPortOuter_" + i;
            portOuter.tag = "LCMGrid";
            portOuter.transform.SetParent(routerGO.transform);
            portOuter.transform.localPosition = new Vector3(posX, 0f, -0.56f);
            portOuter.transform.localScale = new Vector3(0.18f, 0.12f, 0.04f);

            Collider poCol = portOuter.GetComponent<Collider>();
            if (poCol != null) {
                Object.DestroyImmediate(poCol);
            }

            Material poMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            poMat.SetColor("_BaseColor", new Color(0.08f, 0.08f, 0.1f));
            portOuter.GetComponent<Renderer>().material = poMat;

            GameObject portInner = GameObject.CreatePrimitive(PrimitiveType.Cube);
            portInner.name = "EthPortInner_" + i;
            portInner.tag = "LCMGrid";
            portInner.transform.SetParent(routerGO.transform);
            portInner.transform.localPosition = new Vector3(posX, 0f, -0.57f);
            portInner.transform.localScale = new Vector3(0.13f, 0.08f, 0.02f);

            Collider piCol = portInner.GetComponent<Collider>();
            if (piCol != null) {
                Object.DestroyImmediate(piCol);
            }

            Material piMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            piMat.SetColor("_BaseColor", Color.black);
            portInner.GetComponent<Renderer>().material = piMat;
        }

        Material ventMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        ventMat.SetColor("_BaseColor", new Color(0.1f, 0.1f, 0.14f));

        for (int i = 0; i < 5; i++) {
            GameObject vent = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vent.name = "Vent_" + i;
            vent.tag = "LCMGrid";
            vent.transform.SetParent(routerGO.transform);
            vent.transform.localPosition = new Vector3(-0.6f + i * 0.3f, 0.18f, 0f);
            vent.transform.localScale = new Vector3(0.22f, 0.02f, 0.8f);

            Collider ventCol = vent.GetComponent<Collider>();
            if (ventCol != null) {
                Object.DestroyImmediate(ventCol);
            }

            vent.GetComponent<Renderer>().material = ventMat;
        }

        return routerGO;
    }
}
