using System.Collections;
using UnityEngine;
using TMPro;

namespace Kaligo.Combat
{
    /// <summary>
    /// Spawns a floating damage number above a hit position.
    /// Called automatically by HitboxController on every confirmed hit.
    /// No scene setup required — uses a self-contained TextMeshPro world-space label.
    /// </summary>
    public static class DamageNumberSpawner
    {
        private static readonly Color ColorLight  = new Color(1f,   0.92f, 0.3f, 1f);  // yellow — light hit
        private static readonly Color ColorHeavy  = new Color(1f,   0.35f, 0.1f, 1f);  // orange-red — heavy hit
        private static readonly Color ColorCrit   = new Color(1f,   1f,   1f,   1f);   // white — critical

        /// <param name="worldPos">Where to spawn the number (typically hit point).</param>
        /// <param name="damage">Damage value to display.</param>
        /// <param name="isHeavy">True = heavy attack colouring + larger scale.</param>
        public static void Spawn(Vector3 worldPos, float damage, bool isHeavy)
        {
            var go  = new GameObject("DmgNum");
            var tmp = go.AddComponent<TextMeshPro>();

            tmp.text         = Mathf.RoundToInt(damage).ToString();
            tmp.fontSize     = isHeavy ? 5.5f : 4f;
            tmp.alignment    = TextAlignmentOptions.Center;
            tmp.fontStyle    = FontStyles.Bold;
            tmp.color        = isHeavy ? ColorHeavy : ColorLight;
            tmp.sortingOrder = 20;

            go.transform.position = worldPos + Vector3.up * 0.4f;

            // Billboard toward camera
            if (Camera.main != null)
                go.transform.rotation = Camera.main.transform.rotation;

            var mover = go.AddComponent<DamageNumberMover>();
            mover.isHeavy = isHeavy;
        }
    }

    /// <summary>
    /// Drives the floating damage number upward, fading it out, then destroys itself.
    /// </summary>
    internal class DamageNumberMover : MonoBehaviour
    {
        public bool isHeavy;

        private TextMeshPro _tmp;
        private float       _t;
        private float       _duration;
        private float       _riseSpeed;
        private Vector3     _drift;   // slight random horizontal drift

        private void Awake()
        {
            _tmp       = GetComponent<TextMeshPro>();
            _duration  = isHeavy ? 1.1f : 0.8f;
            _riseSpeed = isHeavy ? 2.2f : 1.5f;

            // Random horizontal wobble
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            _drift = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 0.3f;
        }

        private void Update()
        {
            _t += Time.unscaledDeltaTime;  // unscaled so it survives hit-stop

            // Rise + drift
            float dt = Time.unscaledDeltaTime;
            transform.position += (Vector3.up * _riseSpeed + _drift * (1f - _t / _duration)) * dt;

            // Billboard
            if (Camera.main != null)
                transform.rotation = Camera.main.transform.rotation;

            // Fade in last 40% of lifetime
            float progress = _t / _duration;
            if (progress > 0.6f)
            {
                float alpha = 1f - (progress - 0.6f) / 0.4f;
                Color c = _tmp.color;
                c.a = Mathf.Clamp01(alpha);
                _tmp.color = c;
            }

            if (_t >= _duration)
                Destroy(gameObject);
        }
    }
}
