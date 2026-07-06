using System.Collections.Generic;
using UnityEngine;

// Procedural particle system generator for game events.
public static class ParticleSpawner {

    private static Material GetParticleMaterial() {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }
        Material mat = new Material(shader);
        if (shader.name.Contains("Particles")) {
            mat.SetFloat("_Blend", 0);
        } else {
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
        return mat;
    }

    public static void SpawnAlongCurve(List<Vector3> pathPoints, Color color, int count) {
        GameObject go = new GameObject("CableSparks");
        go.tag = "LCMGrid";
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(); // Stop to allow modifying main module parameters without warnings

        var main = ps.main;
        main.duration = 0.6f;
        main.loop = false;
        main.startLifetime = 0.6f;
        main.gravityModifier = new ParticleSystem.MinMaxCurve(0.3f);
        main.playOnAwake = false;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = grad;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, 0f);

        var emission = ps.emission;
        emission.enabled = false;

        var shape = ps.shape;
        shape.enabled = false;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = GetParticleMaterial();

        ps.Play();

        for (int i = 0; i < count; i++) {
            int pIdx = Random.Range(0, pathPoints.Count);
            Vector3 pos = pathPoints[pIdx];

            var emitParams = new ParticleSystem.EmitParams();
            emitParams.position = pos;
            emitParams.startColor = color;
            emitParams.startSize = Random.Range(0.03f, 0.12f);
            emitParams.velocity = Random.insideUnitSphere * 0.4f + Vector3.up * 0.2f;
            ps.Emit(emitParams, 1);
        }

