using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Kaligo.Items;

namespace Kaligo.UI
{
    /// <summary>
    /// Screen-space loot window. Displays the contents of a LootContainer.
    ///
    ///   ┌────────────────────────┐
    ///   │  LOOT            [✕]  │  ← title bar
    ///   ├────────────────────────┤
    ///   │  Item Name       [Take]│  ← one row per item
    ///   │  Item Name  x3   [Take]│
    ///   ├────────────────────────┤
    ///   │     [F]  Loot All      │  ← action bar
    ///   └────────────────────────┘
    ///
    /// Use static Open(container) / Close() — the window is created lazily
    /// the first time it's needed and reused afterwards.
    /// </summary>
    public class LootWindowUI : MonoBehaviour
    {
        // ── Static API ────────────────────────────────────────────────────────

        private static LootWindowUI _instance;

        public static void Open(LootContainer container)
        {
            EnsureInstance();
            if (_instance != null) _instance.ShowContainer(container);
        }

        public static void Close()
        {
            if (_instance != null) _instance.HideWindow();
        }

        // ── Constants ─────────────────────────────────────────────────────────

        private const float WindowW  = 290f;
        private const float TitleH   = 34f;
        private const float RowH     = 46f;
        private const float ActionH  = 44f;
        private const float Pad      = 8f;

        // ── State ─────────────────────────────────────────────────────────────

        private LootContainer _container;
        private GameObject    _root;

        // ── Bootstrap ─────────────────────────────────────────────────────────

        private static void EnsureInstance()
        {
            if (_instance != null) return;

            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null) { Debug.LogWarning("[LootWindowUI] No Canvas found."); return; }

            var go = new GameObject("LootWindowUI", typeof(RectTransform), typeof(LootWindowUI));
            go.transform.SetParent(canvas.transform, false);

            // Stretch to fill canvas so anchor math is correct
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            _instance = go.GetComponent<LootWindowUI>();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            if (_instance == this) _instance = null;
        }

        // ── Show / Hide ───────────────────────────────────────────────────────

        private void ShowContainer(LootContainer container)
        {
            Unsubscribe();
            _container = container;
            _container.OnLootChanged += Rebuild;
            Rebuild();
        }

        private void HideWindow()
        {
            Unsubscribe();
            if (_root != null)
            {
                Destroy(_root);
                _root = null;
            }
        }

        private void Unsubscribe()
        {
            if (_container != null)
            {
                _container.OnLootChanged -= Rebuild;
                _container = null;
            }
        }

        // ── Build ─────────────────────────────────────────────────────────────

