using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Kaligo.Combat;
using Kaligo.Services;
using Kaligo.Services.Local;

namespace Kaligo.UI
{
    public class PlayerHUD : MonoBehaviour
    {
        [Header("Combat Bars (auto-built if left empty)")]
        [SerializeField] private Image hpFill;
        [SerializeField] private Image staminaFill;
        [SerializeField] private Image manaFill;
        [SerializeField] private HealthSystem  health;
        [SerializeField] private StaminaSystem stamina;
        [SerializeField] private ManaSystem    mana;

        [Header("Progression")]
        [SerializeField] private Image           xpFill;
        [SerializeField] private TextMeshProUGUI levelLabel;

        private void Awake()
        {
            // Auto-find player combat systems if not wired in the Inspector
            if (health == null || stamina == null || mana == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    if (health  == null) health  = player.GetComponentInChildren<HealthSystem>();
                    if (stamina == null) stamina = player.GetComponentInChildren<StaminaSystem>();
                    if (mana    == null) mana    = player.GetComponentInChildren<ManaSystem>();
                }
            }

            // Self-build the visual bars if none were pre-wired
            if (hpFill == null)
                BuildBarsUI();
        }

        private void Start()
        {
            if (health != null)
            {
                health.OnHealthChanged += OnHealthChanged;
                OnHealthChanged(health.CurrentHealth, health.MaxHealth);
            }
            if (stamina != null)
            {
                stamina.OnStaminaChanged += OnStaminaChanged;
                OnStaminaChanged(stamina.CurrentStamina, stamina.MaxStamina);
            }
            if (mana != null)
            {
                mana.OnManaChanged += OnManaChanged;
                OnManaChanged(mana.CurrentMana, mana.MaxMana);
            }
            if (GameServices.Progression != null)
            {
                GameServices.Progression.OnXPChanged += OnXPChanged;
                GameServices.Progression.OnLevelUp   += OnLevelUp;
                RefreshXP(GameServices.Progression.XP, GameServices.Progression.Level);
            }
        }

        private void OnDestroy()
        {
            if (health  != null) health.OnHealthChanged   -= OnHealthChanged;
            if (stamina != null) stamina.OnStaminaChanged -= OnStaminaChanged;
            if (mana    != null) mana.OnManaChanged       -= OnManaChanged;
            if (GameServices.Progression != null)
            {
                GameServices.Progression.OnXPChanged -= OnXPChanged;
                GameServices.Progression.OnLevelUp   -= OnLevelUp;
            }
        }

        // ── Callbacks ─────────────────────────────────────────────────────────

        private void OnHealthChanged(float current, float max)
        {
            if (hpFill != null) hpFill.fillAmount = max > 0f ? current / max : 0f;
        }
        private void OnStaminaChanged(float current, float max)
        {
            if (staminaFill != null) staminaFill.fillAmount = max > 0f ? current / max : 0f;
        }
        private void OnManaChanged(float current, float max)
        {
            if (manaFill != null) manaFill.fillAmount = max > 0f ? current / max : 0f;
        }
        private void OnXPChanged(int totalXp)   => RefreshXP(totalXp, GameServices.Progression.Level);
        private void OnLevelUp(int newLevel)     => RefreshXP(GameServices.Progression.XP, newLevel);

        private void RefreshXP(int xp, int level)
        {
            if (xpFill     != null) xpFill.fillAmount = XPTable.LevelProgress(xp);
            if (levelLabel != null) levelLabel.text   = $"Lv {level}";
        }

        // ── Self-building UI ──────────────────────────────────────────────────

        /// <summary>
        /// Constructs the HP / Stamina / Mana bars and XP bar from code.
        /// Called automatically in Awake when no bars are pre-wired in the Inspector.
        /// </summary>
        private void BuildBarsUI()
        {
            var rt = GetComponent<RectTransform>();
            if (rt == null) rt = gameObject.AddComponent<RectTransform>();

            // ── Stats panel (bottom-left) ─────────────────────────────────────
            var panel = new GameObject("StatsPanel");
            panel.transform.SetParent(transform, false);
            var panelRt      = panel.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0f, 0f);
            panelRt.anchorMax = new Vector2(0f, 0f);
            panelRt.pivot     = new Vector2(0f, 0f);
            panelRt.anchoredPosition = new Vector2(16f, 16f);
            panelRt.sizeDelta = new Vector2(180f, 76f);

