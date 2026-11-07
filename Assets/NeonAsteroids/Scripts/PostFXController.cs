using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LoneRetr0.NeonAsteroids
{
    public class PostFXController : MonoBehaviour
    {
        [SerializeField] float baseBloom = 1.3f;
        [SerializeField] float baseChroma = 0.12f;

        static readonly int GlitchId = Shader.PropertyToID("_CRTGlitch");

        Camera cam;
        Vector3 camBase;
        VolumeProfile profile;
        Bloom bloom;
        ChromaticAberration chroma;

        float chromaKick, bloomKick, shake, glitch, hitStopUntil, seed;
        bool paused;

        public void SetPaused(bool value)
        {
            paused = value;
            Time.timeScale = paused ? 0f : 1f;
        }

        public void Init(Camera camera)
        {
            cam = camera;
            camBase = cam.transform.position;
            seed = Random.value * 100f;

            var volumeGO = new GameObject("PostFX Volume (runtime)");
            var volume = volumeGO.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 100f;

            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "NeonProfile (runtime)";

            bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(baseBloom);
            bloom.threshold.Override(0.8f);
            bloom.scatter.Override(0.72f);
            bloom.highQualityFiltering.Override(true);

            chroma = profile.Add<ChromaticAberration>(true);
            chroma.intensity.Override(baseChroma);

            var color = profile.Add<ColorAdjustments>(true);
            color.saturation.Override(20f);
            color.contrast.Override(12f);

            var tonemap = profile.Add<Tonemapping>(true);
            tonemap.mode.Override(TonemappingMode.None);
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0f);

            volume.sharedProfile = profile;
        }

        public void Kick(float chromaAmount, float bloomAmount, float shakeAmount, float glitchAmount = 0f)
        {
            chromaKick = Mathf.Max(chromaKick, chromaAmount);
            bloomKick = Mathf.Max(bloomKick, bloomAmount);
            shake = Mathf.Max(shake, shakeAmount);
            glitch = Mathf.Max(glitch, glitchAmount);
        }

        public void HitStop(float seconds)
        {
            hitStopUntil = Mathf.Max(hitStopUntil, Time.unscaledTime + seconds);
        }

        void LateUpdate()
        {
            if (cam == null || profile == null) return;

            float dt = Time.unscaledDeltaTime;
            if (paused) Time.timeScale = 0f;
            else Time.timeScale = Time.unscaledTime < hitStopUntil ? 0.05f : 1f;

            chromaKick = Mathf.MoveTowards(chromaKick, 0f, dt * 1.8f);
            bloomKick = Mathf.MoveTowards(bloomKick, 0f, dt * 5f);
            shake = Mathf.MoveTowards(shake, 0f, dt * 1.6f);
            glitch = Mathf.MoveTowards(glitch, 0f, dt * 1.4f);

            float breathe = Mathf.Sin(Time.unscaledTime * 2.1f) * 0.08f;
            chroma.intensity.value = Mathf.Clamp01(baseChroma + chromaKick);
            bloom.intensity.value = baseBloom + breathe + bloomKick;
            Shader.SetGlobalFloat(GlitchId, glitch);

            float t = Time.unscaledTime * 28f;
            float s = shake * shake;
            var offset = new Vector3(
                (Mathf.PerlinNoise(seed, t) - 0.5f) * 2f,
                (Mathf.PerlinNoise(seed + 17f, t) - 0.5f) * 2f,
                0f) * s * 0.9f;
            float roll = (Mathf.PerlinNoise(seed + 31f, t) - 0.5f) * s * 4f;
            cam.transform.SetPositionAndRotation(camBase + offset, Quaternion.Euler(0f, 0f, roll));
        }

        void OnDisable()
        {
            Shader.SetGlobalFloat(GlitchId, 0f);
            Time.timeScale = 1f;
        }

        void OnDestroy()
        {
            if (profile != null) Destroy(profile);
        }
    }
}
