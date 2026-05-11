using System.Collections;
using UnityEngine;

namespace Kaligo.World
{
    /// <summary>
    /// Singleton that handles smooth atmospheric transitions as the player
    /// moves between open-world areas.
    ///
    /// Fog density, fog color, and ambient light lerp over <see cref="transitionDuration"/>
    /// seconds when <see cref="TransitionTo"/> is called. Audio sources crossfade
    /// between ambient loops independently.
    ///
    /// Persists across scenes (DontDestroyOnLoad) so dungeon instances can also
    /// use it when they load. Place on a root-level GameObject in the starting scene.
    /// </summary>
    public class AtmosphereManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────

        public static AtmosphereManager Instance { get; private set; }

        // ── Config ────────────────────────────────────────────────────────────

        [Header("Transition")]
        [SerializeField] private float transitionDuration = 4f;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource ambientSource;
        [SerializeField] private AudioSource musicSource;

        // ── State ─────────────────────────────────────────────────────────────

        private AreaDefinition currentArea;
        private Coroutine      activeTransition;

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

            EnsureAudioSources();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Begin a smooth atmosphere transition to <paramref name="area"/>.
        /// Safe to call at any time — interrupts any transition already in progress.
        /// </summary>
        public void TransitionTo(AreaDefinition area)
        {
            if (area == null || area == currentArea) return;
            currentArea = area;

            if (activeTransition != null)
                StopCoroutine(activeTransition);

            activeTransition = StartCoroutine(DoTransition(area));
        }

        // ── Transition coroutine ──────────────────────────────────────────────

        private IEnumerator DoTransition(AreaDefinition area)
        {
            // Snapshot starting values
            float startFogDensity = RenderSettings.fogDensity;
            Color startFogColor   = RenderSettings.fogColor;
            Color startAmbient    = RenderSettings.ambientLight;

            // Crossfade audio
            CrossfadeAmbient(area);
            CrossfadeMusic(area);

            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t  = Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration);

                if (area.fogDensity > 0f)
                {
                    RenderSettings.fog         = true;
                    RenderSettings.fogDensity  = Mathf.Lerp(startFogDensity, area.fogDensity, t);
                    RenderSettings.fogColor    = Color.Lerp(startFogColor,   area.fogColor,   t);
                }
                else if (t > 0.9f)
                {
                    RenderSettings.fog = false;
                }

                RenderSettings.ambientLight = Color.Lerp(startAmbient, area.ambientLight, t);

                yield return null;
            }

            activeTransition = null;
        }

        // ── Audio crossfade ───────────────────────────────────────────────────

        private void CrossfadeAmbient(AreaDefinition area)
        {
            if (ambientSource == null) return;
            if (area.ambientClip == null) { ambientSource.Stop(); return; }
            if (ambientSource.clip == area.ambientClip) return;

            StartCoroutine(CrossfadeSource(ambientSource, area.ambientClip, area.ambientVolume));
        }

        private void CrossfadeMusic(AreaDefinition area)
        {
            if (musicSource == null) return;
            if (area.musicClip == null) { musicSource.Stop(); return; }
            if (musicSource.clip == area.musicClip) return;

            StartCoroutine(CrossfadeSource(musicSource, area.musicClip, area.musicVolume));
        }

        private IEnumerator CrossfadeSource(AudioSource source, AudioClip newClip, float targetVolume)
        {
            float crossDuration = 1.5f;
            float startVolume   = source.volume;

            // Fade out current
            float e = 0f;
            while (e < crossDuration)
            {
                e += Time.deltaTime;
                source.volume = Mathf.Lerp(startVolume, 0f, e / crossDuration);
                yield return null;
            }

            source.clip   = newClip;
            source.loop   = true;
            source.volume = 0f;
            source.Play();

            // Fade in new
            e = 0f;
            while (e < crossDuration)
            {
                e += Time.deltaTime;
                source.volume = Mathf.Lerp(0f, targetVolume, e / crossDuration);
                yield return null;
            }
            source.volume = targetVolume;
        }

        // ── Setup ─────────────────────────────────────────────────────────────

        private void EnsureAudioSources()
        {
            if (ambientSource == null)
            {
                ambientSource      = gameObject.AddComponent<AudioSource>();
                ambientSource.loop = true;
                ambientSource.spatialBlend = 0f; // 2D — atmosphere is everywhere
            }
            if (musicSource == null)
            {
                musicSource        = gameObject.AddComponent<AudioSource>();
                musicSource.loop   = true;
                musicSource.spatialBlend = 0f;
            }
        }
    }
}
