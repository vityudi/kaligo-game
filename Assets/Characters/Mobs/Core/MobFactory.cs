using UnityEngine;

namespace Kaligo.Mobs
{
    /// <summary>
    /// Builds a runtime mob GameObject from a <see cref="MobDefinition"/> — no prefab required.
    ///
    /// When real models are ready:
    ///   1. Add a <c>prefabOverride</c> field to <see cref="MobDefinition"/>.
    ///   2. In <see cref="Create"/>, branch on <c>def.prefabOverride != null</c> and instantiate that instead.
    ///   The spawner and AI code stay unchanged.
    /// </summary>
    public static class MobFactory
    {
        // Layer for mob colliders — assign layer "Mob" (index 8) in Project Settings
        private const int MobLayer = 8;

        /// <summary>
        /// Creates a fully configured mob GameObject at <paramref name="position"/>.
        /// The returned object is active and ready for play.
        /// </summary>
        public static GameObject Create(MobDefinition def, Vector3 position, Quaternion rotation)
        {
            if (def == null)
            {
                Debug.LogError("[MobFactory] Cannot create mob — definition is null.");
                return null;
            }

            // ── Root object ───────────────────────────────────────────────────
            var root = new GameObject($"[Mob] {def.displayName}");
            root.transform.SetPositionAndRotation(position, rotation);
            root.tag   = "Enemy";
            root.layer = MobLayer;

            // ── Placeholder visual ────────────────────────────────────────────
            BuildPlaceholderMesh(root, def);

            // ── Physics ───────────────────────────────────────────────────────
            var cc             = root.AddComponent<CharacterController>();
            cc.height          = def.placeholderHeight;
            cc.radius          = def.placeholderRadius;
            cc.center          = new Vector3(0f, def.placeholderHeight / 2f, 0f);
            cc.slopeLimit      = 45f;
            cc.stepOffset      = 0.3f;

            // ── Health ────────────────────────────────────────────────────────
            var health = root.AddComponent<Kaligo.Combat.HealthSystem>();
            health.SetMaxHealth(def.maxHealth);

            // ── Loot ──────────────────────────────────────────────────────────
            if (def.lootTable != null)
            {
                var loot = root.AddComponent<Kaligo.Items.LootDrop>();
                // Assign via reflection — avoids exposing a public setter in LootDrop
                // If you prefer a public setter, add one to LootDrop.cs instead.
                typeof(Kaligo.Items.LootDrop)
                    .GetField("lootTable",
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance)
                    ?.SetValue(loot, def.lootTable);
            }

            // ── Brain (AI) ────────────────────────────────────────────────────
            MobBrain brain = def.type == MobType.Passive
                ? (MobBrain)root.AddComponent<PassiveMobBrain>()
                : (MobBrain)root.AddComponent<AggressiveMobBrain>();

            // Inject definition via reflection (field is [SerializeField] protected)
            typeof(MobBrain)
                .GetField("definition",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance)
                ?.SetValue(brain, def);

            // ── Health bar ────────────────────────────────────────────────────
            // Aggressive mobs get a world-space HP bar; passive mobs don't need one
            if (def.type == MobType.Aggressive)
                AttachHealthBar(root, def);

            return root;
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private static void BuildPlaceholderMesh(GameObject root, MobDefinition def)
        {
            // Body — capsule scaled to mob's dimensions
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);

            float h = def.placeholderHeight;
            float r = def.placeholderRadius;
            // Unity capsule primitive is 2 units tall and 0.5 unit radius by default
            body.transform.localScale = new Vector3(r / 0.5f, h / 2f, r / 0.5f);
            body.transform.localPosition = new Vector3(0f, h / 2f, 0f);

            // Coloured material
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (mat == null) mat = new Material(Shader.Find("Standard")); // URP fallback
            mat.color = def.placeholderColor;
            body.GetComponent<Renderer>().material = mat;

            // Remove the capsule's own collider — CharacterController handles collision
            Object.Destroy(body.GetComponent<Collider>());

            // Eyes — two tiny white spheres pointing forward so orientation is obvious
            BuildEye(body.transform, new Vector3(-r * 0.4f, h * 0.15f,  r * 0.7f));
            BuildEye(body.transform, new Vector3( r * 0.4f, h * 0.15f,  r * 0.7f));
        }

        private static void BuildEye(Transform parent, Vector3 localPos)
        {
            var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.name = "Eye";
            eye.transform.SetParent(parent, false);
            eye.transform.localPosition = localPos;
            eye.transform.localScale    = Vector3.one * 0.08f;

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (mat == null) mat = new Material(Shader.Find("Standard"));
            mat.color = Color.white;
            eye.GetComponent<Renderer>().material = mat;
            Object.Destroy(eye.GetComponent<Collider>());
        }

        private static void AttachHealthBar(GameObject root, MobDefinition def)
        {
            // World-space Canvas health bar (billboard)
            var canvasGO = new GameObject("HealthBar");
            canvasGO.transform.SetParent(root.transform, false);
            canvasGO.transform.localPosition = new Vector3(0f, def.placeholderHeight + 0.4f, 0f);

            var canvas          = canvasGO.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.WorldSpace;
            canvas.sortingOrder = 1;

            var rt      = canvasGO.GetComponent<UnityEngine.RectTransform>();
            rt.sizeDelta = new Vector2(1.2f, 0.12f);

            // Background (dark)
            var bg     = CreateBarRect("BG", canvasGO.transform,
                new Vector2(1.2f, 0.12f), new Color(0.15f, 0.15f, 0.15f, 0.85f));

            // Fill (red → health)
            var fill   = CreateBarRect("Fill", canvasGO.transform,
                new Vector2(1.1f, 0.08f), new Color(0.85f, 0.15f, 0.15f, 1f));

            // Wire up EnemyHealthBar script (re-used)
            var hb     = canvasGO.AddComponent<Kaligo.Combat.EnemyHealthBar>();

            // EnemyHealthBar caches references in Awake — we inject them via the public API
            // if it exposes SetReferences, or just let it find them by tag.
            // The script uses GetComponentInParent<HealthSystem>() so no further wiring needed.
        }

        private static UnityEngine.RectTransform CreateBarRect(
            string name, Transform parent, Vector2 size, Color color)
        {
            var go  = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt  = go.AddComponent<UnityEngine.RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            var img = go.AddComponent<UnityEngine.UI.Image>();
            img.color = color;
            return rt;
        }
    }
}