        Object.Destroy(go, 0.7f);
    }

    public static void SpawnExplosion(Vector3 position, Color color, int count) {
        GameObject go = new GameObject("Explosion");
        go.tag = "LCMGrid";
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(); // Stop to allow modifying main module parameters without warnings

        var main = ps.main;
        main.duration = 1.2f;
        main.loop = false;
        main.startLifetime = 1.2f;
        main.gravityModifier = new ParticleSystem.MinMaxCurve(0.3f);
        main.playOnAwake = false;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(color, 0f), new GradientColorKey(Color.red, 0.5f), new GradientColorKey(Color.black, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = grad;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1.5f, 0.1f);

        var emission = ps.emission;
        emission.enabled = false;

        var shape = ps.shape;
        shape.enabled = false;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = GetParticleMaterial();

        ps.Play();

        int numParticles = count * 3;
        for (int i = 0; i < numParticles; i++) {
            var emitParams = new ParticleSystem.EmitParams();
            emitParams.position = position + Random.insideUnitSphere * 0.2f;
            emitParams.startColor = color;
            emitParams.startSize = Random.Range(0.08f, 0.25f);
            emitParams.velocity = Random.insideUnitSphere * 2.5f + Vector3.up * 1.5f;
            ps.Emit(emitParams, 1);
        }

        Object.Destroy(go, 1.3f);
    }

    public static void SpawnRotationSparks(Vector3 position, Color color) {
        GameObject go = new GameObject("RotationSparks");
        go.tag = "LCMGrid";
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(); // Stop to allow modifying main module parameters without warnings

        var main = ps.main;
        main.duration = 0.25f;
        main.loop = false;
        main.startLifetime = 0.25f;
        main.gravityModifier = new ParticleSystem.MinMaxCurve(0.1f);
        main.playOnAwake = false;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(color, 0f), new GradientColorKey(Color.white, 0.5f), new GradientColorKey(color, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.4f, 0f), new GradientAlphaKey(0.3f, 0.5f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = grad;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(0.8f, 0.1f);

        var emission = ps.emission;
        emission.enabled = false;

        var shape = ps.shape;
        shape.enabled = false;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = GetParticleMaterial();

        ps.Play();

        int numParticles = 3;
        for (int i = 0; i < numParticles; i++) {
            var emitParams = new ParticleSystem.EmitParams();
            emitParams.position = position + Random.insideUnitSphere * 0.05f;
            emitParams.startColor = color;
            emitParams.startSize = Random.Range(0.005f, 0.012f);
            emitParams.velocity = Random.insideUnitSphere * 0.15f + Vector3.up * 0.08f;
            ps.Emit(emitParams, 1);
        }

        Object.Destroy(go, 0.35f);
    }

    public static void EmitCelebration(Vector3 centerPos, int roundNumber, int gridSize, float particleMultiplier) {
        Vector3[] corners = new Vector3[] {
            new Vector3(0f, 0.1f, 0f),
            new Vector3(gridSize - 1f, 0.1f, 0f),
            new Vector3(0f, 0.1f, gridSize - 1f),
            new Vector3(gridSize - 1f, 0.1f, gridSize - 1f)
        };

        Color[] colors = new Color[] {
            Color.cyan,
            Color.yellow,
            Color.green,
            Color.magenta
        };

        int baseCount = Mathf.RoundToInt(roundNumber * 15 * particleMultiplier);

        for (int i = 0; i < 4; i++) {
            GameObject go = new GameObject("CelebrationSparks_" + i);
            go.tag = "LCMGrid";
            go.transform.position = corners[i];
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(); // Stop to allow modifying main module parameters without warnings

            var main = ps.main;
            main.duration = 1.5f;
            main.loop = false;
            main.startLifetime = 1.5f;
            main.gravityModifier = new ParticleSystem.MinMaxCurve(-0.5f);
            main.playOnAwake = false;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(colors[i], 0f), new GradientColorKey(Color.white, 0.5f), new GradientColorKey(colors[i], 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.8f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = grad;

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1.2f, 0.2f);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = GetParticleMaterial();

            ps.Play();

            for (int k = 0; k < baseCount; k++) {
                var emitParams = new ParticleSystem.EmitParams();
                emitParams.position = corners[i] + Random.insideUnitSphere * 0.1f;
                emitParams.startColor = colors[i];
                emitParams.startSize = Random.Range(0.05f, 0.18f);
                emitParams.velocity = new Vector3(Random.Range(-0.8f, 0.8f), Random.Range(2.0f, 4.0f), Random.Range(-0.8f, 0.8f));
                ps.Emit(emitParams, 1);
            }

            Object.Destroy(go, 2.0f);
        }
    }

    private static ParticleSystem _wormTrailPS = null;

    public static void EmitTrail(Vector3 pos) {
        if (_wormTrailPS == null || _wormTrailPS.gameObject == null) {
            GameObject go = new GameObject("WormTrail");
            go.tag = "LCMGrid";
            _wormTrailPS = go.AddComponent<ParticleSystem>();
            _wormTrailPS.Stop(); // Stop to allow modifying main module parameters without warnings

            var main = _wormTrailPS.main;
            main.startLifetime = 0.35f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
            main.startColor = new Color(0.1f, 0.9f, 0.2f);
            main.gravityModifier = 0.05f;
            main.playOnAwake = false;

            var emission = _wormTrailPS.emission;
            emission.enabled = false;

            var col = _wormTrailPS.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(0.1f, 0.9f, 0.2f), 0f), new GradientColorKey(new Color(0.1f, 0.9f, 0.2f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = new ParticleSystem.MinMaxGradient(g);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = GetParticleMaterial();
        }

        _wormTrailPS.transform.position = pos;
        _wormTrailPS.Emit(2);
    }

    public static void EmitGlitch(Vector3 pos) {
        GameObject go = new GameObject("GlitchBurst");
        go.tag = "LCMGrid";
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(); // Stop to allow modifying main module parameters without warnings

        var main = ps.main;
        main.duration = 0.6f;
        main.loop = false;
        main.startLifetime = 0.5f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
        main.gravityModifier = 0f;
        main.playOnAwake = false;

        var emission = ps.emission;
        emission.enabled = true;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 20) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.15f;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = GetParticleMaterial();

        ps.Play();
        ps.Emit(20);

        var particles = new ParticleSystem.Particle[20];
        int count = ps.GetParticles(particles);
        for (int i = 0; i < count; i++) {
            particles[i].startColor = (i % 2 == 0)
                ? new Color(0.1f, 0.9f, 0.2f)
                : new Color(0.9f, 0.1f, 0.1f);
        }
        ps.SetParticles(particles, count);

        Object.Destroy(go, 0.6f);
    }

    // [NEW - MathUnlock]
    public static void EmitSolve(Vector3 pos) {
        GameObject go = new GameObject("SolveBurst");
        go.tag = "LCMGrid";
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(); // Stop to allow modifying main module parameters without warnings
        
        var main = ps.main;
        main.duration = 0.6f;
        main.loop = false;
        main.startLifetime = 0.6f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
        main.gravityModifier = -0.2f; // float upward
        main.playOnAwake = false;

        var emission = ps.emission;
        emission.enabled = false;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.cyan, 0f),
                new GradientColorKey(Color.white, 0.3f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        col.color = new ParticleSystem.MinMaxGradient(g);

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = GetParticleMaterial();

        go.transform.position = pos;
        ps.Play();

        // Emit 25 particles in a single burst
        ps.Emit(25);

        Object.Destroy(go, 0.7f);
    }
}
