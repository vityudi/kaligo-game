using System.Collections;
using UnityEngine;
using TMPro;

namespace Kaligo.World
{
    /// <summary>
    /// Displays an area-name banner (like RuneScape's area notification or Elden Ring's
    /// location title) when the player crosses into a new area.
    ///
    /// Attach to a Canvas TextMeshPro element in the HUD scene or let it self-create.
    /// Alternatively call <see cref="Show"/> statically — it will create its own
    /// temporary label if no registered instance is found.
    /// </summary>
    public class AreaNameBanner : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────

        public static AreaNameBanner Instance { get; private set; }

        // ── Config ────────────────────────────────────────────────────────────

        [SerializeField] private TextMeshProUGUI titleLabel;
        [SerializeField] private TextMeshProUGUI subtitleLabel;
        [SerializeField] private CanvasGroup      group;

        [SerializeField] private float fadeIn      = 0.6f;
        [SerializeField] private float holdTime    = 2.5f;
        [SerializeField] private float fadeOut     = 1.2f;

        private Coroutine activeRoutine;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            Instance = this;
            if (group == null) group = GetComponent<CanvasGroup>();
            if (group != null) group.alpha = 0f;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Show the banner for the given area. Safe to call statically — no-ops if no instance.
        /// </summary>
        public static void Show(AreaDefinition area)
        {
            if (Instance == null) return;
            Instance.ShowBanner(area);
        }

        private void ShowBanner(AreaDefinition area)
        {
            if (titleLabel    != null) titleLabel.text    = area.displayName;
            if (subtitleLabel != null)
            {
                string sub = area.type == AreaType.Village
                    ? "Safe Area"
                    : $"Level {area.recommendedLevelMin}–{area.recommendedLevelMax}";
                subtitleLabel.text = sub;
            }

            if (activeRoutine != null) StopCoroutine(activeRoutine);
            activeRoutine = StartCoroutine(AnimateBanner(area.nameBannerDuration));
        }

        private IEnumerator AnimateBanner(float hold)
        {
            // Fade in
            yield return Fade(0f, 1f, fadeIn);

            // Hold
            yield return new WaitForSeconds(hold);

            // Fade out
            yield return Fade(1f, 0f, fadeOut);
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            if (group == null) yield break;
            float e = 0f;
            while (e < duration)
            {
                e += Time.deltaTime;
                group.alpha = Mathf.Lerp(from, to, e / duration);
                yield return null;
            }
            group.alpha = to;
        }
    }
}
