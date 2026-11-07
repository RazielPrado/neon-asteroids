using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LoneRetr0.NeonAsteroids
{
    [DefaultExecutionOrder(-100)]
    public class NeonAsteroidsGame : MonoBehaviour
    {
        enum State { Title, Playing, Dead, GameOver, Paused }

        static readonly string[] PauseOptions = { "RESUME", "RESTART", "MAIN MENU", "QUIT GAME" };

        [Header("Shaders")]
        [SerializeField] Shader neonShader;
        [SerializeField] Shader backgroundShader;

        [Header("UI")]
        [SerializeField] GameHUD hud;

        [Header("Presentation")]
        [SerializeField] string gameTitle = "NEON ASTEROIDS";
        [SerializeField] string signature = "Made by LoneRetr0";
        [SerializeField] string musicCredit = "Music by VOiD1 Gaming";
        [SerializeField] float orthographicSize = 10f;
        [SerializeField, Range(1f, 12f)] float neonIntensity = 4f;

        [Header("Rules")]
        [SerializeField] int startLives = 3;
        [SerializeField] int startAsteroids = 4;
        [SerializeField] int maxAsteroidsPerWave = 11;
        [SerializeField] int maxBullets = 8;
        [SerializeField] float respawnInvulnerability = 2.5f;
        [SerializeField] float waveStartShield = 1.5f;
        [SerializeField] float respawnDelay = 1.2f;
        [SerializeField] float maxRespawnWait = 0.6f;
        [SerializeField, Range(0.2f, 0.9f)] float safeAreaFraction = 0.55f;
        [SerializeField] int extraLifeEvery = 10000;

        [Header("Mobile")]
        [SerializeField] Font touchControlsFont;
        [SerializeField] bool forceMobileControls;

        const string MobileControlsHelp =
            "<color=#FFD84D>LEFT</color> AIM & THRUST     <color=#FFD84D>RIGHT</color> FIRE     " +
            "<color=#FFD84D>HYPER</color> JUMP     <color=#FFD84D>II</color> PAUSE";

        bool isMobile;

        public static Vector2 HalfExtents { get; private set; } = new Vector2(17.8f, 10f);

        const string HighScoreKey = "NeonAsteroids_HighScore";

        readonly List<Asteroid> asteroids = new List<Asteroid>();
        readonly List<Bullet> bullets = new List<Bullet>();

        Camera cam;
        Transform world, background;
        Material backgroundMaterial;
        PostFXController fx;
        NeonAudio sfx;
        GameHUD ui;
        PlayerShip player;

        State state, stateBeforePause;
        int pauseIndex;
        float stateTimer, waveTimer;
        int score, highScore, lives, wave, nextExtraLife;

#if UNITY_EDITOR
        void Reset() => FindShaders();
        void OnValidate() => FindShaders();
#endif

        void FindShaders()
        {
            if (neonShader == null) neonShader = Shader.Find("LoneRetr0/NeonUnlit");
            if (backgroundShader == null) backgroundShader = Shader.Find("LoneRetr0/SynthwaveBackground");
        }

        void Awake()
        {
            FindShaders();
            if (neonShader == null)
            {
                Debug.LogError("[NeonAsteroids] Shader LoneRetr0/NeonUnlit not found.");
                enabled = false;
                return;
            }

            SetupCamera();
            NeonFactory.Init(neonShader, neonIntensity);

            world = new GameObject("World").transform;
            NeonDebris.Root = world;

            CreateBackground();
            UpdateBounds();

            fx = gameObject.AddComponent<PostFXController>();
            fx.Init(cam);

            ui = hud;
            if (ui == null)
            {
                Debug.LogError("[NeonAsteroids] HUD not assigned. Use Tools > Neon Asteroids > Create HUD.");
                enabled = false;
                return;
            }

            sfx = GetComponent<NeonAudio>();
            if (sfx == null) sfx = gameObject.AddComponent<NeonAudio>();
            isMobile = forceMobileControls || Application.isMobilePlatform;
            if (isMobile) SetupMobile();
            else if (GetComponent<CursorAutoHide>() == null) gameObject.AddComponent<CursorAutoHide>();

            player = new GameObject("PlayerShip").AddComponent<PlayerShip>();
            player.transform.SetParent(world, false);
            player.Init(this);
            player.gameObject.SetActive(false);

            highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
            EnterTitle();
        }

        void SetupCamera()
        {
            cam = Camera.main;
            if (cam == null)
            {
                cam = new GameObject("Main Camera").AddComponent<Camera>();
                cam.tag = "MainCamera";
            }

            cam.orthographic = true;
            cam.orthographicSize = orthographicSize;
            cam.transform.SetPositionAndRotation(new Vector3(0f, 0f, -10f), Quaternion.identity);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0f, 0.05f);
            cam.allowHDR = true;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;

            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
        }

        void CreateBackground()
        {
            if (backgroundShader == null) return;

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "SynthwaveBackground";
            Destroy(quad.GetComponent<Collider>());
            quad.transform.position = new Vector3(0f, 0f, 20f);

            backgroundMaterial = new Material(backgroundShader) { name = "M_Synthwave (runtime)" };
            var mr = quad.GetComponent<MeshRenderer>();
            mr.sharedMaterial = backgroundMaterial;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;

            background = quad.transform;
        }

        void UpdateBounds()
        {
            float h = cam.orthographicSize;
            HalfExtents = new Vector2(h * cam.aspect, h);

            if (background != null)
            {
                background.localScale = new Vector3(HalfExtents.x * 2.3f, HalfExtents.y * 2.3f, 1f);
                backgroundMaterial.SetFloat("_Aspect", cam.aspect);
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            UpdateBounds();
            ui.Tick(Time.unscaledDeltaTime);
            MobileControls.GameplayVisible = state == State.Playing || state == State.Dead;

            if (state == State.Paused)
            {
                UpdatePauseMenu();
                return;
            }

            if ((state == State.Playing || state == State.Dead) && NeonInput.PausePressed)
            {
                Pause();
                return;
            }

            switch (state)
            {
                case State.Title:
                    if (NeonInput.StartPressed) { sfx.PlayMenuConfirm(); StartGame(); }
                    else if (NeonInput.BackPressed) QuitGame();
                    break;

                case State.Playing:
                    HandleCollisions();
                    CheckWaveCleared(dt);
                    break;

                case State.Dead:
                    HandleCollisions();
                    CheckWaveCleared(dt);
                    stateTimer -= dt;

                    if (stateTimer <= 0f && (IsSpotSafe(Vector2.zero) || stateTimer <= -maxRespawnWait))
                        RespawnPlayer(FindSafestPoint());
                    break;

                case State.GameOver:
                    HandleCollisions();
                    stateTimer -= dt;
                    if (stateTimer <= 0f && NeonInput.StartPressed) { sfx.PlayMenuConfirm(); StartGame(); }
                    else if (NeonInput.BackPressed) { sfx.PlayMenuConfirm(); EnterTitle(); }
                    break;
            }

            CleanupBullets();
        }

        void EnterTitle()
        {
            state = State.Title;
            player.gameObject.SetActive(false);
            ClearWorld();
            SpawnWave(5, 1);
            ui.ShowCenter(gameTitle, isMobile ? "TAP TO START" : "PRESS SPACE  ·  ESC TO QUIT");
            ui.ShowControls(true);
            ui.ShowSignature(signature);
            ui.ShowMusicCredit(musicCredit);
            ui.SetScore(0);
            ui.SetHighScore(highScore);
            ui.SetLives(0);
        }

        void StartGame()
        {
            ClearWorld();
            score = 0;
            lives = startLives;
            wave = 0;
            waveTimer = 0f;
            nextExtraLife = extraLifeEvery;

            ui.ShowCenter("", "");
            ui.ShowSignature("");
            ui.ShowMusicCredit("");
            ui.ShowControls(false);
            ui.SetScore(score);
            ui.SetLives(lives);

            RespawnPlayer(Vector2.zero);
            NextWave();
        }

        void Pause()
        {
            stateBeforePause = state;
            state = State.Paused;
            pauseIndex = 0;
            fx.SetPaused(true);
            sfx.SetPaused(true);
            sfx.PlayMenuMove();
            ui.ShowPause(true);
            ui.SetPauseMenu(PauseOptions, pauseIndex);
        }

        void Unpause()
        {
            fx.SetPaused(false);
            sfx.SetPaused(false);
            ui.ShowPause(false);
        }

        void UpdatePauseMenu()
        {
            if (NeonInput.PausePressed)
            {
                sfx.PlayMenuMove();
                Unpause();
                state = stateBeforePause;
                return;
            }

            if (NeonInput.MenuUp)
            {
                pauseIndex = (pauseIndex + PauseOptions.Length - 1) % PauseOptions.Length;
                sfx.PlayMenuMove();
            }
            if (NeonInput.MenuDown)
            {
                pauseIndex = (pauseIndex + 1) % PauseOptions.Length;
                sfx.PlayMenuMove();
            }
            bool confirm = NeonInput.ConfirmPressed;
            if (NeonInput.TryGetPointerDown(out Vector2 pointer))
            {
                int tapped = ui.PauseOptionAt(pointer);
                if (tapped >= 0)
                {
                    pauseIndex = tapped;
                    confirm = true;
                }
            }
            ui.SetPauseMenu(PauseOptions, pauseIndex);

            if (!confirm) return;

            sfx.PlayMenuConfirm();
            Unpause();
            switch (pauseIndex)
            {
                case 0: state = stateBeforePause; break;
                case 1: SaveHighScore(); StartGame(); break;
                case 2: SaveHighScore(); EnterTitle(); break;
                case 3: QuitGame(); break;
            }
        }

        void SetupMobile()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.AutoRotation;

            var controls = GetComponent<MobileControls>();
            if (controls == null) controls = gameObject.AddComponent<MobileControls>();
            controls.Init(touchControlsFont);
            ui.SetControlsHelp(MobileControlsHelp);
        }

        void OnApplicationPause(bool isPaused)
        {
            if (isPaused && (state == State.Playing || state == State.Dead)) Pause();
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && isMobile && (state == State.Playing || state == State.Dead)) Pause();
        }

        void SaveHighScore()
        {
            PlayerPrefs.SetInt(HighScoreKey, highScore);
            PlayerPrefs.Save();
        }

        void QuitGame()
        {
            SaveHighScore();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void NextWave()
        {
            wave++;
            SpawnWave(Mathf.Min(startAsteroids + wave - 1, maxAsteroidsPerWave), wave);

            if (player.gameObject.activeSelf) player.GrantInvulnerability(waveStartShield);
            ui.Flash($"WAVE {wave}", 1.6f);
            sfx.PlayWaveStart();
            fx.Kick(0.25f, 0.6f, 0f, 0.15f);
        }

        void CheckWaveCleared(float dt)
        {
            if (asteroids.Count > 0) return;
            waveTimer += dt;
            if (waveTimer < 1.5f) return;
            waveTimer = 0f;
            NextWave();
        }

        void EnterGameOver()
        {
            state = State.GameOver;
            stateTimer = 1.2f;
            SaveHighScore();
            ui.ShowCenter("GAME OVER", isMobile ? "TAP TO RESTART" : "SPACE: RESTART  ·  ESC: MAIN MENU");
        }

        void RespawnPlayer(Vector2 position)
        {
            player.Respawn(position, respawnInvulnerability);
            state = State.Playing;
            NeonDebris.Burst(position, NeonPalette.Cyan, 16, 5f, 0.25f, 0.5f);
            fx.Kick(0.2f, 0.6f, 0.1f, 0.2f);
        }

        const float SafeClearance = 3f;
        const float LookAhead = 0.8f;

        float Clearance(Vector2 point)
        {
            float best = float.MaxValue;
            foreach (var a in asteroids)
            {
                Vector2 now = a.transform.position;
                Vector2 soon = now + a.Velocity * LookAhead;
                float d = Mathf.Min(WrappedDistance(point, now), WrappedDistance(point, soon)) - a.Radius;
                if (d < best) best = d;
            }
            return best;
        }

        bool IsSpotSafe(Vector2 point) => Clearance(point) >= SafeClearance;

        Vector2 FindSafestPoint()
        {
            if (IsSpotSafe(Vector2.zero)) return Vector2.zero;

            Vector2 area = HalfExtents * safeAreaFraction;
            Vector2 best = Vector2.zero;
            float bestScore = float.MinValue;
            const int steps = 7;

            for (int ix = 0; ix < steps; ix++)
            for (int iy = 0; iy < steps; iy++)
            {
                var p = new Vector2(
                    Mathf.Lerp(-area.x, area.x, ix / (steps - 1f)),
                    Mathf.Lerp(-area.y, area.y, iy / (steps - 1f)));

                float score = Mathf.Min(Clearance(p), 8f) - p.magnitude * 0.15f;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = p;
                }
            }
            return best;
        }

        void KillPlayer()
        {
            Vector3 pos = player.transform.position;
            NeonDebris.Shatter(player.transform, PlayerShip.HullPoints, true, NeonPalette.Cyan, player.Velocity * 0.3f, 4f, 1.6f);
            NeonDebris.Burst(pos, NeonPalette.Cyan, 24, 9f, 0.3f, 0.9f);
            NeonDebris.Burst(pos, Color.white, 10, 6f, 0.2f, 0.6f);

            fx.Kick(1f, 2.5f, 1f, 1f);
            sfx.PlayDeath();
            fx.HitStop(0.12f);

            player.gameObject.SetActive(false);
            lives--;
            ui.SetLives(lives);

            if (lives <= 0) EnterGameOver();
            else
            {
                state = State.Dead;
                stateTimer = respawnDelay;
            }
        }

        public bool TrySpawnBullet(Vector3 position, Vector2 direction, Vector2 shipVelocity, float speed, float life)
        {
            if (bullets.Count >= maxBullets) return false;

            bullets.Add(Bullet.Create(world, position, direction, shipVelocity, speed, life));
            NeonDebris.Burst(position, NeonPalette.Yellow, 3, 3f, 0.12f, 0.15f);
            fx.Kick(0.02f, 0.1f, 0.03f);
            sfx.PlayShoot();
            return true;
        }

        public void Hyperspace(PlayerShip ship)
        {
            Vector3 from = ship.transform.position;
            NeonDebris.Burst(from, NeonPalette.Magenta, 14, 6f, 0.3f, 0.5f);

            Vector2 area = HalfExtents * safeAreaFraction;
            Vector2 to = Vector2.zero;
            float bestClear = float.MinValue;
            for (int i = 0; i < 6; i++)
            {
                var p = new Vector2(Random.Range(-area.x, area.x), Random.Range(-area.y, area.y));
                float c = Clearance(p);
                if (c > bestClear) { bestClear = c; to = p; }
            }
            ship.Teleport(to);

            NeonFx.Beam(from, to, NeonPalette.Cyan, 0.4f, 0.12f);
            NeonFx.Ring(to, NeonPalette.Yellow, 3.2f, 0.7f, 0.35f, 0.08f);
            NeonFx.Ring(to, NeonPalette.Cyan, 0.6f, 2.6f, 0.5f, 0.06f);
            NeonFx.Ring(from, NeonPalette.Magenta, 0.4f, 1.8f, 0.35f, 0.05f);
            ship.FlashWhite(0.35f);

            NeonDebris.Burst(to, NeonPalette.Cyan, 14, 4f, 0.3f, 0.4f);
            fx.Kick(0.5f, 0.8f, 0.15f, 0.6f);
            sfx.PlayHyperspace();
        }

        const float WaveSpawnClearance = 7f;

        static float WrappedDistance(Vector2 a, Vector2 b)
        {
            Vector2 size = HalfExtents * 2f;
            float dx = Mathf.Abs(a.x - b.x);
            float dy = Mathf.Abs(a.y - b.y);
            dx = Mathf.Min(dx, size.x - dx);
            dy = Mathf.Min(dy, size.y - dy);
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        Vector2 RandomEdgePoint() => Random.value < 0.5f
            ? new Vector2(Random.value < 0.5f ? -HalfExtents.x : HalfExtents.x, Random.Range(-HalfExtents.y, HalfExtents.y))
            : new Vector2(Random.Range(-HalfExtents.x, HalfExtents.x), Random.value < 0.5f ? -HalfExtents.y : HalfExtents.y);

        void SpawnWave(int count, int waveNumber)
        {
            float speedMul = 1f + 0.08f * (waveNumber - 1);
            bool avoidPlayer = player != null && player.gameObject.activeSelf;
            Vector2 playerPos = avoidPlayer ? (Vector2)player.transform.position : Vector2.zero;

            for (int i = 0; i < count; i++)
            {
                Vector2 pos = RandomEdgePoint();
                if (avoidPlayer)
                {
                    float bestDist = WrappedDistance(pos, playerPos);
                    for (int attempt = 0; attempt < 30 && bestDist < WaveSpawnClearance; attempt++)
                    {
                        Vector2 candidate = RandomEdgePoint();
                        float d = WrappedDistance(candidate, playerPos);
                        if (d > bestDist) { bestDist = d; pos = candidate; }
                    }
                }

                Vector2 vel = Random.insideUnitCircle.normalized * Random.Range(1f, 2.2f) * speedMul;
                SpawnAsteroid(3, pos, vel, true);
                NeonDebris.Burst(pos, NeonPalette.Magenta, 6, 4f, 0.25f, 0.4f);
            }
        }

        void SpawnAsteroid(int size, Vector2 position, Vector2 velocity, bool flash)
        {
            asteroids.Add(Asteroid.Create(world, size, position, velocity, flash));
        }

        void HitAsteroid(Asteroid ast)
        {
            asteroids.Remove(ast);
            if (state != State.Title) AddScore(ast.ScoreValue);

            Vector3 pos = ast.transform.position;
            NeonDebris.Shatter(ast.transform, ast.Points, true, ast.BaseColor, ast.Velocity, 2.5f + ast.Size, 0.9f);
            NeonDebris.Burst(pos, ast.BaseColor, 6 + ast.Size * 4, 6f, 0.25f, 0.5f);

            float k = ast.Size / 3f;
            fx.Kick(0.05f + 0.15f * k, 0.8f * k, 0.15f + 0.35f * k * k);
            if (ast.Size == 3) fx.HitStop(0.045f);
            sfx.PlayExplosion(ast.Size);

            if (ast.Size > 1)
            {
                Vector2 baseDir = ast.Velocity.sqrMagnitude > 0.01f ? ast.Velocity.normalized : Random.insideUnitCircle.normalized;
                float speed = ast.Velocity.magnitude * 1.25f + 0.6f;
                for (int i = 0; i < 2; i++)
                {
                    float angle = (i == 0 ? 1f : -1f) * Random.Range(25f, 60f);
                    Vector2 dir = Quaternion.Euler(0f, 0f, angle) * baseDir;
                    SpawnAsteroid(ast.Size - 1, (Vector2)pos + dir * ast.Radius * 0.3f, dir * speed, true);
                }
            }

            Destroy(ast.gameObject);
        }

        void HandleCollisions()
        {
            for (int b = bullets.Count - 1; b >= 0; b--)
            {
                Vector2 bp = bullets[b].transform.position;
                for (int a = asteroids.Count - 1; a >= 0; a--)
                {
                    var ast = asteroids[a];
                    float r = ast.Radius * 0.9f + Bullet.Radius;
                    if (((Vector2)ast.transform.position - bp).sqrMagnitude < r * r)
                    {
                        DestroyBullet(b);
                        HitAsteroid(ast);
                        break;
                    }
                }
            }

            if (state != State.Playing || !player.gameObject.activeSelf || player.Invulnerable) return;

            Vector2 pp = player.transform.position;
            for (int a = asteroids.Count - 1; a >= 0; a--)
            {
                var ast = asteroids[a];
                float r = ast.Radius * 0.8f + PlayerShip.Radius;
                if (((Vector2)ast.transform.position - pp).sqrMagnitude < r * r)
                {
                    HitAsteroid(ast);
                    KillPlayer();
                    break;
                }
            }
        }

        void AddScore(int amount)
        {
            score += amount;
            if (score >= nextExtraLife)
            {
                lives++;
                nextExtraLife += extraLifeEvery;
                ui.SetLives(lives);
                ui.Flash("EXTRA LIFE", 1.2f);
                sfx.PlayExtraLife();
            }
            ui.SetScore(score);

            if (score > highScore)
            {
                highScore = score;
                ui.SetHighScore(highScore);
            }
        }

        void CleanupBullets()
        {
            for (int i = bullets.Count - 1; i >= 0; i--)
                if (bullets[i].Expired) DestroyBullet(i);
        }

        void DestroyBullet(int index)
        {
            Destroy(bullets[index].gameObject);
            bullets.RemoveAt(index);
        }

        void ClearWorld()
        {
            foreach (var a in asteroids) if (a != null) Destroy(a.gameObject);
            foreach (var b in bullets) if (b != null) Destroy(b.gameObject);
            asteroids.Clear();
            bullets.Clear();
        }

        void OnDestroy()
        {
            Time.timeScale = 1f;
            if (backgroundMaterial != null) Destroy(backgroundMaterial);
            if (NeonFactory.LineMaterial != null) Destroy(NeonFactory.LineMaterial);
        }
    }
}