        /// <summary>Tears down and rebuilds the window from current loot.</summary>
        private void Rebuild()
        {
            if (_root != null) { Destroy(_root); _root = null; }
            if (_container == null) return;

            var loot  = _container.Loot;
            int count = loot.Count;

            // Show one placeholder row when the body has nothing
            bool isEmpty = count == 0;
            int  displayRows = isEmpty ? 1 : count;

            float rowsH  = Pad + displayRows * RowH + Pad;
            float totalH = TitleH + rowsH + (isEmpty ? 0f : ActionH);

            // ── Root panel — left-centre of screen ────────────────────────────
            _root = MakeBox(transform, "LootRoot",
                pivot:  new Vector2(0f, 0.5f),
                anchor: new Vector2(0f, 0.5f),
                pos:    new Vector2(24f, 0f),
                size:   new Vector2(WindowW, totalH));
            Img(_root, new Color(0.05f, 0.06f, 0.05f, 0.97f));

            // ── Title bar ─────────────────────────────────────────────────────
            var titleBar = new GameObject("TitleBar", typeof(RectTransform), typeof(Image));
            titleBar.transform.SetParent(_root.transform, false);
            var tbRT = titleBar.GetComponent<RectTransform>();
            tbRT.anchorMin = new Vector2(0f, 1f);
            tbRT.anchorMax = new Vector2(1f, 1f);
            tbRT.pivot     = new Vector2(0.5f, 1f);
            tbRT.offsetMin = new Vector2(0f, -TitleH);
            tbRT.offsetMax = new Vector2(0f, 0f);
            titleBar.GetComponent<Image>().color = new Color(0.10f, 0.15f, 0.10f, 1f);

            MakeLabel(titleBar.transform, "Title", "LOOT",
                Vector2.zero, Vector2.one, 13f, FontStyles.Bold,
                new Color(0.55f, 1f, 0.45f));

            // Close button
            var closeGO = MakeBox(_root.transform, "CloseBtn",
                pivot:  new Vector2(1f, 1f),
                anchor: new Vector2(1f, 1f),
                pos:    Vector2.zero,
                size:   new Vector2(TitleH, TitleH));
            Img(closeGO, new Color(0.3f, 0.08f, 0.08f, 1f));
            MakeLabel(closeGO.transform, "X", "✕",
                Vector2.zero, Vector2.one, 13f, FontStyles.Bold, Color.white);
            closeGO.AddComponent<Button>().onClick.AddListener(HideWindow);

            // ── Item rows (or empty message) ──────────────────────────────────
            if (isEmpty)
            {
                float rowTop = -(TitleH + Pad);
                var rowGO = new GameObject("Row_Empty", typeof(RectTransform), typeof(Image));
                rowGO.transform.SetParent(_root.transform, false);
                var rowRT = rowGO.GetComponent<RectTransform>();
                rowRT.anchorMin = new Vector2(0f, 1f);
                rowRT.anchorMax = new Vector2(1f, 1f);
                rowRT.pivot     = new Vector2(0.5f, 1f);
                rowRT.offsetMin = new Vector2(0f, rowTop - RowH);
                rowRT.offsetMax = new Vector2(0f, rowTop);
                rowGO.GetComponent<Image>().color = new Color(0.10f, 0.11f, 0.10f, 1f);
                MakeLabel(rowGO.transform, "EmptyLbl", "— Nothing found —",
                    Vector2.zero, Vector2.one, 10f, FontStyles.Italic,
                    new Color(0.5f, 0.5f, 0.5f));
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    int   capturedIndex = i;
                    var  (item, qty)    = loot[i];

                    float rowTop = -(TitleH + Pad + i * RowH);

                    var rowGO = new GameObject($"Row_{i}", typeof(RectTransform), typeof(Image));
                    rowGO.transform.SetParent(_root.transform, false);
                    var rowRT = rowGO.GetComponent<RectTransform>();
                    rowRT.anchorMin = new Vector2(0f, 1f);
                    rowRT.anchorMax = new Vector2(1f, 1f);
                    rowRT.pivot     = new Vector2(0.5f, 1f);
                    rowRT.offsetMin = new Vector2(0f, rowTop - RowH);
                    rowRT.offsetMax = new Vector2(0f, rowTop);
                    rowGO.GetComponent<Image>().color = i % 2 == 0
                        ? new Color(0.10f, 0.11f, 0.10f, 1f)
                        : new Color(0.08f, 0.09f, 0.08f, 1f);

                    string qtyStr = qty > 1 ? $"  ×{qty}" : "";
                    var nameGO = MakeLabel(rowGO.transform, "Name",
                        item.displayName + qtyStr,
                        new Vector2(0.04f, 0f), new Vector2(0.68f, 1f),
                        11f, FontStyles.Normal, item.RarityColor());
                    nameGO.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;

                    var takeBtn = MakeBox(rowGO.transform, "TakeBtn",
                        pivot:  new Vector2(1f, 0.5f),
                        anchor: new Vector2(1f, 0.5f),
                        pos:    new Vector2(-Pad, 0f),
                        size:   new Vector2(66f, RowH - 12f));
                    Img(takeBtn, new Color(0.14f, 0.28f, 0.14f, 1f));
                    MakeLabel(takeBtn.transform, "TakeLbl", "Take",
                        Vector2.zero, Vector2.one, 10f, FontStyles.Bold,
                        new Color(0.6f, 1f, 0.5f));

                    var btn = takeBtn.AddComponent<Button>();
                    var cols = btn.colors;
                    cols.highlightedColor = new Color(0.22f, 0.45f, 0.22f);
                    btn.colors = cols;
                    btn.onClick.AddListener(() => _container?.TakeItem(capturedIndex));
                }
            }

            // ── Action bar (hidden when body is empty) ────────────────────────
            if (isEmpty) return;

            float actionTop = -(TitleH + rowsH);

            var actionBar = new GameObject("ActionBar", typeof(RectTransform), typeof(Image));
            actionBar.transform.SetParent(_root.transform, false);
            var abRT = actionBar.GetComponent<RectTransform>();
            abRT.anchorMin = new Vector2(0f, 1f);
            abRT.anchorMax = new Vector2(1f, 1f);
            abRT.pivot     = new Vector2(0.5f, 1f);
            abRT.offsetMin = new Vector2(0f, actionTop - ActionH);
            abRT.offsetMax = new Vector2(0f, actionTop);
            actionBar.GetComponent<Image>().color = new Color(0.07f, 0.10f, 0.07f, 1f);

            var lootAllBtn = MakeBox(actionBar.transform, "LootAllBtn",
                pivot:  new Vector2(0.5f, 0.5f),
                anchor: new Vector2(0.5f, 0.5f),
                pos:    Vector2.zero,
                size:   new Vector2(WindowW - 16f, ActionH - 10f));
            Img(lootAllBtn, new Color(0.14f, 0.32f, 0.14f, 1f));
            MakeLabel(lootAllBtn.transform, "BtnLabel", "[F]  Loot All",
                Vector2.zero, Vector2.one, 12f, FontStyles.Bold,
                new Color(0.65f, 1f, 0.55f));

            var lootAllBtnComp = lootAllBtn.AddComponent<Button>();
            var laCols = lootAllBtnComp.colors;
            laCols.highlightedColor = new Color(0.22f, 0.50f, 0.22f);
            lootAllBtnComp.colors = laCols;
            lootAllBtnComp.onClick.AddListener(() => _container?.LootAll());
        }

        // ── UI helpers ────────────────────────────────────────────────────────

        private static GameObject MakeBox(Transform parent, string name,
            Vector2 pivot, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.pivot            = pivot;
            rt.anchorMin        = anchor;
            rt.anchorMax        = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta        = size;
            return go;
        }

        private static void Img(GameObject go, Color color)
        {
            var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            img.color = color;
        }

        private static GameObject MakeLabel(Transform parent, string name, string text,
            Vector2 anchorMin, Vector2 anchorMax,
            float fontSize, FontStyles style, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text          = text;
            tmp.fontSize      = fontSize;
            tmp.fontStyle     = style;
            tmp.color         = color;
            tmp.alignment     = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            return go;
        }
    }
}
