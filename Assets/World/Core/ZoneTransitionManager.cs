using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Kaligo.World
{
    /// <summary>
    /// Singleton that handles async scene transitions between zones.
    ///
    /// Flow:
    ///   1. ZonePortal calls <see cref="TransitionTo"/>.
    ///   2. Manager fades screen to black.
    ///   3. Async loads the target scene.
    ///   4. Moves the player to the correct <see cref="PlayerSpawnPoint"/>.
    ///   5. Applies zone atmosphere (fog, ambient).
    ///   6. Fades back in.
    ///
    /// The manager lives on a DontDestroyOnLoad object so it persists across scenes.
    /// It creates its own fade Canvas if one isn't already present.
    /// </summary>
    public class ZoneTransitionManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────

        public static ZoneTransitionManager Instance { get; private set; }

        // ── Config ────────────────────────────────────────────────────────────

        [Header("Fade")]
        [SerializeField] private float fadeDuration = 0.6f;
        [SerializeField] private Color fadeColor    = Color.black;

        // ── State ─────────────────────────────────────────────────────────────

        private Canvas    fadeCanvas;
        private Image     fadeImage;
        private bool      isTransitioning;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            BuildFadeCanvas();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Begin a transition to <paramref name="zone"/>. The player will be placed
        /// at the spawn point with id <paramref name="spawnId"/> in the new scene.
        /// Safe to call from any MonoBehaviour.
        /// </summary>
        public void TransitionTo(ZoneDefinition zone, string spawnId = "default")
        {
            if (isTransitioning) return;
            StartCoroutine(DoTransition(zone, spawnId));
        }

        // ── Transition coroutine ──────────────────────────────────────────────

        private IEnumerator DoTransition(ZoneDefinition zone, string spawnId)
        {
            isTransitioning = true;

            // 1. Fade to black
            yield return Fade(0f, 1f, fadeDuration);

            // 2. Load scene asynchronously
            AsyncOperation op = SceneManager.LoadSceneAsync(zone.sceneName, LoadSceneMode.Single);
            op.allowSceneActivation = false;

            while (op.progress < 0.9f)
                yield return null;

            op.allowSceneActivation = true;
            yield return null; // let scene initialise

            // 3. Apply zone atmosphere
            ApplyAtmosphere(zone);

            // 4. Move player to spawn point
            PlacePlayer(spawnId);

            // 5. Fade back in
            yield return Fade(1f, 0f, fadeDuration);

            isTransitioning = false;
        }

        // ── Atmosphere ────────────────────────────────────────────────────────

        private static void ApplyAtmosphere(ZoneDefinition zone)
        {
            if (zone.fogDensity > 0f)
            {
                RenderSettings.fog         = true;
                RenderSettings.fogColor    = zone.fogColor;
                RenderSettings.fogDensity  = zone.fogDensity;
                RenderSettings.fogMode     = FogMode.Exponential;
            }
            else
            {
                RenderSettings.fog = false;
            }

            RenderSettings.ambientLight = zone.ambientLight;
        }

        // ── Player spawn ──────────────────────────────────────────────────────

        private static void PlacePlayer(string spawnId)
        {
            var player = GameObject.FindWithTag("Player");
            if (player == null) return;

            // Disable CharacterController briefly to allow teleport
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            // Find spawn point by ID, fall back to default, then to any spawn point
            PlayerSpawnPoint target = FindSpawnPoint(spawnId);
            if (target != null)
            {
                player.transform.SetPositionAndRotation(
                    target.transform.position,
                    target.transform.rotation);
            }

            if (cc != null) cc.enabled = true;
        }

        private static PlayerSpawnPoint FindSpawnPoint(string spawnId)
        {
            var all = FindObjectsOfType<PlayerSpawnPoint>();
            if (all == null || all.Length == 0) return null;

            // Exact match
            foreach (var sp in all)
                if (sp.spawnId == spawnId) return sp;

            // Default fallback
            foreach (var sp in all)
                if (sp.isDefault) return sp;

            // Any spawn point
            return all[0];
        }

        // ── Fade Canvas ───────────────────────────────────────────────────────

        private void BuildFadeCanvas()
        {
            var go          = new GameObject("[ZoneTransition] FadeCanvas");
            go.transform.SetParent(transform);
            DontDestroyOnLoad(go);

            fadeCanvas                  = go.AddComponent<Canvas>();
            fadeCanvas.renderMode       = RenderMode.ScreenSpaceOverlay;
            fadeCanvas.sortingOrder     = 9999;

            go.AddComponent<UnityEngine.UI.CanvasScaler>();
            go.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            var imgGO       = new GameObject("Overlay");
            imgGO.transform.SetParent(go.transform, false);

            fadeImage       = imgGO.AddComponent<Image>();
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);

            var rt          = imgGO.GetComponent<RectTransform>();
            rt.anchorMin    = Vector2.zero;
            rt.anchorMax    = Vector2.one;
            rt.offsetMin    = Vector2.zero;
            rt.offsetMax    = Vector2.zero;
        }

        private IEnumerator Fade(float fromAlpha, float toAlpha, float duration)
        {
            if (fadeImage == null) yield break;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t  = Mathf.Clamp01(elapsed / duration);
                float a  = Mathf.Lerp(fromAlpha, toAlpha, t);
                fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, a);
                yield return null;
            }
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, toAlpha);
        }
    }
}
