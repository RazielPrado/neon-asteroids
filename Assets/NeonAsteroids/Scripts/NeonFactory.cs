using UnityEngine;
using UnityEngine.Rendering;

namespace LoneRetr0.NeonAsteroids
{
    public static class NeonPalette
    {
        public static readonly Color Cyan    = new Color(0.10f, 0.95f, 1.00f);
        public static readonly Color Magenta = new Color(1.00f, 0.15f, 0.80f);
        public static readonly Color Purple  = new Color(0.62f, 0.30f, 1.00f);
        public static readonly Color Orange  = new Color(1.00f, 0.50f, 0.10f);
        public static readonly Color Yellow  = new Color(1.00f, 0.90f, 0.30f);
    }

    public static class NeonFactory
    {
        public static Material LineMaterial { get; private set; }

        public static void Init(Shader neonShader, float intensity)
        {
            LineMaterial = new Material(neonShader) { name = "M_NeonLine (runtime)" };
            LineMaterial.SetFloat("_Intensity", intensity);
        }

        public static LineRenderer CreateLine(Transform parent, string name, Vector3[] localPoints,
                                              Color color, float width, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = loop;
            lr.positionCount = localPoints.Length;
            lr.SetPositions(localPoints);
            lr.widthMultiplier = width;
            lr.numCornerVertices = 2;
            lr.numCapVertices = 2;
            lr.alignment = LineAlignment.View;
            lr.sharedMaterial = LineMaterial;
            lr.shadowCastingMode = ShadowCastingMode.Off;
            lr.receiveShadows = false;
            SetColor(lr, color);
            return lr;
        }

        public static void SetColor(LineRenderer lr, Color color)
        {
            lr.startColor = color;
            lr.endColor = color;
        }
    }
}