            var panelBg   = panel.AddComponent<Image>();
            panelBg.color = new Color(0f, 0f, 0f, 0.55f);

            // Level label
            var lvlGO  = MakeTMP("LevelLabel", panel.transform,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -20f), new Vector2(0f, 0f));
            levelLabel = lvlGO.GetComponent<TextMeshProUGUI>();
            levelLabel.text      = "Lv 1";
            levelLabel.fontSize  = 12f;
            levelLabel.color     = Color.white;
            levelLabel.alignment = TextAlignmentOptions.Left;

            // HP
            hpFill = MakeBar("HP", panel.transform,
                new Vector2(8f, 52f), new Color(0.85f, 0.15f, 0.15f));

            // Stamina
            staminaFill = MakeBar("Stamina", panel.transform,
                new Vector2(8f, 36f), new Color(0.85f, 0.75f, 0.1f));

            // Mana
            manaFill = MakeBar("Mana", panel.transform,
                new Vector2(8f, 20f), new Color(0.15f, 0.45f, 0.9f));

            // ── XP bar (full width, very bottom) ─────────────────────────────
            var xpBarGO  = new GameObject("XPBar");
            xpBarGO.transform.SetParent(transform, false);
            var xpBarRt  = xpBarGO.AddComponent<RectTransform>();
            xpBarRt.anchorMin = new Vector2(0f, 0f);
            xpBarRt.anchorMax = new Vector2(1f, 0f);
            xpBarRt.pivot     = new Vector2(0.5f, 0f);
            xpBarRt.anchoredPosition = Vector2.zero;
            xpBarRt.sizeDelta = new Vector2(0f, 5f);
            var xpBg     = xpBarGO.AddComponent<Image>();
            xpBg.color   = new Color(0.15f, 0.15f, 0.15f, 0.9f);

            var xpFillGO  = new GameObject("XPFill");
            xpFillGO.transform.SetParent(xpBarGO.transform, false);
            var xpFillRt  = xpFillGO.AddComponent<RectTransform>();
            xpFillRt.anchorMin = new Vector2(0f, 0f);
            xpFillRt.anchorMax = new Vector2(1f, 1f);
            xpFillRt.offsetMin = xpFillRt.offsetMax = Vector2.zero;
            xpFill       = xpFillGO.AddComponent<Image>();
            xpFill.color = new Color(0.6f, 0.4f, 0.9f);
            xpFill.type  = Image.Type.Filled;
            xpFill.fillMethod = Image.FillMethod.Horizontal;
            xpFill.fillAmount = 0f;
        }

        private Image MakeBar(string label, Transform parent, Vector2 localPos, Color color)
        {
            const float barW = 164f, barH = 12f;

            // Background
            var bgGO      = new GameObject($"{label}Bar");
            bgGO.transform.SetParent(parent, false);
            var bgRt      = bgGO.AddComponent<RectTransform>();
            bgRt.anchorMin = bgRt.anchorMax = bgRt.pivot = new Vector2(0f, 0f);
            bgRt.anchoredPosition = localPos;
            bgRt.sizeDelta        = new Vector2(barW, barH);
            var bgImg     = bgGO.AddComponent<Image>();
            bgImg.color   = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            // Fill
            var fillGO    = new GameObject("Fill");
            fillGO.transform.SetParent(bgGO.transform, false);
            var fillRt    = fillGO.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;
            var fill      = fillGO.AddComponent<Image>();
            fill.color    = color;
            fill.type     = Image.Type.Filled;
            fill.fillMethod   = Image.FillMethod.Horizontal;
            fill.fillAmount   = 1f;

            return fill;
        }

        private GameObject MakeTMP(string goName, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var go  = new GameObject(goName);
            go.transform.SetParent(parent, false);
            var rt  = go.AddComponent<RectTransform>();
            rt.anchorMin       = anchorMin;
            rt.anchorMax       = anchorMax;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta       = sizeDelta;
            go.AddComponent<TextMeshProUGUI>();
            return go;
        }
    }
}
