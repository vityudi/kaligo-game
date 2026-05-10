using System.Collections;
using TMPro;
using UnityEngine;
using Kaligo.Services;

namespace Kaligo.UI
{
    /// <summary>
    /// Shows a "LEVEL UP!" banner for a moment when the player levels up.
    /// Attach to a CanvasGroup that starts at alpha 0.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class LevelUpNotification : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI messageLabel;
        [SerializeField] private float           displayDuration = 2f;
        [SerializeField] private float           fadeDuration    = 0.4f;

        private CanvasGroup canvasGroup;
        private Coroutine   showRoutine;

        private void Awake()
        {
            canvasGroup       = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
        }

        private void Start()
        {
            if (GameServices.Progression != null)
                GameServices.Progression.OnLevelUp += OnLevelUp;
        }

        private void OnDestroy()
        {
            if (GameServices.Progression != null)
                GameServices.Progression.OnLevelUp -= OnLevelUp;
        }

        private void OnLevelUp(int newLevel)
        {
            if (messageLabel != null)
                messageLabel.text = $"LEVEL UP!\nLevel {newLevel}";

            if (showRoutine != null) StopCoroutine(showRoutine);
            showRoutine = StartCoroutine(ShowRoutine());
        }

        private IEnumerator ShowRoutine()
        {
            // Fade in
            float t = 0f;
            while (t < fadeDuration)
            {
                canvasGroup.alpha = t / fadeDuration;
                t += Time.deltaTime;
                yield return null;
            }
            canvasGroup.alpha = 1f;

            yield return new WaitForSeconds(displayDuration);

            // Fade out
            t = fadeDuration;
            while (t > 0f)
            {
                canvasGroup.alpha = t / fadeDuration;
                t -= Time.deltaTime;
                yield return null;
            }
            canvasGroup.alpha = 0f;
        }
    }
}
