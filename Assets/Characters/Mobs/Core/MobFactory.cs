using UnityEngine;

namespace Kaligo.Mobs
{
    /// <summary>
    /// Builds a runtime mob GameObject from a MobDefinition — no prefab required.
    /// Uses MobBrain.Initialize() instead of reflection to inject the definition.
    /// </summary>
    public static class MobFactory
    {
        private const int MobLayer = 8;

        public static GameObject Create(MobDefinition def, Vector3 position, Quaternion rotation)
        {
            if (def == null)
            {
                Debug.LogError("[MobFactory] Cannot create mob — definition is null.");
                return null;
            }

            var root = new GameObject($"[Mob] {def.displayName}");
            root.transform.SetPositionAndRotation(position, rotation);
            root.tag   = "Enemy";
            root.layer = MobLayer;

            // Visual must be built BEFORE AddComponent so Animator is discoverable in Awake
            BuildVisual(root, def);

            var cc        = root.AddComponent<CharacterController>();
            cc.height     = def.placeholderHeight;
            cc.radius     = def.placeholderRadius;
            cc.center     = new Vector3(0f, def.placeholderHeight / 2f, 0f);
            cc.slopeLimit = 45f;
            cc.stepOffset = 0.3f;

            var health = root.AddComponent<Kaligo.Combat.HealthSystem>();
            health.SetMaxHealth(def.maxHealth);

            if (def.lootTable != null)
            {
                var loot = root.AddComponent<Kaligo.Items.LootDrop>();
                loot.SetLootTable(def.lootTable);
            }

            MobBrain brain = def.type == MobType.Passive
                ? (MobBrain)root.AddComponent<PassiveMobBrain>()
                : (MobBrain)root.AddComponent<AggressiveMobBrain>();

            brain.Initialize(def);

            if (def.type == MobType.Aggressive)
                AttachHealthBar(root, def);

            return root;
        }

        private static void BuildVisual(GameObject root, MobDefinition def)
        {
            if (def.prefabOverride != null)
            {
                var visual = Object.Instantiate(def.prefabOverride, root.transform);
                visual.name = def.prefabOverride.name;
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                SetLayerRecursive(visual, MobLayer);
            }
            else
            {
                BuildPlaceholderMesh(root, def);
            }
        }

        private static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            for (int i = 0; i < go.transform.childCount; i++)
                SetLayerRecursive(go.transform.GetChild(i).gameObject, layer);
        }

        private static void BuildPlaceholderMesh(GameObject root, MobDefinition def)
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            float h = def.placeholderHeight;
            float r = def.placeholderRadius;
            body.transform.localScale    = new Vector3(r / 0.5f, h / 2f, r / 0.5f);
            body.transform.localPosition = new Vector3(0f, h / 2f, 0f);
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (mat == null) mat = new Material(Shader.Find("Standard"));
            mat.color = def.placeholderColor;
            body.GetComponent<Renderer>().material = mat;
            Object.Destroy(body.GetComponent<Collider>());
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
            var canvasGO = new GameObject("HealthBar");
            canvasGO.transform.SetParent(root.transform, false);
            canvasGO.transform.localPosition = new Vector3(0f, def.placeholderHeight + 0.4f, 0f);

            var canvas          = canvasGO.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.WorldSpace;
            canvas.sortingOrder = 1;

            var bgGO              = new GameObject("BG");
            bgGO.transform.SetParent(canvasGO.transform, false);
            var bgRt              = bgGO.AddComponent<UnityEngine.RectTransform>();
            bgRt.anchorMin        = Vector2.zero;
            bgRt.anchorMax        = Vector2.one;
            bgRt.sizeDelta        = Vector2.zero;
            bgRt.anchoredPosition = Vector2.zero;
            bgGO.AddComponent<UnityEngine.UI.Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.85f);

            var fillGO              = new GameObject("Fill");
            fillGO.transform.SetParent(canvasGO.transform, false);
            var fillRt              = fillGO.AddComponent<UnityEngine.RectTransform>();
            fillRt.anchorMin        = new Vector2(0f, 0.1f);
            fillRt.anchorMax        = new Vector2(1f, 0.9f);
            fillRt.sizeDelta        = Vector2.zero;
            fillRt.anchoredPosition = Vector2.zero;
            fillGO.AddComponent<UnityEngine.UI.Image>().color = new Color(0.85f, 0.15f, 0.15f, 1f);

            canvasGO.AddComponent<Kaligo.Combat.EnemyHealthBar>();
        }
    }
}
